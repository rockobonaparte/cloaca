using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaTests
{
    public delegate void SomeEventType(int aNumber);

    public class TestExtractClass
    {
        public event SomeEventType SomeEvent;
        public int SomeField;

        public static int StaticInt = 0;

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

    // Add an extension method to TestExtractClass so we can test that too
    public static class TestExtractClassExtensions
    {
        public static int AnExtensionMethod(this TestExtractClass testClass)
        {
            return testClass.SomeField;
        }

        public static int AGenericExtensionMethod<T>(this TestExtractClass testClass)
        {
            return testClass.SomeField;
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
        public void ExtractExtensionMethod()
        {
            var extracted = ObjectResolver.GetValue("AnExtensionMethod", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }

        [Test]
        public void ExtractGenericExtensionMethod()
        {
            var extracted = ObjectResolver.GetValue("AGenericExtensionMethod", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }

        [Test]
        public void ExtractEvent()
        {
            var extracted = ObjectResolver.GetValue("SomeEvent", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }

        [Test]
        public void ExtractStaticField()
        {
            var extracted = ObjectResolver.GetValue("StaticInt", new TestExtractClass());
            Assert.IsNotNull(extracted);
        }

        [Test]
        public void ExtractStaticFieldFromType()
        {
            var extracted = ObjectResolver.GetValue("StaticInt", typeof(TestExtractClass));
            Assert.IsNotNull(extracted);
        }
    }

    [TestFixture]
    public class ObjectAssignmentTests
    {
        [Test]
        public void AssignField()
        {
            var testInstance = new TestExtractClass();
            Assert.That(testInstance.SomeField, Is.EqualTo(0));
            ObjectResolver.SetValue("SomeField", testInstance, 3);
            Assert.That(testInstance.SomeField, Is.EqualTo(3));
        }
    }
}
