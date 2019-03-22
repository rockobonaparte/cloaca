using System;
using System.Collections.Generic;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;
using NUnit.Framework;


namespace CloacaTests
{
    [TestFixture]
    public class RunCodeTest
    {
        protected Interpreter runProgram(string program, Dictionary<string, object> variablesIn, int expectedIterations)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var context = parser.program();

            Assert.That(errorListener.Errors.Count, Is.Zero, "There were parse errors:\n" + errorListener.Report());

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

            Dictionary<string, object> variables = null;
            int runCount = 0;

            // This was busted apart when the interpreter was exposed to injected calls.
            // TODO: Re-enable to get those juicy coroutines!
            //while (!interpreter.Terminated)
            //{
            //    interpreter.Run();
            //    runCount += 1;
            //}

            // Fallback code
            interpreter.Run();
            runCount += 1;

            variables = (Dictionary<string, object>)interpreter.DumpVariables();

            Assert.That(runCount, Is.EqualTo(expectedIterations));
            return interpreter;
        }

        protected void runBasicTest(string program, Dictionary<string, object> variablesIn, Dictionary<string, object> expectedVariables, int expectedIterations,
            string[] ignoreVariables)
        {
            var interpreter = runProgram(program, variablesIn, expectedIterations);
            var variables = interpreter.DumpVariables();

            if (ignoreVariables.Length == 0)
            {
                CollectionAssert.AreEquivalent(expectedVariables, variables);
            }
            else
            {
                foreach (var key in expectedVariables.Keys)
                {
                    Assert.That(variables[key], Is.EqualTo(expectedVariables[key]));
                }
                foreach (string ignored in ignoreVariables)
                {
                    Assert.That(variables.ContainsKey(ignored));
                }
            }
        }

        protected void runBasicTest(string program, Dictionary<string, object> variablesIn, Dictionary<string, object> expectedVariables, int expectedIterations)
        {
            runBasicTest(program, variablesIn, expectedVariables, expectedIterations, new string[0]);
        }


        protected void runBasicTest(string program, Dictionary<string, object> expectedVariables, int expectedIterations)
        {
            runBasicTest(program, new Dictionary<string, object>(), expectedVariables, expectedIterations, new string[0]);
        }

        protected void runBasicTest(string program, Dictionary<string, object> expectedVariables, int expectedIterations, string[] ignoreVariables)
        {
            runBasicTest(program, new Dictionary<string, object>(), expectedVariables, expectedIterations, ignoreVariables);
        }
    }

    /// <summary>
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
}
