using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;


namespace CloacaTests
{
    [TestFixture]
    public class BuiltinTests : RunCodeTest
    {
        [Test]
        public async void IsSubclass()
        {
            var context = await runProgram("class Foo:\n" +
                                           "   def __init__(self):\n" +
                                           "      self.a = 1\n" +
                                           "\n" +
                                           "class Bar(Foo):\n" +
                                           "   def change_a(self, new_a):\n" +
                                           "      self.a = self.a + new_a\n" +
                                           "\n" +
                                           "class Unrelated:\n" +
                                           "   pass\n" +
                                           "\n" +
                                           "bar = Bar()\n" +
                                           "class_class = issubclass(Bar, Foo)\n" +
                                           "unrelated_class_class = issubclass(Unrelated, Foo)\n" +
                                           "obj_class = issubclass(type(bar), Foo)\n" +
                                           "unrelated_obj_class = issubclass(type(bar), Unrelated)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var class_class = (bool)variables.Get("class_class");
            var obj_class = (bool)variables.Get("obj_class");
            var unrelated_class_class = (bool)variables.Get("unrelated_class_class");
            var unrelated_obj_class = (bool)variables.Get("unrelated_obj_class");
            Assert.That(class_class, Is.True);
            Assert.That(obj_class, Is.True);
            Assert.That(unrelated_class_class, Is.False);
            Assert.That(unrelated_obj_class, Is.False);
        }

        [Test]
        public void NumericStringConversions()
        {
            runBasicTest(
                "a_string = '1'\n" +
                "as_int = int(a_string)\n" +
                "as_float = float(a_string)\n" +
                "as_bool = bool(a_string)\n" +
                "int_str = str(1)\n" +
                "float_str = str(1.0)\n" +
                "bool_str = str(True)\n",
                new VariableMultimap(new TupleList<string, object>
            {
                { "as_int", PyInteger.Create(1) },
                { "as_float", PyFloat.Create(1.0) },
                { "as_bool", PyBool.True },
                { "int_str", PyString.Create("1") },
                { "float_str", PyString.Create("1.0") },
                { "bool_str", PyString.Create("True") },
            }), 1);
        }
    }
}
