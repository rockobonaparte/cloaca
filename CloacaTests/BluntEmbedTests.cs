using System;
using System.Numerics;
using System.Collections.Generic;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;
using NUnit.Framework;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    /// <summary>
    /// These are a series of experimental tests to try to use built-in types directly. We
    /// put a lot of effort into mimicking Python's own types, but what do we do for external
    /// types? We're going to just try to let them run free.
    /// </summary>
    [TestFixture]
    public class BluntEmbeddingTests : RunCodeTest
    {
        [Test]
        public void AssignExternalObj()
        {
            var a = new ReflectIntoPython(1, "test");

            runBasicTest("b = a\n",
            new Dictionary<string, object>
            {
                { "a", a }
            },
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", a }
            }), 1);
        }

        [Test]
        public void AccessExternalObjProperty()
        {
            // We currently have fields and properties figured out, but we can't assign a method or anything like that. :(
            var a = new ReflectIntoPython(1, "test");

            runBasicTest("b = a.AnInteger\n" +
                         "c = a.AnIntegerProperty\n",
            new Dictionary<string, object>
            {
                { "a", a }
            },
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", 1 },
                { "c", 1 }
            }), 1);
        }
    }
}
