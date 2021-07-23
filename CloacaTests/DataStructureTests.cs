using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using System.Threading.Tasks;

namespace CloacaTests
{
    [TestFixture]
    public class DataStructureTests : RunCodeTest
    {
        [Test]
        public async Task DeclareBasicDictionary()
        {
            // The peephole optimizer figures out basic, constant dictionaries and diverts them to
            // BUILD_CONST_KEY_MAP, so I have to use a more obtuse example here to show BUILD_MAP.
            // return_foo() just returns "foo":
            //
            // >>> def dict_name_maker():
            // ...   return {return_foo(): "bar", "number": 1}
            // ...
            // >>> dis.dis(dict_name_maker)
            //   2           0 LOAD_GLOBAL              0 (return_foo)
            //               2 CALL_FUNCTION            0
            //               4 LOAD_CONST               1 ('bar')
            //               6 LOAD_CONST               2 ('number')
            //               8 LOAD_CONST               3 (1)
            //              10 BUILD_MAP                2
            //              12 RETURN_VALUE
            // 
            var context = await runProgram("a = { \"foo\": \"bar\", \"number\": 1 }\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new Dictionary<PyString, object> {
                { PyString.Create("foo"), PyString.Create("bar") },
                { PyString.Create("number"), PyInteger.Create(1) }
            }));
        }

        [Test]
        public async Task DictionaryReadWrite()
        {
            var context = await runProgram("a = { \"foo\": 1, \"bar\": 2 }\n" +
                "b = a[\"foo\"]\n" +
                "a[\"bar\"] = 200\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new Dictionary<PyString, object> {
                { PyString.Create("foo"), PyInteger.Create(1) },
                { PyString.Create("bar"), PyInteger.Create(200) }
            }));
            Assert.That(variables.ContainsKey("b"));
            Assert.That(variables["b"], Is.EqualTo(PyInteger.Create(1)));
        }


        [Test]
        public async Task IndexIntoDictWithVariable()
        {
            var context = await runProgram("a = { \"foo\": 1, \"bar\": 2 }\n" +
                                         "b = \"foo\"\n" +
                                         "c = a[b]\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("c"));
            var element = (PyInteger)variables["c"];
            Assert.That(element, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task ListReadWrite()
        {
            var context = await runProgram("a = [1, 2]\n" +
                "b = a[0]\n" +
                "a[1] = 200\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new List<object> { PyInteger.Create(1), PyInteger.Create(200) }));
            Assert.That(variables.ContainsKey("b"));
            Assert.That(variables["b"], Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task DeclareBasicTuple()
        {
            // The peephole optimizer for reference Python code can turn the constants straight into a tuple.
            // That's a huge pain, so here's an example that throws in some math to show how it goes under
            // the hood.
            //
            // >>> def big_tuple(x):
            // ...   a = (x + 1, x + 2)
            // ...   return a
            // ...
            // >>> dis.dis(big_tuple)
            // 2 0 LOAD_FAST                0(x)
            //   2 LOAD_CONST               1(1)
            //   4 BINARY_ADD
            //   6 LOAD_FAST                0(x)
            //   8 LOAD_CONST               2(2)
            //   10 BINARY_ADD
            //   12 BUILD_TUPLE              2
            //   14 STORE_FAST               1(a)
            // 
            //   3          16 LOAD_FAST                1(a)
            //   18 RETURN_VALUE
            var context = await runProgram("a = (\"foo\", 1)\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var tuple = (PyTuple) variables["a"];
            Assert.That(tuple.Values, Is.EquivalentTo(new object[] { PyString.Create("foo"), PyInteger.Create(1) }));
        }

        [Test]
        public async Task DeclareSingleElementTuple()
        {
            var context = await runProgram("a = (\"foo\",)\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var tuple = (PyTuple)variables["a"];
            Assert.That(tuple.Values, Is.EquivalentTo(new object[] { PyString.Create("foo") }));
        }

        [Test]
        public async Task DeclareBasicList()
        {
            // 2 0 LOAD_CONST               1 ('foo')
            //   2 LOAD_CONST               2(1)
            //   4 BUILD_LIST               2
            //   6 STORE_FAST               0(a)
            var context = await runProgram("a = [\"foo\", 1]\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            List<object> referenceList = new List<object>();
            referenceList.Add(PyString.Create("foo"));
            referenceList.Add(PyInteger.Create(1));
            var list = (PyList)variables["a"];
            Assert.That(list, Is.EquivalentTo(referenceList));
        }

        [Test]
        public async Task DeclareEmptyList()
        {
            var context = await runProgram("a = []\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            List<object> referenceList = new List<object>();
            var list = (PyList)variables["a"];
            Assert.That(list, Is.EquivalentTo(referenceList));
        }

        [Test]
        public async Task DeclareNewlineBasicList()
        {
            // This scenario stumbles into a problem with parsing where NEWLINE tokens can get inserted into the list declaration.
            // I'm having some fusses with that online so I wanted to put in a test to ensure whatever I do with the grammar
            // to make the REPL work doesn't also break this situation.
            var context = await runProgram("a = [\n" +
                                         "\n" +
                                         "# Look at me, I'm a riot!\n" +
                                         "1\n" +
                                         "]\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            List<object> referenceList = new List<object>();
            referenceList.Add(PyInteger.Create(1));
            var list = (PyList)variables["a"];
            Assert.That(list, Is.EquivalentTo(referenceList));
        }

        [Test]
        public async Task IndexIntoListWithVariable()
        {
            var context = await runProgram("a = [\"foo\", 1]\n" +
                                         "b = 0\n" +
                                         "c = a[b]\n", new Dictionary<string, object>(), 1);
            var variables = context.DumpVariables();
            Assert.That(variables.ContainsKey("c"));
            var element = (PyString)variables["c"];
            Assert.That(element, Is.EqualTo(PyString.Create("foo")));
        }
    }
}
