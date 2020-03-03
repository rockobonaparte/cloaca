using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace LanguageImplementation.DataTypes
{
    public class PySuperType : PyClass
    {
        public PySuperType(CodeObject __init__) :
            base("super", __init__, new PyClass[0])
        {
            __instance = this;

            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PySuper>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PySuperType __instance;
        public static PySuperType Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PySuperType(null);
                }
                return __instance;
            }
        }
    }

    public class PySuper : PyObject
    {
        /// <summary>
        /// This is for DefaultNew. Generally you don't want to use this yourself.
        /// </summary>
        public PySuper()
        {
            
        }

        public static PySuper Create(PyObject self, PyClass superclass)
        {
            var instance = PyTypeObject.DefaultNew<PySuper>(PySuperType.Instance);
            // TODO: Also test for NoneType and assign NoneType
            if(self == null)
            {
                instance.__setattr__("__self__", null);
                instance.__setattr__("__self_class__", null);
            }
            else
            {
                instance.__setattr__("__self__", self);
                instance.__setattr__("__self_class__", self.__class__);
            }

            instance.__setattr__("__this_class__", superclass);
            return instance;
        }


    }
}
