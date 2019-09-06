using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

/// <summary>
/// These represent frames managed by the interpreter. Frames are the complete context of an
/// executing script. This would include the complete stack trace and variable conditions. 
/// These frames are stopped and started at will.
/// 
/// We aren't actually implementing that feature here for this an are just using direct C#
/// calls, but the naming is place to convey the intent.
/// </summary>
[Serializable]
public class InterpreterFrame
{
    Callable script;
    MockInterpreter interpreter;
    public InterpreterFrame(Callable script, MockInterpreter interpreter)
    {
        this.script = script;
        this.interpreter = interpreter;
    }

    public void Run()
    {
        script.Run(interpreter);
    }
}

public interface Callable
{
    Task Run(MockInterpreter interpreter);
}

// This is used by the interpreter to run scheduled tasks within its own context instead of the
// SynchronizationContext. All awaiters used by the interpreter should implement this and interact
// with the interpreter.
public interface ISubscheduledContinuation : ISerializable
{
    void Continue();
    void AssignInterpreter(MockInterpreter interpreter);        // Used on deserialization to get new interpreter handle
}

[Serializable]
public class MockInterpreter
{
    // TODO: Get notice when an InterpreterFrame finishes.
    public List<InterpreterFrame> frames;

    private List<ISubscheduledContinuation> blocked;
    private List<ISubscheduledContinuation> unblocked;
    private List<ISubscheduledContinuation> yielded;

    public MockInterpreter()
    {
        frames = new List<InterpreterFrame>();
        blocked = new List<ISubscheduledContinuation>();
        unblocked = new List<ISubscheduledContinuation>();
        yielded = new List<ISubscheduledContinuation>();
    }

    public void AddScript(Callable script)
    {
        var newFrame = new InterpreterFrame(script, this);
        frames.Add(newFrame);
        newFrame.Run();
    }

    public void SetYielded(ISubscheduledContinuation continuation)
    {
        yielded.Add(continuation);
    }

    public void Tick()
    {
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
}

