using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaTests
{
    [TestFixture]
    public class ExceptionTests : RunCodeTest
    {
        [Test]
        public void RaiseException()
        {
            Assert.Throws(typeof(EscapedPyException), () =>
            {
                var interpreter = runProgram("raise Exception('Hello, World!')\n", new Dictionary<string, object>(), 1);
            }, "Hello, World!");
        }

        [Test]
        public void TryExceptBlank()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except:\n" +
                "  a = 10\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(10)));
        }

        [Test]
        [Ignore("Exception handling not implemented")]
        public void TryExceptFinally()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception:\n" +
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
                "  a = a + 100\n" +
                "finally:\n" +
                "  a = a + 1000\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1111)));
        }

        [Test]
        [Ignore("Exception handling not implemented")]
        public void TryExceptAliasBasic()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception as e:\n" +
                "  a = a + 10\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(11)));
        }

        [Test]
        [Ignore("Exception handling not implemented")]
        public void TryExceptAliasUseMessage()
        {
            var interpreter = runProgram(
                "a = 'Fail'\n" +
                "try:\n" +
                "  raise Exception('Pass')\n" +
                "except Exception as e:\n" +
                "  a = str(e)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo("Pass"));
        }

        [Test]
        [Ignore("Exception handling not implemented; subclassing not implemented yet.")]
        public void TryExceptAliasUseValue()
        {
            var interpreter = runProgram(
                "class MeowException(Exception):\n" +
                "  def __init__(self, number):\n" +
                "    self.number = number\n" +
                "a = 0\n" +
                "try:\n" +
                "  raise MeowException(1)\n" +
                "except MeowException as e:\n" +
                "  a = e\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1)));
        }

        [Test]
        [Ignore("Exception handling not implemented; subclassing not implemented yet.")]
        public void TryExceptTwoExceptions()
        {
            var interpreter = runProgram(
                "class MeowException(Exception):\n" +
                "  def __init__(self, number):\n" +
                "    self.number = number\n" +
                "a = 0\n" +
                "try:\n" +
                "  raise MeowException(1)\n" +
                "except Exception as ignored\n" +
                "  a = ignored\n" +
                "except MeowException as e:\n" +
                "  a = e\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1)));
        }
    }
}
