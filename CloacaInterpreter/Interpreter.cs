using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation;

namespace CloacaInterpreter
{
    // Traditional block in Python has: frame, opcode, handler (pointer to next instruction outside of the loop), value stack size
    // We don't have frames yet, we'll just BS our way through others for now.
    public class Block
    {
        public ByteCodes Opcode
        {
            get; private set;
        }

        public int HandlerAddress
        {
            get; private set;
        }

        public int StackSize
        {
            get; private set;
        }

        public Block(ByteCodes opcode, int handlerAddress, int stackSize)
        {
            this.Opcode = opcode;
            this.HandlerAddress = handlerAddress;
            this.StackSize = stackSize;
        }
    }

    public class Frame
    {
        public int Cursor;
        public Stack<Block> BlockStack;
        public Stack<object> DataStack;
        public CodeObject Program;
        public List<string> LocalNames;
        public List<object> Locals;

        public Frame()
        {
            Cursor = 0;
            BlockStack = new Stack<Block>();
            DataStack = new Stack<object>();
            Program = null;
            LocalNames = new List<string>();
            Locals = new List<object>();
        }

        public Frame(CodeObject program)
        {
            Cursor = 0;
            BlockStack = new Stack<Block>();
            DataStack = new Stack<object>();
            LocalNames = new List<string>();
            Program = program;
            Locals = new List<object>();
        }
    }

    public class Interpreter: IInterpreter
    {
        private CodeObject rootProgram;
        private Stack<Frame> callStack;
        public bool DumpState;
        
        // Implementation of builtins.__build_class__
        // TODO: Add params type to handle one or more base classes (inheritance test)
        public PyClass builtins__build_class(CodeObject func, string name)
        {
            return new PyClass(name, func, this);
        }

        public Frame CurrentFrame
        {
            get
            {
                return callStack.Peek();
            }            
        }

        public int Cursor
        {
            get
            {
                return callStack.Peek().Cursor;
            }
            set
            {
                callStack.Peek().Cursor = value;
            }
        }

        public Stack<Object> DataStack
        {
            get
            {
                return callStack.Peek().DataStack;
            }
        }

        public Stack<Block> BlockStack
        {
            get
            {
                return callStack.Peek().BlockStack;
            }
        }

        public CodeObject Program
        {
            get
            {
                return callStack.Peek().Program;
            }
        }

        public byte[] Code
        {
            get
            {
                return callStack.Peek().Program.Code.Bytes;
            }
        }

        public CodeByteArray CodeBytes
        {
            get
            {
                return callStack.Peek().Program.Code;
            }
        }

        public List<object> Locals
        {
            get
            {
                return callStack.Peek().Locals;
            }
        }

        public List<string> Names
        {
            get
            {
                return callStack.Peek().LocalNames;
            }
        }

        public Dictionary<string, object> DumpVariables()
        {
            var variables = new Dictionary<string, object>();
            for (int i = 0; i < Names.Count; ++i)
            {
                variables.Add(Names[i], Locals[i]);
            }
            return variables;
        }

        public bool Terminated
        {
            get; private set;
        }

        /// <summary>
        /// Retains the current frame state but enters a new child CodeObject. This is equivalent to
        /// using a CALL_FUNCTION opcode to descene into a subroutine or similar, but can be invoked
        /// external into the interpreter. It is used for inner, coordinating code to call back into
        /// the interpreter to get results. For example, this is used in object creation to invoke
        /// __new__ and __init__.
        /// </summary>
        /// <param name="functionToRun">The code object to call into</param>
        /// <param name="args">The arguments for the program. These are put on the existing data stack</param>
        /// <returns>Whatever was provided by the RETURN_VALUE on top-of-stack at the end of the program</returns>
        public object CallInto(CodeObject functionToRun, object[] args)
        {
            Frame nextFrame = new Frame();
            nextFrame.Program = functionToRun;

            // Assigning argument's initial values.
            for (int argIdx = 0; argIdx < args.Length; ++argIdx)
            {
                nextFrame.LocalNames.Add(nextFrame.Program.ArgVarNames[argIdx]);
                nextFrame.Locals.Add(args[argIdx]);
            }
            for (int varIndex = 0; varIndex < nextFrame.Program.VarNames.Count; ++varIndex)
            {
                nextFrame.LocalNames.Add(nextFrame.Program.VarNames[varIndex]);
                nextFrame.Locals.Add(null);
            }

            callStack.Push(nextFrame);      // nextFrame is now the active frame.
            Run();
            if(DataStack.Count > 0)
            {
                return DataStack.Pop();
            }
            else
            {
                return null;
            }            
        }

        public Interpreter(CodeObject program)
        {
            this.rootProgram = program;
            Reset();
        }

        public void SetVariable(string name, object value)
        {
            int varIdx = Names.IndexOf(name);
            if(varIdx < 0)
            {
                throw new KeyNotFoundException("Could not find variable in locals named " + name);
            }
            Locals[varIdx] = value;
        }

        public void Reset()
        {
            Terminated = false;
            callStack = new Stack<Frame>();
            callStack.Push(new Frame(rootProgram));

            foreach (string name in rootProgram.VarNames)
            {
                Names.Add(name);
                Locals.Add(null);
            }
        }

        public void Run()
        {
            bool keepRunning = true;
            while(Cursor < Code.Length && keepRunning)
            {
                //if(DumpState)
                //{
                //    Console.WriteLine(Dis.dis(callStack.Peek().Program, Cursor, 1));
                //}

                var opcode = (ByteCodes)Code[Cursor];
                switch(opcode)
                {
                    case ByteCodes.BINARY_ADD:
                        {
                            dynamic right = DataStack.Pop();
                            dynamic left = DataStack.Pop();
                            DataStack.Push(left + right);
                        }
                        Cursor += 1;
                        break;
                    case ByteCodes.BINARY_SUBTRACT:
                        {
                            dynamic right = DataStack.Pop();
                            dynamic left = DataStack.Pop();
                            DataStack.Push(left - right);
                        }
                        Cursor += 1;
                        break;
                    case ByteCodes.BINARY_MULTIPLY:
                        {
                            dynamic right = DataStack.Pop();
                            dynamic left = DataStack.Pop();
                            DataStack.Push(left * right);
                        }
                        Cursor += 1;
                        break;
                    case ByteCodes.BINARY_DIVIDE:
                        {
                            dynamic right = DataStack.Pop();
                            dynamic left = DataStack.Pop();
                            DataStack.Push(left / right);
                        }
                        Cursor += 1;
                        break;
                    case ByteCodes.LOAD_CONST:
                        {
                            Cursor += 1;
                            DataStack.Push(Program.Constants[CodeBytes.GetUShort(Cursor)]);
                        }
                        Cursor += 2;
                        break;
                    case ByteCodes.STORE_NAME:
                        {
                            throw new NotImplementedException("STORE_NAME is unsupported; we're still trying to figure out what it does");
                        }
                    case ByteCodes.STORE_FAST:
                        {
                            Cursor += 1;
                            var localIdx = CodeBytes.GetUShort(Cursor);
                            Locals[localIdx] = DataStack.Pop();
                        }
                        Cursor += 2;
                        break;
                    case ByteCodes.STORE_GLOBAL:
                        {
                            {
                                Cursor += 1;
                                var globalIdx = CodeBytes.GetUShort(Cursor);
                                var globalName = Program.Names[globalIdx];

                                bool foundVar = false;

                                foreach (var stackFrame in callStack)
                                {
                                    // Skip current stack
                                    if (stackFrame == CurrentFrame)
                                    {
                                        continue;
                                    }

                                    var nameIdx = stackFrame.LocalNames.IndexOf(globalName);
                                    if (nameIdx >= 0)
                                    {
                                        stackFrame.Locals[nameIdx] = DataStack.Pop();
                                        foundVar = true;
                                    }
                                }

                                if (!foundVar)
                                {
                                    throw new Exception("Global '" + globalName + "' was not found!");
                                }
                            }
                            Cursor += 2;
                            break;
                        }
                    case ByteCodes.LOAD_NAME:
                        {
                            throw new NotImplementedException("LOAD_NAME is unsupported; we're still trying to figure out what it does");
                        }
                    case ByteCodes.LOAD_FAST:
                        {
                            Cursor += 1;
                            DataStack.Push(Locals[CodeBytes.GetUShort(Cursor)]);
                        }
                        Cursor += 2;
                        break;
                    case ByteCodes.LOAD_GLOBAL:
                        {
                            {
                                Cursor += 1;
                                var globalIdx = CodeBytes.GetUShort(Cursor);
                                var globalName = Program.Names[globalIdx];

                                object foundVar = null;

                                foreach(var stackFrame in callStack)
                                {
                                    // Skip current stack
                                    if(stackFrame == CurrentFrame)
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
                                    DataStack.Push(foundVar);
                                }
                                else
                                {
                                    throw new Exception("Global '" + globalName + "' was not found!");
                                }
                            }
                            Cursor += 2;
                            break;
                        }
                    case ByteCodes.WAIT:
                        {
                            keepRunning = false;
                        }
                        Cursor += 1;
                        break;
                    case ByteCodes.COMPARE_OP:
                        {
                            Cursor += 1;
                            var compare_op = (CompareOps)Program.Code.GetUShort(Cursor);
                            dynamic right = DataStack.Pop();
                            dynamic left = DataStack.Pop();
                            switch (compare_op)
                            {
                                case CompareOps.Lt:
                                    DataStack.Push(left < right);
                                    break;
                                case CompareOps.Gt:
                                    DataStack.Push(left > right);
                                    break;
                                case CompareOps.Eq:
                                    DataStack.Push(left == right);
                                    break;
                                case CompareOps.Gte:
                                    DataStack.Push(left >= right);
                                    break;
                                case CompareOps.Lte:
                                    DataStack.Push(left <= right);
                                    break;
                                case CompareOps.LtGt:
                                    DataStack.Push(left < right || left > right);
                                    break;
                                case CompareOps.Ne:
                                    DataStack.Push(left != right);
                                    break;
                                case CompareOps.In:
                                    throw new NotImplementedException("'In' comparison operation");
                                case CompareOps.NotIn:
                                    throw new NotImplementedException("'Not In' comparison operation");
                                case CompareOps.Is:
                                    DataStack.Push(left.GetType() == right.GetType() && left == right);
                                    break;
                                case CompareOps.IsNot:
                                    DataStack.Push(left.GetType() != right.GetType() || left != right);
                                    break;
                                default:
                                    throw new Exception("Unexpected comparision operation opcode: " + compare_op);
                            }
                        }
                        Cursor += 2;
                        break;
                    case ByteCodes.JUMP_IF_TRUE:
                        {
                            Cursor += 1;
                            var jumpPosition = CodeBytes.GetUShort(Cursor);
                            var conditional = (bool)DataStack.Pop();
                            if (conditional)
                            {
                                Cursor = jumpPosition;
                                continue;
                            }
                        }
                        Cursor += 2;
                        break;
                    case ByteCodes.JUMP_IF_FALSE:
                        {
                            Cursor += 1;
                            var jumpPosition = CodeBytes.GetUShort(Cursor);
                            var conditional = (bool)DataStack.Pop();
                            if(!conditional)
                            {
                                Cursor = jumpPosition;
                                continue;
                            }
                        }
                        Cursor += 2;
                        break;
                    case ByteCodes.SETUP_LOOP:
                        {
                            Cursor += 1;
                            var loopResumptionPoint = CodeBytes.GetUShort(Cursor);
                            Cursor += 2;
                            BlockStack.Push(new Block(ByteCodes.SETUP_LOOP, loopResumptionPoint, DataStack.Count));
                        }
                        break;
                    case ByteCodes.POP_BLOCK:
                        {
                            var block = BlockStack.Pop();

                            // Restore the stack.
                            while(DataStack.Count > block.StackSize)
                            {
                                DataStack.Pop();
                            }
                        }
                        Cursor += 1;
                        break;
                    case ByteCodes.JUMP_ABSOLUTE:
                        {
                            Cursor += 1;
                            var jumpPosition = CodeBytes.GetUShort(Cursor);
                            Cursor = jumpPosition;
                            continue;
                        }
                    case ByteCodes.JUMP_FORWARD:
                        {
                            Cursor += 1;
                            var jumpOffset = CodeBytes.GetUShort(Cursor);
                            
                            // Offset is based off of the NEXT instruction so add one.
                            Cursor += jumpOffset + 2;
                            continue;
                        }
                    case ByteCodes.MAKE_FUNCTION:
                        {
                            // TOS-1 is the code object
                            // TOS is the function's qualified name
                            Cursor += 1;
                            var functionOpcode = CodeBytes.GetUShort(Cursor);             // Currently not using.
                            string qualifiedName = (string)DataStack.Pop();
                            CodeObject functionCode = (CodeObject)DataStack.Pop();
                            DataStack.Push(functionCode);
                        }
                        Cursor += 2;
                        break;
                    case ByteCodes.CALL_FUNCTION:
                        {
                            Cursor += 1;
                            var argCount = CodeBytes.GetUShort(Cursor);             // Currently not using.

                            // This is annoying. The arguments are at the top of the stack while
                            // the function is under them, but we need the function to assign the
                            // values. So we'll just copy them off piecemeal. Hence they'll show up
                            // in reverse order.
                            var args = new List<object>();
                            for (int argIdx = 0; argIdx < argCount; ++argIdx)
                            {
                                args.Insert(0, DataStack.Pop());
                            }

                            object abstractFunctionToRun = DataStack.Pop();
                            if (abstractFunctionToRun is WrappedCodeObject)
                            {
                                // This is currently done very naively. No conversion of types between
                                // Python and .NET. We're just starting to enable the plumbing before stepping
                                // back and seeing what we got for all the trouble.
                                var functionToRun = (WrappedCodeObject)abstractFunctionToRun;
                                object retVal = functionToRun.Call(args.ToArray());
                                if(functionToRun.MethodInfo.ReturnType != typeof(void))
                                {
                                    DataStack.Push(retVal);
                                }
                                Cursor += 2;
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

                                    // Right now, __new__ is hard-coded because we don't have abstraction to 
                                    // call either Python code or built-in code.
                                    var self = asClass.__new__.Call(new object[] { asClass });
                                    CallInto(asClass.__init__, new object[] { self });
                                    DataStack.Push(self);
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
                                        CallInto(functionToRun, args.ToArray());
                                        DataStack.Push(self);
                                    }

                                    // We're assuming it's a good-old-fashioned CodeObject
                                    var retVal = CallInto(functionToRun, args.ToArray());
                                    if (retVal != null)
                                    {
                                        DataStack.Push(retVal);
                                    }
                                }
                                Cursor += 2;                    // Resume at next instruction in this program.                                
                            }
                            continue;
                        }
                    case ByteCodes.RETURN_VALUE:
                        {
                            Frame returningFrame = callStack.Pop();

                            // The calling frame is now active.
                            // Apparently the return value is the topmost element of the stack
                            // http://www.aosabook.org/en/500L/a-python-interpreter-written-in-python.html
                            // "First it will pop the top value off the data stack of the top frame on the call stack."
                            // We won't add anything if we have nothing to return for now, although we would more appropriately
                            // return NoneType.
                            if (returningFrame.DataStack.Count > 0)
                            {
                                DataStack.Push(returningFrame.DataStack.Pop());
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
                            Cursor += 1;
                            var tupleCount = CodeBytes.GetUShort(Cursor);
                            Cursor += 2;
                            object[] tuple = new object[tupleCount];
                            for(int i = tupleCount-1; i >= 0; --i)
                            {
                                tuple[i] = DataStack.Pop();
                            }
                            DataStack.Push(new PyTuple(tuple));
                        }
                        break;
                    case ByteCodes.BUILD_MAP:
                        {
                            Cursor += 1;
                            var dictSize = CodeBytes.GetUShort(Cursor);
                            Cursor += 2;
                            var dict = new Dictionary<object, object>();
                            for(int i = 0; i < dictSize; ++i)
                            {
                                var value = DataStack.Pop();
                                var key = DataStack.Pop();
                                dict.Add(key, value);
                            }
                            DataStack.Push(dict);
                        }
                        break;
                    case ByteCodes.BUILD_CONST_KEY_MAP:
                        {
                            // NOTE: Our code visitor doesn't generate this opcode.
                            // Top of a stack is the tuple for keys. Operand is how many values to pop off of the
                            // stack, which is kind of interesting since the tuple length should imply that...
                            Cursor += 1;
                            var dictSize = CodeBytes.GetUShort(Cursor);
                            Cursor += 2;
                            var dict = new Dictionary<object, object>();
                            var keyTuple = (PyTuple)DataStack.Pop();
                            for(int i = keyTuple.values.Length-1; i >= 0; --i)
                            {
                                dict[keyTuple.values[i]] = DataStack.Pop();
                            }
                            DataStack.Push(dict);
                        }
                        break;
                    case ByteCodes.BUILD_LIST:
                        {
                            Cursor += 1;
                            var listSize = CodeBytes.GetUShort(Cursor);
                            Cursor += 2;
                            var list = new List<object>();
                            for (int i = listSize - 1; i >= 0; --i)
                            {
                                list.Add(null);
                            }
                            for (int i = listSize - 1; i >= 0; --i)
                            {
                                list[i] = DataStack.Pop();
                            }
                            DataStack.Push(list);
                        }
                        break;
                    case ByteCodes.BINARY_SUBSCR:
                        {
                            Cursor += 1;
                            var index = DataStack.Pop();
                            var container = DataStack.Pop();
                            if(container is Dictionary<object, object>)
                            {
                                var asDict = (Dictionary<object, object>)container;
                                DataStack.Push(asDict[index]);
                            }
                            else if(container is List<object>)
                            {
                                var asList = (List<object>)container;
                                var indexAsBigInt = (BigInteger)index;
                                DataStack.Push(asList[(int) indexAsBigInt]);
                            }
                            else if(container is PyTuple)
                            {
                                var asTuple = (PyTuple)container;
                                var indexAsBigInt = (BigInteger)index;
                                DataStack.Push(asTuple.values[(int) indexAsBigInt]);
                            }
                            else
                            {
                                throw new Exception("Unexpected container type in BINARY_SUBSCR:" + container.GetType().ToString());
                            }
                        }
                        break;
                    case ByteCodes.STORE_SUBSCR:
                        {
                            Cursor += 1;
                            var rawIndex = DataStack.Pop();
                            var rawContainer = DataStack.Pop();
                            var toStore = DataStack.Pop();

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
                            Cursor += 1;
                            // Push builtins.__build_class__ on to the datastack
                            // TODO: Build and register these built-ins just once.
                            Expression<Action<Interpreter>> expr = instance => builtins__build_class(null, null);
                            var methodInfo = ((MethodCallExpression)expr.Body).Method;
                            var class_builder = new WrappedCodeObject("__build_class__", methodInfo, this);
                            DataStack.Push(class_builder);
                        }
                        break;
                    default:
                        throw new Exception("Unexpected opcode: " + opcode);
                }
            }
        }
    }
}
