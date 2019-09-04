using System;
using System.Threading.Tasks;

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
        for (int i = 1; i <= 8; ++i)
        {
            Console.WriteLine("Long-running script iteration #" + i);
            await new YieldTick(interpreter);
        }
    }
}
