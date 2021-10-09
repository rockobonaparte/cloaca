using System;
using System.Linq;
using System.Linq.Expressions;

namespace LanguageImplementation.DataTypes
{
    public class PyModuleClass : PyClass
    {
        public PyModuleClass(PyFunction __init__) :
            base("module", __init__, new PyClass[0])
        {
            __instance = this;

            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyModule>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyModuleClass __instance;
        public static PyModuleClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyModuleClass(null);
                }
                return __instance;
            }
        }
    }

    public class PyModule : PyObject
    {
        public object Name
        {
            get
            {
                return this.__dict__["__name__"];
            }
            set
            {
                this.__dict__["__name__"] = value;
            }
        }

        // Helper to give the name whether it's a string or a PyString
        public string ResolveName()
        {
            object rawName = Name;
            var asString = rawName as string;
            if(asString != null)
            {
                return asString;
            }

            var asPyString = rawName as PyString;
            if(asPyString != null)
            {
                return asPyString.ToString();
            }

            return rawName.ToString();
        }

        public object File
        {
            get
            {
                if(this.__dict__.ContainsKey("__file__"))
                {
                    return this.__dict__["__file__"];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (this.__dict__.ContainsKey("__file__"))
                {
                    this.__dict__["__file__"] = value;
                }
                else
                {
                    this.__dict__.Add("__file__", value);
                }
            }
        }

        public object Doc
        {
            get
            {
                return this.__dict__["__doc__"];
            }
            set
            {
                this.__dict__["__doc__"] = value;
            }
        }

        public PyModule() : base(PyModuleClass.Instance)
        {
        }

        public PyModule(string name) : base(PyModuleClass.Instance)
        {
            this.Name = name;
        }

        public static PyModule Create(string name, string file=null, string doc="")
        {
            var createdModule = PyTypeObject.DefaultNew<PyModule>(PyModuleClass.Instance);
            createdModule.__dict__.Add("__name__", name);
            createdModule.__dict__.Add("__doc__", doc);
            if (file != null)
            {
                createdModule.__dict__.Add("__file__", file);
            }
            return createdModule;           
        }

        public override string ToString()
        {
            return "<module '" + Name + "'>";
        }
    }
}
