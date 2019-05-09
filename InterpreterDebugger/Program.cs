using System;
using System.Collections.Generic;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

using Antlr4.Runtime;

namespace InterpreterDebugger
{
    class Program
    {
        static void Main(string[] args)
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
            // Console.WriteLine(Dis.dis(compiledProgram, 0, 1));
            while (!scheduler.Done)
            {
                var currentTasklet = scheduler.ActiveTasklet;
                if (currentTasklet != null && currentTasklet.Cursor < currentTasklet.CodeBytes.Bytes.Length)
                {
                    DumpState(currentTasklet);
                }
                scheduler.Tick();
            }

            Console.ReadKey();
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
