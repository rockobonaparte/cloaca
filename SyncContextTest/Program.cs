using System;
using System.Collections.Generic;

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
    public InterpreterFrame(Callable script)
    {
        this.script = script;
    }

    public void Run()
    {
        script.Run();
    }
}

public interface Callable
{
    void Run();
}

public class MockInterpreter
{
    public List<InterpreterFrame> frames;

    public MockInterpreter()
    {
        frames = new List<InterpreterFrame>();
    }

    public void AddScript(Callable script)
    {
        frames.Add(new InterpreterFrame(script));
    }

    public void Tick()
    {
        foreach(var frame in frames)
        {
            frame.Run();
        }
    }
}

public class DialogScript : Callable
{
    public void Run()
    {
        SubsystemProvider.Instance.Dialog.Say("Hi! Each of these...");
        SubsystemProvider.Instance.Dialog.Say("...should be coming out...");
        SubsystemProvider.Instance.Dialog.Say("...on different ticks...");
        SubsystemProvider.Instance.Dialog.Say("...due to time delay...");
        SubsystemProvider.Instance.Dialog.Say("...from user and engine...");
        SubsystemProvider.Instance.Dialog.Say("...to acknowledge the output...");
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

public class DialogRequest
{
    public string Text
    {
        get; protected set;
    }
    public DialogRequest(string text)
    {
        Text = text;
    }

    public void SignalDone()
    {

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
    private Queue<DialogRequest> requests;
    private DialogRequest activeRequest;

    public DialogSubsystem()
    {
        requests = new Queue<DialogRequest>();
    }

    public void Tick()
    {
        // An active request will basic run for one "frame." In reality, these prompts would be up for
        // many more frames than that, but having this is enough to show the temporal situation.
        if(activeRequest != null)
        {
            Console.WriteLine("Dialog Subsystem: " + activeRequest.Text);
            activeRequest.SignalDone();
            activeRequest = null;
        }
        while (requests.Count > 0)
        {
            // Only the most recent request will get honored in this system.
            // This will help to prove that we are not vomiting them in at once
            // in the script; that the script is getting properly blocked and feeding
            // in its inputs sequentially.
            activeRequest = requests.Dequeue();
        }
    }

    public void Say(string text)
    {
        requests.Enqueue(new DialogRequest(text));
    }
}

class Program
{
    static void Main()
    {
        var subsystems = SubsystemProvider.Instance;
        subsystems.Interpreter.AddScript(new DialogScript());

        // Mimicking Unity game engine loop. Each iteration is a frame.
        for(int frame = 0; frame < 10; ++frame)
        {
            Console.WriteLine("Frame #" + (frame + 1));
            subsystems.Tick();
        }

        Console.ReadKey();
    }
}
