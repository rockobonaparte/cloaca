using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyFunction : PyObject, IPyCallable
    {
        // needs a reference to:
        // 1. CodeObject
        // 2. Globals           (and how does it get this? When would it be created?)
        public IPyCallable Callable;

        public PyFunction()
        {
            // Default constructor so DefaultNew will work.
        }

        public PyFunction(IPyCallable callable)
        {
            this.Callable = callable;
            __setattr__("__call__", this);
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            return Callable.Call(interpreter, context, args);
        }

        public static PyFunction Create(CodeObject co)
        {
            var function = PyTypeObject.DefaultNew<PyFunction>(PyFloatClass.Instance);
            function.Callable = co;
            return function;
        }
    }

    public class PyFunctionClass : PyClass
    {
        public PyFunctionClass(CodeObject __init__) :
            base("function", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyFunctionClass.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyFunction>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyFunctionClass __instance;
        public static PyFunctionClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PyFunctionClass(null);
                }
                return __instance;
            }
        }

    }
}