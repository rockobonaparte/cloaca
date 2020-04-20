using System;
using System.Collections.Generic;

using LanguageImplementation.DataTypes;

namespace CloacaInterpreter.ModuleImporting
{

    /// <summary>
    /// These are manually-created PyModules. They come basically pre-loaded. No questions asks. This is where
    /// you probably want to put your embedded commands. If you want to create hierarchy, then create new
    /// PyModules under other PyModules with the names of the hierarchy.
    /// 
    /// So if you have a root of "foo" then you create a PyModule named "foo." If you then want "foo.bar", create
    /// a "bar" PyModule and add it as an attribute named "bar" under the foo PyModule.
    /// </summary>
    public class InjectedModuleRepository : ISpecFinder, ISpecLoader
    {
        private Dictionary<string, PyModule> ModuleRoots;

        public InjectedModuleRepository()
        {
            ModuleRoots = new Dictionary<string, PyModule>();
        }

        public void AddNewModuleRoot(PyModule newModule)
        {
            ModuleRoots.Add(newModule.Name, newModule);
        }

        // Returns null if not found. We expect to not find some modules since the finder coexists with other finders
        // that could all get queried.
        private PyModule findModule(string name)
        {
            var splitNames = name.Split('.');

            // Since I have to look at lists with indices, it doesn't really simplify things to make this code
            // recursive. I just get the root from the 0th index and walk left-to-right, querying the modules
            // as I go.
            PyModule parent = null;
            if(!ModuleRoots.ContainsKey(splitNames[0]))
            {
                return null;
            }
            else
            {
                parent = ModuleRoots[splitNames[0]];
            }

            for(int split_i = 1; split_i < splitNames.Length; ++split_i)
            {
                if(!parent.__dict__.ContainsKey(splitNames[split_i]))
                {
                    return null;
                }
                else
                {
                    parent = (PyModule) parent.__dict__[splitNames[split_i]];
                }
            }

            return parent;
        }

        public PyModuleSpec find_spec(string name, string import_path, string target_module)
        {
            var module = findModule(name);
            if(module == null)
            {
                return null;
            }
            else
            {
                return PyModuleSpec.Create(name, this, "", null);
            }
        }

        /// <summary>
        /// This is more of a formality since the injected module loader already has the module loaded, but this gives us conformity with
        /// the module importing system.
        /// </summary>
        /// <param name="spec">The module spec we will load.</param>
        /// <returns>The loaded module, which is just a lookup into our system.</returns>
        public PyModule Load(PyModuleSpec spec)
        {
            // We're doing a double lookup but since the result gets cached, I don't consider it to be a huge performance hit. If it comes to it,
            // we can create some kind of loader class that we create for each lookup that'll just return the module.
            return findModule(spec.Name);
        }
    }
}
