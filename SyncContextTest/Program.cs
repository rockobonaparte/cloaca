using System.Collections.Generic;

/// <summary>
/// These represent frames managed by the interpreter. Frames are the complete context of an
/// executing script. This would include the complete stack trace and variable conditions. 
/// These frames are stopped and started at will.
/// 
/// We aren't actually implementing that feature here for this an are just using direct C#
/// calls, but the naming is place to convey the intent.
/// </summary>
class InterpreterFrame
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

interface Callable
{
    void Run();
}

class MockInterpreter
{
    public List<InterpreterFrame> frames;

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

class DialogRequest
{
    public DialogRequest()
    {
        // Does nothing yet. We'll need to plumb a callback and blocking mechanism or whatever before
        // this goes live.
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
class DialogSubsystem
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
            activeRequest.SignalDone();
            activeRequest = null;
        }
        if (requests.Count > 0)
        {
            activeRequest = requests.Dequeue();
        }
    }

    public void Say(DialogRequest request)
    {
        requests.Enqueue(request);
    }
}

class Program
{
    static void Main()
    {
        var dialogEngine = new DialogSubsystem();
        var interpreter = new MockInterpreter();

        // Mimicking Unity game engine loop. Each iteration is a frame.
        for(int frame = 0; frame < 100; ++frame)
        {
            dialogEngine.Tick();
            interpreter.Tick();
        }
    }
}
