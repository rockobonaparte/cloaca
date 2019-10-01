using System;
using System.Collections.Generic;
using System.IO;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

using Piksel.LibREPL;
using Antlr4.Runtime;

namespace InterpreterDebugger
{
    class Program
    {
        static void Main(string[] cmdline_args)
        {
            if(cmdline_args.Length != 1)
            {
                Console.WriteLine("One argument required: path to script to compile and run.");
                return;
            }
            string program = null;
            using (var inFile = new StreamReader(cmdline_args[0]))
            {
                program = inFile.ReadToEnd();
            }

            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);
            if(errorListener.Errors.Count > 0)
            {
                Console.WriteLine("There were errors trying to compile the script. We cannot run it.");
                return;
            }

            var antlrVisitorContext = parser.program();

            var variablesIn = new Dictionary<string, object>();
            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(antlrVisitorContext);

            CodeObject compiledProgram = visitor.RootProgram.Build();

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            interpreter.DumpState = true;
            scheduler.SetInterpreter(interpreter);

            var context = scheduler.Schedule(compiledProgram);
            foreach (string varName in variablesIn.Keys)
            {
                context.SetVariable(varName, variablesIn[varName]);
            }

            interpreter.StepMode = true;
            scheduler.Home();
            bool traceMode = false;



            var debugRepl = new Repl("dbg> ")
            {
                HeaderTitle = "Cloaca Interpreter Debugger",
                HeaderSubTitle = "Debug Cloaca ByteCode Evaluation"
            };

            debugRepl.Commands.Add("g", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    if (scheduler.Done)
                    {
                        repl.Write("Scheduled programs are all done.");
                    }
                    else
                    {
                        interpreter.StepMode = false;
                        while (!scheduler.Done)
                        {
                            scheduler.Tick();
                        }
                    }
                },
                Description = "Runs until finished"
            });

            debugRepl.Commands.Add("s", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    if (scheduler.Done)
                    {
                        repl.Write("Scheduled programs are all done.");
                    }
                    else
                    {
                        interpreter.StepMode = true;
                        scheduler.Tick();
                        if(traceMode)
                        {
                            DumpState(scheduler);
                        }
                    }
                },
                Description = "Steps one line of bytecode"
            });

            debugRepl.Commands.Add("d", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    if (scheduler.Done)
                    {
                        repl.Write("Scheduled programs are all done.");
                    }
                    else
                    {
                        var currentTasklet = scheduler.ActiveTasklet;
                        if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
                        {
                            DumpDatastack(currentTasklet);
                        }
                    }
                },
                Description = "Dumps the data stack"
            });

            debugRepl.Commands.Add("t", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    traceMode = !traceMode;
                    if(traceMode)
                    {
                        repl.Write("Trace mode on.");
                    }
                    else
                    {
                        repl.Write("Trace mode off.");
                    }
                },
                Description = "Toggle trace mode."
            });

            debugRepl.Commands.Add("c", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    if (scheduler.Done)
                    {
                        repl.Write("Scheduled programs are all done.");
                    }
                    else
                    {
                        DumpCode(scheduler);
                    }
                },
                Description = "Disassembles the current code object."
            });

            debugRepl.Commands.Add("l", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    if (scheduler.Done)
                    {
                        repl.Write("Scheduled programs are all done.");
                    }
                    else
                    {
                        var currentTasklet = scheduler.ActiveTasklet;
                        if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
                        {
                            if (args.Length == 0)
                            {
                                repl.Write(Dis.dis(currentTasklet.Program, currentTasklet.Cursor, 1));
                            }
                            else if (args.Length == 1)
                            {
                                int count = Int32.Parse(args[0]);
                                repl.Write(Dis.dis(currentTasklet.Program, currentTasklet.Cursor, count));
                            }
                        }
                    }
                },
                Description = "Disassembles byte code based on the current location (not implemented yet)."
            });

            debugRepl.Start();
        }

        static void DumpState(Scheduler scheduler)
        {
            var currentTasklet = scheduler.ActiveTasklet;
            if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
            {
                DumpState(currentTasklet);
            }
        }

        static void DumpDatastack(Scheduler scheduler)
        {
            var currentTasklet = scheduler.ActiveTasklet;
            if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
            {
                DumpDatastack(currentTasklet);
            }
        }

        static void DumpCode(Scheduler scheduler)
        {
            var currentTasklet = scheduler.ActiveTasklet;
            if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
            {
                DumpCode(currentTasklet);
            }
        }

        static void DumpCode(FrameContext tasklet)
        {
            Console.WriteLine("Code dump for " + tasklet.Program.Name ?? "<null>");
            Console.WriteLine(Dis.dis(tasklet.Program));
        }

        static void DumpDatastack(FrameContext tasklet)
        {
            if (tasklet.DataStack.Count > 0)
            {
                Console.WriteLine("Data stack:");
                int i = 0;
                foreach (var dataStackObj in tasklet.DataStack)
                {
                    Console.WriteLine("   " + i + " " + dataStackObj);
                }
            }
            else
            {
                Console.WriteLine("Data stack is empty");
            }
            Console.WriteLine("Code cursor = " + tasklet.Cursor);
        }

        static void DumpState(FrameContext tasklet)
        {
            DumpCode(tasklet);
            var currentLine = Dis.dis(tasklet.Program, tasklet.Cursor, 1);
            currentLine = ">>>" + currentLine.Substring(3, currentLine.Length - 3);
            Console.WriteLine(currentLine);
            DumpDatastack(tasklet);
        }
    }
}
