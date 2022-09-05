using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using LanguageImplementation.DataTypes;

namespace LanguageImplementation
{
    public class Frame
    {
        public int Cursor;
        public Stack<Block> BlockStack;
        public Stack<object> DataStack;
        public PyFunction Function;

        public List<object> LocalFasts;             // Used by LOAD/STORE_FAST
        public Dictionary<string, object> Locals;   // Used by LOAD/STORE_NAME

        // In CPython, cell variables are a part of f_localsplus, combined with the stack.
        // Our stack is separate so we're not going to have "localplus."
        public Dictionary<string, PyCellObject> CellVars;   // Cell variables, used by LOAD/STORE_DEREF

        // Technically, globals are owned by the module owning the context of everything we're running.
        // I think in the long term that this will get assigned by those modules. You might see it getting.
        // set elsewhere though.
        public Dictionary<string, object> Globals;

        public Frame()
        {
            Cursor = 0;
            BlockStack = new Stack<Block>();
            DataStack = new Stack<object>();
            Function = null;
            LocalFasts = new List<object>();
            Locals = new Dictionary<string, object>();
            CellVars = new Dictionary<string, PyCellObject>();

            // Perhaps a premature optimization, but we'll be reusing this dictionary so we won't bother
            // setting it to an empty one.
            Globals = null;
        }

        private void createFasts(CodeObject co)
        {
            // Arguments are the first fasts, given in order of their position in the arguments.
            for (int i = 0; i < co.ArgVarNames.Count; ++i)
            {
                LocalFasts.Add(null);
            }
            for (int i = 0; i < co.VarNames.Count; ++i)
            {
                LocalFasts.Add(null);
            }
        }

        public Frame(PyFunction function) : this()
        {
            Function = function;
            createFasts(function.Code);
        }

        public Frame(Dictionary<string, object> globals) : this()
        {
            Globals = globals;
        }

        public Frame(PyFunction function, Dictionary<string, object> globals) : this(function)
        {
            Function = function;
            Globals = globals;
        }

        public Frame(FrameContext parentContext) : this()
        {
            if(parentContext != null && parentContext.callStack.Count > 0)
            {
                Globals = parentContext.callStack.Peek().Globals;
            }
            else
            {
                Globals = new Dictionary<string, object>();
            }
            takeCellVariables(parentContext);
        }

        public Frame(PyFunction function, FrameContext parentContext, Dictionary<string, object> newGlobals=null) : this(function)
        {
            if(newGlobals == null)
            {
                Globals = function.Globals;
            }
            else
            {
                Globals = newGlobals;
            }
            takeCellVariables(parentContext);
        }

        private void takeCellVariables(FrameContext parent)
        {
            foreach(var cellName in Function.Code.CellNames)
            {
                if(parent.Cells.ContainsKey(cellName))
                {
                    CellVars.Add(cellName, parent.Cells[cellName]);
                }
                else
                {
                    CellVars.Add(cellName, new PyCellObject());
                }
            }
        }

        /// <summary>
        /// Helper to prepare a frame for the root of a module. This makes sure that locals==globals.
        /// </summary>
        /// <param name="moduleCode">The module's code</param>
        /// <param name="context">The context that got us to the point of running the module code</param>
        /// <param name="moduleGlobals">The module's globals (which are also its locals)</param>
        /// <returns></returns>
        public static Frame PrepareModuleFrame(PyFunction moduleCode, FrameContext context, Dictionary<string, object> moduleGlobals)
        {
            Frame nextFrame = new Frame(moduleCode, context, moduleGlobals);
            nextFrame.Locals = moduleGlobals;
            return nextFrame;
        }

        public List<string> LocalNames
        {
            get
            {
                return Function.Code.Names;
            }
        }

        public void AddLocal(string name, object value)
        {
            // Can't use AddGetIndex because LocalNames and Locals are paired. If you took the index from
            // LocalNames and assumed to append whenever the index you got was at the end, you'd miss cases
            // where it positively matches the last index and didn't insert!
            var nameIdx = LocalNames.IndexOf(name);
            if(nameIdx == -1)
            {
                LocalNames.Add(name);
                Locals.AddOrSet(name, value);
            }
            else
            {
                Locals[name] = value;
            }    
        }

        public void SetFastLocal(int idx, object value)
        {
                LocalFasts[idx] = value;
        }

        /// <summary>
        /// Added as a helper when it was discovered that null versions of locals were getting thrown in.
        /// Why null versions of locals were also getting added unnecessarily is a different issue.
        /// 
        /// This will only add the local if it hasn't already been added. This makes sure that our call
        /// helpers don't pad with null unnecessarily as a final resort.
        /// </summary>
        /// <param name="name">The name of the local to add</param>
        /// <param name="value">The value of the local to add.</param>
        public void AddOnlyNewLocal(string name, object value)
        {
            if(!LocalNames.Contains(name))
            {
                AddLocal(name, value);
            }
        }

        /// <summary>
        /// Get a local. Raises KeyNotFoundException if the named local could not be found. This is because
        /// the local could actually be null, so we don't rely on null.
        /// </summary>
        /// <param name="name">The name of the local.</param>
        /// <exception cref="KeyNotFoundException">A local with the given name is not registered with this context.</exception>
        /// <returns>The value of the local, which might be null!</returns>
        public object GetLocal(string name)
        {
            var nameIdx = LocalNames.IndexOf(name);
            if (nameIdx == -1)
            {
                throw new KeyNotFoundException("'" + name + "' as not found amongst locals");
            }
            else
            {
                return Locals[name];
            }
        }

        public void AddGlobal(string name, object value)
        {
            if(Globals.ContainsKey(name))
            {
                Globals[name] = value;
            }
            else
            {
                Globals.Add(name, value);
            }
        }

        /// <summary>
        /// This will only add the global if it hasn't already been added.
        /// </summary>
        /// <param name="name">The name of the global to add</param>
        /// <param name="value">The global value to bind to the given name.</param>
        public void AddOnlyNewGlobal(string name, object value)
        {
            if (!Globals.ContainsKey(name))
            {
                Globals.Add(name, value);
            }
        }

        /// <summary>
        /// Get a local. Raises KeyNotFoundException if the named local could not be found. This is because
        /// the local could actually be null, so we don't rely on null.
        /// </summary>
        /// <param name="name">The name of the local.</param>
        /// <exception cref="KeyNotFoundException">A local with the given name is not registered with this context.</exception>
        /// <returns>The value of the local, which might be null!</returns>
        public object GetGlobal(string name)
        {
            return Globals[name];
        }

        public bool HasGlobal(string name)
        {
            return Globals.ContainsKey(name);
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

        /// <summary>
        /// End of block as an offset from the current position when the block was created.
        /// </summary>
        public int HandlerOffset
        {
            get; private set;
        }

        /// <summary>
        /// Address to the opcode that spurred the creation of this block. Or rather, the instruction AFTER the
        /// instruction that created the block. OriginAdress + HandlerOffset = address of instruction to run if
        /// skipping this block.
        /// 
        /// Note that I don't know how this varies from CPython.
        /// </summary>
        public int OriginAddress
        {
            get; private set;
        }

        public int StackSize
        {
            get; private set;
        }

        public Block(ByteCodes opcode, int originAddress, int handlerOffset, int stackSize)
        {
            this.Opcode = opcode;
            this.HandlerOffset = handlerOffset;
            this.StackSize = stackSize;
            this.OriginAddress = originAddress;
        }
    }

    /// <summary>
    /// Returned from scheduling so the submitter has information about what was scheduled and when it finishes/finished
    /// 
    /// You can await on it to suspend the holder of the record until the associated task finishes.
    /// </summary>
    public class TaskEventRecord : INotifyCompletion
    {
        public FrameContext Frame { get; protected set; }
        public ExceptionDispatchInfo EscapedExceptionInfo { get; protected set; }
        public event OnTaskCompleted WhenTaskCompleted = (ignored) => { };
        public event OnTaskExceptionEscaped WhenTaskExceptionEscaped = (ignoredRecord, ignoredExc) => { };
        public bool Completed { get; protected set; }
        public object ExtraMetadata;

        private Action continuationIfAwaited;

        public TaskEventRecord(FrameContext frame)
        {
            Frame = frame;
            Completed = false;
            EscapedExceptionInfo = null;
            ExtraMetadata = null;
        }

        public void NotifyCompleted()
        {
            Completed = true;
            WhenTaskCompleted(this);
        }

        public void NotifyEscapedException(ExceptionDispatchInfo escapedInfo)
        {
            Completed = true;
            EscapedExceptionInfo = escapedInfo;
            WhenTaskExceptionEscaped(this, escapedInfo);
        }

        #region INotifyCompletion and custom awaiter

        public void OnCompleted(Action continuation)
        {
            if (Completed)
            {
                continuationIfAwaited();
            }
            else
            {
                continuationIfAwaited = continuation;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Completed;
            }
        }

        public Task Continue()
        {
            continuationIfAwaited?.Invoke();
            return Task.FromResult(this);
        }

        public TaskEventRecord GetAwaiter()
        {
            return this;
        }

        public TaskEventRecord GetResult()
        {
            return this;
        }

        #endregion INotifyCompletion and custom awaiter
    }

    public delegate void OnTaskCompleted(TaskEventRecord record);
    public delegate void OnTaskExceptionEscaped(TaskEventRecord record, ExceptionDispatchInfo exc);

    public class ScheduledTaskRecord
    {
        public FrameContext Frame;          // Also serves to uniquely identify this record in the scheduler's queues.
        public ISubscheduledContinuation Continuation;
        public TaskEventRecord SubmitterReceipt;

        public ScheduledTaskRecord(FrameContext frame, ISubscheduledContinuation continuation, TaskEventRecord submitterReceipt)
        {
            Frame = frame;
            Continuation = continuation;
            SubmitterReceipt = submitterReceipt;
        }
    }

    public interface IScheduler
    {
        // This is called when the currently-active script is blocking. Call this right before invoking
        // an awaiter from the task in which the script is running.
        void NotifyBlocked(FrameContext frame, ISubscheduledContinuation continuation);

        // Call this for a continuation that has been previously blocked with NotifyBlocked. This won't
        // immediately resume the script, but will set it up to be run in interpreter's tick interval.
        void NotifyUnblocked(FrameContext frame, ISubscheduledContinuation continuation);

        // Use to cooperatively stop running for just a single tick.
        void SetYielded(FrameContext frame, ISubscheduledContinuation continuation);

        /// <summary>
        /// Schedule a new program to run. It returns the FrameContext that would be used to run the application
        /// in order to do any other housekeeping like inject variables into it.
        /// </summary>
        /// <param name="program">The code to schedule.</param>
        /// <returns>The context the interpreter will use to maintain the program's state while it runs.</returns>
        TaskEventRecord Schedule(PyFunction function);

        TaskEventRecord Schedule(PyFunction function, FrameContext context);

        /// <summary>
        /// Schedule a new program to run that requires certain arguments. This is exposed for other scripts to pass
        /// in function calls that need to be spun up as independent coroutines.
        /// </summary>
        /// <param name="function">The code to schedule. The is treated like a function call with zero or more arguments.</param>
        /// <param name="args">The arguments that need to be seeded to the the code to run.</param>
        /// <returns></returns>
        TaskEventRecord Schedule(PyFunction function, FrameContext context, params object[] args);

        /// <summary>
        /// Returns a copy of a list of all active tasks currently in the scheduler.
        /// </summary>
        /// <returns>All active tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are active.
        /// </returns>
        ScheduledTaskRecord[] GetTasksActive();


        /// <summary>
        /// Returns a copy of a list of all tasks that are currently marked as blocked in the scheduler. Blocked tasks are waiting on an asynchronous result.
        /// </summary>
        /// <returns>All blocked tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are blocked
        /// </returns>
        ScheduledTaskRecord[] GetTasksBlocked();

        /// <summary>
        /// Returns a copy of a list of all tasks that are currently marked as unblocked in the scheduler. These are tasks that have finished yielding or otherwise
        /// got a value asynchronously that will let their scripts continue. Normally investigating this will not find much since these are moved into the
        /// active queue after each scheduler tick. It is mostly useful for particular scheduler diagnostics.
        /// </summary>
        /// <returns>All unblocked tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are blocked
        /// </returns>
        ScheduledTaskRecord[] GetTasksUnblocked();

        /// <summary>
        /// Returns a copy of a list of all tasks that are currently marked as yielded in the scheduler. These are tasks that have taken a break from running. This
        /// can happen either from an explicit yield instruction, or they have exhausted their prescribed run time--if that is even a thing with this scheduler.
        /// </summary>
        /// <returns>All yielded tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are blocked
        ScheduledTaskRecord[] GetTasksYielded();

        /// <summary>
        /// Gets the task currently running. This could be null if no task is running.
        /// </summary>
        /// <returns>The task that's currently running. This could be null if no task is running.</returns>
        ScheduledTaskRecord GetCurrentTask();
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
        /// <param name="newGlobals">Globals to use in this context. Globals can be switched out when calling into a module.</param>
        /// <returns>Whatever was provided by the RETURN_VALUE on top-of-stack at the end of the program.</returns>
        Task<object> CallInto(FrameContext context, PyFunction functionToRun, object[] args, Dictionary<string, object> newGlobals=null);

        /// <summary>
        /// Alternate form of CallInto that assumes that the frame to use has been prepared already. This is
        /// particularly useful when creating modules because you have to tie locals to globals.
        /// </summary>
        /// <param name="context">Context of code currently being run through the interpreter by the scheduler.</param>
        /// <param name="frame">The frame to use for running code</param>
        /// <param name="args">The arguments for the program. These are put on the existing data stack.</param>
        /// <returns>Whatever was provided by the RETURN_VALUE on top-of-stack at the end of the program.</returns>
        Task<object> CallInto(FrameContext context, Frame frame, object[] args);

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
        /// Get mapping of built-in definitions that interpreter is using.
        /// </summary>
        /// <returns>Mapping of built-in definitions that interpreter is using.</returns>
        Dictionary<string, object> GetBuiltins();


        /// <summary>
        /// Returns true if an exception was raised and the context would not be in a position to still try to
        /// handle it. This is used when stepping through frame context in debugging to allow the interpreter to
        /// keep trying to process the exception. If you just test the frame context for an exception while stepping,
        /// you'll miss out on the interpreter trying out the except (and finally) clauses that have some stuff left
        /// to do. It also misses out on all the unrolling to properly escape.
        /// </summary>
        bool ExceptionEscaped(FrameContext context);

        /// <summary>
        /// The interpreter retains a handle to the scheduler in order to steer it between tasks or set up a blocking
        /// activity.
        /// </summary>
        IScheduler Scheduler { get; }
    }

    /// <summary>
    /// Data type to denote a **kwargs mapping.
    /// </summary>
    public class KwargsDict : Dictionary<object, object>
    {

    }

    public interface IPyCallable
    {
        Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args,
            Dictionary<string, object> defaultOverrides=null,
            KwargsDict kwargsDict=null);
    }
}
