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

using CloacaInterpreter;
using Language;
using LanguageImplementation;

namespace CloacaGuiDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string program = "a = 1";

            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);
            if (errorListener.Errors.Count > 0)
            {
                Console.WriteLine("There were errors trying to compile the script. We cannot run it.");
                return;
            }

            var antlrVisitorContext = parser.program();

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
    }
}
