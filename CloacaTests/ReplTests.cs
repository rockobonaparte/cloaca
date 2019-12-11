using System;
using System.Threading.Tasks;

using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    [TestFixture]
    public class ReplTests : RunCodeTest
    {
        [Test]
        public async Task BasicRepl()
        {
            var repl = new Repl();
            string consoleOut = await repl.Interpret("a = 1\n");
            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.Empty);
            var variables = repl.ContextVariables;
            Assert.That(variables.Count, Is.EqualTo(1));
            Assert.That(variables["a"], Is.EqualTo(new PyInteger(1)));
        }

        [Test]
        public async Task SetAndQueryVariable()
        {
            var repl = new Repl();
            string consoleOut = await repl.Interpret("a = 1\n");
            consoleOut = await repl.Interpret("a\n");
            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.EqualTo("1\r\n"));
            var variables = repl.ContextVariables;
            Assert.That(variables.Count, Is.EqualTo(1));
            Assert.That(variables["a"], Is.EqualTo(new PyInteger(1)));
        }

        [Test]
        public async Task Dir()
        {
            var repl = new Repl();
            string consoleOut = await repl.Interpret("a = 1\n");
            consoleOut = await repl.Interpret("dir(a)\n");
            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.EqualTo("[__add__, __div__, __eq__, __ge__, __gt__, __init__, __le__, __lt__, __ltgt__, __mul__, __ne__, __repr__, __str__, __sub__]\r\n"));
        }

        // This is a fussy situation related to parsing from single_input. The ANTLR grammar we are using strips
        // newlines from multiline strings. However, we had to fix it so it didn't also strip trailing newlines that
        // we need for the single_input rule. So make sure we don't regress if we play with that, we'll make sure we
        // can still properly parse a string spread across lines.
        [Test]
        public async Task MultilineList()
        {
            var repl = new Repl();
            string consoleOut = await repl.Interpret("a = [\n" +
                                                    "1,\n" +
                                                    "\n" +
                                                    "2,\n" +
                                                    "3\n" +
                                                    "]\n");
            consoleOut = await repl.Interpret("a\n");
            Assert.That(repl.CaughtError, Is.False);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.EqualTo("[1, 2, 3]\r\n"));
        }

        [Test]
        public async Task BasicSyntaxError()
        {
            var repl = new Repl();
            string consoleOut = await repl.Interpret("]\n");
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
            string consoleOut = await repl.Interpret("raise Exception(\"Hi!\")\n");
            Assert.That(repl.CaughtError, Is.True);
            Assert.That(repl.NeedsMoreInput, Is.False);
            Assert.That(consoleOut, Is.EqualTo("Traceback (most recent call list):" + Environment.NewLine +
                                               "\tline 1, in <module>" + Environment.NewLine +
                                               "Hi!" + Environment.NewLine));
        }
    }
}
