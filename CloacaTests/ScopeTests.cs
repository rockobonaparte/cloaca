using NUnit.Framework;

using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    [TestFixture]
    public class ScopeTests : RunCodeTest
    {
        [Test]
        public void InnerAndOuterScopesLocal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   a = 2\n" +
                "foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(1) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void InnerGlobal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   global a\n" +
                "   a = 2\n" +
                "foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });
        }

        /// <summary>
        /// This one was added when I found out something wasn't working with returns inside conditionals. It actually
        /// looks like the code wasn't even getting generated correctly.
        /// 
        /// LOL it turned out I hadn't implemented the return statement =D
        /// </summary>
        [Test]
        public void ConditionalReturn()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   global a\n" +
                "   a = 2\n" +
                "   if a == 2:\n" +
                "      return\n" +
                "   a = 3\n" +
                "\n" +
                "foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void ImplicitlyUsesGlobal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   b = a + 1\n" +
                "   return b\n" +
                "a = foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void GlobalSingle()
        {
            runBasicTest(
                "a = 10\n" +
                "def inner():\n" +
                "  global a\n" +
                "  a = 11\n" +
                "\n" +
                "inner()\n"
                , new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(11) }
            }), 1);
        }

        [Test]
        public void GlobalMulti()
        {
            runBasicTest(
                "a = 10\n" +
                "b = 100\n" +
                "def inner():\n" +
                "  global a, b\n" +
                "  a = 11\n" +
                "  b = 111\n" +
                "\n" +
                "inner()\n"
                , new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(11) },
                { "b", PyInteger.Create(111) },
            }), 1);
        }
    }
}
