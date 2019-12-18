using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
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

    public class ScheduledTaskRecord
    {
        public FrameContext Frame;
        public ISubscheduledContinuation Continuation;
        public ScheduledTaskRecord(FrameContext frame, ISubscheduledContinuation continuation)
        {
            Frame = frame;
            Continuation = continuation;
        }
    }

    /// <summary>
    /// Manages all tasklets and how they're alternated through the interpreter.
    /// </summary>
    public class Scheduler : IScheduler
    {
        private IInterpreter interpreter;
        public int TickCount;

        private List<ScheduledTaskRecord> active;
        private List<ScheduledTaskRecord> blocked;
        private List<ScheduledTaskRecord> unblocked;
        private List<ScheduledTaskRecord> yielded;

        public Scheduler()
        {
            active = new List<ScheduledTaskRecord>();
            blocked = new List<ScheduledTaskRecord>();
            unblocked = new List<ScheduledTaskRecord>();
            yielded = new List<ScheduledTaskRecord>();

            TickCount = 0;
        }

        public void SetInterpreter(IInterpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        /// <summary>
        /// Schedule a new program to run. It returns the FrameContext that would be used to run the application
        /// in order to do any other housekeeping like inject variables into it.
        /// </summary>
        /// <param name="program">The code to schedule.</param>
        /// <returns>The context the interpreter will use to maintain the program's state while it runs.</returns>
        public FrameContext Schedule(CodeObject program)
        {
            var scheduleState = PrepareFrameContext(program);
            unblocked.Add(scheduleState);
            return scheduleState.Frame;
        }

        /// <summary>
        /// Prepare a fresh frame context and continuation for the code object. This will set it up to be run from
        /// scratch.
        /// </summary>
        /// <param name="newProgram">The code to prepare to run.</param>
        /// <returns>Scheduling state containing the the context that the interpreter can use to run the program as
        /// well as the continuation to kick it off (and resume it later).</returns>
        private ScheduledTaskRecord PrepareFrameContext(CodeObject newProgram)
        {
            var newFrameStack = new Stack<Frame>();
            var rootFrame = new Frame(newProgram);

            foreach (string name in newProgram.VarNames)
            {
                rootFrame.AddLocal(name, null);
            }

            newFrameStack.Push(rootFrame);
            var frame = new FrameContext(newFrameStack);
            var initialContinuation = new InitialScheduledContinuation(this.interpreter, frame);
            return new ScheduledTaskRecord(frame, initialContinuation);
        }

        // This is called when the currently-active script is blocking. Call this right before invoking
        // an awaiter from the task in which the script is running.
        public void NotifyBlocked(ISubscheduledContinuation continuation)
        {
            lastScheduled.Continuation = continuation;
            blocked.Add(lastScheduled);
        }

        // Call this for a continuation that has been previously blocked with NotifyBlocked. This won't
        // immediately resume the script, but will set it up to be run in interpreter's tick interval.
        public void NotifyUnblocked(ISubscheduledContinuation continuation)
        {
            // This is a WIP to try to unjam the REPL demo but needs to be explored deeper if it ends up working.
            // The GUI was running sleeps that might have been coming in technically from another thread, so we
            // couldn't assume that we were getting responses from lastScheduled. Generally, the method we were
            // using might just not be robust enough and we'd need to do lookups anyways.
            if(lastScheduled == null)
            {
                ScheduledTaskRecord record = null;
                foreach(var candidate in blocked)
                {
                    if(candidate.Continuation == continuation)
                    {
                        record = candidate;
                        break;
                    }
                }
                if(record != null)
                {
                    if (blocked.Remove(record))
                    {
                        unblocked.Add(record);
                    }
                }
                else
                {
                    throw new Exception("Did not find continuation in blocked queue");
                }
            }
            else
            {
                lastScheduled.Continuation = continuation;
                if (blocked.Remove(lastScheduled))
                {
                    unblocked.Add(lastScheduled);
                }
            }
        }

        // Use to cooperative stop running for just a single tick.
        public void SetYielded(ISubscheduledContinuation continuation)
        {
            lastScheduled.Continuation = continuation;
            yielded.Add(lastScheduled);
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
                lastScheduled = continued;
                active.Add(lastScheduled);
                await continued.Continuation.Continue();

                if (lastScheduled.Frame.EscapedDotNetException != null)
                {
                    // We want to rethrow while retaining the original stack trace.
                    // https://stackoverflow.com/questions/57383/how-to-rethrow-innerexception-without-losing-stack-trace-in-c
                    ExceptionDispatchInfo.Capture(lastScheduled.Frame.EscapedDotNetException).Throw();
                }
                else if (interpreter.ExceptionEscaped(lastScheduled.Frame))
                {
                    throw new EscapedPyException(lastScheduled.Frame.CurrentException);
                }
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
                if (!(interpreter.ExceptionEscaped(lastScheduled.Frame) ||
                      lastScheduled.Frame.EscapedDotNetException != null ||
                      (lastScheduled.Frame.BlockStack.Count == 0 && lastScheduled.Frame.Cursor >= lastScheduled.Frame.CodeBytes.Bytes.Length)
                     )
                   )
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
