using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class RootSerializationRecord
{
    public MockInterpreter Interpreter;
    public DialogSubsystem Dialog;
    public int Frame;
}

class Program
{
    static string GetArgument(IEnumerable<string> args, string option)
    => args.SkipWhile(i => i != option).Skip(1).Take(1).FirstOrDefault();

    static bool HasArgument(IEnumerable<string> args, string option) => args.Contains(option);

    static void Main(string[] args)
    {
        // Args:
        // -r Reset
        // -s Save path. Default to @"C:\temp\cloaca_serialization_test.dat"
        bool reset = HasArgument(args, "-r");
        string savePath = GetArgument(args, "-s") ?? @"C:\temp\cloaca_serialization_test.dat";
        bool saveExists = File.Exists(savePath);

        SubsystemProvider subsystems = null;
        int frame = 0;

        if (reset || !saveExists)
        {
            if(reset && saveExists)
            {
                File.Delete(savePath);
            }
            Console.WriteLine("Initial launch of game state");
            subsystems = SubsystemProvider.Instance;
            subsystems.Interpreter.AddScript(new DialogScript());
            subsystems.Interpreter.AddScript(new YieldingScript());
            frame = 0;
        }
        else if(saveExists)
        {
            // Load previous save and resume.
            Console.WriteLine("Loading save state at " + savePath);
            var s = File.OpenRead(savePath);
            BinaryFormatter b = new BinaryFormatter();
            var record = (RootSerializationRecord)b.Deserialize(s);
            subsystems = SubsystemProvider.Instance;
            subsystems.ReloadSubsystems(record.Interpreter, record.Dialog);
            frame = record.Frame;
        }

        // Mimicking Unity game engine loop. Each iteration is a frame.
        if(frame >= 10)
        {
            Console.WriteLine("No more frames to run");
        }
        else
        {
            frame += 1;
            Console.WriteLine("Frame #" + frame);
            subsystems.Tick();

            var s = File.Create(savePath);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(s, new RootSerializationRecord
            {
                Interpreter = subsystems.Interpreter,
                Dialog = subsystems.Dialog,
                Frame = frame
            });
            s.Close();
        }
    }
}
