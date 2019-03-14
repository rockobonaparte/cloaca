using System;
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
        public List<string> Names;
        public List<object> Locals;

        public Frame()
        {
            Cursor = 0;
            BlockStack = new Stack<Block>();
            DataStack = new Stack<object>();
            Program = null;
            Names = new List<string>();
            Locals = new List<object>();
        }

        public Frame(CodeObject program)
        {
            Cursor = 0;
            BlockStack = new Stack<Block>();
            DataStack = new Stack<object>();
            Names = new List<string>();
            Program = program;
            Locals = new List<object>();
        }
    }

    public class Interpreter
    {
        private CodeObject rootProgram;
        private Stack<Frame> callStack;
        public bool DumpState;

        public Frame CurrentFrame
        {
            get
            {
                return callStack.Peek();
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
                return callStack.Peek().Names;
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

        public object Run()
        {
            // If we terminated, then do nothing.
            if(Terminated)
            {
                return DumpVariables();
            }

            bool keepRunning = true;
            while(Cursor < Code.Length && keepRunning)
            {
                if(DumpState)
                {
                    Console.WriteLine(Dis.dis(callStack.Peek().Program, Cursor, 1));
                }

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

                            var functionToRun = (CodeObject)DataStack.Pop();
                            Frame nextFrame = new Frame();
                            nextFrame.Program = functionToRun;

                            // Assigning argument's initial values.
                            for (int argIdx = 0; argIdx < argCount; ++argIdx)
                            {
                                nextFrame.Names.Add(nextFrame.Program.ArgVarNames[argIdx]);
                                nextFrame.Locals.Add(args[argIdx]);
                            }

                            Cursor += 2;                    // Resume at next instruction in this program.
                            callStack.Push(nextFrame);      // nextFrame is now the active frame.
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
                            continue;
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

                        }
                        break;
                    default:
                        throw new Exception("Unexpected opcode: " + opcode);
                }
            }

            if (Cursor >= Code.Length)
            {
                Terminated = true;
            }

            return DumpVariables();
        }
    }
}
