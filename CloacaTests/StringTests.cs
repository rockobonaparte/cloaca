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
    }
}
