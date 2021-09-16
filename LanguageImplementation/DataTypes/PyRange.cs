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

        [ClassMember]
        public static PyObject __reversed__(PyObject self)
        {
            var asPyRange = self as PyRange;
            return PyRangeIterator.Create(asPyRange.Max, asPyRange.Min, -1 * asPyRange.Step);
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
        public static object __next__(FrameContext context, PyObject self)
        {
            var asIterator = self as PyRangeIterator;
            if(asIterator.Step > 0 && asIterator.Current >= asIterator.Stop)
            {
                context.CurrentException = new StopIteration();
                return null;
            }
            else if (asIterator.Step < 0 && asIterator.Current <= asIterator.Stop)
            {
                context.CurrentException = new StopIteration();
                return null;
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
        public int Start;
        public int Stop;
        public int Step;
        public int Current;

        public PyRangeIterator() : base(PyRangeClass.Instance)
        {

        }

        public static PyRangeIterator Create(PyRange range)
        {
            var iterator = PyTypeObject.DefaultNew<PyRangeIterator>(PyRangeIteratorClass.Instance);
            iterator.Start = range.Min;
            iterator.Stop = range.Max;
            iterator.Step = range.Step;
            iterator.Current = iterator.Start;

            return iterator;
        }

        public static PyRangeIterator Create(int start, int stop, int step)
        {
            var iterator = PyTypeObject.DefaultNew<PyRangeIterator>(PyRangeIteratorClass.Instance);
            iterator.Start = start;
            iterator.Stop = stop;
            iterator.Step = step;
            iterator.Current = start;

            return iterator;
        }
    }
}
