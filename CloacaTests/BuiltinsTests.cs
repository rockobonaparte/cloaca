using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using System.Threading.Tasks;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaTests
{
    [TestFixture]
    public class BuiltinTests : RunCodeTest
    {
        [Test]
        public async Task IsSubclass()
        {
            var context = await runProgram("class Foo:\n" +
                                           "   def __init__(self):\n" +
                                           "      self.a = 1\n" +
                                           "\n" +
                                           "class Bar(Foo):\n" +
                                           "   def change_a(self, new_a):\n" +
                                           "      self.a = self.a + new_a\n" +
                                           "\n" +
                                           "class Unrelated:\n" +
                                           "   pass\n" +
                                           "\n" +
                                           "bar = Bar()\n" +
                                           "class_class = issubclass(Bar, Foo)\n" +
                                           "unrelated_class_class = issubclass(Unrelated, Foo)\n" +
                                           "obj_class = issubclass(type(bar), Foo)\n" +
                                           "unrelated_obj_class = issubclass(type(bar), Unrelated)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var class_class = (bool)variables.Get("class_class");
            var obj_class = (bool)variables.Get("obj_class");
            var unrelated_class_class = (bool)variables.Get("unrelated_class_class");
            var unrelated_obj_class = (bool)variables.Get("unrelated_obj_class");
            Assert.That(class_class, Is.True);
            Assert.That(obj_class, Is.True);
            Assert.That(unrelated_class_class, Is.False);
            Assert.That(unrelated_obj_class, Is.False);
        }

        [Test]
        public async Task NumericStringConversions()
        {
            await runBasicTest(
                "a_string = '1'\n" +
                "as_int = int(a_string)\n" +
                "as_float = float(a_string)\n" +
                "as_bool = bool(a_string)\n" +
                "int_str = str(1)\n" +
                "float_str = str(1.0)\n" +
                "bool_str = str(True)\n",
                new VariableMultimap(new TupleList<string, object>
            {
                { "as_int", PyInteger.Create(1) },
                { "as_float", PyFloat.Create(1.0) },
                { "as_bool", PyBool.True },
                { "int_str", PyString.Create("1") },
                { "float_str", PyString.Create("1.0") },
                { "bool_str", PyString.Create("True") },
            }), 1);
        }

        [Test]
        public async Task LenFunction()
        {
            var dictin = PyDict.Create();
            dictin.InternalDict[PyString.Create("1")] = PyInteger.Create(1);
            dictin.InternalDict[PyString.Create("2")] = PyInteger.Create(2);

            await runBasicTest(
                "listout = len(listin)\n" +
                "dictout = len(dictin)\n" +
                "tupleout = len(tuplein)\n" +
                "strout = len(strin)\n" +
                "rangeout = len(rangein)\n" +
                "arrayout = len(arrayin)\n" +
                "enumerableout = len(enumerablein)\n" +
                "dotnetstrout = len(dotnetstrin)\n",     // I think this should be IEnumerable but I'm not taking chances
            new Dictionary<string, object>()
            {
                { "listin", PyList.Create(new List<object>() { PyInteger.Create(1) }) },
                { "dictin", dictin },
                { "tuplein", PyTuple.Create(new object[] {1, 2, 3 }) },
                { "strin", PyString.Create("1234") },
                { "rangein", PyRange.Create(5, 0, 1) },
                { "arrayin", new int[] {1,2,3,4,5,6 } },
                { "enumerablein", new List<int>() {1,2,3,4,5,6,7 } },
                { "dotnetstrin", "12345678" },
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "listout", PyInteger.Create(1) },
                { "dictout", PyInteger.Create(2) },
                { "tupleout", PyInteger.Create(3) },
                { "strout", PyInteger.Create(4) },
                { "rangeout", PyInteger.Create(5) },
                { "arrayout", PyInteger.Create(6) },
                { "enumerableout", PyInteger.Create(7) },
                { "dotnetstrout", PyInteger.Create(8) },
            }), 1);
        }

        // This is in builtins because __name__ is a "built-in variable"
        [Test]
        public async Task __name__()
        {
            await runBasicTest(
                "name = __name__\n",
                new VariableMultimap(new TupleList<string, object>
            {
                { "name", PyString.Create("__main__") },
            }), 1);
        }

        /// <summary>
        /// This particularly makes sure that __name__ is presented to the compiler for when it sets up defaults.
        /// </summary>
        [Test]
        public async Task __name__def_default()
        {
            await runBasicTest(
                "def returns_name(name=__name__):\n" +
                "   return name\n" +
                "name = returns_name()\n",
                new VariableMultimap(new TupleList<string, object>
            {
                { "name", PyString.Create("__main__") },
            }), 2);
        }

        [Test]
        public async Task reversed_general()
        {
            List<object> referenceList = new List<object>();
            referenceList.Add(PyInteger.Create(2));
            referenceList.Add(PyInteger.Create(1));
            var assertRPyList = PyList.Create(referenceList);

            await runBasicTest(
                "r = []\n" + 
                "for rev in reversed([1,2]):\n" +
                "  r.append(rev)\n",
                new VariableMultimap(new TupleList<string, object>
            {
                { "r", assertRPyList },
            }), 1);

        }

        [Test]
        public async Task zip_general()
        {
            List<object> referenceList = new List<object>();
            referenceList.Add(PyInteger.Create(4));
            referenceList.Add(PyInteger.Create(1));
            referenceList.Add(PyInteger.Create(5));
            referenceList.Add(PyInteger.Create(2));
            var assertResultPyList = PyList.Create(referenceList);

            await runBasicTest(
                "result = []\n" +
                "for a, b in zip([1,2,3],[4,5]):\n" +
                "  result.append(b)\n" +
                "  result.append(a)\n",
                new VariableMultimap(new TupleList<string, object>
            {
                { "result", assertResultPyList },
            }), 1);

        }

        [Test]
        public async Task Range3()
        {
            var runContext = await runProgram(
                "test_range = range(0, 2, 1)\n" +
                "itr = test_range.__iter__()\n" +
                "i0 = itr.__next__()\n" +
                "i1 = itr.__next__()\n" +       // Should raise StopIterationException on following __next__()
                "i2 = itr.__next__()\n", new Dictionary<string, object>(), 1, false);

            Assert.NotNull(runContext.CurrentException);
            Assert.That(runContext.CurrentException.GetType(), Is.EqualTo(typeof(StopIteration)));

            var variables = new VariableMultimap(runContext);
            var i0 = (PyInteger)variables.Get("i0");
            var i1 = (PyInteger)variables.Get("i1");
            Assert.That(i0, Is.EqualTo(PyInteger.Create(0)));
            Assert.That(i1, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task Range2()
        {
            var runContext = await runProgram(
                "test_range = range(1, 100)\n" +
                "itr = test_range.__iter__()\n", 
                new Dictionary<string, object>(), 1, false);

            var variables = new VariableMultimap(runContext);
            var itr = (PyRangeIterator)variables.Get("itr");
            Assert.That(itr.Start, Is.EqualTo(1));
            Assert.That(itr.Stop, Is.EqualTo(100));
            Assert.That(itr.Step, Is.EqualTo(1));
        }

        [Test]
        public async Task Range1()
        {
            var runContext = await runProgram(
                "test_range = range(100)\n" +
                "itr = test_range.__iter__()\n",
                new Dictionary<string, object>(), 1, false);

            var variables = new VariableMultimap(runContext);
            var itr = (PyRangeIterator)variables.Get("itr");
            Assert.That(itr.Start, Is.EqualTo(0));
            Assert.That(itr.Stop, Is.EqualTo(100));
            Assert.That(itr.Step, Is.EqualTo(1));
        }

        [Test]
        public async Task Range1List()
        {
            var runContext = await runProgram(
                "l = list(range(1))\n",
                new Dictionary<string, object>(), 1, false);

            var variables = new VariableMultimap(runContext);
            var l = (PyList)variables.Get("l");

            Assert.That(l.list[0], Is.EqualTo(PyInteger.Create(0)));
        }

        [Test]
        public async Task ReversedRange()
        {
            var runContext = await runProgram(
                "test_range = reversed(range(3))\n" +
                "itr = test_range.__iter__()\n" +
                "i0 = itr.__next__()\n" +
                "i1 = itr.__next__()\n" +       // Should raise StopIterationException on following __next__()
                "i2 = itr.__next__()\n" +
                "i3 = itr.__next__()\n", new Dictionary<string, object>(), 1, false);

            Assert.NotNull(runContext.CurrentException);
            Assert.That(runContext.CurrentException.GetType(), Is.EqualTo(typeof(StopIteration)));

            var variables = new VariableMultimap(runContext);
            var i0 = (PyInteger)variables.Get("i0");
            var i1 = (PyInteger)variables.Get("i1");
            var i2 = (PyInteger)variables.Get("i2");
            Assert.That(i0, Is.EqualTo(PyInteger.Create(2)));
            Assert.That(i1, Is.EqualTo(PyInteger.Create(1)));
            Assert.That(i2, Is.EqualTo(PyInteger.Create(0)));
        }

        [Test]
        public async Task MinSingleElement()
        {
            var runContext = await runProgram(
                "m = min([100])\n",
                new Dictionary<string, object>(), 1, false);

            var variables = new VariableMultimap(runContext);
            var m = (PyInteger)variables.Get("m");

            Assert.That(m, Is.EqualTo(PyInteger.Create(100)));
        }

        [Test]
        public async Task MinMultipleElements()
        {
            var runContext = await runProgram(
                "a = min([1, 2, 3])\n" +
                "b = min([3, 2, 1])\n",
                new Dictionary<string, object>(), 1, false);

            var variables = new VariableMultimap(runContext);
            var a = (PyInteger)variables.Get("a");
            var b = (PyInteger)variables.Get("b");

            Assert.That(a, Is.EqualTo(PyInteger.Create(1)));
            Assert.That(b, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task MaxSingleElement()
        {
            var runContext = await runProgram(
                "m = max([100])\n",
                new Dictionary<string, object>(), 1, false);

            var variables = new VariableMultimap(runContext);
            var m = (PyInteger)variables.Get("m");

            Assert.That(m, Is.EqualTo(PyInteger.Create(100)));
        }

        [Test]
        public async Task MaxMultipleElements()
        {
            var runContext = await runProgram(
                "a = max([1, 2, 3])\n" +
                "b = max([3, 2, 1])\n",
                new Dictionary<string, object>(), 1, false);

            var variables = new VariableMultimap(runContext);
            var a = (PyInteger)variables.Get("a");
            var b = (PyInteger)variables.Get("b");

            Assert.That(a, Is.EqualTo(PyInteger.Create(3)));
            Assert.That(b, Is.EqualTo(PyInteger.Create(3)));
        }
    }
}
