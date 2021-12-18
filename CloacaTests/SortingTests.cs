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
            int[] test = { 4, 3, 2, 1 };
            Sorting.Sort(test);
            Assert.That(test, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
        }
    }
}
