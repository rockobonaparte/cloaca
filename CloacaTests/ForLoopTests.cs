using NUnit.Framework;

using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using System.Collections.Generic;
using LanguageImplementation;
using System.Reflection;

namespace CloacaTests
{
    [TestFixture]
    public class ForLoopTests : RunCodeTest
    {
        [Test]
        public void Range()
        {
            FrameContext runContext = null;

            var exc = Assert.Throws<TargetInvocationException>(
              () => {
                  runProgram(
                "test_range = range(0, 2, 1)\n" +
                "itr = test_range.__iter__()\n" +
                "raised_exception = False\n" +
                "i0 = itr.__next__()\n" +
                "i1 = itr.__next__()\n" +       // Should raise StopIterationException on following __next__()
                "i2 = itr.__next__()\n", new Dictionary<string, object>(), 1, out runContext);
              });

            Assert.That(exc.InnerException.GetType(), Is.EqualTo(typeof(StopIterationException)));

            var variables = new VariableMultimap(runContext);
            var i0 = (PyInteger)variables.Get("i0");
            var i1 = (PyInteger)variables.Get("i1");
            Assert.That(i0, Is.EqualTo(PyInteger.Create(0)));
            Assert.That(i1, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public void ForLoopRange()
        {
            runBasicTest(
                "a = 0\n" +
                "for i in range(0, 10, 1):\n" +
                "   a += i\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9) }
            }), 1);
        }

        [Test]
        public void ForLoopRangeElse()
        {
            runBasicTest(
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
        public void ForLoopRangeBreak()
        {
            runBasicTest(
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
        public void ForLoopRangeContinue()
        {
            runBasicTest(
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
        public void ForInList()
        {
            runBasicTest(
                "testlist = [0, 1, 2]\n" +
                "a = 0\n" +
                "for i in testlist:\n\n" +
                "   a += i\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1 + 2) }
            }), 1);
        }

        [Test]
        public void ForInDictKeys()
        {
            runBasicTest(
                "testdict = {100: 'foo', 200: 'bar'}\n" +
                "a = 0\n" +
                "for key in testdict:\n\n" +
                "   a += key\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(300) }
            }), 1);
        }

        [Test]
        public void ForInDictItems()
        {
            runBasicTest(
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
        public void ForInDictItemsDetupled()
        {
            runBasicTest(
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
        [Ignore("Need to actually stub in")]
        public void ForLoopDotNetArray()
        {

        }

        [Test]
        [Ignore("Need to actually stub in")]
        public void ForLoopDotNetList()
        {

        }

        [Test]
        [Ignore("Need to actually stub in")]
        public void ForLoopDotNetDict()
        {

        }

        [Test]
        [Ignore("Need to actually stub in")]
        public void ForLoopDotNetDictItems()
        {

        }
    }
}
