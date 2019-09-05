using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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

            var s = File.Create(@"C:\temp\cloaca_serialization_test.dat");
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(s, subsystems.Interpreter);
            s.Close();
        }

        Console.ReadKey();
    }
}
