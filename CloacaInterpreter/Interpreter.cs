using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
    public class Interpreter: IInterpreter
    {
        private Dictionary<string, object> builtins;

        public Interpreter()
        {
            builtins = new Dictionary<string, object>
            {
                { "Exception", PyExceptionClass.Instance }
            };
        }

        public bool DumpState;

        /// <summary>
        /// Implementation of builtins.__build_class__. This create a class as a PyClass.
        /// </summary>
        /// <param name="context">The context of script code that wants to make a class.</param>
        /// <param name="func">The class body as interpretable code.</param>
        /// <param name="name">The name of the class.</param>
        /// <returns>Since it calls the CodeObject, it may end up yielding. It will ultimately finish by yielding a
        /// ReturnValue object containing the PyClass of the built class.</returns>
        public IEnumerable<SchedulingInfo> builtins__build_class(FrameContext context, CodeObject func, string name)
        {
            // TODO: Add params type to handle one or more base classes (inheritance test)
            Frame classFrame = new Frame(func);
            classFrame.AddLocal("__name__", name);
            classFrame.AddLocal("__module__", null);
            classFrame.AddLocal("__qualname__", null);

            foreach(var yielding in CallInto(context, classFrame, new object[0]))
            {
                yield return yielding;
            }

            var initIdx = classFrame.LocalNames.IndexOf("__init__");
            CodeObject __init__ = null;
            if(initIdx < 0)
            {
                // Insert a default constructor. This comes up as a "slot wrapper" at least in Python 3.6. For us, we're
                // just making our own no-op __init__ for now.
                // TODO: Replace with a wrapped default when WrappedCodeObject is freely interchangable with CodeObject
                var initBuilder = new CodeObjectBuilder();
                initBuilder.AddInstruction(ByteCodes.LOAD_CONST, 0);
                initBuilder.Constants.Add(null);
                initBuilder.AddInstruction(ByteCodes.RETURN_VALUE);
                initBuilder.Name = "__init__";
                initBuilder.ArgVarNames.Add("self");
                __init__ = initBuilder.Build();
            }
            else
            {
                __init__ = (CodeObject)classFrame.Locals[initIdx];
            }

            var pyclass = new PyClass(name, __init__, this, context);

            foreach(var classMemberName in classFrame.Names)
            {
                var nameIdx = classFrame.LocalNames.IndexOf(classMemberName);
                if (nameIdx >= 0)
                {                    
                    pyclass.__dict__.Add(classMemberName, classFrame.Locals[nameIdx]);
                }
                else
                {
                    pyclass.__dict__.Add(classMemberName, context.GetVariable(classMemberName));
                }
            }
            
            yield return new ReturnValue(pyclass);
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
        /// <returns>The underlying code may yield so this will return various SchedulingInfo. After all inner
        /// yielding is finished, it will return a ReturnValue containing the top of the stack if it contains
        /// anything at the end of the call.</returns>
        public IEnumerable<SchedulingInfo> CallInto(FrameContext context, CodeObject functionToRun, object[] args)
        {
            Frame nextFrame = new Frame();
            nextFrame.Program = functionToRun;

            foreach(var yielding in CallInto(context, nextFrame, args))
            {
                yield return yielding;
            }

            if(context.DataStack.Count > 0)
            {
                yield return new ReturnValue(context.DataStack.Pop());
            }
            else
            {
                yield break;
            }
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
        /// <returns>The underlying code may yield so this will return various SchedulingInfo. After all inner
        /// yielding is finished, it will return a ReturnValue containing the top of the stack if it contains
        /// anything at the end of the call.</returns>
        public IEnumerable<SchedulingInfo> CallInto(FrameContext context, Frame frame, object[] args)
        {
            // Assigning argument's initial values.
            for (int argIdx = 0; argIdx < args.Length; ++argIdx)
            {
                frame.AddLocal(frame.Program.ArgVarNames[argIdx], args[argIdx]);
            }
            for (int varIndex = 0; varIndex < frame.Program.VarNames.Count; ++varIndex)
            {
                frame.AddLocal(frame.Program.VarNames[varIndex], null);
            }

            context.callStack.Push(frame);      // nextFrame is now the active frame.

            foreach(var yielding in Run(context))
            {
                yield return yielding;
            }

            if (context.DataStack.Count > 0)
            {
                yield return new ReturnValue(context.DataStack.Pop());
            }
            else
            {
                yield break;
            }
        }

        /// <summary>
        /// Prepare a fresh frame context to run the given CodeObject.
        /// </summary>
        /// <param name="newProgram">The code to prepare to run.</param>
        /// <returns>The context that the interpreter can use to run the program.</returns>
        public FrameContext PrepareFrameContext(CodeObject newProgram)
        {
            var newFrameStack = new Stack<Frame>();
            var rootFrame = new Frame(newProgram);

            foreach (string name in newProgram.VarNames)
            {
                rootFrame.AddLocal(name, null);
            }

            newFrameStack.Push(rootFrame);
            return new FrameContext(newFrameStack);
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

        /// <summary>
        /// Runs the given frame context until it either finishes normally or yields. This actually intrepts
        /// our Python(ish) code!
        /// 
        /// This call is stateless; all the state changes mae happen in the FrameContext passed into Run().
        /// </summary>
        /// <param name="context">The current state of the frame and stacks to run</param>
        /// <returns>The underlying code may yield so this will return various SchedulingInfo. It will not
        /// return a ReturnValue variant of ScheduleInfo. The ScheduleInfo is supposed to provide context
        /// to the scheduler or parent code that invoked the context.</returns>
        public IEnumerable<SchedulingInfo> Run(FrameContext context)
        {
            while(context.Cursor < context.Code.Length)
            {
                //if(DumpState)
                //{
                //    Console.WriteLine(Dis.dis(callStack.Peek().Program, Cursor, 1));
                //}

                // Are we unwinding from an exception?
                while(context.CurrentException != null && context.BlockStack.Peek().Opcode != ByteCodes.SETUP_FINALLY)
                {
                    if (context.BlockStack.Count > 0)
                    {
                        var block = UnrollCurrentBlock(context);
                        if(block.Opcode == ByteCodes.SETUP_EXCEPT)
                        {
                            // We'll now go to the except routine.
                            context.DataStack.Push(context.CurrentException);
                            context.CurrentException = null;
                            context.Cursor = block.HandlerAddress;
                            break;
                        }
                        else if(block.Opcode == ByteCodes.SETUP_FINALLY)
                        {
                            context.Cursor = block.HandlerAddress;
                            break;
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }

                var opcode = (ByteCodes)context.Code[context.Cursor];
                switch(opcode)
                {
                    case ByteCodes.BINARY_ADD:
                        {
                            dynamic right = context.DataStack.Pop();
                            dynamic left = context.DataStack.Pop();
                            context.DataStack.Push(left + right);
                        }
                        context.Cursor += 1;
                        break;
                    case ByteCodes.BINARY_SUBTRACT:
                        {
                            dynamic right = context.DataStack.Pop();
                            dynamic left = context.DataStack.Pop();
                            context.DataStack.Push(left - right);
                        }
                        context.Cursor += 1;
                        break;
                    case ByteCodes.BINARY_MULTIPLY:
                        {
                            dynamic right = context.DataStack.Pop();
                            dynamic left = context.DataStack.Pop();
                            context.DataStack.Push(left * right);
                        }
                        context.Cursor += 1;
                        break;
                    case ByteCodes.BINARY_DIVIDE:
                        {
                            dynamic right = context.DataStack.Pop();
                            dynamic left = context.DataStack.Pop();
                            context.DataStack.Push(left / right);
                        }
                        context.Cursor += 1;
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

                            var obj = (PyObject)context.DataStack.Pop();
                            var val = context.DataStack.Pop();

                            obj.__setattr__(attrName, val);
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

                                foreach(var stackFrame in context.callStack)
                                {
                                    // Skip current stack
                                    if(stackFrame == context.callStack.Peek())
                                    {
                                        continue;
                                    }

                                    var nameIdx = stackFrame.LocalNames.IndexOf(globalName);
                                    if(nameIdx >= 0)
                                    {
                                        foundVar = stackFrame.Locals[nameIdx];
                                        break;
                                    }                                        
                                }

                                if(foundVar != null)
                                {
                                    context.DataStack.Push(foundVar);
                                }
                                else if(builtins.ContainsKey(globalName))
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

                            var obj = (PyObject)context.DataStack.Pop();
                            var val = obj.__getattribute__(attrName);

                            context.DataStack.Push(val);
                        }
                        context.Cursor += 2;
                        break;
                    case ByteCodes.WAIT:
                        {
                            // *very important!* advance the cursor first! Otherwise, we come right back to this wait
                            // instruction!
                            context.Cursor += 1;
                            yield return new YieldOnePass();
                        }
                        break;
                    case ByteCodes.COMPARE_OP:
                        {
                            context.Cursor += 1;
                            var compare_op = (CompareOps)context.Program.Code.GetUShort(context.Cursor);
                            dynamic right = context.DataStack.Pop();
                            dynamic left = context.DataStack.Pop();
                            switch (compare_op)
                            {
                                case CompareOps.Lt:
                                    context.DataStack.Push(left < right);
                                    break;
                                case CompareOps.Gt:
                                    context.DataStack.Push(left > right);
                                    break;
                                case CompareOps.Eq:
                                    context.DataStack.Push(left == right);
                                    break;
                                case CompareOps.Gte:
                                    context.DataStack.Push(left >= right);
                                    break;
                                case CompareOps.Lte:
                                    context.DataStack.Push(left <= right);
                                    break;
                                case CompareOps.LtGt:
                                    context.DataStack.Push(left < right || left > right);
                                    break;
                                case CompareOps.Ne:
                                    context.DataStack.Push(left != right);
                                    break;
                                case CompareOps.In:
                                    throw new NotImplementedException("'In' comparison operation");
                                case CompareOps.NotIn:
                                    throw new NotImplementedException("'Not In' comparison operation");
                                case CompareOps.Is:
                                    context.DataStack.Push(left.GetType() == right.GetType() && left == right);
                                    break;
                                case CompareOps.IsNot:
                                    context.DataStack.Push(left.GetType() != right.GetType() || left != right);
                                    break;
                                case CompareOps.ExceptionMatch:
                                    {
                                        var rightType = right as PyExceptionClass;
                                        if(rightType == null)
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
                        context.Cursor += 2;
                        break;
                    case ByteCodes.JUMP_IF_TRUE:
                        {
                            context.Cursor += 1;
                            var jumpPosition = context.CodeBytes.GetUShort(context.Cursor);
                            var conditional = (bool)context.DataStack.Peek();
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
                            var conditional = (bool)context.DataStack.Peek();
                            if(!conditional)
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
                            var conditional = (bool)context.DataStack.Peek();
                            if (conditional)
                            {
                                context.Cursor = jumpPosition;
                                context.DataStack.Pop();
                                continue;
                            }
                        }
                        context.Cursor += 2;
                        break;
                    case ByteCodes.POP_JUMP_IF_FALSE:
                        {
                            context.Cursor += 1;
                            var jumpPosition = context.CodeBytes.GetUShort(context.Cursor);
                            var conditional = (bool)context.DataStack.Peek();
                            if (!conditional)
                            {
                                context.Cursor = jumpPosition;
                                context.DataStack.Pop();
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
                            if (abstractFunctionToRun is WrappedCodeObject)
                            {
                                // This is currently done very naively. No conversion of types between
                                // Python and .NET. We're just starting to enable the plumbing before stepping
                                // back and seeing what we got for all the trouble.
                                var functionToRun = (WrappedCodeObject)abstractFunctionToRun;
                                foreach(var continuation in functionToRun.Call(this, context, args.ToArray()))
                                {
                                    if (continuation is ReturnValue)
                                    {
                                        var asReturnValue = continuation as ReturnValue;
                                        if (asReturnValue.Returned != null)
                                        {
                                            context.DataStack.Push(asReturnValue.Returned);
                                        }
                                    }
                                    else
                                    {
                                        yield return continuation;
                                    }
                                }

                                context.Cursor += 2;
                            }
                            else
                            {
                                CodeObject functionToRun = null;
                                if (abstractFunctionToRun is PyClass)
                                {
                                    // To create a class:
                                    // 1. Get a self reference from __new__
                                    // 2. Pass it to __init__
                                    // 3. Return the self reference                                    
                                    var asClass = (PyClass)abstractFunctionToRun;
                                    PyObject self = null;
                                    foreach (var continuation in asClass.__new__.Call(this, context, new object[] { asClass }))
                                    {
                                        if (continuation is ReturnValue)
                                        {
                                            var asReturnValue = continuation as ReturnValue;
                                            self = asReturnValue.Returned as PyObject;
                                        }
                                        else
                                        {
                                            yield return continuation;
                                        }
                                    }

                                    args.Insert(0, self);
                                    foreach(var continuation in asClass.__init__.Call(this, context, args.ToArray()))
                                    {
                                        // Suppress the self reference that gets returned since, well, we already have it.
                                        // We don't need it to escape upwards for cause reschedules.
                                        if (continuation is ReturnValue)
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            yield return continuation;
                                        }
                                    }
                                    context.DataStack.Push(self);
                                }
                                else
                                {
                                    // Could still be a constructor!
                                    functionToRun = (CodeObject)abstractFunctionToRun;

                                    if(functionToRun.Name == "__init__")
                                    {
                                        // Yeap, it's a user-specified constructor. We'll still use our internal __new__
                                        // to make the stub since we don't support overridding __new__ yet.
                                        // TODO: Reconcile this with stubbed __new__. This is such a mess.
                                        var self = new PyObject();      // This is the default __new__ for now.
                                        args.Insert(0, self);
                                        foreach(var continuation in functionToRun.Call(this, context, args.ToArray()))
                                        {
                                            yield return continuation;
                                        }
                                        context.DataStack.Push(self);
                                    }

                                    // We're assuming it's a good-old-fashioned CodeObject
                                    foreach (var continuation in CallInto(context, functionToRun, args.ToArray()))
                                    {
                                        if (continuation is ReturnValue)
                                        {
                                            var asReturnValue = continuation as ReturnValue;
                                            context.DataStack.Push(asReturnValue.Returned);
                                        }
                                        else
                                        {
                                            yield return continuation;
                                        }
                                    }
                                }
                                context.Cursor += 2;                    // Resume at next instruction in this program.                                
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
                            yield break;
                        }
                    case ByteCodes.BUILD_TUPLE:
                        {
                            context.Cursor += 1;
                            var tupleCount = context.CodeBytes.GetUShort(context.Cursor);
                            context.Cursor += 2;
                            object[] tuple = new object[tupleCount];
                            for(int i = tupleCount-1; i >= 0; --i)
                            {
                                tuple[i] = context.DataStack.Pop();
                            }
                            context.DataStack.Push(new PyTuple(tuple));
                        }
                        break;
                    case ByteCodes.BUILD_MAP:
                        {
                            context.Cursor += 1;
                            var dictSize = context.CodeBytes.GetUShort(context.Cursor);
                            context.Cursor += 2;
                            var dict = new Dictionary<object, object>();
                            for(int i = 0; i < dictSize; ++i)
                            {
                                var value = context.DataStack.Pop();
                                var key = context.DataStack.Pop();
                                dict.Add(key, value);
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
                            for(int i = keyTuple.values.Length-1; i >= 0; --i)
                            {
                                dict[keyTuple.values[i]] = context.DataStack.Pop();
                            }
                            context.DataStack.Push(dict);
                        }
                        break;
                    case ByteCodes.BUILD_LIST:
                        {
                            context.Cursor += 1;
                            var listSize = context.CodeBytes.GetUShort(context.Cursor);
                            context.Cursor += 2;
                            var list = new List<object>();
                            for (int i = listSize - 1; i >= 0; --i)
                            {
                                list.Add(null);
                            }
                            for (int i = listSize - 1; i >= 0; --i)
                            {
                                list[i] = context.DataStack.Pop();
                            }
                            context.DataStack.Push(list);
                        }
                        break;
                    case ByteCodes.BINARY_SUBSCR:
                        {
                            context.Cursor += 1;
                            var index = context.DataStack.Pop();
                            var container = context.DataStack.Pop();
                            if(container is Dictionary<object, object>)
                            {
                                var asDict = (Dictionary<object, object>)container;
                                context.DataStack.Push(asDict[index]);
                            }
                            else if(container is List<object>)
                            {
                                var asList = (List<object>)container;
                                var indexAsBigInt = (BigInteger)index;
                                context.DataStack.Push(asList[(int) indexAsBigInt]);
                            }
                            else if(container is PyTuple)
                            {
                                var asTuple = (PyTuple)container;
                                var indexAsBigInt = (BigInteger)index;
                                context.DataStack.Push(asTuple.values[(int) indexAsBigInt]);
                            }
                            else
                            {
                                throw new Exception("Unexpected container type in BINARY_SUBSCR:" + container.GetType().ToString());
                            }
                        }
                        break;
                    case ByteCodes.STORE_SUBSCR:
                        {
                            context.Cursor += 1;
                            var rawIndex = context.DataStack.Pop();
                            var rawContainer = context.DataStack.Pop();
                            var toStore = context.DataStack.Pop();

                            if (rawContainer is Dictionary<object, object>)
                            {
                                var asDict = (Dictionary<object, object>)rawContainer;
                                if(!asDict.ContainsKey(rawIndex))
                                {
                                    asDict.Add(rawIndex, toStore);
                                }
                                else
                                {
                                    asDict[rawIndex] = toStore;
                                }
                            }
                            else if (rawContainer is List<object>)
                            {
                                var asList = (List<object>)rawContainer;
                                var indexAsBigInt = (BigInteger)rawIndex;
                                asList[(int)indexAsBigInt] = toStore;
                            }
                            else if (rawContainer is PyTuple)
                            {
                                throw new Exception("Cannot modify a tuple");
                            }
                            else
                            {
                                throw new Exception("Unexpected container type in STORE_SUBSCR:" + rawContainer.GetType().ToString());
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
                            // Assuming that the parameter is always one for now.
                            context.Cursor += 1;
                            var argCountIgnored = context.CodeBytes.GetUShort(context.Cursor);
                            var theException = (PyException) context.DataStack.Pop();
                            context.Cursor += 2;
                            context.CurrentException = theException;

                            // TODO: Look into this method of handing try-finally when the try gets an exception.
                            // Kind of goofy thing here. We need to get to the finally block for this exception
                            // block if we are in a SETUP_FINALLY. If we have exceptions, we'll be in a SETUP_EXCEPT
                            // again. I don't think this is the best way to manage this but here we are for now.
                            var currentBlock = context.BlockStack.Count == 0 ? null : context.BlockStack.Peek();
                            if(currentBlock != null && currentBlock.Opcode == ByteCodes.SETUP_FINALLY)
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

                if(StepMode)
                {
                    yield return new YieldOnePass();
                }

            }
        }
    }
}
