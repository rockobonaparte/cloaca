using System.Collections.Generic;
using System.IO;

using LanguageImplementation.DataTypes;
using LanguageImplementation;
using System.Threading.Tasks;
using System;

namespace CloacaInterpreter.ModuleImporting
{
    /// <summary>
    /// A module spec finder for modules on disk. Since this is oriented towards an embedded interpreter, it is
    /// constrained to the root paths given to it; it won't look above these roots. More than one root can be
    /// given; we chose this method of over having a finder per root since it's more consistent with how
    /// Python itself does it.
    /// 
    /// It does not run __init__.py files when loading modules yet. See [MODULE_INIT] in TODO.
    /// </summary>
    public class FileBasedModuleFinder : ISpecFinder
    {
        private List<string> moduleRootPaths;
        private FileBasedModuleLoader loader;

        public FileBasedModuleFinder(List<string> moduleRootPaths, FileBasedModuleLoader loader)
        {
            this.moduleRootPaths = moduleRootPaths;
            this.loader = loader;
        }

        /// <summary>
        /// Run per module root in order to find a module under that path.
        /// </summary>
        /// <param name="splitNames">The module's path, split by the dot separators used in its name.</param>
        /// <param name="moduleRoot">The module root path under which to search for this module.</param>
        /// <returns></returns>
        private string findModule(string[] splitNames, string moduleRoot)
        {
            var builtPath = moduleRoot;

            // Need to index explicitly because the last of the split paths might actually be a .py file.
            for (int subPath_i = 0; subPath_i < splitNames.Length; ++subPath_i)
            {
                var subPath = splitNames[subPath_i];
                builtPath = Path.Combine(builtPath, subPath);
                if(!File.Exists(builtPath))
                {
                    if(subPath_i == splitNames.Length - 1)
                    {
                        if(File.Exists(builtPath + ".py"))
                        {
                            return builtPath + ".py";
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return builtPath;
        }

        public PyModuleSpec find_spec(FrameContext context, string name, string import_path, PyModule target)
        {
            var splitNames = name.Split('.');
            foreach (var moduleRoot in moduleRootPaths)
            {
                var foundPath = findModule(splitNames, moduleRoot);
                if(foundPath != null)
                {
                    var spec = PyModuleSpec.Create(name, loader, "", null);
                    spec.LoaderState = foundPath;
                    return spec;
                }
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
    public class FileBasedModuleLoader : ISpecLoader
    {

        public async Task<object> Load(IInterpreter interpreter, FrameContext context, PyModuleSpec spec)
        {
            var foundPath = (string)spec.LoaderState;
            var inFile = File.ReadAllText(foundPath);
            var moduleCode = await ByteCodeCompiler.Compile(inFile, new Dictionary<string, object>(), interpreter.Scheduler);
            await interpreter.CallInto(context, moduleCode, new object[0]);

            if(context.EscapedDotNetException != null)
            {
                throw context.EscapedDotNetException;
            }

            var moduleFrame = context.callStack.Pop();
            var module = PyModule.Create(spec.Name);
            for(int local_i = 0; local_i < moduleFrame.LocalNames.Count; ++local_i)
            {
                module.__setattr__(moduleFrame.LocalNames[local_i], moduleFrame.Locals[local_i]);
            }

            return module;
        }
    }
}
