using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    [TestFixture]
    public class StringTests : RunCodeTest
    {
        [Test]
        public async Task StringConcatenation()
        {
            // Making sure that we're properly parsing and generating all of these when there's multiples of the operator.
            await runBasicTest(
                "a = 'Hello'\n" +
                "a = a + ', World!'\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("Hello, World!") }
                }), 1);
        }

        [Test]
        [Ignore("Currently fails from mixing .NET with PyString. Winds up in DynamicDispatchOperation thinking it's working with numbers")]
        public async Task StringConcatenateDotNetType()
        {
            // Making sure that we're properly parsing and generating all of these when there's multiples of the operator.
            await runBasicTest(
                "a = a + ', World!'\n",
                new Dictionary<string, object>
                {
                    { "a", "Hello" }
                },
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", "Hello, World!" }
                }), 1);
        }

        [Test]
        public async Task StringSubscriptNormal()
        {
            await runBasicTest(
                "a = 'Hello'\n" +
                "b = a[0]\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyString.Create("H") },
                }), 1);
        }

        [Test]
        public async Task StringSubscriptNegative()
        {
            await runBasicTest(
                "a = 'Hello'\n" +
                "b = a[-1]\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyString.Create("o") },
                }), 1);
        }

        [Test]
        public async Task StringSubscriptSlice()
        {
            await runBasicTest(
                "a = 'Hello'\n" +
                "b = a[1:3]\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyString.Create("el") },
                }), 1);
        }

        [Test]
        public async Task Contains()
        {
            await runBasicTest(
                "a = 'e' in 'Hello'\n" +
                "b = 'F' in 'Hello'\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.True },
                    { "b", PyBool.False },
                }), 1);
        }

        [Test]
        public async Task Capitalize()
        {
            await runBasicTest(
                "a = 'hello'.capitalize()\n" +
                "b = 'hello world'.capitalize()\n" +
                "c = ''.capitalize()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("Hello")},
                    { "b", PyString.Create("Hello world")},
                    { "c", PyString.Create()},
                }), 1);
        }

        [Test]
        [Ignore(".NET's string ToLower() doesn't do this and I'm looking up the deal with this.")]
        public async Task Casefold()
        {
            await runBasicTest(
                "a = 'HELLO'.casefold()\n" +
                "b = 'der Fluß'.casefold()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("hello")},
                    { "b", PyString.Create("der Fluss")},
                }), 1);
        }

        [Test]
        public async Task Find()
        {
            await runBasicTest(
                "meowbeep = 'meowbeep'\n" +
                "b = meowbeep.find('ow')\n" +
                "c = meowbeep.find('e', 4)\n" +
                "d = meowbeep.find('e', -2)\n" +
                "e = meowbeep.find('beep', 0, 3)\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyInteger.Create(2)},
                    { "c", PyInteger.Create(5)},
                    { "d", PyInteger.Create(6)},
                    { "e", PyInteger.Create(-1)},
                }), 1);
        }

    }
}
