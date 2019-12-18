using System;
using System.Threading.Tasks;

using LanguageImplementation;

namespace CloacaInterpreter
{
    public class YieldTick : System.Runtime.CompilerServices.INotifyCompletion, ISubscheduledContinuation
    {
        private Action continuation;
        IScheduler scheduler;
        FrameContext context;

        public bool IsCompleted
        {
            get
            {
                return false;
            }
        }

        public string Text
        {
            get; protected set;
        }

        public YieldTick(IScheduler scheduler, FrameContext context)
        {
            this.scheduler = scheduler;
            this.context = context;
        }

        public Task Continue()
        {
            continuation?.Invoke();
            return Task.FromResult(true);
        }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
            scheduler.SetYielded(context);
        }

        public YieldTick GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
            // Empty -- just needed to satisfy the rules for how custom awaiters work.
        }

        public void AssignScheduler(Scheduler scheduler)
        {
            scheduler = scheduler;
        }
    }
}

