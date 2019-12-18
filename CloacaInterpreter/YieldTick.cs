using System;
using System.Threading.Tasks;

using LanguageImplementation;

namespace CloacaInterpreter
{
    public class YieldTick : System.Runtime.CompilerServices.INotifyCompletion, ISubscheduledContinuation
    {
        private Action continuation;
        IScheduler scheduler;

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

        public YieldTick(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Continue()
        {
            continuation?.Invoke();
            return Task.FromResult(true);
        }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
            scheduler.SetYielded(this);
        }

        public YieldTick GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
            // Empty -- just needed to satisfy the rules for how custom awaiters work.
        }

        public void AssignScheduler(IScheduler scheduler)
        {
            scheduler = scheduler;
        }
    }
}

