using System.Collections.Generic;
using System.IO;

using LanguageImplementation.DataTypes;
using LanguageImplementation;

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
            bool found = true;
            foreach(var subPath in splitNames)
            {
                builtPath += subPath;
                if(!File.Exists(builtPath))
                {
                    found = false;
                    break;
                }
            }

            if(!found)
            {
                return null;
            }
            else
            {
                return builtPath;
            }
        }

        public PyModuleSpec find_spec(string name, string import_path, PyModule target)
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
    /// from disk.
    /// </summary>
    public class FileBasedModuleLoader : ISpecLoader
    {
        private IInterpreter interpreter;

        public FileBasedModuleLoader(IInterpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        public PyModule Load(PyModuleSpec spec)
        {
            var foundPath = (string)spec.LoaderState;

            var inFile = File.ReadAllText(foundPath);

            ByteCodeCompiler.Compile(inFile, new Dictionary<string, object>());

            // TODO: Need to extend the existing frame context to start executing this module in the current context.
            // Have the interpreter then run the module and then return the affected namespace as a module.
            // Easy, right? :p

            return null;
        }
    }
}
