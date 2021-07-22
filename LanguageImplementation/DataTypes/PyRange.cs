using LanguageImplementation.DataTypes.Exceptions;
using System;
using System.Linq.Expressions;

namespace LanguageImplementation.DataTypes
{
    public class PyRangeClass : PyClass
    {
        private static PyRangeClass __instance;
        public static PyRangeClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PyRangeClass(null);
                }
                return __instance;
            }
        }

        public PyRangeClass(CodeObject __init__) :
            base("range", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyRange.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyRange>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        [ClassMember]
        public static PyObject __iter__(PyObject self)
        {
            var asPyRange = self as PyRange;
            return PyRangeIterator.Create(asPyRange);
        }

        [ClassMember]
        public static PyInteger __len__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asPyRange = (PyRange)self;
            return PyInteger.Create(Math.Abs((asPyRange.Max - asPyRange.Min)/asPyRange.Step));
        }
    }

    public class PyRange : PyObject
    {
        public int Min;
        public int Max;
        public int Step;

        public PyRange() : base(PyRangeClass.Instance)
        {

        }

        public static PyRange Create(int min, int max, int step)
        {
            var pyRange = PyTypeObject.DefaultNew<PyRange>(PyRangeClass.Instance);
            pyRange.Min = min;
            pyRange.Max = max;
            pyRange.Step = step;

            return pyRange;
        }
    }

    public class PyRangeIteratorClass : PyClass
    {
        private static PyRangeIteratorClass __instance;
        public static PyRangeIteratorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PyRangeIteratorClass(null);
                }
                return __instance;
            }
        }

        public PyRangeIteratorClass(CodeObject __init__) :
            base("range_iterator", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyRangeIterator.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyRangeIterator>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        [ClassMember]
        public static object __next__(PyObject self)
        {
            var asIterator = self as PyRangeIterator;
            if(asIterator.Current >= asIterator.Max)
            {
                throw new StopIterationException();
            }
            else
            {
                var toReturn = PyInteger.Create(asIterator.Current);
                asIterator.Current += asIterator.Step;
                return toReturn;
            }
        }
    }

    public class PyRangeIterator : PyObject
    {
        public int Min;
        public int Max;
        public int Step;
        public int Current;

        public PyRangeIterator() : base(PyRangeClass.Instance)
        {

        }

        public static PyRangeIterator Create(PyRange range)
        {
            var iterator = PyTypeObject.DefaultNew<PyRangeIterator>(PyRangeIteratorClass.Instance);
            iterator.Min = range.Min;
            iterator.Max = range.Max;
            iterator.Step = range.Step;
            iterator.Current = iterator.Min;

            return iterator;
        }
    }
}
