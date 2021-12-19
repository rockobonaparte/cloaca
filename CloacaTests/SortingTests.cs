using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using CloacaInterpreter;

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
    }
}
