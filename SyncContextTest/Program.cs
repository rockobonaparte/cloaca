using System;
using System.Threading.Tasks;

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
