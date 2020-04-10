using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation.DataTypes;
using LanguageImplementation;

namespace CloacaTests
{
    [TestFixture]
    public class ImportTests : RunCodeTest
    {
        [Test]
        [Ignore("Importing has not been implemented yet and imports will cause NotImplementedErrors")]
        public void BasicImport()
        {
            var interpreter = runProgram(
                "import foo\n", new Dictionary<string, object>(), 1);
        }

        [Test]
        [Ignore("Importing has not been implemented yet and imports will cause NotImplementedErrors")]
        public void TwoLevelImport()
        {
            var interpreter = runProgram(
                "import foo.bar\n", new Dictionary<string, object>(), 1);
        }

        [Test]
        [Ignore("Importing has not been implemented yet and imports will cause NotImplementedErrors")]
        public void AliasedImport()
        {
            var interpreter = runProgram(
                "import foo as fruit\n", new Dictionary<string, object>(), 1);
        }

        [Test]
        [Ignore("Importing has not been implemented yet and imports will cause NotImplementedErrors")]
        public void FromImport()
        {
            var interpreter = runProgram(
                "from foo import FooThing\n", new Dictionary<string, object>(), 1);
        }

        [Test]
        [Ignore("Importing has not been implemented yet and imports will cause NotImplementedErrors")]
        public void FromCommaImport()
        {
            var interpreter = runProgram(
                "from foo import FooThing, OtherThing\n", new Dictionary<string, object>(), 1);
        }

        [Test]
        [Ignore("Importing has not been implemented yet and imports will cause NotImplementedErrors")]
        public void FromStarImport()
        {
            var interpreter = runProgram(
                "from foo import *\n", new Dictionary<string, object>(), 1);
        }
    }
}
