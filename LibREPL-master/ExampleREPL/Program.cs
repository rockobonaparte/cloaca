using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Piksel.LibREPL.ExampleREPL
{
    class Program
    {
        static void Main()
        {
            var exampleRepl = new Repl("REPL> ")
            {
                HeaderTitle = "Example REPL",
                HeaderSubTitle = "Example Read Evaluate Print Loop for testing LibREPL"
            };

            exampleRepl.Commands.Add("spintest", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    Thread.Sleep(5000);
                },
                Progress = CommandProgressType.Spinner,
                ProgressMessage = "Spinning for 5 seconds: ",
                ProgressCompleted = "Done!",
                Description = "Tests the spinner"
            });

            exampleRepl.Commands.Add("passtest", new Command()
            {
                Action = (repl, cmd, args) => {
                    var pw = exampleRepl.GetPassword("Password: ");
                    repl.Write(" Your super secret password is: ");
                    repl.Write(pw, ConsoleColor.Yellow);
                    repl.NewLine();
                },
                Description = "Tests password input"
            });

            exampleRepl.Commands.Add("argstest", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    repl.Write(" Arguments as array:\n", ConsoleColor.White);
                    repl.Dir(args);
                    repl.NewLine();
                    repl.Write(" Arguments as dictionary:\n", ConsoleColor.White);
                    repl.Dir(cmd.GetArgumentDictionary(args));
                },
                Progress = CommandProgressType.None,
                Description = "Tests command arguments",
                Arguments = new List<Argument>()
                {
                    new Argument("a", "A description") { Required = true },
                    new Argument("b", "B description") { Required = true },
                    new Argument("c", "C description") { Required = true },
                    new Argument("d", "D description") { Default = "yes", Type = ArgumentType.Boolean }, 
                    new Argument("pi", "Value of PI") { Default = "3.14", Type = ArgumentType.Number },
                    new Argument("long1", "Long1 description") { Default = "L1" },
                    new Argument("long2", "Long2 description") { Default = "L2" },
                }
            });

            exampleRepl.Start();
        }
    }
}
