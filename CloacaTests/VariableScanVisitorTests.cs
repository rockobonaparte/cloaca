using Antlr4.Runtime;
using NUnit.Framework;

using Language;
using LanguageImplementation;

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


            var visitor = new VariableScanVisitor();
            visitor.Visit(antlrVisitorContext);
        }
    }
}
