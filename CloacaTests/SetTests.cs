using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using LanguageImplementation;
using CloacaInterpreter;
using System;
using System.Threading.Tasks;

namespace CloacaTests
{
    /// <summary>
    /// Tests specific to lists. Some list basics may come up elsewhere, be we particularly hammer them here.
    /// </summary>
    [TestFixture]
    public class SetTests : RunCodeTest
    {
        [Test]
        public async Task SetConstructor()
        {
            await runBasicTest(
                "s = set(['able', 'ale', 'apple', 'bale', 'kangaroo'])\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "s", PySet.Create(new HashSet<object>() {
                    PyString.Create("able"),
                    PyString.Create("ale"),
                    PyString.Create("apple"),
                    PyString.Create("bale"),
                    PyString.Create("kangaroo"),
                }) }
            }), 1);
        }

        [Test]
        public async Task DeclareSet()
        {
            await runBasicTest(
                "s = {'able', 'ale', 'apple', 'bale', 'kangaroo'}\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "s", PySet.Create(new HashSet<object>() { 
                    PyString.Create("able"),
                    PyString.Create("ale"),
                    PyString.Create("apple"),
                    PyString.Create("bale"),
                    PyString.Create("kangaroo"),
                }) }
            }), 1);
        }

        [Test]
        public async Task Difference()
        {
            await runBasicTest(
                "a = {1, 2, 3}\n" +
                "b = {1, 4, 5}\n" +
                "c = {3, 4}\n" +
                "s = a.difference(b, c)\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "s", PySet.Create(new HashSet<object>() {
                    PyInteger.Create(2),
                }) }
            }), 1);
        }

        [Test]
        public async Task DifferenceUpdate()
        {
            await runBasicTest(
                "a = {1, 2, 3, 4}\n" +
                "a.difference_update({1}, {2})\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PySet.Create(new HashSet<object>() {
                    PyInteger.Create(3),
                    PyInteger.Create(4),
                }) }
            }), 1);
        }

    }
}
