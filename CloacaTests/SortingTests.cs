using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation.DataTypes;
using LanguageImplementation;

namespace CloacaTests
{
    [TestFixture]
    public class SortingTests
    {
        [Test]
        public void BasicSorting()
        {
            int[] test0 = { };
            int[] test1 = { 1 };
            int[] test2 = { 2, 1 };
            int[] test3 = { 3, 2, 1 };
            int[] test4 = { 4, 3, 2, 1 };
            int[] test5 = { 5, 4, 3, 2, 1 };
            int[] test6 = { 6, 5, 4, 3, 2, 1 };
            int[] test7 = { 7, 6, 5, 4, 3, 2, 1 };
            int[] test8 = { 8, 7, 6, 5, 4, 3, 2, 1 };
            Sorting.Sort(test0);
            Sorting.Sort(test1);
            Sorting.Sort(test2);
            Sorting.Sort(test3);
            Sorting.Sort(test4);
            Sorting.Sort(test5);
            Sorting.Sort(test6);
            Sorting.Sort(test7);
            Sorting.Sort(test8);
            Assert.That(test0, Is.EqualTo(new int[] { }));
            Assert.That(test1, Is.EqualTo(new int[] { 1 }));
            Assert.That(test2, Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(test3, Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(test4, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
            Assert.That(test5, Is.EqualTo(new int[] { 1, 2, 3, 4, 5 }));
            Assert.That(test6, Is.EqualTo(new int[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(test7, Is.EqualTo(new int[] { 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(test8, Is.EqualTo(new int[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        }

        [Test]
        public void ExhaustiveSorting()
        {
            Random rand = new Random();
            int[] test_sizes = new int[] { 7, 8, 9 };

            for (int test_i = 0; test_i < test_sizes.Length; ++test_i)
            {
                int test_size = test_sizes[test_i];
                for (int tries = 0; tries < 100; ++tries)
                {
                    var test_array = new int[test_size];
                    for (int i = 0; i < test_array.Length; ++i)
                    {
                        test_array[i] = rand.Next();
                    }
                    var reference = new int[test_size];
                    Array.Copy(test_array, reference, test_size);
                    Array.Sort(reference);
                    Sorting.Sort(test_array);
                    Assert.That(test_array, Is.EqualTo(reference));
                }
            }
        }

        [Test]
        public async Task BasicSortingPyType()
        {
            var i1 = PyInteger.Create(1);
            var i2 = PyInteger.Create(2);
            var i3 = PyInteger.Create(3);
            var i4 = PyInteger.Create(4);
            var i5 = PyInteger.Create(5);
            var i6 = PyInteger.Create(6);
            var i7 = PyInteger.Create(7);
            var i8 = PyInteger.Create(8);

            var test0 = PyList.Create();
            var test1 = PyList.Create(new List<object> { i1 });
            var test2 = PyList.Create(new List<object> { i1, i2 });
            var test3 = PyList.Create(new List<object> { i1, i2, i3 });
            var test4 = PyList.Create(new List<object> { i1, i2, i3, i4 });
            var test5 = PyList.Create(new List<object> { i1, i2, i3, i4, i5 });
            var test6 = PyList.Create(new List<object> { i1, i2, i3, i4, i5, i6 });
            var test7 = PyList.Create(new List<object> { i1, i2, i3, i4, i5, i6, i7 });
            var test8 = PyList.Create(new List<object> { i1, i2, i3, i4, i5, i6, i7, i8 });

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            var context = new FrameContext();

            await Sorting.Sort(interpreter, context, test0.list);
            await Sorting.Sort(interpreter, context, test1.list);
            await Sorting.Sort(interpreter, context, test2.list);
            await Sorting.Sort(interpreter, context, test3.list);
            await Sorting.Sort(interpreter, context, test4.list);
            await Sorting.Sort(interpreter, context, test5.list);
            await Sorting.Sort(interpreter, context, test6.list);
            await Sorting.Sort(interpreter, context, test7.list);
            await Sorting.Sort(interpreter, context, test8.list);
            Assert.That(test0.list.ToArray(), Is.EqualTo(new PyInteger[] { }));
            Assert.That(test1.list.ToArray(), Is.EqualTo(new PyInteger[] { i1 }));
            Assert.That(test2.list.ToArray(), Is.EqualTo(new PyInteger[] { i1, i2 }));
            Assert.That(test3.list.ToArray(), Is.EqualTo(new PyInteger[] { i1, i2, i3 }));
            Assert.That(test4.list.ToArray(), Is.EqualTo(new PyInteger[] { i1, i2, i3, i4 }));
            Assert.That(test5.list.ToArray(), Is.EqualTo(new PyInteger[] { i1, i2, i3, i4, i5 }));
            Assert.That(test6.list.ToArray(), Is.EqualTo(new PyInteger[] { i1, i2, i3, i4, i5, i6 }));
            Assert.That(test7.list.ToArray(), Is.EqualTo(new PyInteger[] { i1, i2, i3, i4, i5, i6, i7 }));
            Assert.That(test8.list.ToArray(), Is.EqualTo(new PyInteger[] { i1, i2, i3, i4, i5, i6, i7, i8 }));
        }
    }

    [TestFixture]
    public class SortingCodeTests : RunCodeTest
    {
        [Test]
        public async Task BasicSort()
        {
            await runBasicTest(
                "a = sorted([3, 2, 1])\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(1), PyInteger.Create(2), PyInteger.Create(3) }) },
            }), 1);
        }

        [Test]
        public async Task ReversedSort()
        {
            await runBasicTest(
                "a = sorted([1, 2, 3], reversed=True)\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(3), PyInteger.Create(2), PyInteger.Create(1) }) },
            }), 1);
        }

        [Test]
        public async Task KeyedSort()
        {
            await runBasicTest(
                "def keyfunc(x):\n" +
                "   return 100-x\n" +
                "\n" +
                "a = sorted([1, 2, 3], key=keyfunc)\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(3), PyInteger.Create(2), PyInteger.Create(1) }) },
            }), 1);
        }

        [Test]
        public async Task KeyedReversedSort()
        {
            await runBasicTest(
                "def keyfunc(x):\n" +
                "   return 100-x\n" +
                "\n" +
                "a = sorted([2, 1, 3], key=keyfunc, reversed=True)\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(1), PyInteger.Create(2), PyInteger.Create(3) }) },
            }), 1);
        }

    }
}
