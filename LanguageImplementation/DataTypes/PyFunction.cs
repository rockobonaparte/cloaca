using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyFunction : PyObject, IPyCallable
    {
        // needs a reference to:
        // 1. CodeObject
        // 2. Globals           (and how does it get this? When would it be created?)
        private CodeObject _code;
        public Dictionary<string, object> Globals;

        public CodeObject Code
        {
            get
            {
                return _code;
            }
            set
            {
                _code = value;
            }
        }

        public PyFunction()
        {
            // Default constructor so DefaultNew will work.
        }

        public PyFunction(CodeObject callable, Dictionary<string, object> globals)
        {
            Initialize(callable, globals);
        }

        public void Initialize(CodeObject callable, Dictionary<string, object> globals)
        {
            Code = callable;
            Globals = globals;
            __setattr__("__call__", this);
            __setattr__("__code__", this);
            __setattr__("__globals__", Globals);
        }
        
        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args,
                                 Dictionary<string, object> defaultOverrides = null,
                                 KwargsDict kwargsDict = null)
        {
            return interpreter.CallInto(context, this, args, Globals);
        }

        public static PyFunction Create(CodeObject co, Dictionary<string, object> globals)
        {
            var function = PyTypeObject.DefaultNew<PyFunction>(PyFunctionClass.Instance);
            function.Initialize(co, globals);
            return function;
        }
    }

    public class PyFunctionClass : PyClass
    {
        public PyFunctionClass(PyFunction __init__) :
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