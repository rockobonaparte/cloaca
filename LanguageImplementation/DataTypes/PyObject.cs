using System;
using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation.DataTypes.Exceptions;

namespace LanguageImplementation.DataTypes
{
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
    }
}
