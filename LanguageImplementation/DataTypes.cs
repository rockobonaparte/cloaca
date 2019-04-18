using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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

    public class PyTypeObject
    {
        public string Name;
        public Dictionary<string, object> __dict__;
        public WrappedCodeObject __new__;
        public CodeObject __init__;
        private IInterpreter interpreter;
        private FrameContext context;

        private PyObject DefaultNew(PyTypeObject typeObj)
        {
            var newObject = new PyObject();

            // Shallow copy __dict__
            newObject.__dict__ = new Dictionary<string, object>(__dict__);
            return newObject;
        }

        public PyTypeObject(string name, CodeObject __init__, IInterpreter interpreter, FrameContext context)
        {
            __dict__ = new Dictionary<string, object>();
            Name = name;
            this.__init__ = __init__;

            Expression<Action<PyTypeObject>> expr = instance => DefaultNew(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            this.__new__ = new WrappedCodeObject(context, "__new__", methodInfo, this);
        }

        // Python internal calling process for constructing an object
        // static PyObject * type_call(PyTypeObject* type, PyObject* args, PyObject* kwds)
        // Since we're not in C, we can make this an object, so we don't pass in the PyTyoeObject* type;
        // it's in our this pointer!
        public PyObject type_call()
        {
            var newObj = (PyObject) __new__.Call(new object[] { this });
            interpreter.CallInto(context, this.__init__, new object[] { newObj });
            return newObj;
        }
    }

    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, IInterpreter interpreter, FrameContext context) :
            base(name, __init__, interpreter, context)
        {
            // __dict__ used to be set here but was moved upstream
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
