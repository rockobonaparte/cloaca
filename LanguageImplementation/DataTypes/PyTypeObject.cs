using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace LanguageImplementation.DataTypes
{
    public class PyTypeObject : IPyCallable
    {
        public string Name;
        public Dictionary<string, object> __dict__;
        public IPyCallable __new__;
        public IPyCallable __init__;
        private IInterpreter interpreter;
        private FrameContext context;

        public static PyObject DefaultNew(PyTypeObject typeObj)
        {
            var newObject = new PyObject();

            // Shallow copy __dict__
            DefaultNewPyObject(newObject, typeObj);
            return newObject;
        }

        public static void DefaultNewPyObject(PyObject toNew, PyTypeObject classObj)
        {
            toNew.__dict__ = new Dictionary<string, object>(classObj.__dict__);
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
}
