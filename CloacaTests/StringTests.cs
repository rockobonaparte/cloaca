using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation;
using System;

namespace CloacaTests
{
    [TestFixture]
    public class StringTests : RunCodeTest
    {
        [Test]
        public void StringConcatenation()
        {
            // Making sure that we're properly parsing and generating all of these when there's multiples of the operator.
            runBasicTest(
                "a = 'Hello'\n" +
                "a = a + ', World!'\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("Hello, World!") }
                }), 1);
        }
    }
}
