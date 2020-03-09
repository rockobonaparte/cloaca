using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation.DataTypes;

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
        public void DeclareAndCreateClassFrom__call__()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   pass\n" +
                                         "bar = Foo.__call__()\n", new Dictionary<string, object>(), 1);
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
                { "a", PyInteger.Create(2) }
            });
            Assert.DoesNotThrow(() => variables.AssertSubsetEquals(reference));
        }

        [Test]
        public void DeclareConstructorArgument()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self, new_a):\n" +
                                         "      self.a = new_a\n" +
                                         "\n" +
                                         "bar = Foo(2)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var bar = (PyObject)variables.Get("bar");
            Assert.That(bar.__dict__["a"], Is.EqualTo(PyInteger.Create(2)));
        }

        [Test]
        public void DeclareClassMember()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "bar = Foo()\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var bar = (PyObject) variables.Get("bar");
            Assert.That(bar.__dict__["a"], Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public void AccessClassMember()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "bar = Foo()\n" + 
                                         "b = bar.a\n" +
                                         "bar.a = 2\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var bar = (PyObject)variables.Get("bar");
            Assert.That(bar.__dict__["a"], Is.EqualTo(PyInteger.Create(2)));
        }

        [Test]
        public void AccessClassMethod()
        {
            //>>> def make_foo():
            //...   class Foo:
            //...     def __init__(self):
            //...       self.a = 1
            //...     def change_a(self, new_a):
            //...       self.a = new_a
            //...
            //>>> dis(make_foo)
            //  2           0 LOAD_BUILD_CLASS
            //              2 LOAD_CONST               1 (<code object Foo at 0x0000021BD5908D20, file "<stdin>", line 2>)
            //              4 LOAD_CONST               2 ('Foo')
            //              6 MAKE_FUNCTION            0
            //              8 LOAD_CONST               2 ('Foo')
            //             10 CALL_FUNCTION            2
            //             12 STORE_FAST               0 (Foo)
            //             14 LOAD_CONST               0 (None)
            //             16 RETURN_VALUE
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "   def change_a(self, new_a):\n"+
                                         "      self.a = new_a\n" +
                                         "\n" +
                                         "bar = Foo()\n" +
                                         "bar.change_a(2)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var bar = (PyObject)variables.Get("bar");
            Assert.That(bar.__dict__["a"], Is.EqualTo(PyInteger.Create(2)));
        }

        [Test]
        public void SubclassBasic()
        {
            /*
             *   2           0 LOAD_BUILD_CLASS
              2 LOAD_CONST               1 (<code object Foo at 0x000001DBDF5511E0, file "<stdin>", line 2>)
              4 LOAD_CONST               2 ('Foo')
              6 MAKE_FUNCTION            0
              8 LOAD_CONST               2 ('Foo')
             10 CALL_FUNCTION            2
             12 STORE_FAST               0 (Foo)

  5          14 LOAD_BUILD_CLASS
             16 LOAD_CONST               3 (<code object Bar at 0x000001DBDF568390, file "<stdin>", line 5>)
             18 LOAD_CONST               4 ('Bar')
             20 MAKE_FUNCTION            0
             22 LOAD_CONST               4 ('Bar')
             24 LOAD_FAST                0 (Foo)
             26 CALL_FUNCTION            3
             28 STORE_FAST               1 (Bar)

  8          30 LOAD_FAST                1 (Bar)
             32 CALL_FUNCTION            0
             34 STORE_FAST               2 (bar)

  9          36 LOAD_FAST                2 (bar)
             38 LOAD_ATTR                0 (change_a)
             40 LOAD_CONST               5 (1)
             42 CALL_FUNCTION            1
             44 POP_TOP
             46 LOAD_CONST               0 (None)
             48 RETURN_VALUE
             */

            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "class Bar(Foo):\n" +
                                         "   def change_a(self, new_a):\n" +
                                         "      self.a = self.a + new_a\n" +
                                         "\n" +
                                         "bar = Bar()\n" +
                                         "bar.change_a(1)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var bar = (PyObject)variables.Get("bar");
            Assert.That(bar.__dict__["a"], Is.EqualTo(PyInteger.Create(2)));
        }

        [Test]
        public void SubclassSuperconstructor()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "class Bar(Foo):\n" +
                                         "   def __init__(self):\n" +
                                         "      super().__init__()\n" +
                                         "      self.b = 2\n" +
                                         "\n" +
                                         "bar = Bar()\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var bar = (PyObject)variables.Get("bar");
            Assert.That(bar.__dict__["a"], Is.EqualTo(PyInteger.Create(1)));
            Assert.That(bar.__dict__["b"], Is.EqualTo(PyInteger.Create(2)));
        }

        [Test]
        public void ObjectCallsBaseMethods()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   pass\n" +
                                         "\n" +
                                         "f = Foo()\n" +
                                         "testing = f.__eq__(f)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var testing = (PyBool)variables.Get("testing");
            Assert.That(testing, Is.EqualTo(PyBool.True));
        }

        [Test]
        public void IntCallsBaseMethods()
        {
            var interpreter = runProgram("f = 1\n" +
                                         "lt = f.__getattribute__('__lt__')\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var testing = variables.Get("lt");
            Assert.That(testing, Is.Not.Null);
        }
    }
}
