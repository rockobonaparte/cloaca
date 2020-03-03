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

        [ClassMember]
        public static new object __getattribute__(PyObject self, string name)
        {
            var asPySuper = self as PySuper;

            // We can't use __getattribute__ to shovel out __this_class__ so we'll just hit the dict directly.
            var parentClass = asPySuper.__dict__["__this_class__"] as PyClass;
            var returnAttr = parentClass.__getattribute__(name);

            // Welcome to hacktown! If we got a PyMethod, we're going to swap out self for the one we want!
            // Note that this is not supposed to be a long-term thing. Heck, this shouldn't even be in __getattribute__.
            // I need to form a cogent question about how PySuper_Type really works.
            var asMethod = returnAttr as PyMethod;
            if(asMethod != null)
            {
                asMethod.selfHandle = asPySuper.__dict__["__self__"] as PyObject;
                return asMethod;
            }
            return returnAttr;
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

        /// <summary>
        /// Redirect to our SPECIAL __getattribute__.
        /// </summary>
        /// <param name="name">The attribute to look up</param>
        /// <returns>Looked up attribute from the parent class</returns>
        /// <seealso cref="PySuperType.__getattribute__(PyObject, string)"/>
        public override object __getattribute__(string name)
        {
            return PySuperType.__getattribute__(this, name);
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
