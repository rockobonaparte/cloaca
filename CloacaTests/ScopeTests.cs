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

        // Python 3:
        //  3           0 LOAD_CONST               1 (2)
        //              2 STORE_GLOBAL             0 (a)
        //
        //  4           4 LOAD_GLOBAL              0 (a)
        //              6 LOAD_CONST               1 (2)
        //              8 COMPARE_OP               2 (==)
        //             10 POP_JUMP_IF_FALSE       16
        //
        //  5          12 LOAD_CONST               0 (None)
        //             14 RETURN_VALUE
        //
        //  6     >>   16 LOAD_CONST               2 (3)
        //             18 STORE_GLOBAL             0 (a)
        //             20 LOAD_CONST               0 (None)
        //             22 RETURN_VALUE
        //
        //
        // Actual:
        //  4           0  LOAD_CONST              0 (2)
        //              3  STORE_GLOBAL            0 (a)
        //
        //  5           6  LOAD_GLOBAL             0 (a)
        //              9  LOAD_CONST              1 (2)
        //             12  COMPARE_OP              0 (0)
        //
        //  7          15  POP_JUMP_IF_FALSE        18
        //             18  LOAD_CONST              2 (3)
        //             21  STORE_GLOBAL            0 (a)
        //             24  RETURN_VALUE  
        //
        /// <summary>
        /// This one was added when I found out something wasn't working with returns inside conditionals. It actually
        /// looks like the code wasn't even getting generated correctly.
        /// </summary>
        [Test]
        [Ignore("Exposed this during Unity testing. A fix is necessary. It looks to be a problem in code generation!")]
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
