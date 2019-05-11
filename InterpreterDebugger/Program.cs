using System;
using System.Collections.Generic;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

using Piksel.LibREPL;
using Antlr4.Runtime;

namespace InterpreterDebugger
{
    class Program
    {
        static void Main()
        {
            string program = "a = 10\n";
            var variablesIn = new Dictionary<string, object>();

            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.program();

            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(antlrVisitorContext);

            CodeObject compiledProgram = visitor.RootProgram.Build();
            Console.WriteLine(Dis.dis(compiledProgram));

            var interpreter = new Interpreter();
            interpreter.DumpState = true;
            var scheduler = new Scheduler(interpreter);

            var context = scheduler.Schedule(compiledProgram);
            foreach (string varName in variablesIn.Keys)
            {
                context.SetVariable(varName, variablesIn[varName]);
            }

            interpreter.StepMode = true;
            scheduler.Home();

            var debugRepl = new Repl("dbg> ")
            {
                HeaderTitle = "Cloaca Interpreter Debugger",
                HeaderSubTitle = "Debug Cloaca ByteCode Evaluation"
            };

            debugRepl.Commands.Add("g", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    repl.Write("Running until finished");
                    while (!scheduler.Done)
                    {
                        scheduler.Tick();
                    }
                },
                Description = "Runs until finished"
            });

            debugRepl.Commands.Add("s", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    repl.Write("Stepping");
                    interpreter.StepMode = true;
                    scheduler.Tick();
                },
                Description = "Steps one line of bytecode"
            });

            debugRepl.Commands.Add("d", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    repl.Write("Datastack");
                    var currentTasklet = scheduler.ActiveTasklet;
                    if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
                    {
                        DumpState(currentTasklet);
                    }
                },
                Description = "Dumps the data stack"
            });

            debugRepl.Commands.Add("t", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    repl.Write("Trace mode on. Will show current line and data stack after each stop");
                },
                Description = "Toggle trace mode (not implemented yet)."
            });

            debugRepl.Commands.Add("c", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    repl.Write("Disassembles current code");
                },
                Description = "Disassembles the current code object (not implemented yet)."
            });

            debugRepl.Commands.Add("l", new Command()
            {
                Action = (repl, cmd, args) =>
                {
                    repl.Write("Disassembles current code");
                },
                Description = "Disassembles byte code based on the current location (not implemented yet)."
            });

            debugRepl.Start();
        }

        static void DumpState(FrameContext tasklet)
        {
            Console.WriteLine("Dumping");
            Console.WriteLine(Dis.dis(tasklet.Program, tasklet.Cursor, 1));

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
        }
    }
}
