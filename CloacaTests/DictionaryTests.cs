using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    /// <summary>
    /// Tests specific to dictionaries. Some dictionary basics may come up elsewhere, be we particularly hammer them here.
    /// </summary>
    [TestFixture]
    public class DictionaryTests : RunCodeTest
    {
        [Test]
        public async Task Contains()
        {
            await runBasicTest(
                "h = {'foo': 'bar'}\n" +
                "a = 'foo' in h\n" +
                "b = 200 in h\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.True },
                    { "b", PyBool.False },
                }), 1);
        }

        [Test]
        public async Task Keys()
        {
            await runBasicTest(
                "h = {'a': 1, 'b': 2}\n" +
                "k = h.keys()\n" +
                "a = 'a' in k\n" +
                "l = k == ['a', 'b']\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.True },
                    { "l", PyBool.True },
                }), 1);
        }
    }
}
