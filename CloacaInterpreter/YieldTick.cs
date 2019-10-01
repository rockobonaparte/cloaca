using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloacaInterpreter
{
    public class YieldTick : System.Runtime.CompilerServices.INotifyCompletion, ISubscheduledContinuation
    {
        private Action continuation;
        Interpreter interpreter;

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

        public YieldTick(Interpreter interpreterToSubschedule)
        {
            interpreter = interpreterToSubschedule;
        }

        public void Continue()
        {
            continuation?.Invoke();
        }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
            interpreter.Scheduler.SetYielded(this);
        }

        public YieldTick GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
            // Empty -- just needed to satisfy the rules for how custom awaiters work.
        }

        public void AssignInterpreter(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }
    }
}

