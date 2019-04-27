using System;
using System.Linq.Expressions;

namespace LanguageImplementation.DataTypes.Exceptions
{
    public class PyException : PyObject
    {
        public string message;

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

    // Use when the exception gets raised out of the root context.
    public class EscapedPyException : Exception
    {
        public PyException originalException;
        public EscapedPyException(PyException original) : base(original.message)
        {
            originalException = original;
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
