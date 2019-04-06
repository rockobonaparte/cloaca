using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation;

using NUnit.Framework;


namespace CloacaTests
{
    [TestFixture]
    public class ExceptionTests : RunCodeTest
    {
        [Test]
        [Ignore("Exception handling not implemented")]
        public void RaiseException()
        {
            var interpreter = runProgram("raise Exception('Hello, World!')\n", new Dictionary<string, object>(), 1);
        }

        [Test]
        [Ignore("Exception handling not implemented")]
        public void TryExceptFinally()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception as e:\n" + 
                "  a = a + 10\n" +
                "finally:\n" + 
                "  a = a + 1\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(11)));
        }

        [Test]
        [Ignore("Exception handling not implemented")]
        public void TryExceptFinallyElse()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  a = 1" +
                "except Exception as e:\n" +
                "  a = a + 10\n" +
                "else: \n" +
                "  a = a + 1000\n" +
                "finally:\n" +
                "  a = a + 10000\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1101)));
        }
    }
}
