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
    /// List and dictionary comprehension tests
    /// </summary>

    [TestFixture]
    public class ComprehensionTests : RunCodeTest
    {
        [Test]
        [Ignore("List comprehensions are not yet implemented. These tests are added in advance of supporting them.")]
        public async Task BasicCopy()
        {
            string program =
                "a = [1, 2, 3]\n" +
                "b = [x for x in a]\n";

            var list = PyList.Create();
            list.list.Add(PyInteger.Create(1));
            list.list.Add(PyInteger.Create(2));
            list.list.Add(PyInteger.Create(3));

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", list },
                    { "b", list }
                }), 1);
        }

        [Test]
        [Ignore("List comprehensions are not yet implemented. These tests are added in advance of supporting them.")]
        public async Task ListCompMath()
        {
            string program =
                "a = [1, 2, 3]\n" +
                "b = [2*x+1 for x in a]\n";

            var b = PyList.Create();
            b.list.Add(PyInteger.Create(2*1+1));
            b.list.Add(PyInteger.Create(2*2+1));
            b.list.Add(PyInteger.Create(2*3+1));

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", b }
                }), 1);
        }
        [Test]
        [Ignore("List comprehensions are not yet implemented. These tests are added in advance of supporting them.")]
        public async Task DoubleComprehension()
        {
            string program =
                "a = [['Hello', 'World!'], ['Lets', 'Eat!']]\n" +
                "b = [word for words in t for word in words]\n";

            var b = PyList.Create();
            b.list.Add(PyString.Create("Hello"));
            b.list.Add(PyString.Create("World!"));
            b.list.Add(PyString.Create("Lets"));
            b.list.Add(PyString.Create("Eat!"));

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", b }
                }), 1);
        }
    }
}
