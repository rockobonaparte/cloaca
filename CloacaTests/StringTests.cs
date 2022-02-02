﻿using System.Collections.Generic;
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
        public async Task Count()
        {
            await runBasicTest(
                "searchies = 'erererrerere'\n" +
                "a = searchies.count('e')\n" +
                "b = searchies.count('e', 11)\n" +
                "c = searchies.count('e', 11, 12)\n" +
                "d = searchies.count('e', -1)\n" +
                "e = searchies.count('r', 11)\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(6)},
                    { "b", PyInteger.Create(1)},
                    { "c", PyInteger.Create(1)},
                    { "d", PyInteger.Create(1)},
                    { "e", PyInteger.Create(0)},
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

        [Test]
        public async Task Index()
        {
            await runBasicTest(
                "meowbeep = 'meowbeep'\n" +
                "b = meowbeep.index('ow')\n" +
                "c = meowbeep.index('e', 4)\n" +
                "d = meowbeep.index('e', -2)\n" +
                "e_err = False\n" +
                "try:\n" +
                "   e = meowbeep.index('beep', 0, 3)\n" +
                "except ValueError:\n" +
                "   e_err = True\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyInteger.Create(2)},
                    { "c", PyInteger.Create(5)},
                    { "d", PyInteger.Create(6)},
                    { "e_err", PyBool.Create(true)},
                }), 1);
        }

        [Test]
        public async Task IsDigit()
        {
            await runBasicTest(
                "a = '22'.isdigit()\n" +
                "b = '-22'.isdigit()\n" +
                "c = '2.2'.isdigit()\n" +
                "d = ''.isdigit()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(true)},
                    { "b", PyBool.Create(false)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task IsAlpha()
        {
            await runBasicTest(
                "a = '22'.isalpha()\n" +
                "b = 'ae'.isalpha()\n" +
                "c = 'a-'.isalpha()\n" +
                "d = ''.isalpha()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(false)},
                    { "b", PyBool.Create(true)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                }), 1);
        }
    }
}
