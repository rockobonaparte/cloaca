using System;
using System.Linq.Expressions;

// BOOKMARK: Figure out how to embed a 1-step constructor inside the interpreter so we don't have to do the __new__ -> __init__ crap where it isn't applicable.
// Or alternately, don't take the self pointer as an argument when passing to native code. This might not be as bad as I currently think.
namespace LanguageImplementation.DataTypes.Exceptions
{
    /// <summary>
    /// An instance of a PyExceptionClass. These are exceptions meant to be thrown from within the
    /// interpreter and handled within the interpreter. Still, embedded code can raise them in
    /// order to give the interpreter an exception to handle.
    /// 
    /// Construction is currently a little awkward because Python exceptions are also PyObjects
    /// and are initialized in Python using the two-step __new__ and __init__ process.
    /// </summary>
    public class PyException : PyObject
    {
        public string message;

        /// <summary>
        /// Creates an empty shell of a Python exception.
        /// </summary>
        public PyException()
        {

        }

        /// <summary>
        /// Creates a new Python exception from an existing Python exception.
        /// </summary>
        /// <param name="self">An instance formally created using the default constructor</param>
        /// <param name="message">The exception's message</param>
        public PyException(PyException self, string message)
        {
            PythonPyExceptionConstructor(self, message);
        }

        /// <summary>
        /// Exposed as the __init__ for Exception() inside the interpreter.
        /// </summary>
        /// <param name="self">The exception reference</param>
        /// <param name="message">The exception message</param>
        public void PythonPyExceptionConstructor(PyException self, string message)
        {
            this.message = message;
        }
    }

    /// <summary>
    /// Wraps a PyException that escaped out of the interpreter. Nothing handled it so this
    /// will get thrown to show something very unexpected happened in the program.
    /// </summary>
    public class EscapedPyException : Exception
    {
        public PyException originalException;

        /// <summary>
        /// Create the escaped PyException.
        /// </summary>
        /// <param name="escaped">The exception that escaped.</param>
        public EscapedPyException(PyException escaped) : base(escaped.message)
        {
            originalException = escaped;
        }
    }

    /// <summary>
    /// Class object for PyException
    /// </summary>
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
            DefaultNewPyObject(newObject, this);
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
}
