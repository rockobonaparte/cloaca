using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    public class GotAnInt
    {
        public int TheInt;
    }

    [TestFixture]
    public class SysModule : RunCodeTest
    {
        [Test]
        public void SchedulerAnotherFunctionNoArgs()
        {
            runBasicTest(
                "import sys\n" +
                "\n" +
                "a = 0\n" +
                "def change_a():\n" +
                "   global a\n" +
                "   a = 1\n" +
                "\n" + 
                "sys.scheduler.schedule(change_a)\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(1) }
            }), 2);
        }

        [Test]
        public void SchedulerPassOneArg()
        {
            var intHaver = new GotAnInt();
            runBasicTest(
                "import sys\n" +
                "\n" +
                "def change_a(got_an_int):\n" +
                "   got_an_int.TheInt = 1\n" +
                "\n" +
                "sys.scheduler.schedule(change_a, container)\n",
                new Dictionary<string, object>()
            {
                { "container", intHaver }
            }, new VariableMultimap(new TupleList<string, object>
            {
            }), 2);

            Assert.That(intHaver.TheInt, Is.EqualTo(1));
        }
    }
}
