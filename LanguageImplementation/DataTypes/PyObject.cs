using System;
using System.Numerics;
using System.Collections.Generic;

using LanguageImplementation.DataTypes.Exceptions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyObject
    {
        // TODO: Make map of string to PyObject
        public Dictionary<string, object> __dict__;
        public PyClass __class__;
        public string __doc__;
        public IPyCallable __new__;
        public BigInteger __sizeof__;
        public PyClass[] __bases__;

        /// <summary>
        /// Gets a list of all the classes that are the direct CHILD of this class. Yes, CHILD.
        /// </summary>
        /// <returns>List of subclasses of this class.</returns>
        public List<PyClass> __subclasses__()
        {
            throw new NotImplementedException();
        }

        public void __delattr__()
        {
            throw new NotImplementedException();
        }

        public object __getattr__(string name)
        {
            throw new NotImplementedException();
        }

        public object __getattribute__(string name)
        {
            return PyClass.__getattribute__(this, name);
        }

        public void __setattr__(string name, object value)
        {
            PyClass.__setattr__(this, name, value);
        }

        public PyBool __eq__(PyObject other)
        {
            return Equals(other);
        }

        public PyBool __ne__(PyObject other)
        {
            return !Equals(other);
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
            var toCall = __getattribute__(name) as IPyCallable;
            if (toCall == null)
            {
                throw new Exception(name + " is not callable.");
            }
            return toCall.Call(interpreter, context, args);
        }
    }
}
