using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaInterpreter.ModuleImporting
{

    public class ClrModuleFinder : ISpecFinder
    {
        private Dictionary<string, PyModule> ModuleRoots;
        private ClrModuleSpec loader;

        public ClrModuleFinder()
        {
            loader = new ClrModuleSpec();
        }

        public PyModuleSpec find_spec(FrameContext context, string name, string import_path, PyModule target)
        {
            if(context.HasVariable(ClrContext.FrameContextTokenName))
            {
                var clrContext = (ClrContext) context.GetVariable(ClrContext.FrameContextTokenName);
                if(clrContext.AddedReferences.ContainsKey(name))
                {
                    var spec = PyModuleSpec.Create(name, loader, "", null);
                    spec.LoaderState = clrContext.AddedReferences[name];
                    return spec;
                }
            }

            return null;
        }
    }

    public class ClrModuleSpec : ISpecLoader
    {
        /// <summary>
        /// This is more of a formality since the injected module loader already has the module loaded, but this gives us conformity with
        /// the module importing system.
        /// </summary>
        /// <param name="spec">The module spec we will load.</param>
        /// <returns>The loaded module, which is just a lookup into our system.</returns>
        public async Task<PyModule> Load(IInterpreter interpreter, FrameContext context, PyModuleSpec spec)
        {
            var module = PyModule.Create(spec.Name);
            var assembly = (Assembly) spec.LoaderState;

            // TODO: Create a .NET PyModule interface overridding __getattr__ to do these lookups instead of stuffing the
            // module with everything. Tackle that once this has settled down a bit and we know exactly what we're trying to do.
            foreach(var type in assembly.GetTypes())
            {
                if (!module.__dict__.ContainsKey(type.Name))
                {
                    module.__dict__.Add(type.Name, type);
                }
            }
            return module;
        }
    }
}
