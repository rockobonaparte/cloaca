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
        public void BasicImport()
        {
            var fooModule = PyModule.Create("foo");
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);

            var interpreter = runProgram(
                "import foo\n", new Dictionary<string, object>(), modules, 1);

            // TODO: Assert *something*
        }

        [Test]
        public void TwoLevelImport()
        {
            var fooModule = PyModule.Create("foo");
            var barModule = PyModule.Create("bar");
            fooModule.__dict__.Add("bar", barModule);
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);

            var interpreter = runProgram(
                "import foo.bar\n", new Dictionary<string, object>(), modules, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void TwoImportsOneLine()
        {
            var fooModule = PyModule.Create("foo");
            var barModule = PyModule.Create("bar");
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);
            modules.Add("bar", barModule);

            var interpreter = runProgram(
                "import foo, bar\n", new Dictionary<string, object>(), modules, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void AliasedImport()
        {
            var fooModule = PyModule.Create("foo");
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);

            var interpreter = runProgram(
                "import foo as fruit\n", new Dictionary<string, object>(), modules, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void FromImport()
        {
            var fooModule = PyModule.Create("foo");
            var FooThing = PyModule.Create("FooThing");
            fooModule.__dict__.Add("FooThing", FooThing);
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);

            var interpreter = runProgram(
                "from foo import FooThing\n", new Dictionary<string, object>(), modules, 1);
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
