using System;
using System.Collections.Generic;

namespace LanguageImplementation
{
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

        public List<string> Names
        {
            get
            {
                return Program.Names;
            }
        }

        public void AddLocal(string name, object value)
        {
            LocalNames.Add(name);
            Locals.Add(value);
        }
    }

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


    public class FrameContext
    {
        public Stack<Frame> callStack;

        public FrameContext(Stack<Frame> callStack)
        {
            this.callStack = callStack;
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
                return callStack.Peek().Names;
            }
        }

        public List<string> LocalNames
        {
            get
            {
                return callStack.Peek().LocalNames;
            }
        }

        // This is like doing a LOAD_NAME without pushing it on the stack.
        public object GetVariable(string name)
        {
            // Try to resolve locally, then globally, and then in our built-in namespace
            foreach (var stackFrame in callStack)
            {
                // Unlike LOAD_GLOBAL, the current frame is fair game. In fact, we search it first!
                var nameIdx = stackFrame.LocalNames.IndexOf(name);
                if (nameIdx >= 0)
                {
                    return stackFrame.Locals[nameIdx];
                }
            }

            throw new Exception("'" + name + "' not found in local or global namespaces, and we don't resolve built-ins yet.");
        }

        public void SetVariable(string name, object value)
        {
            int varIdx = LocalNames.IndexOf(name);
            if (varIdx < 0)
            {
                throw new KeyNotFoundException("Could not find variable in locals named " + name);
            }
            Locals[varIdx] = value;
        }

        public Dictionary<string, object> DumpVariables()
        {
            var variables = new Dictionary<string, object>();
            for (int i = 0; i < LocalNames.Count; ++i)
            {
                variables.Add(LocalNames[i], Locals[i]);
            }
            return variables;
        }
    }

    public interface IInterpreter
    {
        /// <summary>
        /// Retains the current frame state but enters a new child CodeObject. This is equivalent to
        /// using a CALL_FUNCTION opcode to descene into a subroutine or similar, but can be invoked
        /// external into the interpreter. It is used for inner, coordinating code to call back into
        /// the interpreter to get results. For example, this is used in object creation to invoke
        /// __new__ and __init__.
        /// </summary>
        /// <param name="context">Context of code currently being run through the interpreter by the scheduler.</param>
        /// <param name="functionToRun">The code object to call into.</param>
        /// <param name="args">The arguments for the program. These are put on the existing data stack.</param>
        /// <returns>Whatever was provided by the RETURN_VALUE on top-of-stack at the end of the program.</returns>
        IEnumerable<SchedulingInfo> CallInto(FrameContext context, CodeObject program, object[] args);
    }
}
