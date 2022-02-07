using System.Threading.Tasks;

using NUnit.Framework;

using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    /// <summary>
    /// Tests specific to tuples. Some tuples basics may come up elsewhere, be we particularly hammer them here.
    /// </summary>
    [TestFixture]
    public class TupleTests : RunCodeTest
    {
        [Test]
        public async Task Contains()
        {
            await runBasicTest(
                "t = (1, 2, 3)\n" +
                "a = 1 in t\n" +
                "b = 200 in t\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.True },
                    { "b", PyBool.False },
                }), 1);
        }
    }
}
