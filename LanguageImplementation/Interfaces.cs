using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task<object> CallInto(FrameContext context, CodeObject program, object[] args);
        IScheduler Scheduler { get; }

        /// <summary>
        /// Runs the given frame context until it either finishes normally or yields. This actually interprets
        /// our Python(ish) code!
        /// 
        /// This call is stateless; all the state changes mae happen in the FrameContext passed into Run().
        /// 
        /// Note that in practice, this call will have to be implemented as async.
        /// </summary>
        /// <param name="context">The current state of the frame and stacks to run</param>
        /// <returns>A task if the code being run gets pre-empted cooperatively.</returns>
        Task Run(FrameContext context);

        /// <summary>
        /// Returns true if an exception was raised and the context would not be in a position to still try to
        /// handle it. This is used when stepping through frame context in debugging to allow the interpreter to
        /// keep trying to process the exception. If you just test the frame context for an exception while stepping,
        /// you'll miss out on the interpreter trying out the except (and finally) clauses that have some stuff left
        /// to do. It also misses out on all the unrolling to properly escape.
        /// </summary>
        bool ExceptionEscaped(FrameContext context);

    }

    public interface IPyCallable
    {
        Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args);
    }
}
