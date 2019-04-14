using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

namespace InterpreterWaiting
{
    /// <summary>
    /// Copypasta from unit tests.
    /// Capture parsing errors for the test bench. Each one gets crammed into a list of strings that can be 
    /// examined after parsing.
    /// </summary>
    public class ParseErrorListener : IParserErrorListener
    {
        public List<string> Errors;

        public ParseErrorListener()
        {
            Errors = new List<string>();
        }

        public void ReportAmbiguity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, bool exact, [Nullable] BitSet ambigAlts, [NotNull] ATNConfigSet configs)
        {
            // Ignore
        }

        public void ReportAttemptingFullContext([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, [Nullable] BitSet conflictingAlts, [NotNull] SimulatorState conflictState)
        {
            // Ignore
        }

        public void ReportContextSensitivity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, int prediction, [NotNull] SimulatorState acceptState)
        {
            // Ignore
        }

        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            Errors.Add("line " + line + ":" + charPositionInLine + " " + msg);
        }

        public string Report()
        {
            var report = "";

            foreach (var line in Errors)
            {
                report += line + "\n";
            }

            return report;
        }
    }

    public class Scheduler
    {
        public bool ready;
        public Scheduler()
        {
            ready = false;
        }
    }

    public class WaitOnce : INotifyCompletion
    {
        private Scheduler scheduler;
        public WaitOnce(Scheduler scheduler)
        {
            this.scheduler = scheduler;
            IsCompleted = false;
        }

        public bool IsCompleted
        {
            get;
            private set;
        }

        void GetResult()
        {

        }

        public void OnCompleted(Action continuation)
        {
            scheduler.ready = true;
        }
    }



    class Program
    {
        // TODO:
        // 1. Change Terminated to reflect upon the RootProgram.
        // 2. Get wait to work again in unit tests.
        // 3. Finally get the wait in the class body to work correctly.
        static void RunInterpreterTest()
        {
            // This still runs fine, apparently.
            string program1 =
                "a = 1\n" +
                "wait\n" +
                "a = 2\n";

            // This should be more troublesome since it has to call __build_class__, which causes us to transition
            // Python -> C# (__build_class__) -> Python (class body).
            // Currently it just screws up during parsing...
            string program2 =
                "class Foo:\n" +
                "  wait\n" +
                "  a = 2\n";

            var inputStream = new AntlrInputStream(program1);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var context = parser.program();

            var visitor = new CloacaBytecodeVisitor();
            visitor.Visit(context);

            // We'll do a disassembly here but won't assert against it. We just want to make sure it doesn't crash.
            CodeObject compiledProgram = visitor.RootProgram.Build();

            Dis.dis(compiledProgram);

            var interpreter = new Interpreter(compiledProgram);

            // Terminated doesn't get set any more since the interpreter can have the active program pulled out from
            // under it. So we need to come up with a better mechanism. Probably root program being terminated.
            //
            //int runCount = 0;
            //while (!interpreter.Terminated)
            //{
            //    interpreter.Run();
            //    runCount += 1;
            //    Console.WriteLine("Interpreter pass #" + runCount);
            //}

            for (int runCount = 1; runCount <= 2; ++runCount)
            {
                interpreter.Run();
                Console.WriteLine("Interpreter pass #" + runCount);
                //                var a = interpreter.GetVariable("a");
                //                Console.WriteLine("  a = " + a);
            }

            Console.WriteLine("All done. Press any key.");
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            // Async-await-task-IEnumerator-whatever problem here:
            // 1. Run some code
            // 2. Call something that wants to run some more code with a pause in between
            // 3. Make sure we come back to the top when the pause shows up
            // 4. Make sure we can resume at #2 to finish it right afterwards
            var program =   "a = 10 * (2 + 4) / 3\n" +
                            "wait\n" +
                            "b = a + 3\n";
            var variablesIn = new Dictionary<string, object>();

            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var context = parser.program();

            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(context);

            // We'll do a disassembly here but won't assert against it. We just want to make sure it doesn't crash.
            CodeObject compiledProgram = visitor.RootProgram.Build();

            Dis.dis(compiledProgram);

            var interpreter = new Interpreter(compiledProgram);
            interpreter.DumpState = true;
            foreach (string varName in variablesIn.Keys)
            {
                interpreter.SetVariable(varName, variablesIn[varName]);
            }

            int runCount = 0;


            // This is will become our scheduling logic.
            var interpreterResult = interpreter.Run();
            var interpreterEnumer = interpreterResult.GetEnumerator();

            while(interpreterEnumer.MoveNext())
            {
                var scheduleInfo = interpreterEnumer.Current;
                runCount += 1;
            }

            var variables = interpreter.DumpVariables();
            foreach(var k in variables.Keys)
            {
                Console.WriteLine(k + " = " + variables[k]);
            }
            Console.ReadKey();
        }
    }
}
