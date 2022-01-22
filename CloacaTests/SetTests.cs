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
        [Ignore("We don't generate sets in code yet")]
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
    }
}
