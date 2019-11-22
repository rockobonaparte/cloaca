using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;


namespace CloacaTests
{
    [TestFixture]
    public class DataStructureTests : RunCodeTest
    {
        [Test]
        public void DeclareBasicDictionary()
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
            var interpreter = runProgram("a = { \"foo\": \"bar\", \"number\": 1 }\n", new Dictionary<string, object>(), 1);
            var variables = interpreter.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new Dictionary<PyString, object> {
                { new PyString("foo"), new PyString("bar") },
                { new PyString("number"), new PyInteger(1) }
            }));
        }

        [Test]
        public void DictionaryReadWrite()
        {
            var interpreter = runProgram("a = { \"foo\": 1, \"bar\": 2 }\n" +
                "b = a[\"foo\"]\n" +
                "a[\"bar\"] = 200\n", new Dictionary<string, object>(), 1);
            var variables = interpreter.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new Dictionary<PyString, object> {
                { new PyString("foo"), new PyInteger(1) },
                { new PyString("bar"), new PyInteger(200) }
            }));
            Assert.That(variables.ContainsKey("b"));
            Assert.That(variables["b"], Is.EqualTo(new PyInteger(1)));
        }

        [Test]
        public void ListReadWrite()
        {
            var interpreter = runProgram("a = [1, 2]\n" +
                "b = a[0]\n" +
                "a[1] = 200\n", new Dictionary<string, object>(), 1);
            var variables = interpreter.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new List<object> { new PyInteger(1), new PyInteger(200) }));
            Assert.That(variables.ContainsKey("b"));
            Assert.That(variables["b"], Is.EqualTo(new PyInteger(1)));
        }

        [Test]
        public void DeclareBasicTuple()
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
            var interpreter = runProgram("a = (\"foo\", 1)\n", new Dictionary<string, object>(), 1);
            var variables = interpreter.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var tuple = (PyTuple) variables["a"];
            Assert.That(tuple.values, Is.EquivalentTo(new object[] { new PyString("foo"), new PyInteger(1) }));
        }

        [Test]
        public void DeclareBasicList()
        {
            // 2 0 LOAD_CONST               1 ('foo')
            //   2 LOAD_CONST               2(1)
            //   4 BUILD_LIST               2
            //   6 STORE_FAST               0(a)
            var interpreter = runProgram("a = [\"foo\", 1]\n", new Dictionary<string, object>(), 1);
            var variables = interpreter.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            List<object> referenceList = new List<object>();
            referenceList.Add(new PyString("foo"));
            referenceList.Add(new PyInteger(1));
            var list = (List<object>)variables["a"];
            Assert.That(list, Is.EquivalentTo(referenceList));
        }
    }
}
