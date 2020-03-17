using System;
using System.Linq.Expressions;
using System.Collections.Generic;

using LanguageImplementation;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

namespace CloacaInterpreter
{
    public class Interpreter: IInterpreter
    {
        private Dictionary<string, object> builtins;
        public IScheduler Scheduler
        {
            get; protected set;
        }

        public Interpreter(Scheduler scheduler)
        {
            Expression<Action<PyTypeObject>> super_expr = instance => Builtins.super(null);
            var super_methodInfo = ((MethodCallExpression)super_expr.Body).Method;
            var super_wrapper = new WrappedCodeObject("super", super_methodInfo);

            // Can't use an expression tree for dir because it is async; expression trees don't support async. :(
            var dir_wrapper = new WrappedCodeObject("dir", typeof(Builtins).GetMethod("dir"));

            Expression<Action<PyTypeObject>> issubclass_expr = instance => Builtins.issubclass(null, null);
            var issubclass_methodInfo = ((MethodCallExpression)issubclass_expr.Body).Method;
            var issubclass_wrapper = new WrappedCodeObject("issubclass", issubclass_methodInfo);

            Expression<Action<PyTypeObject>> isinstance_expr = instance => Builtins.isinstance(null, null);
            var isinstance_methodInfo = ((MethodCallExpression)isinstance_expr.Body).Method;
            var isinstance_wrapper = new WrappedCodeObject("isinstance", isinstance_methodInfo);

            Expression<Action<PyTypeObject>> builtin_type_expr = instance => Builtins.builtin_type(null);
            var builtin_type_methodInfo = ((MethodCallExpression)builtin_type_expr.Body).Method;
            var builtin_type_wrapper = new WrappedCodeObject("builtin_type", builtin_type_methodInfo);

            builtins = new Dictionary<string, object>
            {
                { "Exception", PyExceptionClass.Instance },
                { "super", super_wrapper },
                { "dir", dir_wrapper },
                { "issubclass", issubclass_wrapper },
                { "isinstance", isinstance_wrapper },
                { "type", builtin_type_wrapper },
            };

            this.Scheduler = scheduler;
        }

        public void AddBuiltin(WrappedCodeObject toEmbed)
        {
            builtins.Add(toEmbed.Name, toEmbed);
        }

        public bool DumpState;

        /// <summary>
        /// Implementation of builtins.__build_class__. This create a class as a PyClass.
        /// </summary>
        /// <param name="context">The context of script code that wants to make a class.</param>
        /// <param name="func">The class body as interpretable code.</param>
        /// <param name="name">The name of the class.</param>
        /// <param name="bases">Base classes parenting this class.</param>
        /// <returns>Returns a task that will provide the finished PyClass. This calls the class body, which could wait for something,
        /// but will normally be finished immediately. However, since the class body is theoretically asynchronous, it still has to be
        /// provided as a Task return PyClass.</returns>
        public async Task<object> builtins__build_class(FrameContext context, CodeObject func, string name, params PyClass[] bases)
        {
            // TODO: Add params type to handle one or more base classes (inheritance test)
            Frame classFrame = new Frame(func);
            classFrame.AddLocal("__name__", name);
            classFrame.AddLocal("__module__", null);
            classFrame.AddLocal("__qualname__", null);

            await CallInto(context, classFrame, new object[0]);

            // Figure out what kind of constructor we're using:
            // 1. One that was actually defined in code for this specific class
            // 2. A parent constructor, if existing
            // 3. Failing all that, a default constructor
            var initIdx = classFrame.LocalNames.IndexOf("__init__");
            CodeObject __init__ = null;
            if(initIdx < 0)
            {
                if (bases != null && bases.Length > 0)
                {
                    // Default to parent constructor. We hard-cast to a CodeObject
                    // TODO: Test subclassing a .NET object in Python. This will probably fail there and we'll need a more abstract handler for __init__
                    __init__ = (CodeObject)bases[0].__init__;
                }
                else
                {
                    // Insert a default constructor. This comes up as a "slot wrapper" at least in Python 3.6. For us, we're
                    // just making our own no-op __init__ for now.
                    // TODO: Replace with a wrapped default when WrappedCodeObject is freely interchangable with CodeObject
                    var initBuilder = new CodeObjectBuilder();
                    initBuilder.AssertContextGiven = false;
                    initBuilder.AddInstruction(ByteCodes.LOAD_CONST, 0);
                    initBuilder.Constants.Add(null);
                    initBuilder.AddInstruction(ByteCodes.RETURN_VALUE);
                    initBuilder.Name = "__init__";
                    initBuilder.ArgVarNames.Add("self");
                    __init__ = initBuilder.Build();
                }
            }
            else
            {
                __init__ = (CodeObject)classFrame.Locals[initIdx];
            }

            var pyclass = new PyClass(name, __init__, bases);

            foreach(var classMemberName in classFrame.Names)
            {
                var nameIdx = classFrame.LocalNames.IndexOf(classMemberName);
                if (nameIdx >= 0)
                {
                    pyclass.__dict__.AddOrSet(classMemberName, classFrame.Locals[nameIdx]);
                }
                else
                {
                    pyclass.__dict__.AddOrSet(classMemberName, context.GetVariable(classMemberName));
                }
            }
            
            return pyclass;
        }

        /// <summary>
        /// Retains the current frame state but enters a new child CodeObject. This is equivalent to
        /// using a CALL_FUNCTION opcode to descene into a subroutine or similar, but can be invoked
        /// external into the interpreter. It is used for inner, coordinating code to call back into
        /// the interpreter to get results. For example, this is used in object creation to invoke
        /// __new__ and __init__.
        /// </summary>
        /// <param name="context">The context of script code that is makin the call.</param>
        /// <param name="functionToRun">The code object to call into</param>
        /// <param name="args">The arguments for the program. These are put on the existing data stack</param>
        /// <returns>A task that returns some kind of object. This object is the return value of the
        /// callable. It might await for something which is why it is Task.</returns>
        public async Task<object> CallInto(FrameContext context, CodeObject functionToRun, object[] args)
        {
            Frame nextFrame = new Frame();
            nextFrame.Program = functionToRun;

            return await CallInto(context, nextFrame, args);
        }

        /// <summary>
        /// Retains the current frame state but enters the next frame. This is equivalent to
        /// using a CALL_FUNCTION opcode to descend into a subroutine or similar, but can be invoked
        /// externally into the interpreter. It is used for inner, coordinating code to call back into
        /// the interpreter to get results. 
        /// </summary>
        /// <param name="context">The context of script code that is making the call.</param>
        /// <param name="nextFrame">The frame to run through,</param>
        /// <param name="args">The arguments for the program. These are put on the existing data stack</param>
        /// <returns>A task that returns some kind of object. This object is the return value of the
        /// callable. It might await for something which is why it is Task.</returns>
        public async Task<object> CallInto(FrameContext context, Frame frame, object[] args)
        {
            // Assigning argument's initial values.
            for (int argIdx = 0; argIdx < args.Length; ++argIdx)
            {
                frame.AddLocal(frame.Program.ArgVarNames[argIdx], args[argIdx]);
            }
            for (int varIndex = 0; varIndex < frame.Program.VarNames.Count; ++varIndex)
            {
                frame.AddOnlyNewLocal(frame.Program.VarNames[varIndex], null);
            }

            context.callStack.Push(frame);      // nextFrame is now the active frame.
            await Run(context);

            if(context.EscapedDotNetException != null)
            {
                throw context.EscapedDotNetException;
            }

            if (context.DataStack.Count > 0)
            {
                return context.DataStack.Pop();
            }
            else
            {
                return null;
            }
        }

        private Block UnrollCurrentBlock(FrameContext context)
        {
            var block = context.BlockStack.Pop();

            // Restore the stack.
            while (context.DataStack.Count > block.StackSize)
            {
                context.DataStack.Pop();
            }

            return block;
        }

        // If set, runs one bytecode at a time.
        public bool StepMode
        {
            get; set;
        }

        /// <summary>
        /// Returns true if an exception was raised and the context would not be in a position to still try to
        /// handle it. This is used when stepping through frame context in debugging to allow the interpreter to
        /// keep trying to process the exception. If you just test the frame context for an exception while stepping,
        /// you'll miss out on the interpreter trying out the except (and finally) clauses that have some stuff left
        /// to do. It also misses out on all the unrolling to properly escape.
        /// </summary>
        public bool ExceptionEscaped(FrameContext context)
        {
            return context.CurrentException != null && context.BlockStack.Count == 0;
        }

        // Sketching this out for now as an experiment. This looks like a job for multiple dispatch.
        private async Task DynamicDispatchOperation(FrameContext context, dynamic a, dynamic b,
            string op_dunder, string op_fallback, 
            Func<object, dynamic, dynamic> dotNetOp)
        {
            var leftObj = a as PyObject;
            var rightObj = b as PyObject;

            if (leftObj == null && rightObj == null)
            {
                context.DataStack.Push(dotNetOp(a, b));
            }
            else if (leftObj == null)
            {
                context.DataStack.Push(dotNetOp(a, b.number));
            }
            else if (rightObj == null)
            {
                context.DataStack.Push(dotNetOp(a.number, b));
            }
            else
            {
                if (leftObj.__dict__.ContainsKey(op_dunder))
                {
                    PyObject returned = (PyObject)await leftObj.InvokeFromDict(this, context, op_dunder, new PyObject[] { rightObj });
                    context.DataStack.Push(returned);
                }
                else
                {
                    PyObject returned = (PyObject)await leftObj.InvokeFromDict(this, context, op_fallback, new PyObject[] { rightObj });
                    context.DataStack.Push(returned);
                }
            }
            context.Cursor += 1;
        }


        private async Task leftRightOperation(FrameContext context, string op_dunder, string op_fallback, Func<object, dynamic, dynamic> dotNetOp)
        {
            dynamic right = context.DataStack.Pop();
            dynamic left = context.DataStack.Pop();
            await DynamicDispatchOperation(context, left, right, op_dunder, op_fallback, dotNetOp);
        }

        private async Task rightLeftOperation(FrameContext context, string op_dunder, string op_fallback, Func<object, dynamic, dynamic> dotNetOp)
        {
            dynamic left = context.DataStack.Pop();
            dynamic right = context.DataStack.Pop();
            await DynamicDispatchOperation(context, left, right, op_dunder, op_fallback, dotNetOp);
        }

        /// <summary>
        /// Runs the given frame context until it either finishes normally or yields. This actually interprets
        /// our Python(ish) code!
        /// 
        /// This call is stateless; all the state changes mae happen in the FrameContext passed into Run().
        /// </summary>
        /// <param name="context">The current state of the frame and stacks to run</param>
        /// <returns>A task if the code being run gets pre-empted cooperatively.</returns>
        public async Task Run(FrameContext context)
        {
            try
            {
                while (context.Cursor < context.Code.Length)
                {
                    //if(DumpState)
                    //{
                    //    Console.WriteLine(Dis.dis(callStack.Peek().Program, Cursor, 1));
                    //}

                    // Are we unwinding from an exception?
                    while (context.CurrentException != null &&
                        (context.BlockStack.Count > 0 && context.BlockStack.Peek().Opcode != ByteCodes.SETUP_FINALLY))
                    {
                        if (context.BlockStack.Count > 0)
                        {
                            var block = UnrollCurrentBlock(context);
                            if (block.Opcode == ByteCodes.SETUP_EXCEPT)
                            {
                                // We'll now go to the except routine.
                                context.DataStack.Push(context.CurrentException);
                                context.CurrentException = null;
                                context.Cursor = block.HandlerAddress;
                                break;
                            }
                            else if (block.Opcode == ByteCodes.SETUP_FINALLY)
                            {
                                context.Cursor = block.HandlerAddress;
                                break;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    var opcode = (ByteCodes)context.Code[context.Cursor];
                    switch (opcode)
                    {
                        case ByteCodes.UNARY_NOT:
                            {
                                // NOTE: Many of the other unary operations use functions like __and__, __or__, etc. There is NOT
                                // one for not. There is not __not__. The __invert__ fuction implements the ~ operator, which is
                                // distinct from not.
                                var toFlip = context.DataStack.Pop() as PyBool;
                                if(toFlip == PyBool.False)
                                {
                                    context.DataStack.Push(PyBool.True);
                                }
                                else if(toFlip == PyBool.True)
                                {
                                    context.DataStack.Push(PyBool.False);
                                }
                                else
                                {
                                    throw new Exception("UNARY_NOT received a PyBool that is not one of the static types.");
                                }
                            }
                            context.Cursor += 1;
                            break;
                        case ByteCodes.BINARY_ADD:
                            await leftRightOperation(context, "__add__", null,
                                (left, right) => { return (dynamic) left + (dynamic) right; });
                            break;
                        case ByteCodes.INPLACE_ADD:
                            await rightLeftOperation(context, "__iadd__", "__add__", (left, right) =>
                            {
                                var leftEvent = left as EventInstance;
                                var rightCall = right as WrappedCodeObject;
                                if (leftEvent != null)
                                {
                                    // This is what you'll see in Microsoft documentation for getting the parameter and return information for an event. It's... fickle.
                                    MethodInfo eventInvoke = leftEvent.EventInfo.EventHandlerType.GetMethod("Invoke");
                                    var proxyDelegate = CallableDelegateProxy.Create(eventInvoke, leftEvent.EventInfo.EventHandlerType, rightCall, this, context);
                                    leftEvent.EventInfo.AddEventHandler(leftEvent.OwnerObject, proxyDelegate);

                                    // Put the EventInfo back on the stack. This seems odd at first glance but we have to consider that Python
                                    // parlance for += is that it's basically just a normal addition operation and it will put a result on the
                                    // stack. So there will be a STORE_ATTR after this expecting *something*. We will only know that there's an
                                    // object to store something to. What we'll have is the event info that we can then catch in STORE_ATTR and
                                    // suppress.
                                    return leftEvent;
                                }
                                else
                                {
                                    return (dynamic)left + (dynamic)right;
                                }
                            });
                            break;
                        case ByteCodes.BINARY_SUBTRACT:
                            await leftRightOperation(context, "__sub__", null,
                                (left, right) => { return (dynamic)left - (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_SUBTRACT:
                            await rightLeftOperation(context, "__isub__", "__sub__", (left, right) =>
                            {
                                var leftEvent = left as EventInstance;
                                var rightCall = right as WrappedCodeObject;
                                if (leftEvent != null)
                                {
                                    var listeners = leftEvent.EventDelegate.GetInvocationList();

                                    // Apparently we could have used foreach; I guess the invocation list is a copy.
                                    for (int i = 0; i < listeners.Length; ++i)
                                    {
                                        var listener = listeners[i];
                                        var target = listener.Target;
                                        var asProxy = target as CallableDelegateProxy;
                                        if (asProxy != null && asProxy.MatchesTarget(rightCall))
                                        {
                                            leftEvent.EventInfo.RemoveEventHandler(leftEvent.OwnerObject, listener);
                                        }
                                    }

                                    return leftEvent;
                                }
                                else
                                {
                                    return (dynamic)left - (dynamic)right;
                                }
                            });
                            break;
                        case ByteCodes.BINARY_MULTIPLY:
                            await leftRightOperation(context, "__mul__", null,
                                (left, right) => { return (dynamic)left * (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_MULTIPLY:
                            await leftRightOperation(context, "__imul__", "__mul__",
                                (left, right) => { return (dynamic)left * (dynamic)right; });
                            break;
                        case ByteCodes.BINARY_POWER:
                            await leftRightOperation(context, "__pow__", null,
                                (left, right) => { return Math.Pow((double) (dynamic) left, (double) (dynamic) right); });
                            break;
                        case ByteCodes.INPLACE_POWER:
                            await rightLeftOperation(context, "__ipow__", "__pow__",
                                (left, right) => { return Math.Pow((double)(dynamic)left, (double)(dynamic)right); });
                            break;
                        case ByteCodes.BINARY_TRUE_DIVIDE:
                            await leftRightOperation(context, "__truediv__", null,
                                (left, right) => { return ((decimal) (dynamic)left) / ((decimal) (dynamic)right); });
                            break;
                        case ByteCodes.INPLACE_TRUE_DIVIDE:
                            await rightLeftOperation(context, "__itruediv__", "__truediv__",
                                (left, right) => { return ((decimal)(dynamic)left) / ((decimal)(dynamic)right); });
                            break;
                        case ByteCodes.BINARY_FLOOR_DIVIDE:
                            await leftRightOperation(context, "__floordiv__", null,
                                (left, right) => { return (dynamic)left / (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_FLOOR_DIVIDE:
                            await rightLeftOperation(context, "__ifloordiv__", "__floordiv__",
                                (left, right) => { return (dynamic)left / (dynamic)right; });
                            break;
                        case ByteCodes.BINARY_MODULO:
                            await leftRightOperation(context, "__mod__", null,
                                (left, right) => { return (dynamic)left % (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_MODULO:
                            await rightLeftOperation(context, "__imod__", "__mod__",
                                (left, right) => { return (dynamic)left % (dynamic)right; });
                            break;
                        case ByteCodes.BINARY_AND:
                            await leftRightOperation(context, "__and__", null,
                                (left, right) => { return (dynamic)left & (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_AND:
                            await leftRightOperation(context, "__iand__", "__and__",
                                (left, right) => { return (dynamic)left & (dynamic)right; });
                            break;
                        case ByteCodes.BINARY_OR:
                            await leftRightOperation(context, "__or__", null,
                                (left, right) => { return (dynamic)left | (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_OR:
                            await rightLeftOperation(context, "__ior__", "__or__",
                                (left, right) => { return (dynamic)left | (dynamic)right; });
                            break;
                        case ByteCodes.BINARY_XOR:
                            await leftRightOperation(context, "__xor__", null,
                                (left, right) => { return (dynamic)left ^ (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_XOR:
                            await leftRightOperation(context, "__ixor__", "__xor__",
                                (left, right) => { return (dynamic)left ^ (dynamic)right; });
                            break;
                        case ByteCodes.BINARY_RSHIFT:
                            await leftRightOperation(context, "__rshift__", null,
                                (left, right) => { return (int) (dynamic) left >> (int) (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_RSHIFT:
                            await rightLeftOperation(context, "__irshift__", "__rshift__",
                                (left, right) => { return (int) (dynamic)left >> (int) (dynamic)right; });
                            break;
                        case ByteCodes.BINARY_LSHIFT:
                            await leftRightOperation(context, "__lshift__", null,
                                (left, right) => { return (int) (dynamic)left << (int) (dynamic)right; });
                            break;
                        case ByteCodes.INPLACE_LSHIFT:
                            await rightLeftOperation(context, "__ilshift__", "__lshift__",
                                (left, right) => { return (int) (dynamic)left << (int) (dynamic)right; });
                            break;
                        case ByteCodes.LOAD_CONST:
                            {
                                context.Cursor += 1;
                                context.DataStack.Push(context.Program.Constants[context.CodeBytes.GetUShort(context.Cursor)]);
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.STORE_NAME:
                            {
                                context.Cursor += 1;
                                string name = context.Names[context.CodeBytes.GetUShort(context.Cursor)];

                                bool foundVar = false;

                                // Try to resolve locally, then globally, and then in our built-in namespace
                                foreach (var stackFrame in context.callStack)
                                {
                                    // Unlike LOAD_GLOBAL, the current frame is fair game. In fact, we search it first!
                                    var nameIdx = stackFrame.LocalNames.IndexOf(name);
                                    if (nameIdx >= 0)
                                    {
                                        stackFrame.Locals[nameIdx] = context.DataStack.Pop();
                                        foundVar = true;
                                        break;
                                    }
                                }

                                // If we don't find it, then we'll make it local!
                                if (!foundVar)
                                {
                                    context.callStack.Peek().AddLocal(name, context.DataStack.Pop());
                                }
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.STORE_FAST:
                            {
                                context.Cursor += 1;
                                var localIdx = context.CodeBytes.GetUShort(context.Cursor);
                                context.Locals[localIdx] = context.DataStack.Pop();
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.STORE_GLOBAL:
                            {
                                {
                                    context.Cursor += 1;
                                    var globalIdx = context.CodeBytes.GetUShort(context.Cursor);
                                    var globalName = context.Program.Names[globalIdx];

                                    bool foundVar = false;

                                    foreach (var stackFrame in context.callStack)
                                    {
                                        // Skip current stack
                                        if (stackFrame == context.callStack.Peek())
                                        {
                                            continue;
                                        }

                                        var nameIdx = stackFrame.LocalNames.IndexOf(globalName);
                                        if (nameIdx >= 0)
                                        {
                                            stackFrame.Locals[nameIdx] = context.DataStack.Pop();
                                            foundVar = true;
                                        }
                                    }

                                    if (!foundVar)
                                    {
                                        throw new Exception("Global '" + globalName + "' was not found!");
                                    }
                                }
                                context.Cursor += 2;
                                break;
                            }
                        case ByteCodes.STORE_ATTR:
                            {
                                context.Cursor += 1;
                                var nameIdx = context.CodeBytes.GetUShort(context.Cursor);
                                var attrName = context.Program.Names[nameIdx];

                                var rawObject = context.DataStack.Pop();
                                var val = context.DataStack.Pop();
                                var rawPyObject = rawObject as PyObject;
                                if (rawPyObject != null)
                                {
                                    rawPyObject.__setattr__(attrName, val);
                                }
                                else
                                {
                                    // This is a .NET instance. If we're assigning an event, then this is a residual of doing a 
                                    // += or -=. We can suppress the assignment because we already completed the operation in
                                    // the INPLACE_X opcode. We just put the EventInstance back on the stack to signal later that's
                                    // what happened. This isn't *that* hacky; regular INPLACE_ operations also put their result
                                    // on the stack like a regular BINARY opcode!
                                    var asEventInstance = val as EventInstance;
                                    if(asEventInstance == null)
                                    {
                                        throw new NotImplementedException("Cannot use STORE_ATTR on non-PyObjects yet, although we do need it for .NET object members");
                                    }
                                    else
                                    {
                                        // Suppressing!
                                    }
                                }
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.LOAD_NAME:
                            {
                                context.Cursor += 1;
                                string name = context.Names[context.CodeBytes.GetUShort(context.Cursor)];
                                context.DataStack.Push(context.GetVariable(name));
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.LOAD_FAST:
                            {
                                context.Cursor += 1;
                                context.DataStack.Push(context.Locals[context.CodeBytes.GetUShort(context.Cursor)]);
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.LOAD_GLOBAL:
                            {
                                {
                                    context.Cursor += 1;
                                    var globalIdx = context.CodeBytes.GetUShort(context.Cursor);
                                    var globalName = context.Program.Names[globalIdx];

                                    object foundVar = null;

                                    foreach (var stackFrame in context.callStack)
                                    {
                                        // Skip current stack
                                        if (stackFrame == context.callStack.Peek())
                                        {
                                            continue;
                                        }

                                        var nameIdx = stackFrame.LocalNames.IndexOf(globalName);
                                        if (nameIdx >= 0)
                                        {
                                            foundVar = stackFrame.Locals[nameIdx];
                                            break;
                                        }
                                    }

                                    if (foundVar != null)
                                    {
                                        context.DataStack.Push(foundVar);
                                    }
                                    else if (builtins.ContainsKey(globalName))
                                    {
                                        context.DataStack.Push(builtins[globalName]);
                                    }
                                    else
                                    {
                                        throw new Exception("Global '" + globalName + "' was not found!");
                                    }
                                }
                                context.Cursor += 2;
                                break;
                            }
                        case ByteCodes.LOAD_ATTR:
                            {
                                context.Cursor += 1;
                                var nameIdx = context.CodeBytes.GetUShort(context.Cursor);
                                var attrName = context.Program.Names[nameIdx];
                                var rawObj = context.DataStack.Pop();
                                context.DataStack.Push(ObjectResolver.GetValue(attrName, rawObj));
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.WAIT:
                            {
                                context.Cursor += 1;
                                await new YieldTick(this.Scheduler, context);
                            }
                            break;
                        case ByteCodes.COMPARE_OP:
                            {
                                context.Cursor += 1;
                                var compare_op = (CompareOps)context.Program.Code.GetUShort(context.Cursor);
                                dynamic right = context.DataStack.Pop();
                                dynamic left = context.DataStack.Pop();

                                if (left is PyObject && right is PyObject)
                                {
                                    var leftObj = left as PyObject;
                                    var rightObj = right as PyObject;
                                    string compareFunc = null;
                                    switch (compare_op)
                                    {
                                        case CompareOps.Lt:
                                            compareFunc = "__lt__";
                                            break;
                                        case CompareOps.Gt:
                                            compareFunc = "__gt__";
                                            break;
                                        case CompareOps.Eq:
                                            compareFunc = "__eq__";
                                            break;
                                        case CompareOps.Ge:
                                            compareFunc = "__ge__";
                                            break;
                                        case CompareOps.Le:
                                            compareFunc = "__le__";
                                            break;
                                        case CompareOps.LtGt:
                                            compareFunc = "__ltgt__";
                                            break;
                                        case CompareOps.Ne:
                                            compareFunc = "__ne__";
                                            break;
                                        case CompareOps.In:
                                            throw new NotImplementedException("'In' comparison operation");
                                        case CompareOps.NotIn:
                                            throw new NotImplementedException("'Not In' comparison operation");
                                        case CompareOps.Is:
                                            context.DataStack.Push(new PyBool(left.GetType() == right.GetType() && left == right));
                                            break;
                                        case CompareOps.IsNot:
                                            context.DataStack.Push(new PyBool(left.GetType() != right.GetType() || left != right));
                                            break;
                                        case CompareOps.ExceptionMatch:
                                            {
                                                var rightType = right as PyClass;
                                                if (rightType == null || !Builtins.issubclass(rightType, PyExceptionClass.Instance))
                                                {
                                                    // Well, now we're raising a type error!
                                                    // TypeError: catching classes that do not inherit from BaseException is not allowed
                                                    context.CurrentException = new TypeError("TypeError: catching classes that do not inherit from BaseException is not allowed");
                                                }
                                                else
                                                {
                                                    var leftObject = left as PyObject;
                                                    context.DataStack.Push(new PyBool(leftObject.__class__ == right));
                                                }
                                                break;
                                            }
                                        default:
                                            throw new Exception("Unexpected comparison operation opcode: " + compare_op);
                                    }
                                    if (compareFunc != null)
                                    {
                                        var returned = await leftObj.InvokeFromDict(this, context, compareFunc, new PyObject[] { rightObj });
                                        if (returned != null)
                                        {
                                            context.DataStack.Push((PyBool)returned);
                                        }
                                    }
                                }
                                else
                                {
                                    switch (compare_op)
                                    {
                                        case CompareOps.Lt:
                                            context.DataStack.Push(new PyBool(left < right));
                                            break;
                                        case CompareOps.Gt:
                                            context.DataStack.Push(new PyBool(left > right));
                                            break;
                                        case CompareOps.Eq:
                                            context.DataStack.Push(new PyBool(left == right));
                                            break;
                                        case CompareOps.Ge:
                                            context.DataStack.Push(new PyBool(left >= right));
                                            break;
                                        case CompareOps.Le:
                                            context.DataStack.Push(new PyBool(left <= right));
                                            break;
                                        case CompareOps.LtGt:
                                            context.DataStack.Push(new PyBool(left < right || left > right));
                                            break;
                                        case CompareOps.Ne:
                                            context.DataStack.Push(new PyBool(left != right));
                                            break;
                                        case CompareOps.In:
                                            throw new NotImplementedException("'In' comparison operation");
                                        case CompareOps.NotIn:
                                            throw new NotImplementedException("'Not In' comparison operation");
                                        case CompareOps.Is:
                                            context.DataStack.Push(new PyBool(left.GetType() == right.GetType() && left == right));
                                            break;
                                        case CompareOps.IsNot:
                                            context.DataStack.Push(new PyBool(left.GetType() != right.GetType() || left != right));
                                            break;
                                        case CompareOps.ExceptionMatch:
                                            {
                                                var rightType = right as PyClass;
                                                if (rightType == null || !Builtins.issubclass(rightType, PyExceptionClass.Instance))
                                                {
                                                    // Well, now we're raising a type error!
                                                    // TypeError: catching classes that do not inherit from BaseException is not allowed
                                                    context.CurrentException = new TypeError("TypeError: catching classes that do not inherit from BaseException is not allowed");
                                                }
                                                else
                                                {
                                                    var leftObject = left as PyObject;
                                                    context.DataStack.Push(leftObject.__class__ == right);
                                                }
                                                break;
                                            }
                                        default:
                                            throw new Exception("Unexpected comparison operation opcode: " + compare_op);
                                    }
                                }
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.JUMP_IF_TRUE:
                            {
                                context.Cursor += 1;
                                var jumpPosition = context.CodeBytes.GetUShort(context.Cursor);
                                var conditional = (PyBool)context.DataStack.Peek();
                                if (conditional)
                                {
                                    context.Cursor = jumpPosition;
                                    continue;
                                }
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.JUMP_IF_FALSE:
                            {
                                context.Cursor += 1;
                                var jumpPosition = context.CodeBytes.GetUShort(context.Cursor);
                                var conditional = (PyBool)context.DataStack.Peek();
                                if (!conditional)
                                {
                                    context.Cursor = jumpPosition;
                                    continue;
                                }
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.POP_JUMP_IF_TRUE:
                            {
                                context.Cursor += 1;
                                var jumpPosition = context.CodeBytes.GetUShort(context.Cursor);
                                var conditional = (PyBool)context.DataStack.Pop();
                                if (conditional)
                                {
                                    context.Cursor = jumpPosition;
                                    continue;
                                }
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.POP_JUMP_IF_FALSE:
                            {
                                context.Cursor += 1;
                                var jumpPosition = context.CodeBytes.GetUShort(context.Cursor);
                                var conditional = (PyBool)context.DataStack.Pop();
                                if (!conditional)
                                {
                                    context.Cursor = jumpPosition;
                                    continue;
                                }
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.SETUP_LOOP:
                            {
                                context.Cursor += 1;
                                var loopResumptionPoint = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor += 2;
                                context.BlockStack.Push(new Block(ByteCodes.SETUP_LOOP, loopResumptionPoint, context.DataStack.Count));
                            }
                            break;
                        case ByteCodes.POP_BLOCK:
                            {
                                UnrollCurrentBlock(context);
                            }
                            context.Cursor += 1;
                            break;
                        case ByteCodes.POP_TOP:
                            {
                                context.DataStack.Pop();
                                context.Cursor += 1;
                                break;
                            }
                        case ByteCodes.DUP_TOP:
                            {
                                context.DataStack.Push(context.DataStack.Peek());
                                context.Cursor += 1;
                                break;
                            }
                        case ByteCodes.SETUP_EXCEPT:
                            {
                                context.Cursor += 1;
                                var exceptionCatchPoint = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor += 2;
                                context.BlockStack.Push(new Block(ByteCodes.SETUP_EXCEPT, context.Cursor + exceptionCatchPoint, context.DataStack.Count));
                            }
                            break;
                        case ByteCodes.SETUP_FINALLY:
                            {
                                context.Cursor += 1;
                                var finallyClausePoint = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor += 2;
                                context.BlockStack.Push(new Block(ByteCodes.SETUP_FINALLY, context.Cursor + finallyClausePoint, context.DataStack.Count));
                            }
                            break;
                        case ByteCodes.JUMP_ABSOLUTE:
                            {
                                context.Cursor += 1;
                                var jumpPosition = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor = jumpPosition;
                                continue;
                            }
                        case ByteCodes.JUMP_FORWARD:
                            {
                                context.Cursor += 1;
                                var jumpOffset = context.CodeBytes.GetUShort(context.Cursor);

                                // Offset is based off of the NEXT instruction so add one.
                                context.Cursor += jumpOffset + 2;
                                continue;
                            }
                        case ByteCodes.MAKE_FUNCTION:
                            {
                                // TOS-1 is the code object
                                // TOS is the function's qualified name
                                context.Cursor += 1;
                                var functionOpcode = context.CodeBytes.GetUShort(context.Cursor);             // Currently not using.
                                string qualifiedName = (string)context.DataStack.Pop();
                                CodeObject functionCode = (CodeObject)context.DataStack.Pop();
                                context.DataStack.Push(functionCode);
                            }
                            context.Cursor += 2;
                            break;
                        case ByteCodes.CALL_FUNCTION:
                            {
                                context.Cursor += 1;
                                var argCount = context.CodeBytes.GetUShort(context.Cursor);             // Currently not using.

                                // This is annoying. The arguments are at the top of the stack while
                                // the function is under them, but we need the function to assign the
                                // values. So we'll just copy them off piecemeal. Hence they'll show up
                                // in reverse order.
                                var args = new List<object>();
                                for (int argIdx = 0; argIdx < argCount; ++argIdx)
                                {
                                    args.Insert(0, context.DataStack.Pop());
                                }

                                object abstractFunctionToRun = context.DataStack.Pop();
                                var asPyObject = abstractFunctionToRun as PyObject;

                                if(asPyObject != null)
                                {
                                    try
                                    {
                                        var __call__ = asPyObject.__getattribute__("__call__");
                                        var functionToRun = (IPyCallable)__call__;

                                        // Copypasta from next if clause. Hopefully this will replace it!
                                        var returned = await functionToRun.Call(this, context, args.ToArray());
                                        if (returned != null && !(returned is FutureVoidAwaiter))
                                        {
                                            if (returned is IGetsFutureAwaiterResult)
                                            {
                                                returned = ((IGetsFutureAwaiterResult)returned).GetGenericResult();
                                            }
                                            context.DataStack.Push(returned);
                                        }
                                        else if (returned == null)
                                        {
                                            context.DataStack.Push(NoneType.Instance);
                                        }
                                        context.Cursor += 2;
                                        break;
                                    }
                                    catch (EscapedPyException e)
                                    {
                                        // We'll just proceed as usual.
                                    }
                                }

                                // Treat this kind of like an else if. We don't do that literally because we have
                                // to test for __call__ in the PyObject, and also because it might not be a PyObject
                                // to begin with.
                                if (abstractFunctionToRun is IPyCallable)
                                {
                                    var functionToRun = (IPyCallable)abstractFunctionToRun;

                                    var returned = await functionToRun.Call(this, context, args.ToArray());
                                    if (returned != null && !(returned is FutureVoidAwaiter))
                                    {
                                        if(returned is IGetsFutureAwaiterResult)
                                        {
                                            returned = ((IGetsFutureAwaiterResult)returned).GetGenericResult();
                                        }
                                        context.DataStack.Push(returned);
                                    }
                                    else if(returned == null)
                                    {
                                        context.DataStack.Push(NoneType.Instance);
                                    }

                                    context.Cursor += 2;
                                }
                                else
                                {
                                    throw new InvalidCastException("Cannot use " + abstractFunctionToRun.GetType() + " as a callable function");
                                }
                                break;
                            }
                        case ByteCodes.RETURN_VALUE:
                            {
                                Frame returningFrame = context.callStack.Pop();

                                // The calling frame is now active.
                                // Apparently the return value is the topmost element of the stack
                                // http://www.aosabook.org/en/500L/a-python-interpreter-written-in-python.html
                                // "First it will pop the top value off the data stack of the top frame on the call stack."
                                // We won't add anything if we have nothing to return for now, although we would more appropriately
                                // return NoneType.
                                if (returningFrame.DataStack.Count > 0)
                                {
                                    context.DataStack.Push(returningFrame.DataStack.Pop());
                                }

                                // VERY BIG DEAL: We return from RETURN_VALUE. This is kind of tricky! The problem right now is 
                                // the interpreter is half set up to call into subroutines using a new Run() call while it's also
                                // set up to just automatically dig in using this big while loop. So it's half recursive and half
                                // iterative. The recursive nature is probably what will stick based on how I see CPython doing things.
                                // That's because I don't have a more obvious way to have Python code call a built-in that itself needs
                                // to resolve some Python code. That latter code will return. At that point, we'll lose sync with the
                                // frames.
                                return;
                            }
                        case ByteCodes.BUILD_TUPLE:
                            {
                                context.Cursor += 1;
                                var tupleCount = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor += 2;
                                var tupleObj = await PyTupleClass.Instance.Call(this, context, new object[0]);
                                var tuple = (PyTuple)tupleObj;

                                PyObject[] tupleArray = new PyObject[tupleCount];

                                // Items come off the stack in reverse order!
                                for (int i = 0; i < tupleCount; ++i)
                                {
                                    tupleArray[tupleCount - i - 1] = (PyObject)context.DataStack.Pop();
                                }
                                tuple.Values = tupleArray;
                                context.DataStack.Push(tuple);
                            }
                            break;
                        case ByteCodes.BUILD_MAP:
                            {
                                context.Cursor += 1;
                                var dictSize = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor += 2;
                                var dictObj = await PyDictClass.Instance.Call(this, context, new object[0]);
                                var dict = (PyDict) dictObj;
                                for (int i = 0; i < dictSize; ++i)
                                {
                                    var valueRaw = context.DataStack.Pop();
                                    var keyRaw = context.DataStack.Pop();
                                    var value = valueRaw as PyObject;
                                    var key = keyRaw as PyObject;
                                    if(key == null)
                                    {
                                        throw new Exception("BUILD_MAP key " + key.GetType().Name + " could not be cast to PyObject.");
                                    }
                                    if(value == null)
                                    {
                                        throw new Exception("BUILD_MAP value " + value.GetType().Name + " could not be cast to PyObject.");
                                    }
                                    PyDictClass.__setitem__(dict, key, value);
                                }
                                context.DataStack.Push(dict);
                            }
                            break;
                        case ByteCodes.BUILD_CONST_KEY_MAP:
                            {
                                // NOTE: Our code visitor doesn't generate this opcode.
                                // Top of a stack is the tuple for keys. Operand is how many values to pop off of the
                                // stack, which is kind of interesting since the tuple length should imply that...
                                context.Cursor += 1;
                                var dictSize = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor += 2;
                                var dict = new Dictionary<object, object>();
                                var keyTuple = (PyTuple)context.DataStack.Pop();
                                for (int i = keyTuple.Values.Length - 1; i >= 0; --i)
                                {
                                    dict[keyTuple.Values[i]] = context.DataStack.Pop();
                                }
                                context.DataStack.Push(dict);
                            }
                            break;
                        case ByteCodes.BUILD_LIST:
                            {
                                context.Cursor += 1;
                                var listSize = context.CodeBytes.GetUShort(context.Cursor);
                                context.Cursor += 2;
                                var listObj = await PyListClass.Instance.Call(this, context, new object[0]);
                                var list = (PyList)listObj;

                                // Items come off the stack in reverse order!
                                for (int i = 0; i < listSize; ++i)
                                {
                                    PyListClass.prepend(list, (PyObject) context.DataStack.Pop());
                                }
                                context.DataStack.Push(list);
                            }
                            break;
                        case ByteCodes.BINARY_SUBSCR:
                            {
                                context.Cursor += 1;
                                var index = context.DataStack.Pop();
                                var container = context.DataStack.Pop();

                                var containerPyObject = container as PyObject;
                                var indexPyObject = index as PyObject;

                                if (containerPyObject == null)
                                {
                                    // TODO: Expand to cover lists and objects with indexing operators; probably also need to worry about dictionaries.
                                    // Look at https://stackoverflow.com/questions/14462820/check-if-indexing-operator-exists
                                    if (!container.GetType().IsArray)
                                    {
                                        throw new Exception("TypeError: '" + container.GetType().Name + "' object is not subscriptable; could not be converted to PyObject nor .NET array");
                                    }
                                    else
                                    {
                                        var asArray = container as Array;
                                        int arrayIndex = -1;
                                        if (indexPyObject != null)
                                        {
                                            var asPyInt = index as PyInteger;
                                            if (asPyInt == null)
                                            {
                                                throw new Exception("TypeError: Attempted to use non - PyInteger '" + index.GetType().Name + "' as a subscript key.");
                                            }
                                            else
                                            {
                                                arrayIndex = (int)asPyInt.number;
                                            }
                                        }
                                        else
                                        {
                                            // Not a PyObject, so hopefully it's a .NET type?
                                            try
                                            {
                                                arrayIndex = (int)index;
                                            }
                                            catch (InvalidCastException cast_e)
                                            {
                                                throw new Exception("TypeError: Attempted to use '" + index.GetType().Name + "' as a subscript key. Could not cast to int for .NET array.");
                                            }
                                        }

                                        // Still here? Let's try to get the array value!
                                        if (arrayIndex < 0 || arrayIndex >= asArray.Length)
                                        {
                                            throw new Exception("IndexError: list index out of range");
                                        }
                                        context.DataStack.Push(asArray.GetValue(arrayIndex));
                                    }
                                }
                                else
                                {
                                    if (indexPyObject == null)
                                    {
                                        throw new Exception("Attempted to use non-PyObject '" + index.GetType().Name + "' as a subscript key.");
                                    }

                                    try
                                    {
                                        var getter = containerPyObject.__getattribute__("__getitem__");
                                        var functionToRun = getter as IPyCallable;

                                        if (functionToRun == null)
                                        {
                                            throw new Exception("TypeError: '" + container.GetType().Name + "' object is not subscriptable; could not be converted to IPyCallable");
                                        }

                                        var returned = await functionToRun.Call(this, context, new object[] { indexPyObject });
                                        if (returned != null)
                                        {
                                            context.DataStack.Push(returned);
                                        }

                                    }
                                    catch (KeyNotFoundException)
                                    {
                                        // TODO: use __class__.__name__
                                        throw new Exception("TypeError: '" + containerPyObject.__class__.GetType().Name + "' object is not subscriptable");
                                    }
                                }
                            }
                            break;
                        case ByteCodes.STORE_SUBSCR:
                            {
                                // TODO: Stop checking between BigInt and friends once data types are all objects
                                // TODO: Raw index conversion to int should probably be moved to its more local section
                                context.Cursor += 1;
                                var rawIndex = context.DataStack.Pop();
                                int convertedIndex = 0;
                                var idxAsPyObject = rawIndex as PyObject;
                                if (idxAsPyObject == null)
                                {
                                    // TODO: use __class__.__name__
                                    // throw new InvalidCastException("TypeError: list indices must be integers or slices, not " + rawIndex.GetType().Name);
                                    throw new Exception("Attempted to use non-PyObject '" + rawIndex.GetType().Name + "' as a subscript key.");
                                }

                                var rawContainer = context.DataStack.Pop();
                                var toStore = context.DataStack.Pop();

                                var containerPyObject = rawContainer as PyObject;
                                if (containerPyObject == null)
                                {
                                    throw new Exception("TypeError: '" + rawContainer.GetType().Name + "' object is not subscriptable; could not be converted to PyObject");
                                }

                                try
                                {
                                    var setter = containerPyObject.__getattribute__("__setitem__");
                                    var functionToRun = setter as IPyCallable;

                                    if (functionToRun == null)
                                    {
                                        throw new Exception("TypeError: '" + containerPyObject.GetType().Name + "' object is not subscriptable; could not be converted to IPyCallable");
                                    }

                                    var returned = await functionToRun.Call(this, context, new object[] { idxAsPyObject, toStore });
                                    if (returned != null)
                                    {
                                        context.DataStack.Push(returned);
                                    }
                                }
                                catch (KeyNotFoundException)
                                {
                                    // TODO: use __class__.__name__
                                    throw new Exception("TypeError: '" + containerPyObject.__class__.GetType().Name + "' object is not subscriptable");
                                }
                            }
                            break;
                        case ByteCodes.BUILD_CLASS:
                            {
                                context.Cursor += 1;
                                // Push builtins.__build_class__ on to the datastack
                                // TODO: Build and register these built-ins just once.
                                Expression<Action<Interpreter>> expr = instance => builtins__build_class(null, null, null);
                                var methodInfo = ((MethodCallExpression)expr.Body).Method;
                                var class_builder = new WrappedCodeObject(context, "__build_class__", methodInfo, this);
                                context.DataStack.Push(class_builder);
                            }
                            break;
                        case ByteCodes.RAISE_VARARGS:
                            {
                                context.Cursor += 1;

                                // Assuming that the parameter is always one for now.
                                var argCountIgnored = context.CodeBytes.GetUShort(context.Cursor);
                                if (argCountIgnored != 1)
                                {
                                    throw new NotImplementedException("RAISE_VARARGS with none-one fields not yet implemented.");
                                }

                                var theException = (PyObject)context.DataStack.Pop();

                                // Make sure it's an exception!
                                // Replace the exception with "exceptions must derive from BaseException"
                                if (!Builtins.issubclass(theException, PyExceptionClass.Instance))
                                {
                                    theException = new PyException("exceptions must derive from BaseException");
                                }

                                var offendingFrame = context.callStack.Peek();
                                if (theException.__dict__.ContainsKey(PyException.TracebackName))
                                {
                                    theException.__dict__[PyException.TracebackName] =
                                        new PyTraceback((PyTraceback)theException.__dict__[PyException.TracebackName],
                                        offendingFrame,
                                        offendingFrame.Program.GetCodeLine(context.Cursor));
                                }
                                else
                                {
                                    theException.__dict__.Add(PyException.TracebackName,
                                        new PyTraceback(null,
                                        offendingFrame,
                                        offendingFrame.Program.GetCodeLine(context.Cursor)));
                                }

                                context.Cursor += 2;
                                context.CurrentException = theException;

                                // TODO: Look into this method of handing try-finally when the try gets an exception.
                                // Kind of goofy thing here. We need to get to the finally block for this exception
                                // block if we are in a SETUP_FINALLY. If we have exceptions, we'll be in a SETUP_EXCEPT
                                // again. I don't think this is the best way to manage this but here we are for now.
                                var currentBlock = context.BlockStack.Count == 0 ? null : context.BlockStack.Peek();
                                if (currentBlock != null && currentBlock.Opcode == ByteCodes.SETUP_FINALLY)
                                {
                                    context.Cursor = currentBlock.HandlerAddress;
                                }

                                break;
                            }
                        case ByteCodes.END_FINALLY:
                            {
                                context.Cursor += 1;
                                context.BlockStack.Pop();
                                break;
                            }
                        default:
                            throw new Exception("Unexpected opcode: " + opcode);
                    }

                    if (StepMode)
                    {
                        await new YieldTick(this.Scheduler, context);
                    }
                }                
            }
            // Associate escaped exceptions with this current task so the scheduler can scoop them up and detect
            // them. Catching these externally has gotten MUCH fussier now that we're using async-await.                
            catch (Exception e)
            {
                context.EscapedDotNetException = e;
            }
        }
    }
}
