using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using CloacaInterpreter;

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
            else if (IsFontInstalled("lucida console"))
            {
                richTextBox1.Font = new Font("lucida console", 12, FontStyle.Regular);
            }

            repl = new Repl();
            ongoingUserProgram = new StringBuilder();

            richTextBox1.AppendText(">>> ");
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

        private async void WhenKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                richTextBox1.AppendText(Environment.NewLine);
                string newUserInput = richTextBox1.Text.Substring(lastAnchorPosition, richTextBox1.Text.Length - lastAnchorPosition);
                ongoingUserProgram.Append(newUserInput);

                var output = await repl.Interpret(ongoingUserProgram.ToString());
                if (repl.NeedsMoreInput)
                {
                    richTextBox1.AppendText("... ");
                }
                else
                {
                    if(repl.CaughtException)
                    {
                        richTextBox1.SelectionColor = Color.Red;
                        richTextBox1.AppendText(output);
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                    }
                    else
                    {
                        richTextBox1.Text += output;
                    }
                    ongoingUserProgram.Clear();
                    richTextBox1.AppendText(Environment.NewLine + ">>> ");
                }

                SetCursorToEnd();
                e.Handled = true;
            }
        }
    }
}
