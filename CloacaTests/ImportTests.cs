using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using CloacaInterpreter.ModuleImporting;

namespace CloacaTests
{
    [TestFixture]
    public class ImporterTests
    {
        [Test]
        public void InjectedModulesRootLevel()
        {
            var repo = new InjectedModuleRepository();
            var fooModule = PyModule.Create("foo");
            repo.AddNewModuleRoot(fooModule);

            var fooSpec = repo.find_spec("foo", null, null);

            Assert.That(fooSpec, Is.Not.Null);

            var fooLoaded = repo.Load(fooSpec);

            Assert.That(fooLoaded, Is.EqualTo(fooModule));
        }

        [Test]
        public void InjectedModulesSecondLevel()
        {
            var repo = new InjectedModuleRepository();
            var fooModule = PyModule.Create("foo");
            var barModule = PyModule.Create("bar");
            fooModule.__dict__.Add("bar", barModule);
            repo.AddNewModuleRoot(fooModule);

            var fooSpec = repo.find_spec("foo", null, null);
            var barSpec = repo.find_spec("foo.bar", null, null);

            Assert.That(fooSpec, Is.Not.Null);
            Assert.That(barSpec, Is.Not.Null);

            var fooLoaded = repo.Load(fooSpec);
            var barLoaded = repo.Load(barSpec);

            Assert.That(fooLoaded, Is.EqualTo(fooModule));
            Assert.That(barLoaded, Is.EqualTo(barModule));
        }
    }

    [TestFixture]
    public class ImportSyntaxTests : RunCodeTest
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
            // TODO: Assert *something*
        }

        [Test]
        public void FromCommaImport()
        {
            var fooModule = PyModule.Create("foo");
            var FooThing = PyModule.Create("FooThing");
            var OtherThing = PyModule.Create("OtherThing");
            fooModule.__dict__.Add("FooThing", FooThing);
            fooModule.__dict__.Add("OtherThing", OtherThing);
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);

            var interpreter = runProgram(
                "from foo import FooThing, OtherThing\n", new Dictionary<string, object>(), modules, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void FromImportStar()
        {
            var fooModule = PyModule.Create("foo");
            var FooThing = PyModule.Create("FooThing");
            var OtherThing = PyModule.Create("OtherThing");
            fooModule.__dict__.Add("FooThing", FooThing);
            fooModule.__dict__.Add("OtherThing", OtherThing);
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);

            var interpreter = runProgram(
                "from foo import *\n", new Dictionary<string, object>(), modules, 1);
            // TODO: Assert *something*
        }

        [Test]
        [Ignore("We can set module import levels but the current module system doesn't have an awareness of hierarchical position (yet).")]
        public void FromDotDotImportStar()
        {
            var fooModule = PyModule.Create("foo");
            var modules = new Dictionary<string, PyModule>();
            modules.Add("foo", fooModule);

            // TODO: Need to actually set this test up to be a level below or whatever.

            var interpreter = runProgram(
                "from .. import foo\n", new Dictionary<string, object>(), modules, 1);
            // TODO: Assert *something*
        }
    }
}
