using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// These represent frames managed by the interpreter. Frames are the complete context of an
/// executing script. This would include the complete stack trace and variable conditions. 
/// These frames are stopped and started at will.
/// 
/// We aren't actually implementing that feature here for this an are just using direct C#
/// calls, but the naming is place to convey the intent.
/// </summary>
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
        if(blocked.Remove(continuation))
        {
            unblocked.Add(continuation);
        }
    }
}

public class DialogScript : Callable
{
    public async Task Run(MockInterpreter interpreter)
    {
        await SubsystemProvider.Instance.Dialog.Say("Hi! Each of these...", interpreter);
        await SubsystemProvider.Instance.Dialog.Say("...should be coming out...", interpreter);
        await SubsystemProvider.Instance.Dialog.Say("...on different ticks...", interpreter);
        await SubsystemProvider.Instance.Dialog.Say("...due to time delay...", interpreter);
        await SubsystemProvider.Instance.Dialog.Say("...from user and engine...", interpreter);
        await SubsystemProvider.Instance.Dialog.Say("...to acknowledge the output...", interpreter);
    }
}

/// <summary>
/// This doesn't represent a script with explicit yielding written into it. Rather, it 
/// represents an ongoing, more intensive script that is being pre-empted by the interpreter
/// for running so long. Tried the yielding as if the interpreter running the frame has had
/// enough and is punching out of this script for now.
/// </summary>
public class YieldingScript : Callable
{
    public async Task Run(MockInterpreter interpreter)
    {
        for(int i = 1; i <= 8; ++i)
        {
            Console.WriteLine("Long-running script iteration #" + i);
            await new YieldTick(interpreter);
        }
    }
}

// The subsystems are in a global-ish context. We'll use a singleton.
public class SubsystemProvider
{
    public DialogSubsystem Dialog
    {
        get; protected set;
    }

    public MockInterpreter Interpreter
    {
        get; protected set;
    }

    private SubsystemProvider()
    {
        Dialog = new DialogSubsystem();
        Interpreter = new MockInterpreter();
    }

    public void Tick()
    {
        Dialog.Tick();
        Interpreter.Tick();
    }

    private static SubsystemProvider __instance;
    public static SubsystemProvider Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = new SubsystemProvider();
            }
            return __instance;
        }
    }
}

public class CustomPausingAwaiter : INotifyCompletion
{
    public CustomPausingAwaiter(MockInterpreter interpreter)
    {

    }

    private Action continuation;

    public void OnCompleted(Action continuation)
    {
        this.continuation = continuation;
    }

    public void Continue()
    {
        continuation?.Invoke();
    }
}

// This is used by the interpreter to run scheduled tasks within its own context instead of the
// SynchronizationContext. All awaiters used by the interpreter should implement this and interact
// with the interpreter.
public interface ISubscheduledContinuation
{
    void Continue();
}

public class DialogRequest
{
    public FutureVoidAwaiter Future
    {
        get; protected set;
    }

    public DialogRequest(string text, MockInterpreter interpreterToSubschedule)
    {
        Text = text;
        Future = new FutureVoidAwaiter(interpreterToSubschedule);
    }

    public string Text
    {
        get; protected set;
    }
}

/// <summary>
/// Representing a subsystem that's taking care of GUI dialogs. This mocks printing some text to the
/// screen and block control until it's acknowledged with a button press. However, it can't literally
/// block the engine or else the control input would never get acknowledged. So this has to call back
/// to the requestor once acknowledged. The trick is we want to make these requests using very simple,
/// procedural calls within a scripting interpreter. So we want those calls to look very natural
/// (half-sync/half-async kind of thing).
/// </summary>
public class DialogSubsystem
{
    private DialogRequest activeRequest;

    public void Tick()
    {
        // An active request will basic run for one "frame." In reality, these prompts would be up for
        // many more frames than that, but having this is enough to show the temporal situation.
        if(activeRequest != null)
        {
            Console.WriteLine("Dialog Subsystem: " + activeRequest.Text);
            var oldRequest = activeRequest;
            activeRequest = null;
            oldRequest.Future.SignalDone();
        }
    }

    // The interpreter is needed to create the DialogRequest properly; the request needs to know how to
    // phone home to the interpreter to get scheduled properly in the embedded subscheduler.
    public async Task Say(string text, MockInterpreter interpreter)
    {
        activeRequest = new DialogRequest(text, interpreter);
        interpreter.NotifyBlocked(activeRequest.Future);
        await activeRequest.Future;
    }
}

class Program
{
    static void Main()
    {
        var subsystems = SubsystemProvider.Instance;
        subsystems.Interpreter.AddScript(new DialogScript());
        subsystems.Interpreter.AddScript(new YieldingScript());

        // Mimicking Unity game engine loop. Each iteration is a frame.
        for (int frame = 0; frame < 10; ++frame)
        {
            Console.WriteLine("Frame #" + (frame + 1));
            subsystems.Tick();
        }

        Console.ReadKey();
    }
}
