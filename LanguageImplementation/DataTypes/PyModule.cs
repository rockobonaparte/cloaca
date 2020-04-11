using System;
using System.Linq;
using System.Linq.Expressions;

namespace LanguageImplementation.DataTypes
{
    public class PyModuleClass : PyClass
    {
        public PyModuleClass(CodeObject __init__) :
            base("module", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyString.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyString>(null);
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
        public string name;
        public PyModule() : base(PyModuleClass.Instance)
        {
        }

        public PyModule(string name) : base(PyModuleClass.Instance)
        {
            this.name = name;
        }

        public static PyModule Create(string name)
        {
            return PyTypeObject.DefaultNew<PyModule>(PyModuleClass.Instance);
        }

        public override string ToString()
        {
            return "<module '" + name + "'>";
        }
    }
}
