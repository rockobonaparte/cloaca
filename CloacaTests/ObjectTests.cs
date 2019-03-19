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
        [Ignore("Have not enabled custom class constructors yet")]
        public void DeclareConstructor()
        {
            runBasicTest("a = 1\n" +
                         "class Foo:\n" +
                         "   def __init__(self):\n" +
                         "      a = 2\n", 
                         new Dictionary<string, object>(),
                         new Dictionary<string, object>
                         {
                             { "a", new BigInteger(2) }
                         },
                         1);
        }

        [Test]
        [Ignore("Class members have not been enabled yet")]
        public void DeclareClassMember()
        {
            var interpreter = runProgram("class Foo:\n" +
                                         "   def __init__(self):\n" +
                                         "      self.a = 1\n" +
                                         "\n" +
                                         "bar = Foo()\n", new Dictionary<string, object>(), 1);
            // TODO: Figure out how to extract "a"
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
