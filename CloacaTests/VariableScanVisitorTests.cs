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
        [Test]
        public void Hello()
        {
            string program = "a = 1\n";

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

            Assert.That(names.Count, Is.EqualTo(1));
            Assert.That(names[0], Is.EqualTo("a"));
        }
    }
}
