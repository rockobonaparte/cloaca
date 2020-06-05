using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    [TestFixture]
    public class ForLoopTests : RunCodeTest
    {
        // Range is just an object with __getitem__ that runs the whole range. Raises IndexError when it goes out of bounds.
        [Test]
        [Ignore("Range not yet implemented")]
        public void Range()
        {
            runBasicTest(
                "test_range = range(0, 2, 1)\n" +
                "raised_exception = False\n" +
                "i0 = test_range(0)\n" +
                "i1 = test_range(1)\n" +
                "try:\n" +
                "   i2 = test_range(2)\n" +
                "except IndexError:\n" +
                "   raised_exception = True\n", new VariableMultimap(new TupleList<string, object>
            {
                { "i0", PyInteger.Create(0) },
                { "i1", PyInteger.Create(1) },
                { "raised_exception", PyBool.True },
            }), 1);
        }

        [Test]
        [Ignore("For-loops yet implemented")]
        public void ForLoopRange()
        {
            runBasicTest(
                "a = 0\n" +
                "for i in range(0, 10, 1):\n\n" +
                "   a += i\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9) }
            }), 1);
        }

        [Test]
        [Ignore("For-loops of lists not yet implemented")]
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
        [Ignore("For-loops of dicts not yet implemented")]
        public void ForInDict()
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
        [Ignore("For-loops of dicts not yet implemented")]
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
        [Ignore("For-loops of dicts not yet implemented")]
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
