using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Manages all tasklets and how they're alternated through the interpreter.
    /// </summary>
    public class Scheduler
    {
        private Interpreter interpreter;
        private List<FrameContext> tasklets;
        private List<IEnumerable<SchedulingInfo>> contexts;
        private int currentTaskIndex;

        public Scheduler(Interpreter interpreter)
        {
            this.interpreter = interpreter;
            this.tasklets = new List<FrameContext>();
            this.contexts = new List<IEnumerable<SchedulingInfo>>();
            currentTaskIndex = -1;
        }

        public void Schedule(CodeObject program)
        {
            var tasklet = interpreter.PrepareFrameContext(program);
            tasklets.Add(tasklet);
            contexts.Add(null);            
        }

        /// <summary>
        /// Run until next yield, program termination, or completion of scheduled tasklets.
        /// </summary>
        public void Tick()
        {
            if(tasklets.Count == 0)
            {
                currentTaskIndex = -1;
                return;
            }

            ++currentTaskIndex;
            if(currentTaskIndex >= tasklets.Count)
            {
                currentTaskIndex = 0;
            }

            if(contexts[currentTaskIndex] == null)
            {
                contexts[currentTaskIndex] = interpreter.Run(tasklets[currentTaskIndex]);
            }

            var taskEnumerator = contexts[currentTaskIndex].GetEnumerator();
            if (!taskEnumerator.MoveNext())
            {
                // Done. Rewind the taskindex since it'll move up on the next tick.
                contexts.RemoveAt(currentTaskIndex);
                tasklets.RemoveAt(currentTaskIndex);
                --currentTaskIndex;
            }
        }

        public bool Done
        {
            get
            {
                return tasklets.Count == 0;
            }            
        }

    }

    class Program
    {
        static CodeObject compileCode(string program, Dictionary<string, object> variablesIn)
        {
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

            CodeObject compiledProgram1 = compileCode(program1, variablesIn);
            CodeObject compiledProgram2 = compileCode(program2, variablesIn);
            CodeObject compiledProgram3 = compileCode(program3, variablesIn);

            var interpreter = new Interpreter();
            interpreter.DumpState = true;

            var scheduler = new Scheduler(interpreter);

            scheduler.Schedule(compiledProgram1);
            scheduler.Schedule(compiledProgram2);
            scheduler.Schedule(compiledProgram3);

            while(!scheduler.Done)
            {
                scheduler.Tick();
            }

            Console.ReadKey();
        }
    }
}
