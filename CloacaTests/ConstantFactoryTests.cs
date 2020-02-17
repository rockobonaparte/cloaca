using NUnit.Framework;
using CloacaInterpreter;

using Antlr4.Runtime.Tree;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    public class MockContext : IParseTree
    {
        public string text;

        public MockContext(string text)
        {
            this.text = text;
        }

        public IParseTree Parent => throw new System.NotImplementedException();

        public Interval SourceInterval => throw new System.NotImplementedException();

        public object Payload => throw new System.NotImplementedException();

        public int ChildCount => throw new System.NotImplementedException();

        ITree ITree.Parent => throw new System.NotImplementedException();

        public T Accept<T>(IParseTreeVisitor<T> visitor)
        {
            throw new System.NotImplementedException();
        }

        public IParseTree GetChild(int i)
        {
            throw new System.NotImplementedException();
        }

        public string GetText()
        {
            return text;
        }

        public string ToStringTree(Parser parser)
        {
            throw new System.NotImplementedException();
        }

        public string ToStringTree()
        {
            throw new System.NotImplementedException();
        }

        ITree ITree.GetChild(int i)
        {
            throw new System.NotImplementedException();
        }
    }

    [TestFixture]
    public class ConstantFactoryTests
    {
        [Test]
        public void BasicDoubleQuoteString()
        {
            var context = new MockContext("\"Hello\"");
            PyString result = ConstantsFactory.CreateString(context);
            Assert.That(result, Is.EqualTo(PyString.Create("Hello")));
        }

        [Test]
        public void BasicSingleQuoteString()
        {
            var context = new MockContext("'Hello'");
            PyString result = ConstantsFactory.CreateString(context);
            Assert.That(result, Is.EqualTo(PyString.Create("Hello")));
        }

        [Test]
        public void LongString()
        {
            var context = new MockContext("\"\"\"Hello\"\"\"");
            PyString result = ConstantsFactory.CreateString(context);
            Assert.That(result, Is.EqualTo(PyString.Create("Hello")));
        }

        [Test]
        public void Bools()
        {
            Assert.That(ConstantsFactory.CreateBool(new MockContext("True")), Is.EqualTo(PyBool.True));
            Assert.That(ConstantsFactory.CreateBool(new MockContext("False")), Is.EqualTo(PyBool.False));
        }
    }
}
