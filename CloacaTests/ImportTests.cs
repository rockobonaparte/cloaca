using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

using CloacaInterpreter.ModuleImporting;
using CloacaInterpreter;
using LanguageImplementation.DataTypes;
using LanguageImplementation;

namespace CloacaTests
{
    [TestFixture]
    public class FileImporterTests
    {
        [Test]
        [Ignore("Getting this test to pass is a work-in-progress on this topic branch.")]
        public async Task BasicImport()
        {
            var dir = Path.GetDirectoryName(typeof(FileImporterTests).Assembly.Location);
            Environment.CurrentDirectory = dir;

            var repoRoots = new List<string>();
            repoRoots.Add("fake_module_root");

            var scheduler = new Scheduler();                // Might not need the actual scheduler...
            var interpreter = new Interpreter(scheduler);

            var loader = new FileBasedModuleLoader();
            var rootContext = new FrameContext();
            var repo = new FileBasedModuleFinder(repoRoots, loader);

            var spec = repo.find_spec("test", null, null);
            var testLoaded = await spec.Loader.Load(interpreter, rootContext, spec);
        }
    }

    [TestFixture]
    public class InjectedImporterTests
    {
        [Test]
        public async Task InjectedModulesRootLevel()
        {
            var repo = new InjectedModuleRepository();
            var fooModule = PyModule.Create("foo");
            repo.AddNewModuleRoot(fooModule);

            var fooSpec = repo.find_spec("foo", null, null);

            Assert.That(fooSpec, Is.Not.Null);

            var fooLoaded = await fooSpec.Loader.Load(null, null, fooSpec);

            Assert.That(fooLoaded, Is.EqualTo(fooModule));
        }

        [Test]
        public async Task InjectedModulesSecondLevel()
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

            var fooLoaded = await fooSpec.Loader.Load(null, null, fooSpec);
            var barLoaded = await barSpec.Loader.Load(null, null, barSpec);

            Assert.That(fooLoaded, Is.EqualTo(fooModule));
            Assert.That(barLoaded, Is.EqualTo(barModule));
        }
    }

    [TestFixture]
    public class ImportSyntaxTests : RunCodeTest
    {
        private List<ISpecFinder> moduleFinders;

        [SetUp]
        public void setupFinders()
        {
            var repo = new InjectedModuleRepository();
            var fooModule = PyModule.Create("foo");

            var barModule = PyModule.Create("bar");
            var FooThing = PyModule.Create("FooThing");
            var OtherThing = PyModule.Create("OtherThing");
            fooModule.__dict__.Add("bar", barModule);
            fooModule.__dict__.Add("FooThing", FooThing);
            fooModule.__dict__.Add("OtherThing", OtherThing);

            var foo2Module = PyModule.Create("foo2");

            repo.AddNewModuleRoot(fooModule);
            repo.AddNewModuleRoot(foo2Module);

            moduleFinders = new List<ISpecFinder>();
            moduleFinders.Add(repo);
        }

        [Test]
        public void BasicImport()
        {
            var interpreter = runProgram(
                "import foo\n", new Dictionary<string, object>(), moduleFinders, 1);

            // TODO: Assert *something*
        }

        [Test]
        public void TwoLevelImport()
        {
            var interpreter = runProgram(
                "import foo.bar\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void TwoImportsOneLine()
        {
            var interpreter = runProgram(
                "import foo, foo2\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void AliasedImport()
        {
            var interpreter = runProgram(
                "import foo as fruit\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void FromImport()
        {
            var interpreter = runProgram(
                "from foo import FooThing\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void FromCommaImport()
        {
            var interpreter = runProgram(
                "from foo import FooThing, OtherThing\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }

        [Test]
        public void FromImportStar()
        {
            var interpreter = runProgram(
                "from foo import *\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }

        [Test]
        [Ignore("We can set module import levels but the current module system doesn't have an awareness of hierarchical position (yet).")]
        public void FromDotDotImportStar()
        {
            // TODO: Need to actually set this test up to be a level below or whatever.

            var interpreter = runProgram(
                "from .. import foo\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }
    }
}
