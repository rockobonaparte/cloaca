using LanguageImplementation.DataTypes.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyTupleClass : PyClass
    {
        public PyTupleClass(PyFunction __init__) :
            base("tuple", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyTuple.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyTuple>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyTupleClass __instance;
        public static PyTupleClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PyTupleClass(null);
                }
                return __instance;
            }
        }

        [ClassMember]
        public static PyObject __iter__(PyObject self)
        {
            var asTuple = self as PyTuple;
            return PyTupleIterator.Create(asTuple);
        }

        [ClassMember]
        public static PyBool __contains__(PyTuple self, PyObject v)
        {
            if (Array.IndexOf(self.Values, v) >= 0)
            {
                return PyBool.True;
            }
            else
            {
                return PyBool.False;
            }
        }

        // TODO: Test
        [ClassMember]
        public static PyBool __eq__(PyTuple self, PyObject other)
        {
            var otherTuple = other as PyTuple;
            if (otherTuple == null)
            {
                return PyBool.False;
            }

            if (otherTuple.Values.Length != self.Values.Length)
            {
                return PyBool.False;
            }

            for (int i = 0; i < self.Values.Length; ++i)
            {
                var selfValue = self.Values[i] as PyObject;
                var otherValue = otherTuple.Values[i] as PyObject;

                if(selfValue != null && otherValue != null)
                {
                    if (selfValue.__eq__(otherValue).InternalValue == false)
                    {
                        return PyBool.False;
                    }
                }
                else
                {
                    // TODO: [TUPLE OBJECT] Support regular objects in tuples along with dunders like __eq__
                    throw new NotImplementedException("PyTuple cannot compare non-PyObject types yet");
                }

            }
            return PyBool.True;
        }

        [ClassMember]
        public static object __getitem__(PyTuple self, PyInteger i)
        {
            try
            {
                return self.Values[(int)i.InternalValue];
            }
            catch (IndexOutOfRangeException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("IndexError: tuple index out of range");
            }
        }

        [ClassMember]
        public static Task<PyString> __str__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            return __repr__(interpreter, context, self);
        }

        [ClassMember]
        public static async Task<PyString> __repr__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asTuple = (PyTuple)self;
            PyString retStr = PyString.Create("(");
            for (int i = 0; i < asTuple.Values.Length; ++i)
            {
                var pyObj = asTuple.Values[i] as PyObject;
                if (pyObj == null)
                {
                    retStr = (PyString)PyStringClass.__add__(retStr, PyString.Create(asTuple.Values[i].ToString()));
                }
                else
                {

                    var __repr__ = pyObj.__getattribute__(PyClass.__REPR__);
                    var functionToRun = __repr__ as IPyCallable;

                    var returned = await functionToRun.Call(interpreter, context, new object[] { pyObj });
                    if (returned != null)
                    {
                        var asPyString = (PyString)returned;
                        retStr = (PyString)PyStringClass.__add__(retStr, asPyString);
                    }
                }

                // Appending commas except on last index
                if (i < asTuple.Values.Length - 1)
                {
                    retStr = (PyString)PyStringClass.__add__(retStr, PyString.Create(", "));
                }
            }
            return (PyString)PyStringClass.__add__(retStr, PyString.Create(")"));
        }

        [ClassMember]
        public static PyBool __ne__(PyTuple self, PyObject other)
        {
            return !__eq__(self, other);
        }

        [ClassMember]
        public static PyInteger __len__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asTuple = (PyTuple)self;
            return PyInteger.Create(asTuple.Values.Length);
        }

        // TODO: [TUPLE DUNDERS] Supporting remaining tuple features by implementing the remaining dunders.
        //       You only implemented the bare minimum to use them with variable arguments. Look at PyList for
        //       some helpful takeaways.
    }

    public class PyTuple : PyObject, IEnumerable
    {
        public object[] Values;

        public PyTuple()
        {
        }

        public PyTuple(List<PyObject> values)
        {
            this.Values = values.ToArray();
        }

        public PyTuple(PyObject[] values)
        {
            this.Values = values;
        }

        public static PyTuple Create(List<object> values)
        {
            var pyTuple = PyTypeObject.DefaultNew<PyTuple>(PyTupleClass.Instance);
            pyTuple.Values = values.ToArray();
            return pyTuple;
        }

        public static PyTuple Create(object[] values)
        {
            var pyTuple = PyTypeObject.DefaultNew<PyTuple>(PyTupleClass.Instance);
            pyTuple.Values = values;
            return pyTuple;
        }

        public override bool Equals(object obj)
        {
            var asTuple = obj as PyTuple;
            if (asTuple == null)
            {
                return false;
            }
            else
            {
                return PyTupleClass.__eq__(this, asTuple).InternalValue;
            }
        }

        // Implemented mostly for the sake of using container assertions in NUnit.
        public IEnumerator GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public override int GetHashCode()
        {
            return Values.GetHashCode();
        }

        public override string ToString()
        {
            return "PyTuple(contents are not yet displayed)";
        }
    }

    public class PyTupleIteratorClass : PyClass
    {
        private static PyTupleIteratorClass __instance;

        public static PyTupleIteratorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PyTupleIteratorClass(null);
                }
                return __instance;
            }
        }

        public PyTupleIteratorClass(PyFunction __init__) :
            base("tuple_iterator", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyRangeIterator.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyTupleIterator>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        [ClassMember]
        public static object __next__(PyObject self)
        {
            var asIterator = self as PyTupleIterator;
            if (asIterator.CurrentIdx >= asIterator.IteratedTuple.Values.Length)
            {
                throw new StopIterationException();
            }
            else
            {
                asIterator.CurrentIdx += 1;
                return PyTupleClass.__getitem__(asIterator.IteratedTuple, asIterator.CurrentIdx - 1);
            }
        }
    }

    public class PyTupleIterator : PyObject
    {
        public int CurrentIdx;
        public PyTuple IteratedTuple;

        public static PyTupleIterator Create(PyTuple tuple)
        {
            var iterator = PyTypeObject.DefaultNew<PyTupleIterator>(PyTupleIteratorClass.Instance);
            iterator.CurrentIdx = 0;
            iterator.IteratedTuple = tuple;
            return iterator;
        }
    }

}
