using System.Collections.Generic;
using System.IO;

using LanguageImplementation.DataTypes;
using LanguageImplementation;
using System.Threading.Tasks;
using System;

namespace CloacaInterpreter.ModuleImporting
{
    /// <summary>
    /// A module spec loader for code represented as raw strings. This is mostly used for testing, but could plausibly
    /// be used to construct a hierarchy of uncompiled code in general use. There, it would probably be better to at least
    /// use precompiled code (or cache the CodeObjects afterwards)
    /// 
    /// It does not run __init__.py files when loading modules yet. See [MODULE_INIT] in TODO. It might never do that
    /// given the focus on this finder on specific testing.
    /// </summary>
    public class StringCodeModuleFinder : ISpecFinder
    {
        public Dictionary<string, string> CodeLookup;      // 'some.module.path' = print("Pile of code")
        private StringCodeModuleLoader loader;

        public StringCodeModuleFinder()
        {
            this.CodeLookup = new Dictionary<string, string>();
            this.loader = new StringCodeModuleLoader(this.CodeLookup);
        }

        public PyModuleSpec find_spec(FrameContext context, string name, string import_path, PyModule target)
        {
            if(CodeLookup.ContainsKey(name))
            {
                var spec = PyModuleSpec.Create(name, loader, "", null);
                spec.LoaderState = name;
                return spec;
            }

            // Fall-through: Module not found. Return null according to specifications.
            return null;
        }
    }

    /// <summary>
    /// Loader of FileBasedModuleFinder PyModuleSpecs. This will actually load the module
    /// from disk. It will then execute the code inside that module file. The final module is a fresh
    /// module created from scratch that is populated with the executed code's namespace.
    /// </summary>
    public class StringCodeModuleLoader : ISpecLoader
    {
        private Dictionary<string, string> lookup;

        public StringCodeModuleLoader(Dictionary<string, string> lookup)
        {
            this.lookup = lookup;
        }

        public async Task<object> Load(IInterpreter interpreter, FrameContext context, PyModuleSpec spec)
        {
            var code = lookup[(string)spec.LoaderState];
            var moduleGlobals = new Dictionary<string, object>();
            moduleGlobals.Add("__name__", spec.Name);
            var moduleCode = await ByteCodeCompiler.Compile(code, new Dictionary<string, object>(), moduleGlobals, interpreter.Scheduler);

            string modulename = spec.Origin.Length == 0 ? spec.Name : spec.Origin + "." + spec.Name;

            await interpreter.CallInto(context, moduleCode, new object[0], moduleGlobals);


            if (context.EscapedDotNetException != null)
            {
                throw context.EscapedDotNetException;
            }

            // Notes:
            // This sets __name__ to __main__ which is false. It should be the module's name.
            // This should take globals out of the module instead of taking them from the parent.
            // I think we need to create this frame and use it to call into the module code instead
            // of using the parent frame. We'll have to generalize this for all the loaders.

            ////////////
            var moduleFrame = context.callStack.Pop();
            var module = PyModule.Create(spec.Name);

            for (int local_i = 0; local_i < moduleFrame.LocalNames.Count; ++local_i)
            {
                var name = moduleFrame.LocalNames[local_i];
                if (moduleFrame.Locals.ContainsKey(name))
                {
                    module.__setattr__(name, moduleFrame.Locals[name]);
                }
            }
            /////////////
            return module;
        }
    }
}
