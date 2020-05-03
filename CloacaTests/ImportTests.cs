using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

using CloacaInterpreter.ModuleImporting;
using CloacaInterpreter;
using LanguageImplementation.DataTypes;
using LanguageImplementation;
using System.Reflection;

namespace CloacaTests
{
    [TestFixture]
    public class FileImporterTests
    {
        [Test]
        public async Task BasicImport()
        {
            var repoRoots = new List<string>();
            var fake_module_root = Path.Combine(Path.GetDirectoryName(typeof(FileImporterTests).Assembly.Location),
                "fake_module_root");
            repoRoots.Add(fake_module_root);

            var scheduler = new Scheduler();                // Might not need the actual scheduler...
            var interpreter = new Interpreter(scheduler);

            var loader = new FileBasedModuleLoader();
            var rootContext = new FrameContext();
            var repo = new FileBasedModuleFinder(repoRoots, loader);

            var spec = repo.find_spec(null, "test", null, null);
            Assert.NotNull(spec);

            var loadedModule = await spec.Loader.Load(interpreter, rootContext, spec);
            Assert.That(loadedModule.__dict__, Contains.Key("a_string"));
            Assert.That(loadedModule.__dict__["a_string"], Is.EqualTo(PyString.Create("Yay!")));
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

            var fooSpec = repo.find_spec(null, "foo", null, null);

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

            var fooSpec = repo.find_spec(null, "foo", null, null);
            var barSpec = repo.find_spec(null, "foo.bar", null, null);

            Assert.That(fooSpec, Is.Not.Null);
            Assert.That(barSpec, Is.Not.Null);

            var fooLoaded = await fooSpec.Loader.Load(null, null, fooSpec);
            var barLoaded = await barSpec.Loader.Load(null, null, barSpec);

            Assert.That(fooLoaded, Is.EqualTo(fooModule));
            Assert.That(barLoaded, Is.EqualTo(barModule));
        }
    }

    [TestFixture]
    public class ClrImporterTests
    {

        [Test]
        public async Task SystemBasic()
        {
            var finder = new ClrModuleFinder();
            var mockStack = new Stack<Frame>();
            var mockFrame = new Frame();
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack);
            var clrLoader = new ClrModuleInternals();
            clrLoader.AddReference(mockContext, "System");

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec);
            Assert.That(module.Name, Is.EqualTo("System"));
        }

        /// <summary>
        /// This is the SystemBasic but making sure we can resolve it if the reference was added as 
        /// a default to the finder instead of from the AddReference call.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task SystemBasicFromDefaults()
        {
            var finder = new ClrModuleFinder();
            finder.AddDefaultAssembly(Assembly.LoadWithPartialName("System"));
            var mockStack = new Stack<Frame>();
            var mockFrame = new Frame();
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack);

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec);
            Assert.That(module.Name, Is.EqualTo("System"));
        }

        [Test]
        public async Task SystemBasicFullName()
        {
            var finder = new ClrModuleFinder();
            var mockStack = new Stack<Frame>();
            var mockFrame = new Frame();
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack);
            var clrLoader = new ClrModuleInternals();
            clrLoader.AddReference(mockContext, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec);
            Assert.That(module.Name, Is.EqualTo("System"));
        }

        [Test]
        public async Task SystemDotEnvironment()
        {
            var finder = new ClrModuleFinder();
            var mockStack = new Stack<Frame>();
            var mockFrame = new Frame();
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack);
            var clrLoader = new ClrModuleInternals();
            clrLoader.AddReference(mockContext, "mscorlib");

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec);
            Assert.That(module.Name, Is.EqualTo("System"));
        }

    }

    [TestFixture]
    public class ClrImporterCodeTests : RunCodeTest
    {
        [Test]
        public void ImportSystemEnvironmentInCode()
        {
            var finishedFrame = runProgram(
                "import clr\n" +
                "clr.AddReference('mscorlib')\n" +
                "from System import Environment\n" +
                "machine_name = Environment.MachineName\n", new Dictionary<string, object>(), 1);

            var machine_name = finishedFrame.GetVariable("machine_name");
            Assert.That(machine_name, Is.EqualTo(System.Environment.MachineName));
        }

        /// <summary>
        /// This tests shows we can construct something from an imported assembly and work with it.
        /// It requires that are .NET interaction generally is correct in the first place, so this
        /// just reinforces that we can ingest this stuff externally from imports.
        /// </summary>
        [Test]
        //[Ignore("Internals can't seem to accept the type as a thing to use. It's a similar cockup to what the object resolver was doing.")]
        public void Guid()
        {
            var finishedFrame = runProgram(
                "import clr\n" +
                "clr.AddReference('mscorlib')\n" +
                "from System import Guid\n" +
                "guid = Guid('dddddddd-dddd-dddd-dddd-dddddddddddd')\n", new Dictionary<string, object>(), 1);

            var guid = finishedFrame.GetVariable("guid");
            Assert.That(guid, Is.EqualTo(new System.Guid("dddddddd-dddd-dddd-dddd-dddddddddddd")));
        }
    }

    [TestFixture]
    public class ImportSyntaxTests : RunCodeTest
    {
        private List<ISpecFinder> moduleFinders;
        private PyObject fooThing;
        private PyObject otherThing;
        private PyModule fooModule;
        private PyModule foo2Module;
        private PyModule barModule;


        [SetUp]
        public void setupFinders()
        {
            var repo = new InjectedModuleRepository();
            fooModule = PyModule.Create("foo");

            barModule = PyModule.Create("bar");
            fooThing = PyString.Create("FooThing");
            otherThing = PyString.Create("OtherThing");
            fooModule.__dict__.Add("bar", barModule);
            fooModule.__dict__.Add("FooThing", fooThing);
            fooModule.__dict__.Add("OtherThing", otherThing);

            foo2Module = PyModule.Create("foo2");

            repo.AddNewModuleRoot(fooModule);
            repo.AddNewModuleRoot(foo2Module);

            moduleFinders = new List<ISpecFinder>();
            moduleFinders.Add(repo);
        }

        [Test]
        public void BasicImport()
        {
            var finishedFrame = runProgram(
                "import foo\n", new Dictionary<string, object>(), moduleFinders, 1);

            var foo = finishedFrame.GetVariable("foo");
            Assert.That(foo, Is.EqualTo(fooModule));
        }

        [Test]
        public void TwoLevelImport()
        {
            var finishedFrame = runProgram(
                "import foo.bar\n", new Dictionary<string, object>(), moduleFinders, 1);
            
            var foobar = finishedFrame.GetVariable("foo.bar");
            Assert.That(foobar, Is.EqualTo(barModule));
        }

        [Test]
        public void TwoImportsOneLine()
        {
            var finishedFrame = runProgram(
                "import foo, foo2\n", new Dictionary<string, object>(), moduleFinders, 1);
            
            var foo = finishedFrame.GetVariable("foo");
            var foo2 = finishedFrame.GetVariable("foo2");
            Assert.That(foo, Is.EqualTo(fooModule));
            Assert.That(foo2, Is.EqualTo(foo2Module));
        }

        [Test]
        public void AliasedImport()
        {
            var finishedFrame = runProgram(
                "import foo as fruit\n", new Dictionary<string, object>(), moduleFinders, 1);

            var fooFruit = finishedFrame.GetVariable("fruit");
            Assert.That(fooFruit, Is.EqualTo(fooModule));
        }

        [Test]
        public void FromImport()
        {
            var finishedFrame = runProgram(
                "from foo import FooThing\n", new Dictionary<string, object>(), moduleFinders, 1);

            var importedFooThing = finishedFrame.GetVariable("FooThing");
            Assert.That(importedFooThing, Is.EqualTo(fooThing));
        }

        [Test]
        public void FromCommaImport()
        {
            var finishedFrame = runProgram(
                "from foo import FooThing, OtherThing\n", new Dictionary<string, object>(), moduleFinders, 1);

            var importedFooThing = finishedFrame.GetVariable("FooThing");
            var importedOtherThing = finishedFrame.GetVariable("OtherThing");
            Assert.That(importedFooThing, Is.EqualTo(fooThing));
            Assert.That(importedOtherThing, Is.EqualTo(otherThing));
        }

        [Test]
        public void FromImportStar()
        {
            var finishedFrame = runProgram(
                "from foo import *\n", new Dictionary<string, object>(), moduleFinders, 1);

            var importedFooThing = finishedFrame.GetVariable("FooThing");
            var importedOtherThing = finishedFrame.GetVariable("OtherThing");
            var importedBar = finishedFrame.GetVariable("bar");
            Assert.That(importedFooThing, Is.EqualTo(fooThing));
            Assert.That(importedOtherThing, Is.EqualTo(otherThing));
            Assert.That(importedBar, Is.EqualTo(barModule));
        }

        [Test]
        [Ignore("We can set module import levels but the current module system doesn't have an awareness of hierarchical position (yet).")]
        public void FromDotDotImportStar()
        {
            // TODO: Need to actually set this test up to be a level below or whatever.

            var finishedFrame = runProgram(
                "from .. import foo\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }
    }
}
