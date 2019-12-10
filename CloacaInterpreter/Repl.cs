using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;

using Language;
using LanguageImplementation;
using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation.DataTypes;
using System.Runtime.ExceptionServices;

namespace CloacaInterpreter
{
    public class ReplParseErrorListener : IParserErrorListener
    {
        public List<string> Errors;
        public bool ExpectedMoreText
        {
            get; private set;
        }

        public ReplParseErrorListener()
        {
            Errors = new List<string>();
            ExpectedMoreText = false;
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
            if (e != null)
            {
                var expected_tokens = e.GetExpectedTokens();
                if ((e.OffendingToken.Type == CloacaLexer.Eof ||
                    (expected_tokens.Count > 0 && (expected_tokens.Contains(CloacaParser.INDENT) || expected_tokens.Contains(CloacaParser.NEWLINE)))))
                {
                    // Eat the error if it's just complaining that it expected more text in the REPL.
                    ExpectedMoreText = true;
                    return;
                }
            }
            else if (offendingSymbol.Text == "<EOF>" && charPositionInLine == 0)
            {
                // Eat the error if it's just complaining that it expected more text in the REPL.
                ExpectedMoreText = true;
                return;
            }
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

        public void Clear()
        {
            Errors.Clear();
            ExpectedMoreText = false;
        }
    }

    public class Repl
    {
        private ReplParseErrorListener errorListener;
        private Dictionary<string, object> contextVariables;

        /// <summary>
        /// Set from Interpret() if the output is a traceback from an uncaught exception. This is particularly
        /// useful if you intend to report the exceptions different--such as with a different color. It gets
        /// reset on the next Interpret() call.
        /// </summary>
        public bool CaughtException
        {
            get; private set;
        }

        public Repl()
        {
            errorListener = new ReplParseErrorListener();
        }

        /// <summary>
        /// Set from Interpret() if the output is the secondary prompt used to get more information. This is
        /// a helper indicator to declare for certain that we're entering the secondary prompt. It gets
        /// reset on the next Interpret() call.
        /// </summary>
        public bool NeedsMoreInput
        {
            get
            {
                return errorListener.ExpectedMoreText;
            }
        }

        public async Task<string> Interpret(string input)
        {
            CaughtException = false;

            var inputStream = new AntlrInputStream(input);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            errorListener.Clear();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);
            if (errorListener.Errors.Count > 0)
            {
                StringBuilder errorBuilder = new StringBuilder("There were errors trying to compile the script. We cannot run it:");
                foreach (var error in errorListener.Errors)
                {
                    errorBuilder.Append(error);
                }
                return errorBuilder.ToString();
            }

            var antlrVisitorContext = parser.single_input();
            if (errorListener.ExpectedMoreText)
            {
                return "... ";
            }

            if (contextVariables == null)
            {
                contextVariables = new Dictionary<string, object>();
            }

            var visitor = new CloacaBytecodeVisitor(contextVariables);
            visitor.Visit(antlrVisitorContext);

            CodeObject compiledProgram = visitor.RootProgram.Build();

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            scheduler.SetInterpreter(interpreter);

            var context = scheduler.Schedule(compiledProgram);
            foreach (string varName in contextVariables.Keys)
            {
                context.SetVariable(varName, contextVariables[varName]);
            }

            while (!scheduler.Done)
            {
                try
                {
                    scheduler.Tick().Wait();
                }
                catch (AggregateException wrappedEscapedException)
                {
                    // Given the nature of exception handling, we should normally only have one of these!
                    var inner = wrappedEscapedException.InnerExceptions[0];
                    if (inner is EscapedPyException)
                    {
                        CaughtException = true;
                        return inner.Message;
                    }
                    else
                    {
                        // Rethrow exceptions that weren't part of the interpreter runtime. These are
                        // crazy, bad exceptions that indicate internal bugs.
                        ExceptionDispatchInfo.Capture(inner).Throw();
                    }
                }
            }

            var stack_output = new StringBuilder();
            foreach (var stack_var in context.DataStack)
            {
                var stack_var_obj = stack_var as PyObject;
                if (stack_var_obj == null || !stack_var_obj.__dict__.ContainsKey(PyClass.__REPR__))
                {
                    stack_output.Append(stack_var.ToString());
                }
                else
                {
                    var __repr__ = stack_var_obj.__dict__[PyClass.__REPR__];
                    var functionToRun = __repr__ as IPyCallable;

                    var returned = await functionToRun.Call(interpreter, context, new object[] { stack_var_obj });
                    if (returned != null)
                    {
                        stack_output.Append(returned);
                    }
                }
                stack_output.Append(Environment.NewLine);
            }

            contextVariables = context.DumpVariables();
            return stack_output.ToString();
        }
    }
}
