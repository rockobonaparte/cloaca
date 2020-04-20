using System;
using System.Linq.Expressions;

namespace LanguageImplementation.DataTypes
{

    public interface ISpecLoader
    {
        PyModule find_spec(string name, string import_path, string target_module);
    }

    public class PyModuleSpecClass : PyClass
    {
        public PyModuleSpecClass(CodeObject __init__) :
            base("ModuleSpec", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyString.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyString>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyModuleSpecClass __instance;
        public static PyModuleSpecClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyModuleSpecClass(null);
                }
                return __instance;
            }
        }
    }

    public class PyModuleSpec : PyObject
    {
        public string Name
        {
            get
            {
                return (string) PyClass.__getattribute__(this, "name");
            }
            set
            {
                PyClass.__setattr__(this, "name", value);
            }
        }

        public ISpecLoader Loader
        {
            get
            {
                return (ISpecLoader)PyClass.__getattribute__(this, "loader");
            }
            set
            {
                PyClass.__setattr__(this, "loader", value);
            }
        }

        public string Origin
        {
            get
            {
                return (string) PyClass.__getattribute__(this, "origin");
            }
            set
            {
                PyClass.__setattr__(this, "origin", value);
            }
        }

        public string[] SubmoduleSearchLocations
        {
            // submodule_search_locations
            get
            {
                return (string[])PyClass.__getattribute__(this, "submodule_search_locations");
            }
            set
            {
                PyClass.__setattr__(this, "submodule_search_locations", value);
            }
        }

        public PyModuleSpec() : base(PyModuleClass.Instance)
        {
        }

        public PyModuleSpec(string name, ISpecLoader loader, string origin, string[] submodule_search_locations) : base(PyModuleClass.Instance)
        {
            Name = name;
            Loader = loader;
            Origin = origin;
            SubmoduleSearchLocations = submodule_search_locations;
        }

        public static PyModuleSpec Create(string name, ISpecLoader loader, string origin, string[] submodule_search_locations)
        {
            var spec = PyTypeObject.DefaultNew<PyModuleSpec>(PyModuleSpecClass.Instance);
            spec.Name = name;
            spec.Loader = loader;
            spec.Origin = origin;
            spec.SubmoduleSearchLocations = submodule_search_locations;
            return spec;
        }

        public override string ToString()
        {
            return "<module spec '" + Name + "'>";
        }
    }
}
