using System.Collections.Generic;

using LanguageImplementation;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
    /// <summary>
    /// Manages all tasklets and how they're alternated through the interpreter.
    /// </summary>
    public class Scheduler
    {
        private Interpreter interpreter;
        private List<FrameContext> tasklets;
        private List<IEnumerable<SchedulingInfo>> contexts;
        private int currentTaskIndex;
        public int TickCount;

        private List<ISubscheduledContinuation> blocked;
        private List<ISubscheduledContinuation> unblocked;
        private List<ISubscheduledContinuation> yielded;

        public Scheduler()
        {
            contexts = new List<IEnumerable<SchedulingInfo>>();
            tasklets = new List<FrameContext>();
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
            var tasklet = interpreter.PrepareFrameContext(program);
            tasklets.Add(tasklet);
            contexts.Add(null);
            return tasklet;
        }

        /// <summary>
        /// Particularly used in debugging to make sure the scheduler is pointing to a valid tasklet.
        /// This makes the first dump from it point to something.
        /// </summary>
        public void Home()
        {
            if (tasklets.Count == 0)
            {
                currentTaskIndex = -1;
                return;
            }

            ++currentTaskIndex;
            if (currentTaskIndex >= tasklets.Count)
            {
                currentTaskIndex = 0;
            }
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
        public void Tick()
        {
            if (tasklets.Count == 0)
            {
                currentTaskIndex = -1;
                return;
            }

            ++currentTaskIndex;
            if (currentTaskIndex >= tasklets.Count)
            {
                currentTaskIndex = 0;
            }

            if (contexts[currentTaskIndex] == null)
            {
                contexts[currentTaskIndex] = interpreter.Run(tasklets[currentTaskIndex]);
            }

            var activeTask = tasklets[currentTaskIndex];
            var taskEnumerator = contexts[currentTaskIndex].GetEnumerator();

            // This will run the current context to its next yield.
            var taskIsFinished = !taskEnumerator.MoveNext();

            if (interpreter.ExceptionEscaped(activeTask))
            {
                throw new EscapedPyException(activeTask.CurrentException);
            }

            if (taskIsFinished)
            {
                // Done. Rewind the taskindex since it'll move up on the next tick.
                contexts.RemoveAt(currentTaskIndex);
                tasklets.RemoveAt(currentTaskIndex);
                --currentTaskIndex;
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
                continuation.Continue();
            }

            oldUnblocked.Clear();
            //////////////////////// END: Taken from async-await demo scheduler

            ++TickCount;
        }

        public void RunUntilDone()
        {
            while(!Done)
            {
                Tick();
            }
        }

        public bool Done
        {
            get
            {
                return tasklets.Count == 0;
            }
        }

        public FrameContext ActiveTasklet
        {
            get
            {
                if(currentTaskIndex >= 0)
                {
                    return tasklets[currentTaskIndex];
                }
                else
                {
                    return null;
                }
            }
        }

    }
}
