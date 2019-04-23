using System.Collections.Generic;

using LanguageImplementation;

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

        public Scheduler(Interpreter interpreter)
        {
            this.interpreter = interpreter;
            this.tasklets = new List<FrameContext>();
            this.contexts = new List<IEnumerable<SchedulingInfo>>();
            currentTaskIndex = -1;
            TickCount = 0;
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

            var taskEnumerator = contexts[currentTaskIndex].GetEnumerator();
            if (!taskEnumerator.MoveNext())
            {
                // Done. Rewind the taskindex since it'll move up on the next tick.
                contexts.RemoveAt(currentTaskIndex);
                tasklets.RemoveAt(currentTaskIndex);
                --currentTaskIndex;
            }
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

    }
}
