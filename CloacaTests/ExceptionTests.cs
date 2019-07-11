using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation;

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

        // TryExceptBlank, TryExceptTyped, and TryExceptAliasBasic work their way up to a more and more advanced except block
        // They all have the same effect; this mostly just makes sure we don't totally choke on them.
        // Other tests will ensure we properly qualify the type of exception and use the value.
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
        public void TryExceptTyped()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception:\n" +
                "  a = 10\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(10)));
        }

        [Test]
        [Ignore("Raising from an Exception class currently not supported")]
        public void RaiseFromClass()
        {
            var interpreter = runProgram(
                "a = False\n" +
                "try:\n" +
                "  raise Exception\n" +
                "except Exception:\n" +
                "  a = True\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (bool)variables.Get("a");
            Assert.That(a, Is.True);
        }

        [Test]
        public void TryExceptAliasBasic()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception as e:\n" +
                "  a = 10\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(10)));
        }
        [Test]
        public void TryExceptFinally()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception:\n" +
                "  a = 10\n" +
                "finally:\n" +
                "  a = a + 1\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(11)));
        }

        [Test]
        public void TryUnhandledFinally()
        {
            FrameContext runContext = null;

            Assert.Throws<EscapedPyException>(
              () => {
                  runProgram(
                    "a = 0\n" +
                    "try:\n" +
                    "  raise Exception('Hello, World!')\n" +
                    "finally:\n" +
                    "  a = 1\n", new Dictionary<string, object>(), 1, out runContext);
              }, "Hello, World!");

            var variables = new VariableMultimap(runContext);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1)));
        }

        [Test]
        public void TryExceptElse()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  a = 1\n" +
                "except Exception as e:\n" +
                "  a = a + 10\n" +
                "else:\n" +
                "  a = a + 100\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(101)));
        }

        [Test]
        public void TryExceptFinallyElse()
        {
            var interpreter = runProgram(
                "a = 0\n" +
                "try:\n" +
                "  a = 1\n" +
                "except Exception as e:\n" +
                "  a = a + 10\n" +
                "else: \n" +
                "  a = a + 100\n" +
                "finally:\n" +
                "  a = a + 1000\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1101)));
        }

        [Test]
        [Ignore("Need to implement str()")]
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
                "  a = e.number\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1)));
        }

        [Test]
        public void TryExceptTwoExceptions()
        {
            var interpreter = runProgram(
                "class MeowException(Exception):\n" +
                "  def __init__(self, number):\n" +
                "    self.number = number\n" +
                "a = 0\n" +
                "try:\n" +
                "  raise MeowException(1)\n" +
                "except Exception as ignored:\n" +
                "  a = -1\n" +
                "except MeowException as e:\n" +
                "  a = e.number\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(interpreter);
            var a = (BigInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(new BigInteger(1)));
        }
    }
}
