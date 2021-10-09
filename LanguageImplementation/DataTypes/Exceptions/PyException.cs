using System;
using System.Linq.Expressions;
using System.Text;

namespace LanguageImplementation.DataTypes.Exceptions
{
    /// <summary>
    /// An instance of a PyExceptionClass. These are exceptions meant to be thrown from within the
    /// interpreter and handled within the interpreter. Still, embedded code can raise them in
    /// order to give the interpreter an exception to handle--using EscapedPyException in particular.
    /// </summary>
    public class PyException : PyObject
    {
        public const string TracebackName = "tb";

        public PyTraceback tb
        {
            get
            {
                return (PyTraceback)__getattribute__(TracebackName);
            }
            set
            {
                __setattr__(TracebackName, value);
            }
        }

        public string Message
        {
            get
            {
                return (string)__getattribute__("message");
            }
            set
            {
                __setattr__("message", value);
            }
        }

        /// <summary>
        /// Creates an empty shell of a Python exception.
        /// </summary>
        public PyException()
        {
            __class__ = PyExceptionClass.Instance;
            tb = null;
            Message = null;
        }

        /// <summary>
        /// Creates an empty shell of a Python exception subclass.
        /// </summary>
        public PyException(PyClass subclass)
        {
            __class__ = subclass;
            tb = null;
            Message = null;
        }

        /// <summary>
        /// Create a new Python exception based on a message
        /// </summary>
        /// <param name="message">The exception message</param>
        public PyException(string message)
        {
            __class__ = PyExceptionClass.Instance;
            PyTypeObject.DefaultNewPyObject(this, PyExceptionClass.Instance);
            tb = null;
            Message = message;
        }

        /// <summary>
        /// Create a new Python exception based on a PyString message
        /// </summary>
        /// <param name="message">The exception message as a PyString</param>
        public PyException(PyString message) : this(message.InternalValue)
        {
        }

        /// <summary>
        /// Exposed as the __init__ for Exception() inside the interpreter. The self parameter
        /// is exposed in order to invoke the constructor using the class Python class method
        /// signature where the 'this' pointer is the first argument. It is just a handle to
        /// the current object.
        /// </summary>
        /// <param name="self">The exception reference created from __init__. Included for API consistency to Python
        /// but otherwise not useful in C# since we impliciy have the this pointer.</param>
        /// <param name="message">The exception message</param>
        public void PythonPyExceptionConstructor(PyException self, string message)
        {
            Message = message;
            tb = null;
        }
    }

    /// <summary>
    /// Wraps a PyException that escaped out of the interpreter. Nothing handled it so this
    /// will get thrown to show something very unexpected happened in the program.
    /// </summary>
    public class EscapedPyException : Exception
    {
        public PyObject OriginalException;

        private static string extractExceptionMessage(PyObject exc)
        {
            var tb = (PyTraceback)exc.__getattribute__(PyException.TracebackName);
            var b = new StringBuilder();
            b.AppendLine("Traceback (most recent call list):");
            if (tb != null)
            {
                b.AppendLine(tb.DumpStack());
            }
            else
            {
                b.AppendLine("No traceback available. This exception was probably created outside of a running program.");
            }
            if (exc.__dict__.ContainsKey("message"))
            {
                b.AppendLine((string)exc.__getattribute__("message"));
            }
            return b.ToString();
        }

        /// <summary>
        /// Create the escaped PyException.
        /// </summary>
        /// <param name="escaped">The exception that escaped.</param>
        public EscapedPyException(PyObject escaped) : base(extractExceptionMessage(escaped))
        {
            OriginalException = escaped;
        }
    }

    /// <summary>
    /// Class object for PyException
    /// </summary>
    public class PyExceptionClass : PyClass
    {
        /// <summary>
        /// Embedded implementation of a Python Exception constructor.
        /// </summary>
        /// <param name="self">The PyObject to use as an exception created via __new__</param>
        /// <param name="message">The Exception message</param>
        /// <returns>The same object, properly populated. This is the convention for constructors,
        /// although the interpreter just assumes the object it passed in is the returned value anyways.
        /// </returns>
        private PyObject __init__impl(PyException self, string message)
        {
            self.PythonPyExceptionConstructor(self, message);
            return self;
        }

        private PyObject __init__impl(PyException self, PyString message)
        {
            return __init__impl(self, message.InternalValue);
        }

        // TODO: Migrate to a built-in that can be invoked as necessary. One problem will be to fetch it out to run from C# code to create exceptions.
        private static PyExceptionClass instance = null;
        public static PyExceptionClass Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new PyExceptionClass();
                }
                return instance;
            }           
        }

        public PyExceptionClass() : base("Exception", null, new PyClass[0])
        {
            Expression<Action<PyTypeObject>> __new__expr = instance => DefaultNew<PyException>(null);
            var __new__methodInfo = ((MethodCallExpression)__new__expr.Body).Method;
            this.__new__ = new WrappedCodeObject("__new__", __new__methodInfo, this);

            Expression<Action<PyTypeObject>> __init__expr = instance => __init__impl(null, null);
            var __init__methodInfo = ((MethodCallExpression)__init__expr.Body).Method;
            this.__init__ = new WrappedCodeObject("__init__", __init__methodInfo, this);
        }

        public PyExceptionClass(string classname, PyFunction __init__, PyClass[] bases) : base(classname, __init__, bases)
        {
            Expression<Action<PyTypeObject>> __new__expr = instance => DefaultNew<PyException>(null);
            var __new__methodInfo = ((MethodCallExpression)__new__expr.Body).Method;
            this.__new__ = new WrappedCodeObject("__new__", __new__methodInfo, this);

            Expression<Action<PyTypeObject>> __init__expr = instance => __init__impl(null, null);
            var __init__methodInfo = ((MethodCallExpression)__init__expr.Body).Method;
            this.__init__ = new WrappedCodeObject("__init__", __init__methodInfo, this);
        }
    }
}
