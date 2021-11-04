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
        // These values can be all kinds of stuff. I tried with int, float, and str in Python and it was happy to make them.
        public object Start;
        public object Step;
        public object Stop;

        public PySlice() : base(PySliceClass.Instance)
        {

        }

        public static PySlice Create(object start, object stop, object step)
        {
            var pySlice = PyTypeObject.DefaultNew<PySlice>(PySliceClass.Instance);
            pySlice.Start = start;
            pySlice.Stop = stop;
            pySlice.Step = step;

            return pySlice;
        }

        public static PySlice Create(object start, object stop)
        {
            return Create(start, stop, 1);
        }

        public static PySlice Create(object stop)
        {
            return Create(0, stop, 1);
        }

        public override bool Equals(object obj)
        {
            var asSlice = obj as PySlice;
            if(asSlice == null)
            {
                return false;
            }

            // This is annoying to do due to mismatching of null
            if(Start == null && asSlice != null)
            {
                return false;
            }
            else if(!Start.Equals(asSlice.Start))
            {
                return false;
            }

            if (Stop == null && asSlice != null)
            {
                return false;
            }
            else if (!Stop.Equals(asSlice.Stop))
            {
                return false;
            }

            if (Step == null && asSlice != Step)
            {
                return false;
            }
            else if (!Step.Equals(asSlice.Step))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            hash += Start != null ? Start.GetHashCode() : 0;
            hash += Stop != null ? Stop.GetHashCode() : 0;
            hash += Step != null ? Step.GetHashCode() : 0;
            return hash;
        }
    }
}
