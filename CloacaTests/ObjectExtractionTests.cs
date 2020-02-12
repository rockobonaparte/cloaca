using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaTests
{
    public delegate void SomeEventType(int aNumber);

    public class TestExtractClass
    {
        public event SomeEventType SomeEvent;

        public void SimpleMethod()
        {

        }

        public int OverloadedMethod()
        {
            return 100;
        }
        public int OverloadedMethod(string dontcare)
        {
            return 101;
        }

        // Will return true if the first character of the parameter type T is even. False otherwise.
        public bool GenericMethod<T>()
        {
            return typeof(T).Name[0] % 2 == 0;
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
        public void ExtractOverloadMethod()
        {
            var extracted = ObjectResolver.GetValue("OverloadedMethod", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }

        [Test]
        public void ExtractGenericMethod()
        {
            var extracted = ObjectResolver.GetValue("GenericMethod", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }

        [Test]
        public void ExtractEvent()
        {
            var extracted = ObjectResolver.GetValue("SomeEvent", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }
    }
}
