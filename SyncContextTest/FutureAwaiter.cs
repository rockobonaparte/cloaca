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


/// <summary>
/// Helper class to give the FutureAwaiter a void type. We can't do that by default since we can't
/// set void as a result type. We don't want to rely on null since we want a null result in the FutureAwaiter
/// to indicate actual null--or NoneType per the interpreter. Hence, we declare a custom object type. It is
/// implied that the interpreter will keep an eye out for this sentinel from FutureAwaiters and not push
/// anything on the stack if it sees it.
/// </summary>
public class VoidSentinel
{
    private VoidSentinel()
    {

    }

    private static VoidSentinel __instance;

    public static VoidSentinel Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = new VoidSentinel();
            }
            return __instance;
        }
    }
}

// A void version because we can't use void as a parameter type.
public class FutureVoidAwaiter : FutureAwaiter<VoidSentinel>
{
    public FutureVoidAwaiter(MockInterpreter interpreterToSubschedule) : base(interpreterToSubschedule)
    {
    }

    /// <summary>
    /// Sets the result to the void sentinel. This is just a convenience helper so consumers don't need to
    /// actually know about the void sentinel.
    /// </summary>
    public void SignalDone()
    {
        SetResult(VoidSentinel.Instance);
    }
}
