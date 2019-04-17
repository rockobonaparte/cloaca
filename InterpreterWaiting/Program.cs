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

            // TODO: Set per-frame instead of using the interpreter
            // Currently, this is a no-op since we don't have any variables in variablesIn.
            foreach (string varName in variablesIn.Keys)
            {
                interpreter.SetVariable(varName, variablesIn[varName]);
            }

            int runCount = 0;

            var tasklets = new List<Stack<Frame>>();
            var cursors = new List<int>();
            var contexts = new List<IEnumerable<SchedulingInfo>>();
            tasklets.Add(interpreter.PrepareFrameStack(compiledProgram1));
            tasklets.Add(interpreter.PrepareFrameStack(compiledProgram2));
            tasklets.Add(interpreter.PrepareFrameStack(compiledProgram3));
            cursors.Add(0);
            cursors.Add(0);
            cursors.Add(0);
            contexts.Add(interpreter.Run(tasklets[0], 0));
            contexts.Add(interpreter.Run(tasklets[1], 0));
            contexts.Add(interpreter.Run(tasklets[2], 0));

            while (tasklets.Count > 0)
            {
                int taskIdx = 0;
                while(taskIdx < tasklets.Count)
                {
                    var tasklet = tasklets[taskIdx];
                    var cursor = cursors[taskIdx];
                    var interpreterEnumer = contexts[taskIdx].GetEnumerator();

                    interpreter.SetContext(tasklet, cursor);

                    // This doesn't work as intended because the Cursor resets when we change tasklets.
                    // We need to encapsulate the cursor in our task state in some way.
                    if (!interpreterEnumer.MoveNext())
                    {
                        var topFrame = tasklets[taskIdx].Peek();
                        for (int varIdx = 0; varIdx < tasklets[taskIdx].Peek().Locals.Count; ++varIdx)
                        {
                            Console.WriteLine(topFrame.LocalNames[varIdx] + " = " + topFrame.Locals[varIdx]);
                        }

                        tasklets.RemoveAt(taskIdx);
                        cursors.RemoveAt(taskIdx);
                        contexts.RemoveAt(taskIdx);
                        // Don't advance taskIdx
                    }
                    else
                    {
                        var scheduleInfo = interpreterEnumer.Current;
                        runCount += 1;

                        var topFrame = tasklets[taskIdx].Peek();
                        for (int varIdx = 0; varIdx < tasklets[taskIdx].Peek().Locals.Count; ++varIdx)
                        {
                            Console.WriteLine(topFrame.LocalNames[varIdx] + " = " + topFrame.Locals[varIdx]);
                        }

                        cursors[taskIdx] = interpreter.Cursor;
                        ++taskIdx;
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
