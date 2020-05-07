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
    public class SysModule : RunCodeTest
    {
        [Test]
        [Ignore("Current fails due to bugs in the scheduler.")]
        public void SchedulerAnotherFunctionNoArgs()
        {
            runBasicTest(
                "import sys\n" +
                "\n" +
                "a = 0\n" +
                "def change_a():\n" +
                "   a = 1\n" +
                "\n" + 
                "sys.scheduler.schedule(change_a)\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(1) }
            }), 2);
        }
    }
}
