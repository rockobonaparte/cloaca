using Antlr4.Runtime;
using NUnit.Framework;

using Language;
using LanguageImplementation;
using System.Collections.Generic;

namespace CloacaTests
{
    [TestFixture]
    class VariableScanVisitorTests
    {
        public void RunTest(string program, string[] in_names)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.file_input();

            errorListener.AssertNoErrors();

            var names = new List<string>();
            var visitor = new VariableScanVisitor(names);
            visitor.Visit(antlrVisitorContext);

            Assert.That(names.ToArray(), Is.EqualTo(in_names));
        }

        [Test]
        public void Hello()
        {
            string program = "a = 1\n";
            RunTest(program, new string[] { "a" });
        }

        [Test]
        public void HelloFinc()
        {
            string program = "a = 1\n" +
                             "def foo():\n" +
                             "   b = 2\n" +
                             "   return b";
            RunTest(program, new string[] { "a", "b" });
        }
    }
}
