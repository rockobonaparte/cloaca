using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{

    public interface ISpecLoader
    {
        /// <summary>
        /// Load the module. This is asynchronous because the module may need to run some code, and that code may
        /// have to yield. Since it has to run code, it needs a handle to the interpreter as well as the context into
        /// which to load the module.
        /// </summary>
        /// <param name="interpreter">Interpreter with which to run any module bootstrapping code.</param>
        /// <param name="context">Context on which to attach any code that has to be run while loading the module.</param>
        /// <param name="spec">The module spec to load.</param>
        /// <returns></returns>
        Task<PyModule> Load(IInterpreter interpreter, FrameContext context, PyModuleSpec spec);
    }

    public interface ISpecFinder
    {
        /// <summary>
        /// Locates a module binding spec. This is using the same signature of the Python equivalent--including
        /// the formatting of the name of the method (find_spec).
        /// 
        /// It's not assumed that the finder will even find a binding spec for this module; it will just return
        /// null in that situation.
        /// </summary>
        /// <param name="context">The context at the current place in the callstack where the module was requested.
        /// This isn't directly part of the API when making a Python version of this, but we needed it for some of the
        /// non-Python implementations. Also, I suppose a Python implementation could get at it through various means
        /// regardless (A Python invocation internally would get it).</param>
        /// <param name="name">The name of the module to import.</param>
        /// <param name="import_path">The path from which to import the module (if applicable).</param>
        /// <param name="target">(Optional, can be null) Module object to use as the target for loading the module.
        /// Normally this is null, which means the target module has not been created yet. According to PEP 451, this
        /// is used in particular when reloading a module.</param>
        /// <returns></returns>
        PyModuleSpec find_spec(FrameContext context, string name, string import_path, PyModule target);
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

        /// <summary>
        /// Per https://www.python.org/dev/peps/pep-0451/
        /// loader_state - a container of extra module-specific data for use during loading.
        /// </summary>
        public object LoaderState
        {
            get
            {
                return PyClass.__getattribute__(this, "loader_state");
            }
            set
            {
                PyClass.__setattr__(this, "loader_state", value);
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
