using System;
using System.Threading.Tasks;

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

    public void ReloadSubsystems(MockInterpreter interpreter, DialogSubsystem dialog)
    {
        Interpreter = interpreter;
        Dialog = dialog;
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

[Serializable]
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
[Serializable]
public class DialogSubsystem
{
    private DialogRequest activeRequest;

    public void Tick()
    {
        // An active request will basic run for one "frame." In reality, these prompts would be up for
        // many more frames than that, but having this is enough to show the temporal situation.
        if (activeRequest != null)
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
