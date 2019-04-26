using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace LanguageImplementation
{
    public class NoneType
    {
        private static NoneType _instance;
        public static NoneType Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new NoneType();
                }
                return _instance;
            }
        }

        private NoneType()
        {
        }
    }

    public class PyTuple
    {
        public object[] values;
        public PyTuple(List<object> values)
        {
            this.values = values.ToArray();
        }

        public PyTuple(object[] values)
        {
            this.values = values;
        }
    }

    public class PyTypeObject : IPyCallable
    {
        public string Name;
        public Dictionary<string, object> __dict__;
        public IPyCallable __new__;
        public IPyCallable __init__;
        private IInterpreter interpreter;
        private FrameContext context;

        private PyObject DefaultNew(PyTypeObject typeObj)
        {
            var newObject = new PyObject();

            // Shallow copy __dict__
            DefaultNewPyObject(newObject);
            return newObject;
        }

        protected void DefaultNewPyObject(PyObject toNew)
        {
            toNew.__dict__ = new Dictionary<string, object>(__dict__);
        }

        public PyTypeObject(string name, CodeObject __init__, IInterpreter interpreter, FrameContext context)
        {
            __dict__ = new Dictionary<string, object>();
            Name = name;
            this.__init__ = __init__;

            // DefaultNew doesn't invoking any yielding code so we won't pass along its context to the wrapper.
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            this.__new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        public IEnumerable<SchedulingInfo> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            // Right now, __new__ is hard-coded because we don't have abstraction to 
            // call either Python code or built-in code.
            PyObject self = null;
            foreach (var continuation in __new__.Call(interpreter, context, new object[] { this }))
            {
                if (continuation is ReturnValue)
                {
                    var asReturnValue = continuation as ReturnValue;
                    self = asReturnValue.Returned as PyObject;
                }
                else
                {
                    yield return continuation;
                }
            }
            if (self == null)
            {
                throw new Exception("__new__ invocation did not return a PyObject");
            }

            foreach (var continuation in __init__.Call(interpreter, context, new object[] { self }))
            {
                // Suppress the self reference that gets returned since, well, we already have it.
                // We don't need it to escape upwards for cause reschedules.
                if (continuation is ReturnValue)
                {
                    continue;
                }
                else
                {
                    yield return continuation;
                }
            }

            yield return new ReturnValue(self);
        }

        //// Python internal calling process for constructing an object
        //// static PyObject * type_call(PyTypeObject* type, PyObject* args, PyObject* kwds)
        //// Since we're not in C, we can make this an object, so we don't pass in the PyTyoeObject* type;
        //// it's in our this pointer!
        //public PyObject type_call()
        //{
        //    var newObj = (PyObject) __new__.Call(new object[] { this });
        //    interpreter.CallInto(context, this.__init__, new object[] { newObj });
        //    return newObj;
        //}
    }

    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, IInterpreter interpreter, FrameContext context) :
            base(name, __init__, interpreter, context)
        {
            // __dict__ used to be set here but was moved upstream
        }
    }

    public class PyException : PyObject
    {
        private string message;

        public PyException()
        {

        }

        public PyException(PyException self, string message)
        {
            PythonPyExceptionConstructor(self, message);
        }

        public void PythonPyExceptionConstructor(PyException self, string message)
        {
            this.message = message;
        }
    }

    public class PyExceptionClass : PyClass
    {
        private PyObject __init__impl(PyException self, string message)
        {
            self.PythonPyExceptionConstructor(self, message);
            return self;
        }

        // Keeping signature of DefaultNew for consistency even though we don't need it.
        private PyObject exceptionNew(PyTypeObject typeObjIgnored)
        {
            var newObject = new PyException();

            // Shallow copy __dict__
            DefaultNewPyObject(newObject);
            return newObject;

        }

        public PyExceptionClass() : base("Exception", null, null, null)
        {
            Expression<Action<PyTypeObject>> __new__expr = instance => exceptionNew(null);
            var __new__methodInfo = ((MethodCallExpression)__new__expr.Body).Method;
            this.__new__ = new WrappedCodeObject("__init__", __new__methodInfo, this);

            Expression<Action<PyTypeObject>> __init__expr = instance => __init__impl(null, null);
            var __init__methodInfo = ((MethodCallExpression)__init__expr.Body).Method;
            this.__init__ = new WrappedCodeObject("__init__", __init__methodInfo, this);
        }
    }

    public class AttributeError : Exception
    {
        public AttributeError(string msg) : base(msg)
        {

        }
    }

    public class PyObject
    {
        public Dictionary<string, object> __dict__;
        public PyClass __class__;
        public string __doc__;
        public CodeObject __new__;
        public BigInteger __sizeof__;
        public List<PyClass> __bases__;
        public List<PyClass> __subclasses__()
        {
            throw new NotImplementedException();
        }

        public void __delattr__()
        {
            throw new NotImplementedException();
        }

        // I don't fully understand the difference between __getattribute__ and __getattr__
        // yet, but I believe I default to __getattribute__
        public object __getattr__(string name)
        {
            throw new NotImplementedException();
        }

        public object __getattribute__(string name)
        {
            if(!__dict__.ContainsKey(name))
            {
                throw new AttributeError("'" + __class__.Name + "' object has no attribute named '" + name + "'");
            }
            return __dict__[name];
        }

        public void __setattr__(string name, object value)
        {
            if(__dict__.ContainsKey(name))
            {
                __dict__[name] = value;
            }
            else
            {
                __dict__.Add(name, value);
            }
        }

        public PyObject()
        {
            __dict__ = new Dictionary<string, object>();
        }
    }
}
