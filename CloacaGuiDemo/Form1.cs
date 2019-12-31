using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CloacaInterpreter;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

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
        private bool quitSignaled;

        private Label[] blipLabels;

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
            repl.WhenReplCommandDone += WhenReplDone;
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("print", typeof(Form1).GetMethod("print_func"), this));
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("quit", typeof(Form1).GetMethod("quit_func"), this));
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("set_blip", typeof(Form1).GetMethod("set_blip_wrapper"), this));
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("get_blip", typeof(Form1).GetMethod("get_blip_wrapper"), this));
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("set_pos", typeof(Form1).GetMethod("set_player_pos_wrapper"), this));
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("get_pos", typeof(Form1).GetMethod("get_player_pos_wrapper"), this));
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("sleep", typeof(Form1).GetMethod("sleep_wrapper"), this));
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("dialog", typeof(Form1).GetMethod("dialog_wrapper"), this));
            ongoingUserProgram = new StringBuilder();

            richTextBox1.AppendText(">>> ");
            SetCursorToEnd();

            quitSignaled = false;

            ShowDialogs(new string[]
            {
                "Radio Button 1",
                "Radio Button 2",
                "Radio Button 3",
            });

            blipLabels = new Label[]
            {
                blip0,
                blip1,
                blip2,
                blip3,
            };

            // Set up some background tasks to run.
            // TODO: Send this in through a separate mechanism that doesn't parse this in the REPL but as a regular program.
            //repl.Interpret("while True:\n" +
            //                "   blip = get_blip(0)\n" +
            //                "   blip = not blip\n" +
            //                "   set_blip(0, blip)\n" +
            //                "   sleep(1.0)\n" +
            //                "   set_blip(1, blip)\n\n");
        }

        private async Task mock_sleep_subsystem_daemon(FutureVoidAwaiter future, int sleep_time)
        {
            await Task.Delay(sleep_time).ConfigureAwait(true);
            future.SignalDone();
            repl.Run();             // TODO: Major temporality here. Need to make this more straightforward.
        }

        public async Task<FutureVoidAwaiter> sleep_wrapper(IScheduler scheduler, FrameContext context, PyFloat sleepTime)
        {
            var future = new FutureVoidAwaiter(scheduler, context);
            scheduler.NotifyBlocked(context, future);
            mock_sleep_subsystem_daemon(future, (int)(sleepTime.number * 1000.0m));
            await future;
            return future;
        }

        private PyInteger choicePyInt;
        private FutureAwaiter<PyInteger> dialogFuture;
        public async Task<FutureAwaiter<PyInteger>> dialog_wrapper(IInterpreter interpreter, IScheduler scheduler, FrameContext context, PyString message, PyList choices)
        {
            var choicesStr = new string[choices.list.Count];
            for(int i = 0; i < choices.list.Count; ++i)
            {
                var iPyStr = (PyString) choices.list[i];
                choicesStr[i] = iPyStr.str;
            }
            ShowDialogs(choicesStr);

            // Prep the integer since we have all the things we need to invoke the constructor right here.
            choicePyInt = (PyInteger)await PyIntegerClass.Instance.Call(interpreter, context, new object[0]);
            dialogFuture = new FutureAwaiter<PyInteger>(scheduler, context);

            scheduler.NotifyBlocked(context, dialogFuture);
            //dialogFuture.SetResult(choicePyInt);
            await dialogFuture;
            return dialogFuture;
        }

        public void set_player_pos_wrapper(PyFloat x, PyFloat y)
        {
            playerXLabel.Text = x.number.ToString();
            playerYLabel.Text = y.number.ToString();
        }

        // TODO: Make every part of this much easier. Creating the objects properly will need to be simplified. One-step
        // construction with passed-in values will greatly help. This could be done with a factory, but we should investigate
        // if we can just use the basic type constructors in some way first.
        public async Task<PyTuple> get_player_pos_wrapper(IInterpreter interpreter, FrameContext context)
        {
            PyFloat f1 = (PyFloat) await PyFloatClass.Instance.Call(interpreter, context, new object[0]);
            f1.number = Decimal.Parse(playerXLabel.Text);
            PyFloat f2 = (PyFloat)await PyFloatClass.Instance.Call(interpreter, context, new object[0]);
            f2.number = Decimal.Parse(playerYLabel.Text);

            var tuples = new PyObject[2]
            {
                f1,
                f2
            };
            var new_pytup = (PyTuple) await PyTupleClass.Instance.Call(interpreter, context, new object[0]);
            new_pytup.Values = tuples;
            return new_pytup;
        }

        public void SetBlip(int i, bool value)
        {
            blipLabels[i].Text = value == true ? "1" : "0";
            blipLabels[i].BackColor = value == true ? Color.LightGreen : Color.Red;
        }

        public void set_blip_wrapper(PyInteger i, PyBool value)
        {
            SetBlip((int) i.number, value.boolean);
        }

        public bool GetBlip(int i)
        {
            return blipLabels[i].Text == "1";
        }

        public PyBool get_blip_wrapper(PyInteger i)
        {
            return new PyBool(GetBlip((int)i.number));
        }

        public void ClearDialogs()
        {
            while (dialogRadioFlow.Controls.Count > 0)
            {
                var asRadioButton = (RadioButton)dialogRadioFlow.Controls[0];
                dialogRadioFlow.Controls.Remove(asRadioButton);
                asRadioButton.Dispose();
            }
        }

        public void ShowDialogs(string[] dialogs)
        {
            ClearDialogs();
            foreach(var dialog in dialogs)
            {
                var newRadioButton = new RadioButton() { Text = dialog };
                newRadioButton.Click += (sender, e) => { dialogOkButton.Enabled = true; };
                dialogRadioFlow.Controls.Add(newRadioButton);
            }
            dialogOkButton.Enabled = false;
        }

        public async void print_func(IInterpreter interpreter, FrameContext context, PyObject to_print)
        {
            var str_func = (IPyCallable) to_print.__dict__[PyClass.__STR__];

            var returned = await str_func.Call(interpreter, context, new object[] { to_print });
            if (returned != null)
            {
                var asPyString = (PyString)returned;
                richTextBox1.AppendText(asPyString.str);
                SetCursorToEnd();
            }
        }

        public async void quit_func(IInterpreter interpreter, FrameContext context)
        {
            quitSignaled = true;
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

        private void WhenReplDone(Repl repl, string output)
        {
            if (repl.NeedsMoreInput)
            {
                richTextBox1.AppendText("... ");
            }
            else
            {
                if (repl.CaughtError)
                {
                    richTextBox1.SelectionColor = Color.Red;
                    richTextBox1.AppendText(output);
                    richTextBox1.SelectionColor = richTextBox1.ForeColor;
                }
                else
                {
                    richTextBox1.AppendText(output);
                }
                ongoingUserProgram.Clear();
                richTextBox1.AppendText(Environment.NewLine + ">>> ");
            }
            SetCursorToEnd();
        }

        private async void WhenKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                richTextBox1.AppendText(Environment.NewLine);
                string newUserInput = richTextBox1.Text.Substring(lastAnchorPosition, richTextBox1.Text.Length - lastAnchorPosition);
                ongoingUserProgram.Append(newUserInput);

                // We will get the result of interpretation in the WhenReplDone handler so we don't have to await this!
                repl.Interpret(ongoingUserProgram.ToString());
                e.Handled = true;
            }
            else if(e.KeyData == Keys.Left || e.KeyData == Keys.Back)
            {
                if(richTextBox1.SelectionStart <= lastAnchorPosition)
                {
                    e.Handled = true;
                }
            }
            else if (e.KeyData == Keys.Down || e.KeyData == Keys.Up)
            {
                // Suppress up/down for now, although it might be nice to have them scroll history eventually.
                e.Handled = true;
            }
            else if(e.KeyData == Keys.Home)
            {
                richTextBox1.SelectionStart = lastAnchorPosition;
                e.Handled = true;
            }

            if(quitSignaled)
            {
                Close();
            }
        }

        private void WhenDialogOK_Clicked(object sender, EventArgs e)
        {
            if (dialogFuture != null)
            {
                for(int i = 0; i < dialogRadioFlow.Controls.Count; ++i)
                {
                    var asButton = dialogRadioFlow.Controls[i] as RadioButton;
                    if(asButton.Checked)
                    {
                        choicePyInt.number = i;
                        dialogFuture.SetResult(choicePyInt);
                        repl.Run();             // TODO: Major temporality here. Need to make this more straightforward.
                        break;
                    }
                }
            }
            ClearDialogs();
            dialogOkButton.Enabled = false;
        }
    }
}
