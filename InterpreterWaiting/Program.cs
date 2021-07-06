using System;
using System.Collections.Generic;

using Antlr4.Runtime;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

namespace InterpreterWaiting
{
    class Program
    {
        static CodeObject compileCode(string program, Dictionary<string, object> variablesIn, Scheduler scheduler)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var context = parser.file_input();

            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(context);
            visitor.PostProcess(scheduler);

            // We'll do a disassembly here but won't assert against it. We just want to make sure it doesn't crash.
            CodeObject compiledProgram = visitor.RootProgram.Build();

            return compiledProgram;
        }

        static void Main(string[] args)
        {
            // Async-await-task-IEnumerator-whatever problem here:
            // 1. Run some code
            // 2. Call something that wants to run some more code with a pause in between
            // 3. Make sure we come back to the top when the pause shows up
            // 4. Make sure we can resume at #2 to finish it right afterwards
            var program1 = "a = 10 * (2 + 4) / 3\n" +
                           "wait\n" +
                           "b = a + 3\n";
            var program2 = "c = 2\n" +
                           "wait\n" +
                           "d = c + 3\n";
            var program3 = "e = 7\n" +
                           "wait\n" +
                           "f = e + 2\n";
            var variablesIn = new Dictionary<string, object>();

            throw new NotImplementedException("Currently blocking out code here while figuring out how to get paramenter defaults to be calculated");
            //CodeObject compiledProgram1 = compileCode(program1, variablesIn);
            //CodeObject compiledProgram2 = compileCode(program2, variablesIn);
            //CodeObject compiledProgram3 = compileCode(program3, variablesIn);

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            interpreter.DumpState = true;
            scheduler.SetInterpreter(interpreter);

            //scheduler.Schedule(compiledProgram1);
            //scheduler.Schedule(compiledProgram2);
            //scheduler.Schedule(compiledProgram3);

            while(!scheduler.Done)
            {
                scheduler.Tick();
            }

            Console.ReadKey();
        }
    }
}
