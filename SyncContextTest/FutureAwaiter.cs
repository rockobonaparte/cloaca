﻿using System;
using System.Runtime.Serialization;

/// Custom awaiter implemented like a future connected to the scheduler.
/// 
[Serializable]
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

    public FutureAwaiter<T> GetAwaiter()
    {
        return this;
    }

    public T GetResult()
    {
        return result;
    }

    public void AssignInterpreter(MockInterpreter interpreter)
    {
        this.interpreter = interpreter;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("continuation", Checkpoint.SerializeContinuation(continuation));
        info.AddValue("result", result);
        info.AddValue("finished", finished);
    }

    protected FutureAwaiter(SerializationInfo info, StreamingContext context)
    {
        var methodState = (Checkpoint.AsyncMethodState)info.GetValue("continuation", typeof(Checkpoint.AsyncMethodState));
        continuation = Checkpoint.DeserializeContinuation(methodState);
        result = (T) info.GetValue("result", typeof(T));
        finished = info.GetBoolean("finished");
    }
}


/// <summary>
/// Helper class to give the FutureAwaiter a void type. We can't do that by default since we can't
/// set void as a result type. We don't want to rely on null since we want a null result in the FutureAwaiter
/// to indicate actual null--or NoneType per the interpreter. Hence, we declare a custom object type. It is
/// implied that the interpreter will keep an eye out for this sentinel from FutureAwaiters and not push
/// anything on the stack if it sees it.
/// </summary>
[Serializable]
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
[Serializable]
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