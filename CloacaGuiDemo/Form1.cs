using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

namespace CloacaGuiDemo
{
    public partial class Form1 : Form
    {
        // TODO: Remove after debugging parsing crap
        public RichTextBox rtb_debug
        {
            get
            {
                return richTextBox1;
            }
        }

        Repl repl;
        private int lastAnchorPosition;
        private StringBuilder ongoingUserProgram;
        public Form1()
        {
            InitializeComponent();
        }

        // Checking if a font exists
        // https://stackoverflow.com/questions/113989/test-if-a-font-is-installed
        private static bool IsFontInstalled(string fontName)
        {
            using (var testFont = new Font(fontName, 8))
            {
                return 0 == string.Compare(
                  fontName,
                  testFont.Name,
                  StringComparison.InvariantCultureIgnoreCase);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Let's go down a list of fonts I like. If we somehow have NONE of these then we just go with whatever the default
            // is. It's probably variable-width, but we also likely won't fall all the way through either.
            if (IsFontInstalled("consolas"))
            {
                richTextBox1.Font = new Font("consolas", 12, FontStyle.Regular);
            }
            else if (IsFontInstalled("liberation mono"))
            {
                richTextBox1.Font = new Font("liberation mono", 12, FontStyle.Regular);
            }
            else if(IsFontInstalled("lucida console"))
            {
                richTextBox1.Font = new Font("lucida console", 12, FontStyle.Regular);
            }

            repl = new Repl();
            ongoingUserProgram = new StringBuilder();

            richTextBox1.Text += ">>> ";
            SetCursorToEnd();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // TODO: Revert to null
        public void SetCursorToEnd()
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            lastAnchorPosition = richTextBox1.Text.Length;
        }

        private void WhenKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                richTextBox1.Text += Environment.NewLine;
                string newUserInput = richTextBox1.Text.Substring(lastAnchorPosition, richTextBox1.Text.Length - lastAnchorPosition);
                ongoingUserProgram.Append(newUserInput);

                //richTextBox1.Text += ongoingUserProgram.ToString();
                //SetCursorToEnd();

                var output = repl.Interpret(ongoingUserProgram.ToString(), this);
                if (repl.NeedsMoreInput)
                {
                    richTextBox1.Text += "... ";
                }
                else
                {
                    richTextBox1.Text += output;
                    ongoingUserProgram.Clear();
                    richTextBox1.Text += Environment.NewLine + ">>> ";
                }

                SetCursorToEnd();
                e.Handled = true;
            }
        }
    }

    public class ReplParseErrorListener : IParserErrorListener
    {
        public List<string> Errors;
        public Form1 gui;
        public bool ExpectedMoreText
        {
            get; private set;
        }

        public bool ReplMode;

        public ReplParseErrorListener()
        {
            Errors = new List<string>();
            ExpectedMoreText = false;
            ReplMode = false;
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
            //if(gui != null)
            //{
            //    gui.rtb_debug.Text += msg + Environment.NewLine;
            //    gui.SetCursorToEnd();
            //}

            if (e != null)
            {
                var expected_tokens = e.GetExpectedTokens();
                if (ReplMode && expected_tokens.Count > 0 && (expected_tokens.Contains(CloacaParser.INDENT) || expected_tokens.Contains(CloacaParser.NEWLINE)))
                {
                    // Eat the error if it's just complaining that it expected more text in the REPL.
                    ExpectedMoreText = true;
                    return;
                }
            }
            else if(offendingSymbol.Text == "<EOF>" && charPositionInLine == 0)
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

        public Repl()
        {
            errorListener = new ReplParseErrorListener();
        }

        public bool NeedsMoreInput
        {
            get
            {
                return errorListener.ExpectedMoreText;
            }
        }

        public string Interpret(string input, Form1 form)
        {
            var inputStream = new AntlrInputStream(input);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            errorListener.ReplMode = true;
            errorListener.Clear();
            var parser = new CloacaParser(commonTokenStream);
            errorListener.gui = form;
            parser.AddErrorListener(errorListener);
            if (errorListener.Errors.Count > 0)
            {
                StringBuilder errorBuilder = new StringBuilder("There were errors trying to compile the script. We cannot run it:");
                foreach(var error in errorListener.Errors)
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

            if(contextVariables == null)
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
                    ExceptionDispatchInfo.Capture(wrappedEscapedException.InnerExceptions[0]).Throw();
                }
            }

            var stack_output = new StringBuilder();
            foreach(var stack_var in context.DataStack)
            {
                stack_output.Append(stack_var.ToString());
                stack_output.Append(Environment.NewLine);
            }

            contextVariables = context.DumpVariables();
            //foreach (var var_pair in contextVariables)
            //{
            //    if(var_pair.Value != null)
            //    {
            //        stack_output.Append(var_pair.Key + " = " + var_pair.Value.ToString() + Environment.NewLine);
            //    }
            //}

            return stack_output.ToString();
        }
    }

}
