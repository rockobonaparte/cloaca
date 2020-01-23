using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaTests
{
    public class TestExtractClass
    {
        public void SimpleMethod()
        {

        }

        public int OverriddenMethod()
        {
            return 100;
        }
        public int OverriddenMethod(string dontcare)
        {
            return 101;
        }
    }

    [TestFixture]
    public class ObjectExtractionTests
    {
        [Test]
        public void ExtractSimpleMethod()
        {
            var extracted = ObjectResolver.GetValue("SimpleMethod", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }

        [Test]
        public void ExtractOverriddenMethod()
        {
            Assert.Throws(typeof(EscapedPyException), () =>
            {
                var extracted = ObjectResolver.GetValue("OverriddenMethod", new TestExtractClass());
                Assert.IsNotNull(extracted);
            }, "'TestExtractClass' object attribute named 'OverriddenMethod' is a method overridden multiple ways, which we cannot yet wrap.");
        }
    }
}
