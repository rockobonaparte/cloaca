using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace LanguageImplementation.DataTypes
{
    public class PyTypeObject : PyObject, IPyCallable
    {
        public string Name;
        public IPyCallable __init__;
        private IInterpreter interpreter;
        private FrameContext context;

        /// <summary>
        /// A generic __new__ implementation for embedded class to create various kinds of PyObject;
        /// the specific type of PyObject to create is given by the generic argument.
        /// </summary>
        /// <typeparam name="T">The type of PyObject to create. It must have a default constructor.</typeparam>
        /// <param name="typeObj">An instance of the class with which to popular the object's __dict__.</param>
        /// <returns>An instance of the object. It still needs to be constructed with __init__, but its class
        /// properties have been stubbed in.
        /// </returns>
        public static T DefaultNew<T>(PyTypeObject typeObj) where T : PyObject, new()
        {
            var newObject = new T();

            // Shallow copy __dict__
            DefaultNewPyObject(newObject, typeObj);
            return newObject;
        }

        /// <summary>
        /// The default __new__ implementation for objects. This works with a 
        /// generic PyObject.
        /// </summary>
        /// <param name="typeObj">The actual type object.</param>
        /// <returns>A PyObject handle that can subsequently by initialized with __init__ from the
        /// particular class that needs a new instance.
        /// </returns>
        public static PyObject DefaultNew(PyTypeObject typeObj)
        {
            return DefaultNew<PyObject>(typeObj);
        }

        public static void DefaultNewPyObject(PyObject toNew, PyTypeObject classObj)
        {
            toNew.__dict__ = new Dictionary<string, object>(classObj.__dict__);
            toNew.__class__ = (PyClass) classObj;

            // Class functions become bound methods when we have an instance.
            // Gotta love how you can't modify dictionaries while iterating them...
            // TODO: Is the default __new__ actually the properly place to put this?
            var methods = new List<PyMethod>();
            var methodKeys = new List<string>();            
            foreach(var key in toNew.__dict__.Keys)
            {
                var val = toNew.__dict__[key];
                if(val is CodeObject)
                {
                    methods.Add(new PyMethod(toNew, val as CodeObject));
                    methodKeys.Add(key);
                }
            }

            for(int i = 0; i < methods.Count; ++i)
            {
                toNew.__dict__[methodKeys[i]] = methods[i];
            }
        }

        public PyTypeObject(string name, CodeObject __init__, IInterpreter interpreter, FrameContext context)
        {
            __dict__ = new Dictionary<string, object>();
            Name = name;
            this.__init__ = __init__;
            __dict__["__init__"] = this.__init__;

            // DefaultNew doesn't invoking any yielding code so we won't pass along its context to the wrapper.
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            this.__new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        /// <summary>
        /// Data types can be called directly. Consider class constructors in particular. So this is an implementation
        /// of IPyCallable that runs the __new__ -> __init__ chain that happens when a class is invoked. The
        /// implementations of __new__ and __init__ have embedded defaults that *don't* yield, but they could have
        /// been specified with Python scripts in the program and DO yield, hence it yields IEnumerables. When it
        /// returns a ReturnValue of the self pointer, then it has finished object initialization and you have a new
        /// instance of the given type.
        /// </summary>
        /// <param name="interpreter">The interpreter instance that has invoked this code.</param>
        /// <param name="context">The call stack and state at the time this code was invoked.</param>
        /// <param name="args">Arguments given to this class's call.</param>
        /// <returns>A fully-initialized instance of this type, with __new__ and __init__ invoked.</returns>
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
    }
}
