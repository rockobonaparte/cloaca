using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation.DataTypes;
using LanguageImplementation;
using System.Threading.Tasks;

namespace CloacaTests
{
    [TestFixture]
    public class ExceptionTests : RunCodeTest
    {
        [Test]
        public async Task RaiseException()
        {
            Assert.ThrowsAsync(typeof(EscapedPyException), async () =>
            {
                var context = await runProgram("raise Exception('Hello, World!')\n", new Dictionary<string, object>(), 1);
            }, "Hello, World!");
        }

        // TryExceptBlank, TryExceptTyped, and TryExceptAliasBasic work their way up to a more and more advanced except block
        // They all have the same effect; this mostly just makes sure we don't totally choke on them.
        // Other tests will ensure we properly qualify the type of exception and use the value.
        [Test]
        public async Task TryExceptBlank()
        {
            var context = await runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except:\n" +
                "  a = 10\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(10)));
        }

        [Test]
        public async Task TryExceptTyped()
        {
            var context = await runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception:\n" +
                "  a = 10\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(10)));
        }

        [Test]
        [Ignore("Raising from an Exception class currently not supported")]
        public async Task RaiseFromClass()
        {
            var context = await runProgram(
                "a = False\n" +
                "try:\n" +
                "  raise Exception\n" +
                "except Exception:\n" +
                "  a = True\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (bool)variables.Get("a");
            Assert.That(a, Is.True);
        }

        [Test]
        public async Task TryExceptAliasBasic()
        {
            var context = await runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception as e:\n" +
                "  a = 10\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(10)));
        }
        [Test]
        public async Task TryExceptFinally()
        {
            var context = await runProgram(
                "a = 0\n" +
                "try:\n" +
                "  raise Exception('Hello, World!')\n" +
                "except Exception:\n" +
                "  a = 10\n" +
                "finally:\n" +
                "  a = a + 1\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(11)));
        }

        [Test]
        public async Task TryUnhandledFinally()
        {
            FrameContext runContext = null;

            Assert.ThrowsAsync<EscapedPyException>(
              async () => {
                  runContext = await runProgram(
                    "a = 0\n" +
                    "try:\n" +
                    "  raise Exception('Hello, World!')\n" +
                    "finally:\n" +
                    "  a = 1\n", new Dictionary<string, object>(), 1);
              }, "Hello, World!");

            var variables = new VariableMultimap(runContext);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task TryExceptElse()
        {
            var context = await runProgram(
                "a = 0\n" +
                "try:\n" +
                "  a = 1\n" +
                "except Exception as e:\n" +
                "  a = a + 10\n" +
                "else:\n" +
                "  a = a + 100\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(101)));
        }

        [Test]
        public async Task TryExceptFinallyElse()
        {
            var context = await runProgram(
                "a = 0\n" +
                "try:\n" +
                "  a = 1\n" +
                "except Exception as e:\n" +
                "  a = a + 10\n" +
                "else: \n" +
                "  a = a + 100\n" +
                "finally:\n" +
                "  a = a + 1000\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(1101)));
        }

        [Test]
        [Ignore("Need to implement str()")]
        public async Task TryExceptAliasUseMessage()
        {
            var context = await runProgram(
                "a = 'Fail'\n" +
                "try:\n" +
                "  raise Exception('Pass')\n" +
                "except Exception as e:\n" +
                "  a = str(e)\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo("Pass"));
        }

        [Test]
        public async Task TryExceptAliasUseValue()
        {
            var context = await runProgram(
                "class MeowException(Exception):\n" +
                "  def __init__(self, number):\n" +
                "    self.number = number\n" +
                "a = 0\n" +
                "try:\n" +
                "  raise MeowException(1)\n" +
                "except MeowException as e:\n" +
                "  a = e.number\n", new Dictionary<string, object>(), 1);
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        public async Task TryExceptTwoExceptions()
        {
            var context = await runProgram(
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
            var variables = new VariableMultimap(context);
            var a = (PyInteger)variables.Get("a");
            Assert.That(a, Is.EqualTo(PyInteger.Create(1)));
        }
    }

    /// <summary>
    /// Same test case over and over, but we will assert different subcategories.
    /// </summary>
    [TestFixture]
    public class ExceptionInternalsTests : RunCodeTest
    {
        private EscapedPyException escaped;

        [SetUp]
        public async Task RunMainTestCase()
        {
            try
            {
                var ignored = runProgram(
                    "class MeowException(Exception):\n" +
                    "  def __init__(self, number):\n" +
                    "    self.number = number\n" +
                    "\n" +
                    "def meow_loudly():\n" +
                    "   raise MeowException(1)\n" +
                    "\n" +
                    "meow_loudly()\n", new Dictionary<string, object>(), 1);
            }
            catch(EscapedPyException e)
            {
                escaped = e;
            }            
        }

        [Test]
        public void SanityTest()
        {
            Assert.That(escaped, Is.Not.Null);
        }

        [Test]
        public void TraceBackObject()
        {
            Assert.That(escaped.OriginalException.__dict__, Contains.Key(PyException.TracebackName));
            var tb = escaped.OriginalException.__dict__[PyException.TracebackName];
        }

        [Test]
        public void Message()
        {
            Assert.That(escaped.Message, Is.EqualTo("Traceback (most recent call list):\r\n" +
                                                    "\tline 6, in meow_loudly\r\n"));
        }
    }
}
