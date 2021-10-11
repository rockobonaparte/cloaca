using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.IO;

using Antlr4.Runtime;
using Piksel.LibREPL;

using CloacaInterpreter;
using Language;
using LanguageImplementation;
using CloacaInterpreter.ModuleImporting;
using LanguageImplementation.DataTypes;

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

            var antlrVisitorContext = parser.file_input();

            var variablesIn = new Dictionary<string, object>();
            var visitor = new CloacaBytecodeVisitor(variablesIn, variablesIn);
            visitor.Visit(antlrVisitorContext);

            PyFunction compiledFunction = visitor.RootProgram.Build(variablesIn);

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            interpreter.DumpState = true;

            // We're setting up some module import paths so we can test the standard library.
            var repoRoots = new List<string>();
            repoRoots.Add(@"C:\coding\cloaca_git\StandardPythonLibrary");
            interpreter.AddModuleFinder(new FileBasedModuleFinder(repoRoots, new FileBasedModuleLoader()));

            scheduler.SetInterpreter(interpreter);

            var context = scheduler.Schedule(compiledFunction).Frame;

            //foreach (string varName in new List<string>(variablesIn.Keys))
            //{
            //    context.AddVariable(varName, variablesIn[varName]);
            //}

            interpreter.StepMode = true;
            bool traceMode = false;

            var debugRepl = new Piksel.LibREPL.Repl("dbg> ")
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
                            try
                            {
                                scheduler.Tick().Wait();
                            }
                            catch (AggregateException wrappedEscapedException)
                            {
                                // Given the nature of exception handling, we should normally only have one of these!
                                ExceptionDispatchInfo.Capture(wrappedEscapedException.InnerExceptions[0]).Throw();
                            }

                            if (scheduler.LastTasklet.EscapedDotNetException != null)
                            {
                                ExceptionDispatchInfo.Capture(scheduler.LastTasklet.EscapedDotNetException).Throw();
                            }
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
                        try
                        {
                            scheduler.Tick().Wait();
                        }
                        catch (AggregateException wrappedEscapedException)
                        {
                            // Given the nature of exception handling, we should normally only have one of these!
                            ExceptionDispatchInfo.Capture(wrappedEscapedException.InnerExceptions[0]).Throw();
                        }

                        if(scheduler.LastTasklet.EscapedDotNetException != null)
                        {
                            ExceptionDispatchInfo.Capture(scheduler.LastTasklet.EscapedDotNetException).Throw();
                        }

                        if (traceMode)
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
                        var currentTasklet = scheduler.LastTasklet;
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
                        var currentTasklet = scheduler.LastTasklet;
                        if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
                        {
                            if (args.Length == 0)
                            {
                                repl.Write(Dis.dis(currentTasklet.Function.Code, currentTasklet.Cursor, 1));
                            }
                            else if (args.Length == 1)
                            {
                                int count = Int32.Parse(args[0]);
                                repl.Write(Dis.dis(currentTasklet.Function.Code, currentTasklet.Cursor, count));
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
            var currentTasklet = scheduler.LastTasklet;
            if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
            {
                DumpState(currentTasklet);
            }
        }

        static void DumpDatastack(Scheduler scheduler)
        {
            var currentTasklet = scheduler.LastTasklet;
            if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
            {
                DumpDatastack(currentTasklet);
            }
        }

        static void DumpCode(Scheduler scheduler)
        {
            var currentTasklet = scheduler.LastTasklet;
            if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
            {
                DumpCode(currentTasklet);
            }
        }

        static void DumpCode(FrameContext tasklet)
        {
            Console.WriteLine("Code dump for " + tasklet.Function.Code.Name ?? "<null>");
            Console.WriteLine(Dis.dis(tasklet.Function.Code));
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
            var currentLine = Dis.dis(tasklet.Function.Code, tasklet.Cursor, 1);
            currentLine = ">>>" + currentLine.Substring(3, currentLine.Length - 3);
            Console.WriteLine(currentLine);
            DumpDatastack(tasklet);
        }
    }
}
