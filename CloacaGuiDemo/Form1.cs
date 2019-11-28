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
    public class ReplParseErrorListener : IParserErrorListener
    {
        public List<string> Errors;
        public bool needsMoreText;

        public ReplParseErrorListener()
        {
            Errors = new List<string>();
            needsMoreText = false;
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
            var expected_tokens = e.GetExpectedTokens();
            if(expected_tokens.Count > 0 && expected_tokens.Contains(CloacaParser.INDENT))
            {
                needsMoreText = true;
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
    }


    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //string program = "a = 1\na = a + 1\n";
            // TODO: This shouldn't fully parse. Or rather, we should know that this isn't a complete program and we have more work to do.
            string program = "a = 1\n" + 
                             "if True:\n" +
                             "   if False:\n" +
                             "       a = 2\n";

            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ReplParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);
            if (errorListener.Errors.Count > 0)
            {
                Console.WriteLine("There were errors trying to compile the script. We cannot run it.");
                return;
            }

            var antlrVisitorContext = parser.program();
            if(errorListener.needsMoreText)
            {
                richTextBox1.Text += "Needs more text\n";
            }

            var variablesIn = new Dictionary<string, object>();
            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(antlrVisitorContext);

            CodeObject compiledProgram = visitor.RootProgram.Build();

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            scheduler.SetInterpreter(interpreter);

            var context = scheduler.Schedule(compiledProgram);
            foreach (string varName in variablesIn.Keys)
            {
                context.SetVariable(varName, variablesIn[varName]);
            }

            while(!scheduler.Done)
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

            var variables = context.DumpVariables();
            foreach(var var_pair in variables)
            {
                richTextBox1.Text += var_pair.Key + " = " + var_pair.Value.ToString() + "\n";
            }
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void WhenKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyData == Keys.Enter)
            {
                richTextBox1.Text += "Entered pressed!\n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                e.Handled = true;
            }
        }
    }
}
