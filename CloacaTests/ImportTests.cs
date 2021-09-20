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
using System.Reflection.Emit;
using System.Threading;

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
            var rootContext = new FrameContext(new Dictionary<string, object>());
            var repo = new FileBasedModuleFinder(repoRoots, loader);

            var spec = repo.find_spec(null, "test", null, null);
            Assert.NotNull(spec);

            var loadedModule = await spec.Loader.Load(interpreter, rootContext, spec) as PyModule;
            Assert.That(loadedModule.__dict__, Contains.Key("a_string"));
            Assert.That(loadedModule.__dict__["a_string"], Is.EqualTo(PyString.Create("Yay!")));
        }
    }

    [TestFixture]
    public class StringCodeImporterTests
    {
        [Test]
        public async Task BasicImport()
        {
            var scheduler = new Scheduler();                // Might not need the actual scheduler...
            var interpreter = new Interpreter(scheduler);

            var rootContext = new FrameContext(new Dictionary<string, object>());
            var repo = new StringCodeModuleFinder();
            repo.CodeLookup.Add("foo", "bar = 'Hello, World!'\n");

            var spec = repo.find_spec(null, "foo", null, null);
            Assert.NotNull(spec);

            var loadedModule = await spec.Loader.Load(interpreter, rootContext, spec) as PyModule;
            Assert.That(loadedModule.__dict__, Contains.Key("bar"));
            Assert.That(loadedModule.__dict__["bar"], Is.EqualTo(PyString.Create("Hello, World!")));
        }
    }

    [TestFixture]
    public class InjectedImporterTests
    {
        [Test]
        public async Task RootLevel()
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
        public async Task SecondLevel()
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

        [Test]
        [Ignore("It looks like this *shouldn't work*")]
        public async Task SecondLevelNonModule()
        {
            // Exception should be something like:
            // ModuleNotFoundError: No module named 'foo.bar.barstring'; 'foo.bar.barstring' is not a package
            // They would be forced to use the "from foo.bar import barstring"
            var repo = new InjectedModuleRepository();
            var fooModule = PyModule.Create("foo");
            var barModule = PyModule.Create("bar");
            var barString = PyString.Create("bar string");
            barModule.__dict__.Add("barstring", barString);
            fooModule.__dict__.Add("bar", barModule);
            repo.AddNewModuleRoot(fooModule);

            var barStringSpec = repo.find_spec(null, "foo.bar.barstring", null, null);

            Assert.That(barStringSpec, Is.Not.Null);

            var barStringLoaded = await barStringSpec.Loader.Load(null, null, barStringSpec);

            Assert.That(barStringLoaded, Is.EqualTo(barString));
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
            mockFrame.Program = new CodeObject(new byte[0]);
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack, new Dictionary<string, object>());
            var clrLoader = new ClrModuleInternals();
            clrLoader.AddReference(mockContext, "System");

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec) as PyModule;
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
            mockFrame.Program = new CodeObject(new byte[0]);
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack, new Dictionary<string, object>());

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec) as PyModule;
            Assert.That(module.Name, Is.EqualTo("System"));
        }

        [Test]
        public async Task SystemBasicFullName()
        {
            var finder = new ClrModuleFinder();
            var mockStack = new Stack<Frame>();
            var mockFrame = new Frame();
            mockFrame.Program = new CodeObject(new byte[0]);
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack, new Dictionary<string, object>());
            var clrLoader = new ClrModuleInternals();
            clrLoader.AddReference(mockContext, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec) as PyModule;
            Assert.That(module.Name, Is.EqualTo("System"));
        }

        [Test]
        public async Task SystemDotEnvironment()
        {
            var finder = new ClrModuleFinder();
            var mockStack = new Stack<Frame>();
            var mockFrame = new Frame();
            mockFrame.Program = new CodeObject(new byte[0]);
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack, new Dictionary<string, object>());
            var clrLoader = new ClrModuleInternals();
            clrLoader.AddReference(mockContext, "mscorlib");

            var spec = finder.find_spec(mockContext, "System", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec) as PyModule;
            Assert.That(module.Name, Is.EqualTo("System"));
        }

        [Test]
        public async Task NoNamespace()
        {
            // Rather than set up a mock project just to create on mock object with no namespace, we're going to dynamically
            // create one and stuff it into the import system.
            AssemblyBuilder ab = null;
            AssemblyName an = new AssemblyName();
            an.Version = new Version(1, 0, 0, 0);
            an.Name = "NoNamespaceMockAssembly";
            ab = Thread.GetDomain().DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
            // Define a dynamic module and the filename of the assembly 
            ModuleBuilder modBuilder = ab.DefineDynamicModule("NoNamespaceMockModule");
            TypeBuilder typeBuilder = modBuilder.DefineType("NoNamespaceClass",
                                            TypeAttributes.Public |
                                            TypeAttributes.Class |
                                            TypeAttributes.AutoClass |
                                            TypeAttributes.AnsiClass |
                                            TypeAttributes.BeforeFieldInit |
                                            TypeAttributes.AutoLayout
                                            , null);
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            FieldBuilder fieldBuilder = typeBuilder.DefineField("some_number", typeof(int), FieldAttributes.Public);
            var mockType = typeBuilder.CreateType();

            // Some sanity checks since we don't generally write this kind of crazy code.
            Assert.That(mockType.Name, Is.EqualTo("NoNamespaceClass"));
            Assert.That(mockType.Assembly.GetName().Name, Is.EqualTo("NoNamespaceMockAssembly"));
            Assert.That(mockType.Namespace, Is.Null);

            // If we made it this far, then the mock assembly should be set up like one on disk that previously screwed up
            // versions of the CLR importer.
            var finder = new ClrModuleFinder();
            finder.AddDefaultAssembly(mockType.Assembly);

            var mockStack = new Stack<Frame>();
            var mockFrame = new Frame();
            mockFrame.Program = new CodeObject(new byte[0]);
            mockStack.Push(mockFrame);
            var mockContext = new FrameContext(mockStack, new Dictionary<string, object>());

            var spec = finder.find_spec(mockContext, "NoNamespaceClass", null, null);
            Assert.NotNull(spec);

            var module = await spec.Loader.Load(null, mockContext, spec);
            Assert.That(module, Is.EqualTo(mockType));
        }
    }

    [TestFixture]
    public class ClrImporterCodeTests : RunCodeTest
    {
        [Test]
        public async Task ImportSystemEnvironmentInCode()
        {
            var finishedFrame = await runProgram(
                "import clr\n" +
                "clr.AddReference('mscorlib')\n" +
                "from System import Environment\n" +
                "machine_name = Environment.MachineName\n", new Dictionary<string, object>(), 1);

            var machine_name = finishedFrame.GetVariable("machine_name");
            Assert.That(machine_name, Is.EqualTo(System.Environment.MachineName));
        }

        [Test]
        public async Task ImportGenericList()
        {
            var finishedFrame = await runProgram(
                "import clr\n" +
                "clr.AddReference('mscorlib')\n" +
                "from System.Collections.Generic import List\n" +
                "from System import String\n" +
                "a_list = List(String)\n", new Dictionary<string, object>(), 1);

            var machine_name = finishedFrame.GetVariable("a_list");
            Assert.That(machine_name.GetType(), Is.EqualTo(typeof(List<string>)));
        }

        /// <summary>
        /// This tests shows we can construct something from an imported assembly and work with it.
        /// It requires that are .NET interaction generally is correct in the first place, so this
        /// just reinforces that we can ingest this stuff externally from imports.
        /// </summary>
        [Test]
        public async Task Guid()
        {
            var finishedFrame = await runProgram(
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
            fooThing = PyString.Create("this_is_a_foo_thing");
            otherThing = PyString.Create("this_is_some_other_thing");
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
        public async Task BasicImport()
        {
            var finishedFrame = await runProgram(
                "import foo\n", new Dictionary<string, object>(), moduleFinders, 1);

            var foo = finishedFrame.GetVariable("foo");
            Assert.That(foo, Is.EqualTo(fooModule));
        }

        [Test]
        public async Task TwoLevelImport()
        {
            var finishedFrame = await runProgram(
                "import foo.bar\n", new Dictionary<string, object>(), moduleFinders, 1);
            
            var foobar = finishedFrame.GetVariable("foo.bar");
            Assert.That(foobar, Is.EqualTo(barModule));
        }

        [Test]
        public async Task TwoImportsOneLine()
        {
            var finishedFrame = await runProgram(
                "import foo, foo2\n", new Dictionary<string, object>(), moduleFinders, 1);
            
            var foo = finishedFrame.GetVariable("foo");
            var foo2 = finishedFrame.GetVariable("foo2");
            Assert.That(foo, Is.EqualTo(fooModule));
            Assert.That(foo2, Is.EqualTo(foo2Module));
        }

        [Test]
        public async Task AliasedImport()
        {
            var finishedFrame = await runProgram(
                "import foo as fruit\n", new Dictionary<string, object>(), moduleFinders, 1);

            var fooFruit = finishedFrame.GetVariable("fruit");
            Assert.That(fooFruit, Is.EqualTo(fooModule));
        }

        [Test]
        public async Task FromImport()
        {
            var finishedFrame = await runProgram(
                "from foo import FooThing\n", new Dictionary<string, object>(), moduleFinders, 1);

            var importedFooThing = finishedFrame.GetVariable("FooThing");
            Assert.That(importedFooThing, Is.EqualTo(fooThing));
        }

        [Test]
        public async Task FromCommaImport()
        {
            var finishedFrame = await runProgram(
                "from foo import FooThing, OtherThing\n", new Dictionary<string, object>(), moduleFinders, 1);

            var importedFooThing = finishedFrame.GetVariable("FooThing");
            var importedOtherThing = finishedFrame.GetVariable("OtherThing");
            Assert.That(importedFooThing, Is.EqualTo(fooThing));
            Assert.That(importedOtherThing, Is.EqualTo(otherThing));
        }

        [Test]
        public async Task FromImportStar()
        {
            var finishedFrame = await runProgram(
                "from foo import *\n", new Dictionary<string, object>(), moduleFinders, 1);

            var importedFooThing = finishedFrame.GetVariable("FooThing");
            var importedOtherThing = finishedFrame.GetVariable("OtherThing");
            var importedBar = finishedFrame.GetVariable("bar");
            Assert.That(importedFooThing, Is.EqualTo(fooThing));
            Assert.That(importedOtherThing, Is.EqualTo(otherThing));
            Assert.That(importedBar, Is.EqualTo(barModule));
        }

        /// <summary>
        /// Tests that after importing a module, we can properly reference it with the dot operator.
        /// </summary>
        [Test]
        public async Task ReferenceModuleVariable()
        {
            var finishedFrame = await runProgram(
                "import foo\n" +
                "bar = foo.FooThing\n", new Dictionary<string, object>(), moduleFinders, 1);

            var foobar = finishedFrame.GetVariable("bar");
            Assert.That(foobar, Is.EqualTo(PyString.Create("this_is_a_foo_thing")));
        }

        /// <summary>
        /// Tests that after importing a module, we can call functions under it without it thinking it's
        /// an object that takes the module as a first, self argument!
        /// </summary>
        [Test]
        [Ignore("Currently broken. The module is passed as the argument instead of the actual argument. It thinks it's an object.")]
        public async Task CallModuleFunction()
        {
            string subProgramCode = "def subprogram(x):\n" +
                                    "   return x\n";
            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            var compiledSubProgram = await ByteCodeCompiler.Compile(subProgramCode, new Dictionary<string, object>(), scheduler);
            fooModule.__dict__.Add("subprogram", compiledSubProgram.Constants[0]);

            var finishedFrame = await runProgram(
                "import foo\n" +
                "bar = foo.subprogram(1)\n", new Dictionary<string, object>(), moduleFinders, 1);

            var foobar = finishedFrame.GetVariable("bar");
            Assert.That(foobar, Is.EqualTo(PyInteger.Create(1)));
        }

        [Test]
        [Ignore("We can set module import levels but the current module system doesn't have an awareness of hierarchical position (yet).")]
        public async Task FromDotDotImportStar()
        {
            // TODO: Need to actually set this test up to be a level below or whatever.

            var finishedFrame = await runProgram(
                "from .. import foo\n", new Dictionary<string, object>(), moduleFinders, 1);
            // TODO: Assert *something*
        }
    }
}
