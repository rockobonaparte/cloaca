﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
    public delegate void NewScheduleTaskDelegate(ScheduledTaskRecord taskRecord);

    public class InitialScheduledContinuation : ISubscheduledContinuation
    {
        private IInterpreter interpreter;
        public FrameContext TaskletFrame
        {
            get; private set;
        }

        public InitialScheduledContinuation(IInterpreter interpreter, FrameContext taskletFrame)
        {
            this.interpreter = interpreter;
            TaskletFrame = taskletFrame;
        }

        public void AssignScheduler(IScheduler scheduler)
        {
            // no-op in this case; our interpreter already has the scheduler.
        }

        public async Task Continue()
        {
            // All I know is I should *not* await this. It jams up YieldTick, for example.
            // TODO: Understand why this shouldn't be awaited, we shouldn't return the result of Run(), and why we still need an async Task signature.
            interpreter.Run(TaskletFrame);
        }
    }

    /// <summary>
    /// Manages all tasklets and how they're alternated through the interpreter.
    /// </summary>
    public class Scheduler : IScheduler
    {
        public IInterpreter Interpreter
        {
            get; private set;
        }
        public int TickCount;

        /// <summary>
        /// Fired each time a task is scheduled. This is useful for attaching on to the task
        /// record for exceptions so they can be logged.
        /// </summary>
        public event NewScheduleTaskDelegate OnTaskScheduled = (x) => { };

        private List<ScheduledTaskRecord> active;
        private List<ScheduledTaskRecord> blocked;
        private List<ScheduledTaskRecord> unblocked;
        private List<ScheduledTaskRecord> yielded;
        private ScheduledTaskRecord currentTask;

        public Scheduler()
        {
            active = new List<ScheduledTaskRecord>();
            blocked = new List<ScheduledTaskRecord>();
            unblocked = new List<ScheduledTaskRecord>();
            yielded = new List<ScheduledTaskRecord>();
            currentTask = null;

            TickCount = 0;
        }

        /// <summary>
        /// Returns a copy of a list of all active tasks currently in the scheduler.
        /// </summary>
        /// <returns>All active tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are active.
        /// </returns>
        public ScheduledTaskRecord[] GetTasksActive()
        {
            return active.ToArray();
        }

        /// <summary>
        /// Returns a copy of a list of all tasks that are currently marked as blocked in the scheduler. Blocked tasks are waiting on an asynchronous result.
        /// </summary>
        /// <returns>All blocked tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are blocked
        /// </returns>
        public ScheduledTaskRecord[] GetTasksBlocked()
        {
            return blocked.ToArray();
        }

        /// <summary>
        /// Returns a copy of a list of all tasks that are currently marked as unblocked in the scheduler. These are tasks that have finished yielding or otherwise
        /// got a value asynchronously that will let their scripts continue. Normally investigating this will not find much since these are moved into the
        /// active queue after each scheduler tick. It is mostly useful for particular scheduler diagnostics.
        /// </summary>
        /// <returns>All unblocked tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are blocked
        /// </returns>
        public ScheduledTaskRecord[] GetTasksUnblocked()
        {
            return unblocked.ToArray();
        }

        /// <summary>
        /// Returns a copy of a list of all tasks that are currently marked as yielded in the scheduler. These are tasks that have taken a break from running. This
        /// can happen either from an explicit yield instruction, or they have exhausted their prescribed run time--if that is even a thing with this scheduler.
        /// </summary>
        /// <returns>All yielded tasks in this scheduler. This list is a copy so manipulating the list doesn't directly manipulate which tasks are blocked
        public ScheduledTaskRecord[] GetTasksYielded()
        {
            return yielded.ToArray();
        }

        /// <summary>
        /// Gets the task currently running. This could be null if no task is running.
        /// </summary>
        /// <returns>The task that's currently running. This could be null if no task is running.</returns>
        public ScheduledTaskRecord GetCurrentTask()
        {
            return currentTask;
        }

        private int findTaskRecordIndex(FrameContext frame, List<ScheduledTaskRecord> records)
        {
            for(int i = 0; i < records.Count; ++i)
            {
                if(records[i].Frame == frame)
                {
                    return i;
                }
            }
            throw new KeyNotFoundException("Could not find continuation record");
        }

        private void transferRecord(FrameContext frame, List<ScheduledTaskRecord> fromRecords, List<ScheduledTaskRecord> toRecords, ISubscheduledContinuation continuation)
        {
            var recordIdx = findTaskRecordIndex(frame, fromRecords);
            ScheduledTaskRecord record = fromRecords[recordIdx];
            record.Continuation = continuation;
            fromRecords.RemoveAt(recordIdx);
            toRecords.Add(record);
        }

        public void SetInterpreter(IInterpreter interpreter)
        {
            this.Interpreter = interpreter;
        }

        /// <summary>
        /// Schedule a new program to run. It returns the FrameContext that would be used to run the application
        /// in order to do any other housekeeping like inject variables into it.
        /// </summary>
        /// <param name="program">The code to schedule.</param>
        /// <returns>The context the interpreter will use to maintain the program's state while it runs.</returns>
        public TaskEventRecord Schedule(PyFunction function)
        {
            var scheduleState = PrepareFrameContext(function, Interpreter.GetBuiltins());
            unblocked.Add(scheduleState);
            OnTaskScheduled(scheduleState);
            return scheduleState.SubmitterReceipt;
        }

        /// <summary>
        /// Schedule a new program to run. It returns the FrameContext that would be used to run the application
        /// in order to do any other housekeeping like inject variables into it.
        /// </summary>
        /// <param name="program">The code to schedule.</param>
        /// <param name="globals">Additional root-level globals to introduce to the program from the outside.</param>
        /// <returns>The context the interpreter will use to maintain the program's state while it runs.</returns>
        public TaskEventRecord Schedule(PyFunction function, Dictionary<string, object> globals)
        {
            var scheduleState = PrepareFrameContext(function, Interpreter.GetBuiltins());

            // Some of the stuff in this block is too gross:
            // 1. Checking that we aren't setting variables when globals == locals (root context?)
            // 2. Use SetVariableIfExists
            // It's too hacky and I should find a better means of getting around the problems that required
            // the logic in the first place.
            if (globals != scheduleState.Frame.Locals)
            {
                foreach (string varName in globals.Keys)
                {
                    scheduleState.Frame.SetVariableIfExists(varName, globals[varName]);
                }
            }

            unblocked.Add(scheduleState);
            OnTaskScheduled(scheduleState);
            return scheduleState.SubmitterReceipt;
        }

        public TaskEventRecord Schedule(PyFunction function, FrameContext context)
        {
            var scheduleState = PrepareFrameContext(function, context);
            unblocked.Add(scheduleState);
            OnTaskScheduled(scheduleState);
            return scheduleState.SubmitterReceipt;
        }

        /// <summary>
        /// Schedule a new program to run that requires certain arguments. This is exposed for other scripts to pass
        /// in function calls that need to be spun up as independent coroutines.
        /// </summary>
        /// <param name="function">The code to schedule. The is treated like a function call with zero or more arguments.</param>
        /// <param name="args">The arguments that need to be seeded to the the code to run.</param>
        /// <returns></returns>
        public TaskEventRecord Schedule(PyFunction function, FrameContext context, params object[] args)
        {
            if(args.Length != function.Code.ArgVarNames.Count)
            {
                throw new Exception("The given code object requires " + function.Code.ArgCount + " arguments but " + args.Length + " arguments were given");
            }

            var scheduleState = PrepareFrameContext(function, context);

            for (int argIdx = 0; argIdx < args.Length; ++argIdx)
            {
                scheduleState.Frame.LocalFasts[argIdx] = args[argIdx];
            }

            unblocked.Add(scheduleState);
            OnTaskScheduled(scheduleState);
            return scheduleState.SubmitterReceipt;
        }

        /// <summary>
        /// Prepare a fresh frame context and continuation for the code object. This will set it up to be run from
        /// scratch.
        /// </summary>
        /// <param name="newFunction">The code to prepare to run.</param>
        /// <returns>Scheduling state containing the the context that the interpreter can use to run the program as
        /// well as the continuation to kick it off (and resume it later).</returns>
        /// <param name="builtins">Mapping of builtins the frame can reference for resolving builtin calls</param>
        private ScheduledTaskRecord PrepareFrameContext(PyFunction newFunction, Dictionary<string, object> builtins)
        {
            // We get an Exception here the first time we run anything in a net Visual Studio instance here, so keep
            // a breakpoint on this, stop what you're doing, and figure out what its problem is.
            return PrepareFrameContext(newFunction, null, builtins);
        }

        /// <summary>
        /// Creates a new frame context as a child of the super context. If the super context is null, then this
        /// context is considered a root context. If it is defined, this context is considered a subcontext.
        /// Subcontexts come up when defining functions in functions and then scheduling them out.
        /// 
        /// There's some miscellaneous management of builtins based on what is passed:
        /// 1. superContext != null: new frame's built-ins assigned to superContext's builtins.
        /// 2. superContext == null; seperateBuiltins != null: new frame's built-ins assigned to separateBuiltins.
        /// 3. superContext == null; separateBuiltins == null: new frame's built-ins assigned to an empty dictionary.
        /// </summary>
        /// <param name="newFunction">Function to prepare a frame for running</param>
        /// <param name="superContext">The parent context under which this context would run. Will extract builtins from here and not
        /// separateBuiltins if this is not null.</param>
        /// <param name="separateBuiltins">Built-ins to use by this frame context if a superContext is not defined</param>
        /// <returns></returns>
        private ScheduledTaskRecord PrepareFrameContext(PyFunction newFunction, FrameContext superContext, Dictionary<string, object> separateBuiltins=null)
        {
            var newFrameStack = new Stack<Frame>();
            Frame rootFrame = new Frame(newFunction, superContext);

            // At the root level in CPython, locals() == globals(). They are the same object.
            // They have the same id and everything!
            //
            // Set module-level globals here if they haven't been established by the module.
            rootFrame.AddOnlyNewGlobal("__name__", PyString.Create("__main__"));
            rootFrame.Locals = rootFrame.Globals;

            newFrameStack.Push(rootFrame);

            FrameContext subContext;
            if (superContext != null) 
            {
                subContext = superContext.CreateSubcontext(newFrameStack, superContext.Builtins);
            } else if(separateBuiltins != null)
            {
                subContext = new FrameContext(newFrameStack, separateBuiltins);
            }
            else
            {
                subContext = new FrameContext(newFrameStack, new Dictionary<string, object>());
            }
                

            var initialContinuation = new InitialScheduledContinuation(this.Interpreter, subContext);
            return new ScheduledTaskRecord(subContext, initialContinuation, new TaskEventRecord(subContext));
        }

        // This is called when the currently-active script is blocking. Call this right before invoking
        // an awaiter from the task in which the script is running.
        public void NotifyBlocked(FrameContext frame, ISubscheduledContinuation continuation)
        {
            transferRecord(frame, active, blocked, continuation);
        }

        // Call this for a continuation that has been previously blocked with NotifyBlocked. This won't
        // immediately resume the script, but will set it up to be run in interpreter's tick interval.
        public void NotifyUnblocked(FrameContext frame, ISubscheduledContinuation continuation)
        {
            transferRecord(frame, blocked, unblocked, continuation);
        }

        // Use to cooperatively stop running for just a single tick.
        public void SetYielded(FrameContext frame, ISubscheduledContinuation continuation)
        {
            transferRecord(frame, active, yielded, continuation);
        }

        /// <summary>
        /// Run until next yield, program termination, or completion of scheduled tasklets.
        /// </summary>
        public async Task Tick()
        {
            // Queue flip because unblocked tasks might unblock further tasks.
            // TODO: Clear and flip pre-allocated lists instead of constructing a new one each time.
            var oldUnblocked = unblocked;
            unblocked = new List<ScheduledTaskRecord>();

            oldUnblocked.AddRange(yielded);
            yielded.Clear();

            foreach (var continued in oldUnblocked)
            {
                currentTask = null;
                currentTask = continued;
                active.Add(currentTask);
                var theContinuation = currentTask.Continuation;
                await theContinuation.Continue();
                currentTask = null;
            }
            lastScheduled = null;

            oldUnblocked.Clear();

            var oldActiveFrames = active;
            active = new List<ScheduledTaskRecord>();
            foreach (var scheduled in oldActiveFrames)
            {
                lastScheduled = scheduled;

                // Currently, we won't bump into these exception escape clauses since we'll bomb out from the checks run after every
                // active task above, but we're keeping them here for later when we try to make the scheduler more resiliant against
                // rogue scripts and keep running.
                //
                // We need to check the call stack because we have started scheduling functions. Those return from themselves and
                // fully nuke their call stacks unlike root programs.
                if(lastScheduled.Frame.EscapedDotNetException != null)
                {
                    // We want to rethrow while retaining the original stack trace.
                    // https://stackoverflow.com/questions/57383/how-to-rethrow-innerexception-without-losing-stack-trace-in-c
                    scheduled.SubmitterReceipt.NotifyEscapedException(ExceptionDispatchInfo.Capture(lastScheduled.Frame.EscapedDotNetException));

                }
                else if (Interpreter.ExceptionEscaped(lastScheduled.Frame))
                {
                    scheduled.SubmitterReceipt.NotifyEscapedException(ExceptionDispatchInfo.Capture(new EscapedPyException(lastScheduled.Frame.CurrentException)));
                }
                else if(lastScheduled.Frame.callStack.Count == 0)
                {
                    // Executed a return statement and that nuked the whole stack. This code is done!
                    scheduled.SubmitterReceipt.NotifyCompleted();
                }
                else if (lastScheduled.Frame.BlockStack.Count == 0 &&
                    lastScheduled.Frame.Cursor >= lastScheduled.Frame.CodeBytes.Bytes.Length)
                {
                    scheduled.SubmitterReceipt.NotifyCompleted();
                }
                else
                {
                    active.Add(lastScheduled);
                }
            }

            ++TickCount;
        }

        public async Task RunUntilDone()
        {
            while (!Done)
            {
                try
                {
                    Tick().Wait();
                }
                catch (AggregateException wrappedEscapedException)
                {
                    // Given the nature of exception handling, we should normally only have one of these!
                    ExceptionDispatchInfo.Capture(wrappedEscapedException.InnerExceptions[0]).Throw();
                }
            }
        }

        public bool Done
        {
            get
            {
                return active.Count == 0 && yielded.Count == 0 && blocked.Count == 0 && unblocked.Count == 0;
            }
        }

        /// <summary>
        /// All tasks that still exist are blocked:
        /// 1. None are yielding tasks.
        /// 2. There's at least one blocked task.
        /// 3. There are no unblocked tasks.
        /// 
        /// Use in conjunection with Done to determine if you should stop ticking.
        /// </summary>
        public bool AllBlocked
        {
            get
            {
                return yielded.Count == 0 && blocked.Count > 0 && unblocked.Count == 0;
            }
        }

        // This used to be used more in actual scheduling decision, but now it's just maintained for debugging.
        // LastTasklet is grabbed when stepping through the interpreter interactively using the the project's
        // debug tools.
        private ScheduledTaskRecord lastScheduled;
        public FrameContext LastTasklet
        {
            get
            {
                return lastScheduled.Frame;
            }
        }

    }
}
