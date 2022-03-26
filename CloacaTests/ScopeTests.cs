using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using CloacaInterpreter.ModuleImporting;
using LanguageImplementation.DataTypes;

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

        /// <summary>
        /// We found out that globals were not being bound to their modules correctly. If you called a function
        /// in a module that called another function inside that module, we couldn't resolve it because the
        /// interpreter didn't recognize that the module should have its globals active (as opposed to the parent's
        /// globals). This test checks against that.
        /// </summary>
        [Test]
        public async Task ModulesCallsIntoItself()
        {
            var repo = new StringCodeModuleFinder();
            repo.CodeLookup.Add("foo", 
                "def terminal_call():\n" +
                "   return 1\n" +
                "\n" +
                "def outer_call():\n" +
                "   return terminal_call()\n" +
                "\n");

            var context = await runProgram(
                "import foo\n" +
                "bar = foo.outer_call()\n",
                new Dictionary<string, object>(),
                new List<ISpecFinder>() { repo },
                1);

            Assert.That(context.HasVariable("bar"), Is.EqualTo(true));
            var bar = context.GetVariable("bar");
            Assert.That(bar, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task InnerFunctionReadsOuter()
        {
            string program =
                "def outer():\n" +
                "  a = 100\n" +
                "  def inner():\n" +
                "    return a + 1\n" +
                "  b = inner()\n" +
                "  return b\n" +
                "c = outer()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "c", PyInteger.Create(101) }
                }), 1);
        }

        [Test]
        public async Task InnerFunctionWritersOuterNonlocal()
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

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyInteger.Create(201) }
                }), 1);
        }
    }
}
