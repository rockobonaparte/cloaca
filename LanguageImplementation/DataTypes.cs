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
        public WrappedCodeObject __new__;
        public CodeObject __init__;
        private IInterpreter interpreter;

        private PyObject DefaultNew(PyTypeObject typeObj)
        {
            return new PyObject();
        }

        public PyTypeObject(string name, CodeObject __init__, IInterpreter interpreter)
        {
            Name = name;
            this.__init__ = __init__;

            Expression<Action<PyTypeObject>> expr = instance => DefaultNew(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            this.__new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        // Python internal calling process for constructing an object
        // static PyObject * type_call(PyTypeObject* type, PyObject* args, PyObject* kwds)
        // Since we're not in C, we can make this an object, so we don't pass in the PyTyoeObject* type;
        // it's in our this pointer!
        public PyObject type_call()
        {
            var newObj = (PyObject) __new__.Call(new object[] { this });
            interpreter.CallInto(this.__init__, new object[] { newObj });
            return newObj;
        }
    }

    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, IInterpreter interpreter) :
            base(name, __init__, interpreter)
        {
        }
    }

    public class PyObject
    {
        public PyClass __class__;
        public string __doc__;
        public CodeObject __new__;
        public BigInteger __sizeof__;
        public Dictionary<string, PyObject> __dict__;
        public List<PyClass> __bases__;
        public List<PyClass> __subclasses__()
        {
            throw new NotImplementedException();
        }

        public void __delattr__()
        {
            throw new NotImplementedException();
        }

        public void __getattribute__(string name)
        {
            throw new NotImplementedException();
        }

        public void __setattr__(string name, PyObject value)
        {
            throw new NotImplementedException();
        }
    }
}
