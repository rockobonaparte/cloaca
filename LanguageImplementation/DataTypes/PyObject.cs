using System;
using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation.DataTypes.Exceptions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyObject
    {
        public Dictionary<string, object> __dict__;
        public PyClass __class__;
        public string __doc__;
        public IPyCallable __new__;
        public BigInteger __sizeof__;
        public PyClass[] __bases__;
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
            if (!__dict__.ContainsKey(name))
            {
                var className = __class__ != null ? __class__.Name : "(Null Class!)";
                throw new EscapedPyException(new AttributeError("'" + className + "' object has no attribute named '" + name + "'"));
            }
            return __dict__[name];
        }

        public void __setattr__(string name, object value)
        {
            if (__dict__.ContainsKey(name))
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

        public PyObject(PyTypeObject fromType)
        {
            // TODO: Determine if there needs to be additional properties.
            __dict__ = fromType.__dict__;
        }

        public Task<object> InvokeFromDict(IInterpreter interpreter, FrameContext context, string name, params PyObject[] args)
        {
            if (__dict__.ContainsKey(name))
            {
                IPyCallable toCall = __dict__[name] as IPyCallable;
                if (toCall == null)
                {
                    throw new Exception(name + " is not callable.");
                }
                PyObject[] argsWithSelf = new PyObject[args.Length + 1];
                argsWithSelf[0] = this;
                Array.Copy(args, 0, argsWithSelf, 1, args.Length);
                return toCall.Call(interpreter, context, argsWithSelf);
            }
            else
            {
                throw new NotImplementedException("This object does not implement " + name);
            }
        }
    }
}
