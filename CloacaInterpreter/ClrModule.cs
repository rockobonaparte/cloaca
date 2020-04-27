using LanguageImplementation.DataTypes;
using System.Collections.Generic;
using System.Reflection;

namespace CloacaInterpreter
{
    /// <summary>
    /// Start of a CLR module a la IronPython or Python.NET. The internals are obfuscated and expodes as a PyModule.
    /// This is not yet connected to anything and only does the basic assembly load; it's not connected into the
    /// namespace yet.
    /// </summary>
    public class ClrModuleInternals
    {
        private List<Assembly> references;

        public ClrModuleInternals()
        {
            references = new List<Assembly>();
        }

        private void addReference(string name)
        {
            // TODO: Don't double-load-add an existing assembly.
            var assembly = Assembly.Load(name);
            references.Add(assembly);
        }

        /// <summary>
        /// Intended to be called once by the Cloaca interpreter to create the clr module in an injectable
        /// module loader.
        /// </summary>
        /// <returns></returns>
        public static PyModule CreateClrModule()
        {
            var internals = new ClrModuleInternals();
            var module = PyModule.Create("clr");
            module.__dict__.Add("AddReference", internals.GetType().GetMethod("addReference"));
            module.__dict__.Add("References", internals.references);
            return module;
        }
    }
}
