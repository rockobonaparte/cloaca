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

    /// <summary>
    /// Manages all tasklets and how they're alternated through the interpreter.
    /// </summary>
    public class Scheduler
    {
        private Interpreter interpreter;
        private int currentTaskIndex;
        public int TickCount;

        private List<FrameContext> activeFrames;
        private List<ISubscheduledContinuation> blocked;
        private List<ISubscheduledContinuation> unblocked;
        private List<ISubscheduledContinuation> yielded;

        public Scheduler()
        {
            activeFrames = new List<FrameContext>();
            blocked = new List<ISubscheduledContinuation>();
            unblocked = new List<ISubscheduledContinuation>();
            yielded = new List<ISubscheduledContinuation>();

            currentTaskIndex = -1;
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
            var newFrame = interpreter.PrepareFrameContext(program);
            activeFrames.Add(newFrame);
            var initialContinuation = new InitialScheduledContinuation(interpreter, newFrame);
            unblocked.Add(initialContinuation);
            return initialContinuation.TaskletFrame;
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
            blocked.Add(continuation);
        }

        // Call this for a continuation that has been previously blocked with NotifyBlocked. This won't
        // immediately resume the script, but will set it up to be run in interpreter's tick interval.
        public void NotifyUnblocked(ISubscheduledContinuation continuation)
        {
            if (blocked.Remove(continuation))
            {
                unblocked.Add(continuation);
            }
        }

        // Use to cooperative stop running for just a single tick.
        public void SetYielded(ISubscheduledContinuation continuation)
        {
            yielded.Add(continuation);
        }

        /// <summary>
        /// Run until next yield, program termination, or completion of scheduled tasklets.
        /// </summary>
        public async Task Tick()
        {
            var oldActiveFrames = activeFrames;
            activeFrames = new List<FrameContext>();
            foreach (var frame in oldActiveFrames)
            {
                if(interpreter.ExceptionEscaped(frame))
                {
                    throw new EscapedPyException(frame.CurrentException);
                }

                if (!(interpreter.ExceptionEscaped(frame) || (frame.BlockStack.Count == 0 && frame.Cursor >= frame.CodeBytes.Bytes.Length)))
                {
                    activeFrames.Add(frame);
                }
            }

            //////////////////////// BEGIN: Taken from async-await demo scheduler
            // Queue flip because unblocked tasks might unblock further tasks.
            // TODO: Clear and flip pre-allocated lists instead of constructing a new one each time.
            // TODO: Revisit more than one time per tick.
            var oldUnblocked = unblocked;
            unblocked = new List<ISubscheduledContinuation>();

            oldUnblocked.AddRange(yielded);
            yielded.Clear();

            foreach (var continuation in oldUnblocked)
            {
                await continuation.Continue();
            }

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
                return activeFrames.Count == 0;
            }
        }

        public FrameContext ActiveTasklet
        {
            get
            {
                throw new NotImplementedException("The old method of having an active tasklet has been thrown into turmoil by the async-await reimplementation. ActiveTasklet accessor not yet patched up.");
                return null;
            }
        }

    }
}
