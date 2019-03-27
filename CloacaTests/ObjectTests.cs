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

        [Test]
        public void DeclareAndCreateClassNoConstructor()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   pass\n" +
                                         "bar = Foo()\n", new Dictionary<string, object>(), 1);
            var variables = interpreter.DumpVariables();
            Assert.That(variables, Contains.Key("bar"));
        }

        [Test]
        public void DeclareAndCreateClassDefaultConstructor()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      pass\n" +
                                         "bar = Foo()\n", new Dictionary<string, object>(), 1);

            var variables = new VariableMultimap(interpreter);
            Assert.That(variables.ContainsKey("bar"));
            var bar = variables.Get("bar", typeof(PyObject));
        }

        [Test]
        public void DeclareConstructor()
        {
            var interpreter = runProgram("a = 1\n" +
                                         "class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      global a\n" +
                                         "      a = 2\n" +
                                         "\n" +
                                         "bar = Foo()\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var reference = new VariableMultimap(new TupleList<string, object> {
                { "a", new BigInteger(2) }
            });
            Assert.DoesNotThrow(() => variables.AssertSubsetEquals(reference));
        }

        [Test]
        [Ignore("Class members have not been enabled yet. (but we have started generating code for it!)")]
        public void DeclareClassMember()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "bar = Foo()\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
        }

        [Test]
        [Ignore("Class members have not been enabled yet")]
        public void AccessClassMember()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "bar = Foo()\n" + 
                                         "b = bar.a\n" +
                                         "bar.a = 2\n", new Dictionary<string, object>(), 1);
            // TODO: Figure out how to extract "a"
        }

        [Test]
        [Ignore("Class methods have not been enabled yet")]
        public void AccessClassMethod()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "   def change_a(new_a):\n"+
                                         "      self.a = new_a\n" +
                                         "\n" +
                                         "bar = Foo()\n" +
                                         "b = bar.a\n" +
                                         "bar.change_a(2)\n", new Dictionary<string, object>(), 1);
            // TODO: Figure out how to extract "a"
        }
    }
}
