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
        public void RunTest(string program, string compare_dump, string[] globals = null)
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

            var result = visitor.RootNode.ToReportString();

            Assert.That(result, Is.EqualTo(compare_dump));
        }

        [Test]
        public void Hello()
        {
            string program = "a = 1\n";
            RunTest(program, "a: Local\n");
        }

        [Test]
        public void HelloFunc()
        {
            string program = "a = 1\n" +
                             "def foo():\n" +
                             "   b = 2\n" +
                             "   return b";
            RunTest(program, "a: Local\n" +
                             "foo:\n" +
                             "  b: Local\n");
        }

        [Test]
        public void FuncParameters()
        {
            string program = "a = 1\n" +
                             "def foo(c):\n" +
                             "   b = 2\n" +
                             "   return b";
            RunTest(program, "a: Local\n" +
                             "foo:\n" +
                             "  b: Local\n" +
                             "  c: Local\n");
        }

        /// <summary>
        /// This came from ScopeTests and was one of two that motivated adding a first-pass to get
        /// variable context.
        /// </summary>        
        [Test]
        public void InnerFunctionReadsOuter()
        {
            string program =
                "def outer():\n" +
                "  a = 100\n" +
                "  def inner():\n" +
                "    return a + 1\n" +
                "  b = inner()\n" +
                "  return b\n" +
                "c = outer()\n";

            RunTest(program, "c: Local\n" +
                             "outer:\n" +
                             "  a: EnclosedRead\n" +
                             "  b: Local\n" +
                             "  inner:\n" +
                             "    a: EnclosedRead\n");
        }

        /// <summary>
        /// This came from ScopeTests and was test two of two that motivated adding a first-pass to get
        /// variable context.
        /// 
        /// It also adds the nonlocal to make variable 'a' writeable.
        /// </summary>
        [Test]
        public void InnerFunctionWritersOuterNonlocal()
        {
            string program =
                "def outer():\n" +
                "  a = 100\n" +
                "  def inner():\n" +
                "    nonlocal a\n" +
                "    a += 1\n" +
                "    return a\n" +
                "  return a + inner()\n" +
                "b = outer()\n";

            // TODO: I think I need additional context to tell if something is enclosed and writable!
            RunTest(program, "b: Local\n" +
                             "outer:\n" +
                             "  a: EnclosedReadWrite\n" +
                             "  inner:\n" +
                             "    a: EnclosedReadWrite\n");
        }

        // TODO: Add a test that tries various grammars to make sure we get variables in things like:
        // for-loops
        // try-except blocks
        // conditionals
        // Arrays
        // Functions inside functions
        // ...and more...
        [Test]
        public void ParseVariousBlocks()
        {
            string program =
                "for for_i in range(10):\n" +
                "  in_for = for_i + 1\n" +
                "try:\n" +
                "  try_var = 3\n" +
                "except Exception as exc_var:" +
                "  exc_blk_var = exc_var\n";

            RunTest(program, "exc_blk_var: Local\n" +
                             "exc_var: Local\n" +
                             "for_i: Local\n" +
                             "in_for: Local\n" +
                             "try_var: Local\n"
                             );
        }
    
    }
}
