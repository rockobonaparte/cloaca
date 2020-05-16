using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyTupleClass : PyClass
    {
        public PyTupleClass(CodeObject __init__) :
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

        [ClassMember]
        public static PyObject __getitem__(PyTuple self, PyInteger i)
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

        // TODO: Test
        [ClassMember]
        public static PyBool __eq__(PyTuple self, PyObject other)
        {
            var otherList = other as PyTuple;
            if (otherList == null)
            {
                return PyBool.False;
            }

            if (otherList.Values.Length != self.Values.Length)
            {
                return PyBool.False;
            }

            for (int i = 0; i < self.Values.Length; ++i)
            {
                if (self.Values[i].__eq__(otherList.Values[i]).InternalValue == false)
                {
                    return PyBool.False;
                }
            }
            return PyBool.True;
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
                var pyObj = asTuple.Values[i];

                var __repr__ = pyObj.__getattribute__(PyClass.__REPR__);
                var functionToRun = __repr__ as IPyCallable;

                var returned = await functionToRun.Call(interpreter, context, new object[] { pyObj });
                if (returned != null)
                {
                    var asPyString = (PyString)returned;
                    retStr = (PyString)PyStringClass.__add__(retStr, asPyString);
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
    }

    public class PyTuple : PyObject, IEnumerable
    {
        public PyObject[] Values;

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

        public static PyTuple Create(List<PyObject> values)
        {
            var pyTuple = PyTypeObject.DefaultNew<PyTuple>(PyTupleClass.Instance);
            pyTuple.Values = values.ToArray();
            return pyTuple;
        }

        public static PyTuple Create(PyObject[] values)
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

}
