using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Piksel.LibREPL
{
    partial class Repl
    {
        private void AddInternalCommands()
        {
            Commands.Add("help", new Command()
            {
                Action = cmdHelp,
                Arguments = new List<Argument>()
                {
                    new Argument( "topic", "What to get help about.")
                },
                Description = "Provides help for commands and other functions of the REPL."
            });

            Commands.Add("debug", new Command()
            {
                Action = cmdDebug,
                Arguments = new List<Argument>()
                {
                    new Argument("toggle","Whether to enable or disable debugging") {
                        Type = ArgumentType.Boolean,
                    }
                },
                Description = "Toggle debugging information."
                
            });

            Commands.Add("version", new Command()
            {
                Action = cmdVersion,
                Description = "Show version information."
            });

            var cQuit = new Command()
            {
                Action = (repl, command, args) =>
                {
                    _userQuit = true;
                    Write(" Bye!\n", ConsoleColor.White);
                },
                Description = "Exits the REPL."
            };
            Commands.Add("quit", cQuit);
            Commands.Add("exit", new AliasCommand("quit"));
            Commands.Add("bye", new AliasCommand("quit"));

            var cClear = new Command()
            {
                Action = (repl, command, args) =>
                {
                    repl.Clear();
                },
                Description = "Clears the screen."
            };
            Commands.Add("clear", cClear);
            Commands.Add("cls", new AliasCommand("clear"));

            Commands.Add("reset", new Command()
            {
                Action = (repl, command, args) =>
                {
                    repl.Reset(
                        command.GetBoolArgumentOrDefault(args, 0),
                        command.GetBoolArgumentOrDefault(args, 1),
                        command.GetBoolArgumentOrDefault(args, 2));
                },
                Description = "Resets the terminal.",
                Arguments = new List<Argument>()
                {
                    new Argument("colors") {
                        Type = ArgumentType.Boolean,
                        Description = "Whether to reset the teriminal default colors.",
                        Default = "yes"
                    },
                    new Argument("cursor") {
                        Type = ArgumentType.Boolean,
                        Description = "Whether to reset the cursor position.",
                        Default = "yes"
                    },
                    new Argument("screen") {
                        Type = ArgumentType.Boolean,
                        Description = "Whether to clear the screen.",
                        Default = "yes"
                    }
                }
            });
        }

        private void cmdVersion(Repl repl, Command c, string[] args)
        {
            Hr();
            NewLine();

            Write(" " + Software.Name, ConsoleColor.White);
            var sv = Software.Version;
            Write($" v{sv.Major}.{sv.Minor}.{sv.Revision} build {sv.Build}\n");
            Write(" "+Software.Copyright + "\n");
            if (!string.IsNullOrEmpty(Software.License))
            {
                Write(" License: ", ConsoleColor.White);
                Write(Software.License+"\n");
            }
            if (!string.IsNullOrEmpty(Software.Message))
            {
                Write(Software.Message);
                NewLine();
            }

            NewLine();
            Hr();
            NewLine();

            var lv = Assembly.GetAssembly(GetType()).GetName().Version;
            Write(" LibREPL", ConsoleColor.White);
            Write($" v{lv.Major}.{lv.Minor}.{lv.Revision} build {lv.Build}\n");
            Write(" Copyright © 20"+(lv.Build / 1000)+", piksel bitworks\n");
            NewLine();

            Hr();
        }

        private void cmdDebug(Repl repl, Command c, string[] args)
        {

            if (args.Length > 0)
            {

                var newDebug = Repl.ParseBoolean(args[0]);
                if (newDebug.HasValue)
                {
                    PrintInput = newDebug.Value;
                }
                else {

                    Write(" Error", ConsoleColor.Red);
                    Write(": Unknown boolean value ");
                    Write(args[0], ConsoleColor.Cyan);
                    Write(".\n");
                }

            }
            Write(" Debug is ");
            if (PrintInput)
                Write("ENABLED", ConsoleColor.Green);
            else
                Write("DISABLED", ConsoleColor.Red);
            Write(".\n");
        }

        private void cmdHelp(Repl repl, Command c, string[] args)
        {
            if (args.Length < 1)
                PrintHelp();
            else
            {
                switch(args[0].ToLower())
                {
                    case "command":
                        Write(" No, as in substitute COMMAND for a command you want help on.\n Use ");
                        Write("help commands", ConsoleColor.White);
                        Write(" to list available commands.\n");
                        break;
                    case "commands":
                        Write(" Registered commands:\n");

                        var maxNameLength = 0;
                        foreach (var name in Commands.Keys)
                            if(maxNameLength < name.Length) maxNameLength = name.Length;

                        foreach (var cmdKvp in Commands)
                        {
                            var cmd = cmdKvp.Value;
                            var name = cmdKvp.Key;
                            Write(" " + name.PadLeft(maxNameLength, ' '), ConsoleColor.Cyan);
                            WriteFormatted(" " + cmd.Description);
                            NewLine();
                        }
                        break;
                    default:
                        if (Commands.ContainsKey(args[0]))
                        {
                            var cc = Commands[args[0]];

                            WriteFormatted(" " + cc.Description + "\n\n");

                            Write(" Usage:\n");
                            Write("  " + args[0], ConsoleColor.White);

                            var maxArgLength = 0;
                            foreach (var ca in cc.Arguments)
                            {
                                if (ca.Keyword.Length > maxArgLength)
                                    maxArgLength = ca.Keyword.Length;
                                Write(" " + ca.Keyword, ca.Required ? ConsoleColor.Red : ConsoleColor.Cyan);
                            }
                            Write("\n\n Arguments:\n");
                            if (cc.Arguments.Count > 0)
                            {
                                foreach (var ca in cc.Arguments)
                                {
                                    Write("  " + ca.Keyword.PadLeft(maxArgLength, ' '), ConsoleColor.White);

                                    if (true || ca.Type != ArgumentType.String)
                                    {
                                        Write(" ");
                                        Dir(ca.Type);
                                        Write(" ");
                                    }
                                    if (!string.IsNullOrEmpty(ca.Default)) {
                                        Write("(Default: ");
                                        Write(ca.Default, ConsoleColor.Yellow);
                                        Write(") ");
                                    }
                                    if (ca.Required)
                                        Write("(Required) ");
                                    NewLine();

                                    Write(new string(' ', maxArgLength+4));
                                    WriteFormatted(ca.Description + "\n\n");
                                }
                            }
                            else
                            {
                                Write(" (does not take any arguments)\n", ConsoleColor.Yellow);
                            }
                        }
                        break;
                }
            }

        }

        private void PrintHelp()
        {
            //Write(" Help\n", ConsoleColor.White);
            //Hr();
            Write(" Type ");
            Write("help ", ConsoleColor.White);
            Write("COMMAND", ConsoleColor.Magenta);
            Write(" to get help regarding a command.");
            NewLine();
        }
    }
}
