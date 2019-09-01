using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// Custom awaiter implemented like a future connected to the scheduler.
/// 
public class FutureAwaiter<T> : System.Runtime.CompilerServices.INotifyCompletion, ISubscheduledContinuation
{
    private T result;
    private bool finished;
    private Action continuation;
    MockInterpreter interpreter;

    public bool IsCompleted
    {
        get
        {
            return finished;
        }
    }

    public string Text
    {
        get; protected set;
    }

    public FutureAwaiter(MockInterpreter interpreterToSubschedule)
    {
        finished = false;
        interpreter = interpreterToSubschedule;
    }

    public void SetResult(T result)
    {
        this.result = result;
        finished = true;
        interpreter.NotifyUnblocked(this);
    }

    public void Continue()
    {
        Console.WriteLine("Invoking continuation");
        continuation?.Invoke();
    }

    public void OnCompleted(Action continuation)
    {
        if (finished)
        {
            this.continuation();
        }
        else
        {
            Console.WriteLine("register continuation to call when request is filled");
            this.continuation = continuation;
        }
    }

    public FutureAwaiter<T> GetAwaiter()
    {
        return this;
    }

    public T GetResult()
    {
        return result;
    }
}


// A void version. We can't use void as a parameter type.
public class FutureVoidAwaiter : INotifyCompletion, ISubscheduledContinuation
{
    private bool finished;
    private Action continuation;
    MockInterpreter interpreter;

    public bool IsCompleted
    {
        get
        {
            return finished;
        }
    }

    public FutureVoidAwaiter(MockInterpreter interpreterToSubschedule)
    {
        finished = false;
        interpreter = interpreterToSubschedule;
    }

    public void SignalDone()
    {
        finished = true;
        interpreter.NotifyUnblocked(this);
    }

    public void Continue()
    {
        continuation?.Invoke();
    }

    public void OnCompleted(Action continuation)
    {
        if (finished)
        {
            this.continuation();
        }
        else
        {
            this.continuation = continuation;
        }
    }

    public FutureVoidAwaiter GetAwaiter()
    {
        return this;
    }

    public void GetResult()
    {

    }
}
