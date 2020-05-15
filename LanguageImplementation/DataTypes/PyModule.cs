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
        public string Name;
        public PyModule() : base(PyModuleClass.Instance)
        {
        }

        public PyModule(string name) : base(PyModuleClass.Instance)
        {
            this.Name = name;
        }

        public static PyModule Create(string name)
        {
            var createdModule = PyTypeObject.DefaultNew<PyModule>(PyModuleClass.Instance);
            createdModule.Name = name;
            createdModule.__dict__.Add("__name__", name);
            return createdModule;           
        }

        public override string ToString()
        {
            return "<module '" + Name + "'>";
        }
    }
}
