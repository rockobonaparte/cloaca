using NUnit.Framework;

using LanguageImplementation.DataTypes;
using System.Threading.Tasks;

namespace CloacaTests
{
    [TestFixture]
    public class ScopeTests : RunCodeTest
    {
        [Test]
        public async Task InnerAndOuterScopesLocal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   a = 2\n" +
                "foo()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(1) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task InnerGlobal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   global a\n" +
                "   a = 2\n" +
                "foo()\n";

            await runBasicTest(program,
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
        public async Task ConditionalReturn()
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

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task ReturnsNoneProperly()
        {
            string program =
                "def inner():\n" +
                "  return 3\n" +
                "\n" +
                "def outer():\n" +
                "  inner()\n" +
                "\n" +
                "a = outer()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", NoneType.Instance }
                }), 1, new string[] { "inner", "outer" });
        }

        [Test]
        public async Task ImplicitlyUsesGlobal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   b = a + 1\n" +
                "   return b\n" +
                "a = foo()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task GlobalSingle()
        {
            await runBasicTest(
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
        public async Task GlobalMulti()
        {
            await runBasicTest(
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
