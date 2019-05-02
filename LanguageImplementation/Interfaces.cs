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

    public interface IPyCallable
    {
        IEnumerable<SchedulingInfo> Call(IInterpreter interpreter, FrameContext context, object[] args);
    }
}
