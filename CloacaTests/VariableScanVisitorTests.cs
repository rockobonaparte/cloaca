using Antlr4.Runtime;
using NUnit.Framework;

using Language;
using LanguageImplementation;
using System.Collections.Generic;
using System;

namespace CloacaTests
{
    [TestFixture]
    class VariableScanVisitorTests
    {
        public void RunTest(string program, string[] in_names, string[] globals=null)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.file_input();

            errorListener.AssertNoErrors();

            var visitor = new VariableScanVisitor(globals ?? new string[0]);
            visitor.Visit(antlrVisitorContext);

            var rootNamesKeys = visitor.RootNode.NamedScopes.Keys;
            var rootNames = new string[rootNamesKeys.Count];
            var enumerator = rootNamesKeys.GetEnumerator();
            for(int i = 0; i < rootNames.Length; ++i)
            {
                enumerator.MoveNext();
                rootNames[i] = enumerator.Current;
            }
            Array.Sort(rootNames);
            Array.Sort(in_names);

            Assert.That(rootNames, Is.EqualTo(in_names));
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
