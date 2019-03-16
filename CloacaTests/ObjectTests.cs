using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation;

using NUnit.Framework;


namespace CloacaTests
{
    [TestFixture]
    public class ObjectTests : RunCodeTest
    {
        [Test]
        public void DeclareClass()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   pass\n", new Dictionary<string, object>(), 1);
        }
    }
}
