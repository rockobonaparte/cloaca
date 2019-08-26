//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//// Experiments with making the IEnumerator-based coroutine pattern more mature. Taking the same situation from SyncContextTest and using IEnumerator instead.
//public class InterpreterFrame
//{
//    Callable script;
//    public InterpreterFrame(Callable script)
//    {
//        this.script = script;
//    }

//    public void Run()
//    {
//        script.Run();
//    }
//}

//public interface Callable
//{
//    IEnumerable<SchedulingInfo> Run();
//}

//public class MockInterpreter
//{
//    public List<InterpreterFrame> frames;

//    private IEnumerable<SchedulingInfo> CallUntilResult(Callable call, out object returned)
//    {
//        returned = null;
//        foreach (var continuation in call.Run())
//        {
//            if (continuation is ReturnValue)
//            {
//                var asReturnValue = continuation as ReturnValue;
//                returned = asReturnValue.Returned;
//                break;
//            }
//            else
//            {
//                yield return continuation;
//            }
//        }
//    }

//    public MockInterpreter()
//    {
//        frames = new List<InterpreterFrame>();
//    }

//    public void AddScript(Callable script)
//    {
//        var newFrame = new InterpreterFrame(script);
//        frames.Add(newFrame);
//        newFrame.Run();
//    }

//    public void Tick()
//    {
//        // BOOKMARK: This is wrong. Need to get the tasks and continue in next tick.
//        //foreach(var frame in frames)
//        //{
//        //    frame.Run();
//        //}
//    }
//}

//public class DialogScript : Callable
//{
//    public IEnumerator Run()
//    {
//        yield SubsystemProvider.Instance.Dialog.Say("Hi! Each of these...");
//        yield SubsystemProvider.Instance.Dialog.Say("...should be coming out...");
//        yield SubsystemProvider.Instance.Dialog.Say("...on different ticks...");
//        yield SubsystemProvider.Instance.Dialog.Say("...due to time delay...");
//        yield SubsystemProvider.Instance.Dialog.Say("...from user and engine...");
//        yield SubsystemProvider.Instance.Dialog.Say("...to acknowledge the output...");

//    }
//}

///// <summary>
///// This doesn't represent a script with explicit yielding written into it. Rather, it 
///// represents an ongoing, more intensive script that is being pre-empted by the interpreter
///// for running so long. Tried the yielding as if the interpreter running the frame has had
///// enough and is punching out of this script for now.
///// </summary>
//public class YieldingScript : Callable
//{
//    public async Task Run()
//    {
//        for (int i = 1; i <= 8; ++i)
//        {
//            Console.WriteLine("Long-running script iteration #" + i);
//            await Task.Yield();
//        }
//    }
//}

//// The subsystems are in a global-ish context. We'll use a singleton.
//public class SubsystemProvider
//{
//    public DialogSubsystem Dialog
//    {
//        get; protected set;
//    }

//    public MockInterpreter Interpreter
//    {
//        get; protected set;
//    }

//    private SubsystemProvider()
//    {
//        Dialog = new DialogSubsystem();
//        Interpreter = new MockInterpreter();
//    }

//    public void Tick()
//    {
//        Dialog.Tick();
//        Interpreter.Tick();
//    }

//    private static SubsystemProvider __instance;
//    public static SubsystemProvider Instance
//    {
//        get
//        {
//            if (__instance == null)
//            {
//                __instance = new SubsystemProvider();
//            }
//            return __instance;
//        }
//    }
//}

//public class DialogRequest : INotifyCompletion
//{
//    private bool finished;
//    private Action continuation;

//    //public bool IsPaused
//    //{
//    //    get
//    //    {
//    //        return !finished;
//    //    }
//    //}

//    public bool IsCompleted
//    {
//        get
//        {
//            //Console.WriteLine("Checked IsCompleted. It is " + finished);
//            return finished;
//        }
//    }

//    public string Text
//    {
//        get; protected set;
//    }

//    public DialogRequest(string text)
//    {
//        Text = text;
//        finished = false;
//    }

//    public void SignalDone()
//    {
//        Console.WriteLine("Signalled done");
//        finished = true;
//        Console.WriteLine("Invoking continuation");
//        continuation?.Invoke();
//    }

//    public void OnCompleted(Action continuation)
//    {
//        if (finished)
//        {
//            continuation();
//        }
//        else
//        {
//            Console.WriteLine("register continuation to call when request is filled");
//            this.continuation = continuation;
//        }
//    }

//    public DialogRequest GetAwaiter()
//    {
//        return this;
//    }

//    public void GetResult()
//    {

//    }
//}

///// <summary>
///// Representing a subsystem that's taking care of GUI dialogs. This mocks printing some text to the
///// screen and block control until it's acknowledged with a button press. However, it can't literally
///// block the engine or else the control input would never get acknowledged. So this has to call back
///// to the requestor once acknowledged. The trick is we want to make these requests using very simple,
///// procedural calls within a scripting interpreter. So we want those calls to look very natural
///// (half-sync/half-async kind of thing).
///// </summary>
//public class DialogSubsystem
//{
//    private DialogRequest activeRequest;

//    public void Tick()
//    {
//        // An active request will basic run for one "frame." In reality, these prompts would be up for
//        // many more frames than that, but having this is enough to show the temporal situation.
//        if (activeRequest != null)
//        {
//            Console.WriteLine("Dialog Subsystem: " + activeRequest.Text);
//            var oldRequest = activeRequest;
//            activeRequest = null;
//            oldRequest.SignalDone();
//        }
//    }

//    public async Task Say(string text)
//    {
//        activeRequest = new DialogRequest(text);
//        Console.WriteLine("Enqueued request");

//        Console.WriteLine("awaiting request result");
//        await activeRequest;
//        Console.WriteLine("request done");
//    }
//}

//class Program
//{
//    static void Main()
//    {
//        var subsystems = SubsystemProvider.Instance;
//        subsystems.Interpreter.AddScript(new DialogScript());
//        subsystems.Interpreter.AddScript(new YieldingScript());

//        // Mimicking Unity game engine loop. Each iteration is a frame.
//        for (int frame = 0; frame < 10; ++frame)
//        {
//            Console.WriteLine("Frame #" + (frame + 1));
//            subsystems.Tick();
//        }

//        Console.ReadKey();
//    }
//}
