using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaInterpreter.ModuleImporting
{

    public class ClrModuleFinder : ISpecFinder
    {
        public List<Assembly> DefaultAssemblies;
        private ClrModuleSpec loader;

        public ClrModuleFinder()
        {
            loader = new ClrModuleSpec();
            DefaultAssemblies = new List<Assembly>();
        }

        public void AddDefaultAssembly(Assembly defaultAssembly)
        {
            if (!DefaultAssemblies.Contains(defaultAssembly))
            {
                DefaultAssemblies.Add(defaultAssembly);
            }
        }

        private PyModuleSpec findInAssemblies(string name, List<Assembly> assemblies, ClrContext loaderContext)
        {
            // TODO: Try to find a faster way to do these lookups.
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Namespace == name)
                    {
                        var spec = PyModuleSpec.Create(name, loader, "", null);
                        spec.LoaderState = loaderContext;
                        return spec;
                    }
                }

                // Still here? Maybe it's an actual type, not an assembly!
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == name)
                    {
                        var spec = PyModuleSpec.Create(name, loader, "", null);
                        spec.LoaderState = loaderContext;
                        return spec;
                    }
                }
            }
            return null;
        }

        public PyModuleSpec find_spec(FrameContext context, string name, string import_path, PyModule target)
        {
            PyModuleSpec found = null;
            ClrContext clrContext = null;
            if (context.HasVariable(ClrContext.FrameContextTokenName))
            {
                clrContext = (ClrContext) context.GetVariable(ClrContext.FrameContextTokenName);
                found = findInAssemblies(name, clrContext.AddedReferences, clrContext);
                if(found != null)
                {
                    return found;
                }
            }

            // If there isn't a CLR context yet, we still search the default assemblies and load from there.
            // Attach the context using the default modules as the added references.
            if (found == null)
            {
                found = findInAssemblies(name, DefaultAssemblies, clrContext);
                if(found != null)
                {
                    if(clrContext == null)
                    {
                        clrContext = new ClrContext();
                        clrContext.AddAssembliesToReferences(DefaultAssemblies);
                        context.AddVariable(ClrContext.FrameContextTokenName, clrContext);
                        found.LoaderState = clrContext;
                    }
                    return found;
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
        /// <returns>The loaded asset... which might be a PyModule wrapper if this is a namespace. Otherwise, the asset itself.</returns>
        public async Task<object> Load(IInterpreter interpreter, FrameContext context, PyModuleSpec spec)
        {
            // Don't get too fixated on this being a module at the end of the day.
            var module = PyModule.Create(spec.Name);
            var clrContext = (ClrContext) spec.LoaderState;

            // TODO: [CLR - OPTIMIZE GETATTR] Create a .NET PyModule interface overridding __getattr__ to do these lookups instead of stuffing the
            // module with everything. Tackle that once this has settled down a bit and we know exactly what we're trying to do.

            // Some LINQ magic could probably do this but I think I want to be able to step through it.
            bool foundAny = false;
            foreach(var assembly in clrContext.AddedReferences)
            {
                foreach(var type in assembly.GetTypes())
                {
                    if(type.Namespace == spec.Name)
                    {
                        if(!module.__dict__.ContainsKey(type.Name))
                        {
                            foundAny = true;
                            module.__dict__.Add(type.Name, type);
                        }
                    }
                }
            }

            // Maybe it's not a namespace!
            if(!foundAny)
            {
                foreach (var assembly in clrContext.AddedReferences)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == spec.Name)
                        {
                            return type;
                        }
                    }
                }
            }

            return module;
        }
    }
}
