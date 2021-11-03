using LanguageImplementation.DataTypes.Exceptions;
using System;
using System.Linq.Expressions;

namespace LanguageImplementation.DataTypes
{
    public class PySliceClass : PyClass
    {
        private static PySliceClass __instance;
        public static PySliceClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PySliceClass(null);
                }
                return __instance;
            }
        }

        public PySliceClass(PyFunction __init__) :
            base("slice", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PySlice.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PySlice>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        [ClassMember]
        public static PyObject indices(PyObject self, PyObject index)
        {
            throw new NotImplementedException("We don't understand the PySlice indices method enough to implement it yet.");
        }

    }

    public class PySlice : PyObject
    {
        public int Start;
        public int Step;
        public int Stop;

        public PySlice() : base(PySliceClass.Instance)
        {

        }

        public static PySlice Create(int start, int stop, int step)
        {
            var pySlice = PyTypeObject.DefaultNew<PySlice>(PySliceClass.Instance);
            pySlice.Start = start;
            pySlice.Stop = stop;
            pySlice.Step = step;

            return pySlice;
        }

        public static PySlice Create(int start, int stop)
        {
            return Create(start, stop, 1);
        }

        public static PySlice Create(int stop)
        {
            return Create(0, stop, 1);
        }
    }
}
