using System;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    class PrintContainer
    {
        public StringBuilder accumulatedOutput;
        public PrintContainer()
        {
            accumulatedOutput = new StringBuilder();
        }

        public async Task PrintFunc(IInterpreter interpreter, FrameContext context, object to_print)
        {
            if (to_print is PyObject)
            {
                var str_func = (IPyCallable) ((PyObject) to_print).__getattribute__(PyClass.__STR__);

                var returned = await str_func.Call(interpreter, context, new object[0]);
                if (returned != null)
                {
                    var asPyString = (PyString)returned;
                    accumulatedOutput.Append(asPyString.InternalValue);
                }
            }
            else
            {
                accumulatedOutput.Append(to_print.ToString());
            }
        }

        public void AddAsBuiltin(Repl repl)
        {
            repl.Interpreter.AddBuiltin(new WrappedCodeObject("print", typeof(PrintContainer).GetMethod("PrintFunc"), this));
        }

        public string GetOutput()
        {
            return accumulatedOutput.ToString();
        }
    }

    [TestFixture]
    public class ReplTests : RunCodeTest
    {
        private async Task<string> tickAwait(Task<String> scheduled, Repl repl)
        {
            repl.Scheduler.Tick();
            return await scheduled;
        }

        [Test]
        public async Task BasicRepl()
        {
            var repl = new Repl();
            var replAwaiter = repl.InterpretAsync("a = 1\n");
            string consoleOut = await tickAwait(replAwaiter, repl);
            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.Empty);
            var variables = repl.ContextVariables;
            Assert.That(variables.Count, Is.EqualTo(2));                // Also contains __name__
            Assert.That(variables["a"], Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task SetAndQueryVariable()
        {
            var repl = new Repl();
            var consoleContainer = new PrintContainer();
            consoleContainer.AddAsBuiltin(repl);

            var replAwaiter = repl.InterpretAsync("a = 1\n");
            string consoleOut = await tickAwait(replAwaiter, repl);
            replAwaiter = repl.InterpretAsync("a\n");
            consoleOut = await tickAwait(replAwaiter, repl);

            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleContainer.GetOutput(), Is.EqualTo("1"));
            var variables = repl.ContextVariables;
            Assert.That(variables.Count, Is.EqualTo(2));                // Also contains __name__
            Assert.That(variables["a"], Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task Dir()
        {
            var repl = new Repl();
            var consoleContainer = new PrintContainer();
            consoleContainer.AddAsBuiltin(repl);

            var replAwaiter = repl.InterpretAsync("a = 1\n");
            string consoleOut = await tickAwait(replAwaiter, repl);
            replAwaiter = repl.InterpretAsync("dir(a)\n");
            consoleOut = await tickAwait(replAwaiter, repl);

            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            // Ultimately I'm not sure how correct this is since I think it's showing more than it needs, but this is what
            // we get using the current means of copying class functions over to PyMethods in the child.            
            Assert.That(consoleContainer.GetOutput(), Is.EqualTo("[__add__, __and__, __call__, __delattr__, __eq__, __floordiv__, __ge__, __getattr__, __getattribute__, __gt__, __index__, __init__, __le__, __lshift__, __lt__, __ltgt__, __mod__, __mul__, __ne__, __neg__, __or__, __pow__, __repr__, __rshift__, __setattr__, __str__, __sub__, __truediv__, __xor__]"));
        }

        // This is a fussy situation related to parsing from single_input. The ANTLR grammar we are using strips
        // newlines from multiline strings. However, we had to fix it so it didn't also strip trailing newlines that
        // we need for the single_input rule. So make sure we don't regress if we play with that, we'll make sure we
        // can still properly parse a string spread across lines.
        [Test]
        public async Task MultilineList()
        {
            var repl = new Repl();
            var consoleContainer = new PrintContainer();
            consoleContainer.AddAsBuiltin(repl);

            var replAwaiter = repl.InterpretAsync("a = [\n" +
                                                  "1,\n" +
                                                  "\n" +
                                                  "2,\n" +
                                                  "3\n" +
                                                  "]\n");
            string consoleOut = await tickAwait(replAwaiter, repl);
            replAwaiter = repl.InterpretAsync("a\n");
            consoleOut = await tickAwait(replAwaiter, repl);

            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleContainer.GetOutput(), Is.EqualTo("[1, 2, 3]"));
        }

        [Test]
        public async Task DontPrintNoneReturns()
        {
            var repl = new Repl();
            var consoleContainer = new PrintContainer();
            consoleContainer.AddAsBuiltin(repl);
            var replAwaiter = repl.InterpretAsync("def does_nothing():\n" +
                                                  "  pass\n" +
                                                  "\n");
            string consoleOut = await tickAwait(replAwaiter, repl);
            replAwaiter = repl.InterpretAsync("does_nothing()\n");
            consoleOut = await tickAwait(replAwaiter, repl);
            Assert.That(consoleContainer.GetOutput(), Is.Empty);
        }

        [Test]
        public async Task BasicSyntaxError()
        {
            var repl = new Repl();
            var consoleContainer = new PrintContainer();
            consoleContainer.AddAsBuiltin(repl);
            string consoleOut = await repl.InterpretAsync("]\n");
            Assert.That(repl.CaughtError, Is.True);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.EqualTo("There were errors trying to compile the script. We cannot run it:" +
                                                Environment.NewLine +
                                                "line 1:0 extraneous input ']' expecting {'del', 'pass', 'break', 'continue', 'return', 'raise', 'from', 'import', '...', 'global', 'nonlocal', 'assert', 'if', 'while', 'for', 'try', 'with', 'lambda', 'not', '~', 'None', 'True', 'False', 'class', 'yield', STRING, NUMBER, 'wait', 'def', '*', '@', '+', '-', NAME, NEWLINE, '(', '[', '{', ASYNC, AWAIT}" +
                                                Environment.NewLine));
        }

        [Test]
        public async Task Exception()
        {
            var repl = new Repl();
            var consoleContainer = new PrintContainer();
            consoleContainer.AddAsBuiltin(repl);
            var replAwaiter = repl.InterpretAsync("raise Exception(\"Hi!\")\n");
            string consoleOut = await tickAwait(replAwaiter, repl);
            Assert.That(repl.CaughtError, Is.True);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.EqualTo("Traceback (most recent call list):" + Environment.NewLine +
                                               "\tline 1, in <module>" + Environment.NewLine +
                                               "Hi!" + Environment.NewLine));
        }
    }
}
