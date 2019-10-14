using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
    public class InitialScheduledContinuation : ISubscheduledContinuation
    {
        private Interpreter interpreter;
        public FrameContext TaskletFrame
        {
            get; private set;
        }

        public InitialScheduledContinuation(Interpreter interpreter, FrameContext taskletFrame)
        {
            AssignInterpreter(interpreter);
            TaskletFrame = taskletFrame;
        }

        public void AssignInterpreter(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        public async Task Continue()
        {
            await interpreter.Run(TaskletFrame);
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
    public class Scheduler
    {
        private Interpreter interpreter;
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

        public void SetInterpreter(Interpreter interpreter)
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
            var initialContinuation = new InitialScheduledContinuation(interpreter, frame);
            return new ScheduledTaskRecord(frame, initialContinuation);
        }

        /// <summary>
        /// Particularly used in debugging to make sure the scheduler is pointing to a valid tasklet.
        /// This makes the first dump from it point to something.
        /// </summary>
        public void Home()
        {
            throw new NotImplementedException("The old method of having an active tasklet has been thrown into turmoil by the async-await reimplementation. Home() not yet patched up.");
        }

        // This is called when the currently-active script is blocking. Call this right before invoking
        // an awaiter from the task in which the script is running.
        public void NotifyBlocked(ISubscheduledContinuation continuation)
        {
            currentlyScheduled.Continuation = continuation;
            blocked.Add(currentlyScheduled);
        }

        // Call this for a continuation that has been previously blocked with NotifyBlocked. This won't
        // immediately resume the script, but will set it up to be run in interpreter's tick interval.
        public void NotifyUnblocked(ISubscheduledContinuation continuation)
        {
            currentlyScheduled.Continuation = continuation;
            if (blocked.Remove(currentlyScheduled))
            {
                unblocked.Add(currentlyScheduled);
            }
        }

        // Use to cooperative stop running for just a single tick.
        public void SetYielded(ISubscheduledContinuation continuation)
        {
            currentlyScheduled.Continuation = continuation;
            yielded.Add(currentlyScheduled);
        }

        /// <summary>
        /// Run until next yield, program termination, or completion of scheduled tasklets.
        /// </summary>
        public async Task Tick()
        {
            var oldActiveFrames = active;
            active = new List<ScheduledTaskRecord>();
            foreach (var scheduled in oldActiveFrames)
            {
                currentlyScheduled = scheduled;
                if(interpreter.ExceptionEscaped(currentlyScheduled.Frame))
                {
                    throw new EscapedPyException(currentlyScheduled.Frame.CurrentException);
                }

                if (!(interpreter.ExceptionEscaped(currentlyScheduled.Frame) || (currentlyScheduled.Frame.BlockStack.Count == 0 && currentlyScheduled.Frame.Cursor >= currentlyScheduled.Frame.CodeBytes.Bytes.Length)))
                {
                    active.Add(currentlyScheduled);
                }
            }
            currentlyScheduled = null;

            //////////////////////// BEGIN: Taken from async-await demo scheduler
            // Queue flip because unblocked tasks might unblock further tasks.
            // TODO: Clear and flip pre-allocated lists instead of constructing a new one each time.
            // TODO: Revisit more than one time per tick.
            var oldUnblocked = unblocked;
            unblocked = new List<ScheduledTaskRecord>();

            oldUnblocked.AddRange(yielded);
            yielded.Clear();

            foreach (var continued in oldUnblocked)
            {
                currentlyScheduled = continued;
                active.Add(currentlyScheduled);
                await continued.Continuation.Continue();
            }
            currentlyScheduled = null;

            oldUnblocked.Clear();
            //////////////////////// END: Taken from async-await demo scheduler

            ++TickCount;
        }

        public async Task RunUntilDone()
        {
            while(!Done)
            {
                try
                {
                    Tick().Wait();
                }
                catch(AggregateException wrappedEscapedException)
                {
                    // Given the nature of exception handling, we should normally only have one of these!
                    throw wrappedEscapedException.InnerExceptions[0];
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

        private ScheduledTaskRecord currentlyScheduled;
        public FrameContext ActiveTasklet
        {
            get
            {
                return currentlyScheduled.Frame;
            }
        }

    }
}
