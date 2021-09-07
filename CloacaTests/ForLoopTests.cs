using NUnit.Framework;

using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using System.Collections.Generic;
using LanguageImplementation;
using System.Reflection;
using System.Numerics;
using System.Threading.Tasks;

namespace CloacaTests
{
    public class ForLoopTestDotNetContainers
    {
        public int[] IntArray;
        public List<int> IntList;
        public Dictionary<int, int> IntDict;

        public ForLoopTestDotNetContainers()
        {
            IntArray = new int[] { 1, 2, 3 };
            IntList = new List<int>() { 1, 2, 3 };
            IntDict = new Dictionary<int, int>() { { 1, 1000 },
                { 2, 2000 },
                { 3, 3000 }};
        }
    }

    [TestFixture]
    public class ForLoopTests : RunCodeTest
    {
        [Test]
        public async Task Range()
        {
            FrameContext runContext = await runProgram(
                "test_range = range(0, 2, 1)\n" +
                "itr = test_range.__iter__()\n" +
                "raised_exception = False\n" +
                "i0 = itr.__next__()\n" +
                "i1 = itr.__next__()\n" +       // Should raise StopIterationException on following __next__()
                "i2 = itr.__next__()\n", new Dictionary<string, object>(), 1, false);

            // TODO: [Escaped StopIteration] StopIteration (and other Python exceptions thrown in .NET should be caught as regular Python exceptions)
            Assert.NotNull(runContext.EscapedDotNetException);
            Assert.That(runContext.EscapedDotNetException.InnerException.GetType(), Is.EqualTo(typeof(StopIterationException)));

            var variables = new VariableMultimap(runContext);
            var i0 = (PyInteger)variables.Get("i0");
            var i1 = (PyInteger)variables.Get("i1");
            Assert.That(i0, Is.EqualTo(PyInteger.Create(0)));
            Assert.That(i1, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task ForLoopRange()
        {
            await runBasicTest(
                "a = 0\n" +
                "for i in range(0, 10, 1):\n" +
                "   a += i\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9) }
            }), 1);
        }

        [Test]
        public async Task ForLoopRangeElse()
        {
            await runBasicTest(
                "a = 0\n" +
                "for i in range(0, 3, 1):\n" +
                "   a += i\n" +
                "else:\n" +
                "   a += 10\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1 + 2 + 10) }
            }), 1);
        }

        [Test]
        public async Task ForLoopRangeBreak()
        {
            await runBasicTest(
                "a = 0\n" +
                "for i in range(0, 3, 1):\n" +
                "   if i == 2:\n" +
                "      break\n" +
                "   a += i\n" +
                "else:\n" +
                "   a += 10\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1) }
            }), 1);
        }

        [Test]
        public async Task ForLoopRangeContinue()
        {
            await runBasicTest(
                "a = 0\n" +
                "for i in range(0, 3, 1):\n" +
                "   if i == 2:\n" +
                "      continue\n" +
                "   a += i\n" +
                "else:\n" +
                "   a += 10\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1 + 10) }
            }), 1);
        }

        [Test]
        public async Task ForInList()
        {
            await runBasicTest(
                "testlist = [0, 1, 2]\n" +
                "a = 0\n" +
                "for i in testlist:\n\n" +
                "   a += i\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1 + 2) }
            }), 1);
        }

        [Test]
        public async Task ForInDictKeys()
        {
            await runBasicTest(
                "testdict = {100: 'foo', 200: 'bar'}\n" +
                "a = 0\n" +
                "for key in testdict:\n\n" +
                "   a += key\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(300) }
            }), 1);
        }

        [Test]
        public async Task ForInDictItems()
        {
            await runBasicTest(
                "testdict = {100: 1000, 200: 2000}\n" +
                "a = 0\n" +
                "b = 0\n" +
                "for kv in testdict.items():\n\n" +
                "   a += kv[0]\n" +
                "   b += kv[1]\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(300) },
                { "b", PyInteger.Create(3000) }
            }), 1);
        }

        [Test]
        public async Task ForInDictItemsDetupled()
        {
            await runBasicTest(
                "testdict = {100: 1000, 200: 2000}\n" +
                "a = 0\n" +
                "b = 0\n" +
                "for k, v in testdict.items():\n\n" +
                "   a += k\n" +
                "   b += v\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(300) },
                { "b", PyInteger.Create(3000) }
            }), 1);
        }

        [Test]
        public async Task ForLoopDotNetArray()
        {
            await runBasicTest(
                "a = 0\n" +
                "for i in containers.IntArray:\n\n" +
                "   a += i\n", new Dictionary<string, object>()
            {
                { "containers", new ForLoopTestDotNetContainers() }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", new BigInteger(1 + 2 + 3) }
            }), 1);
        }

        [Test]
        public async Task ForLoopDotNetList()
        {
            await runBasicTest(
                "a = 0\n" +
                "for i in containers.IntList:\n\n" +
                "   a += i\n", new Dictionary<string, object>()
            {
                { "containers", new ForLoopTestDotNetContainers() }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", new BigInteger(1 + 2 + 3) }
            }), 1);
        }

        [Test]
        public async Task ForLoopDotNetDict()
        {
            await runBasicTest(
                "a = 0\n" +
                "for i in containers.IntDict.Keys:\n\n" +
                "   a += i\n", new Dictionary<string, object>()
            {
                { "containers", new ForLoopTestDotNetContainers() }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", new BigInteger(1 + 2 + 3) }
            }), 1);
        }

        [Test]
        public async Task ForLoopDotNetDictItems()
        {
            await runBasicTest(
                "a = 0\n" +
                "for kv in containers.IntDict:\n\n" +
                "   a += kv.Key + kv.Value\n", new Dictionary<string, object>()
            {
                { "containers", new ForLoopTestDotNetContainers() }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", new BigInteger(1 + 2 + 3 + 1000 + 2000 + 3000) }
            }), 1);
        }

        /// <summary>
        /// Runs append() inside the loop, which pushes None onto the stack. This will trip up
        /// the for-loop opcodes unless this result is popped. This test checks that things don't
        /// get gummed up.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task ExprStmtPopsOffDatastack()
        {
            await runBasicTest(
                "l = [1,2]\n" +
                "r = []\n" +
                "for n in l:\n" +
                "   r.append(n)\n", new VariableMultimap(new TupleList<string, object>()), 1);
        }
    }
}
