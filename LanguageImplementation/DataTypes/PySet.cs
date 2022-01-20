using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LanguageImplementation.DataTypes.Exceptions;

namespace LanguageImplementation.DataTypes
{
    public class PySetClass : PyClass
    {
        public PySetClass(PyFunction __init__) :
            base("set", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PySet.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PySet>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PySetClass __instance;
        public static PySetClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PySetClass(null);
                }
                return __instance;
            }
        }

        [ClassMember]
        public static PyObject __iter__(PyObject self)
        {
            var asList = self as PySet;
            return PySetIterator.Create(asList);
        }

        [ClassMember]
        public static PyBool __contains__(PySet self, PyObject v)
        {
            if(self.set.Contains(v))
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
        public static PyBool __eq__(PySet self, object other)
        {
            var otherSet = other as PySet;
            if(otherSet == null)
            {
                return PyBool.False;
            }

            if(otherSet.set.Count != self.set.Count)
            {
                return PyBool.False;
            }

            return self.set.Equals(otherSet.set) ? PyBool.True : PyBool.False;
        }

        [ClassMember]
        public static PyBool __ne__(PySet self, PyObject other)
        {
            return !__eq__(self, other);
        }

        [ClassMember]
        public static void clear(PySet self)
        {
            self.set.Clear();
        }

        [ClassMember]
        public static void add(PySet self, PyObject toAdd)
        {
            self.set.Add(toAdd);
        }

        [ClassMember]
        public static Task<PyString> __str__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            return __repr__(interpreter, context, self);
        }

        [ClassMember]
        public static async Task<PyString> __repr__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asSet = (PySet)self;
            PyString retStr = PyString.Create("[");
            int i = 0;
            foreach(var setElement in asSet.set)
            {
                var pyObj = setElement as PyObject;
                if (pyObj != null)
                {
                    var __repr__ = pyObj.__getattribute__(PyClass.__REPR__);
                    var functionToRun = __repr__ as IPyCallable;
                    var returned = await functionToRun.Call(interpreter, context, new object[0]);
                    if (returned != null)
                    {
                        var asPyString = (PyString)returned;
                        retStr = (PyString)PyStringClass.__add__(context, retStr, asPyString);
                    }
                }
                else if(setElement == null)
                {
                    retStr = (PyString)PyStringClass.__add__(context, retStr, PyString.Create("null"));
                }
                else
                {
                    retStr = (PyString)PyStringClass.__add__(context, retStr, PyString.Create(setElement.ToString()));
                }

                // Appending commas except on last index
                if (i < asSet.set.Count - 1)
                {
                    retStr = (PyString)PyStringClass.__add__(context, retStr, PyString.Create(", "));
                }
                i += 1;
            }
            return (PyString) PyStringClass.__add__(context, retStr, PyString.Create("]"));
        }

        [ClassMember]
        public static PyInteger __len__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asList = (PySet)self;
            return PyInteger.Create(asList.set.Count);
        }
    }

    public class PySet : PyObject, IEnumerable
    {
        public HashSet<object> set;
        public PySet()
        {
            set = new HashSet<object>();
        }

        // TODO: [.NET PYCONTAINERS] Container types should be able to accept object type, not just PyObject.
        //       We could use .NET objects for a keys in a PyDict, for example.
        public PySet(HashSet<object> existingSet)
        {
            set = existingSet;
        }

        public static PySet Create(HashSet<object> existingSet)
        {
            var pySet = PyTypeObject.DefaultNew<PySet>(PySetClass.Instance);
            pySet.set = existingSet;
            return pySet;
        }

        public static PySet Create()
        {
            var pySet = PyTypeObject.DefaultNew<PySet>(PySetClass.Instance);
            return pySet;
        }

        public override bool Equals(object obj)
        {
            var asList = obj as PySet;
            if(asList == null)
            {
                return false;
            }
            else
            {
                return PySetClass.__eq__(this, asList).InternalValue;
            }
        }

        // Implemented mostly for the sake of using container assertions in NUnit.
        public IEnumerator GetEnumerator()
        {
            return set.GetEnumerator();
        }

        public override int GetHashCode()
        {
            return set.GetHashCode();
        }

        public override string ToString()
        {
            return "PySet(contents are not yet displayed)";
        }

        public void SetList(HashSet<object> newSet)
        {
            set = newSet;
        }
    }

    public class PySetIteratorClass : PyClass
    {
        private static PySetIteratorClass __instance;

        public static PySetIteratorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PySetIteratorClass(null);
                }
                return __instance;
            }
        }

        public PySetIteratorClass(PyFunction __init__) :
            base("set_iterator", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyRangeIterator.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PySetIterator>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        [ClassMember]
        public static async Task<object> __next__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asIterator = self as PySetIterator;
            return await asIterator.Next(interpreter, context, self);
        }
    }

    public class PySetIterator : PyObject, PyIterable
    {
        private IEnumerator<object> enumerator;

        public PySetIterator() : base(PySetClass.Instance)
        {

        }

        public static PySetIterator Create(PySet pySet)
        {
            var iterator = PyTypeObject.DefaultNew<PySetIterator>(PySetIteratorClass.Instance);
            iterator.enumerator = pySet.set.GetEnumerator();
            return iterator;
        }

        public async Task<object> Next(IInterpreter interpreter, FrameContext context, object selfHandle)
        {
            var asItr = selfHandle as PySetIterator;
            if(!asItr.enumerator.MoveNext())
            {
                context.CurrentException = new StopIteration();
                return null;
            }
            else
            {
                return asItr.enumerator.Current;
            }
        }
    }
}
