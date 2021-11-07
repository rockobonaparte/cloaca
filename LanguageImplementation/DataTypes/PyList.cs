﻿using LanguageImplementation.DataTypes.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyListClass : PyClass
    {
        public PyListClass(PyFunction __init__) :
            base("list", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyList.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyList>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyListClass __instance;
        public static PyListClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyListClass(null);
                }
                return __instance;
            }
        }

        [ClassMember]
        public static PyObject __iter__(PyObject self)
        {
            var asList = self as PyList;
            return PyListIterator.Create(asList);
        }

        [ClassMember]
        public static PyBool __contains__(PyList self, PyObject v)
        {
            if(self.list.Contains(v))
            {
                return PyBool.True;
            }
            else
            {
                return PyBool.False;
            }
        }

        [ClassMember]
        public static void __delitem__(PyList self, PyInteger i)
        {
            try
            {
                self.list.RemoveAt((int) i.InternalValue);
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("IndexError: list assignment index out of range");
            }
        }

        private static async Task<int> castSliceIndex(IInterpreter interpreter, FrameContext context, object sliceIdx)
        {
            var asPyObject = sliceIdx as PyObject;
            if(asPyObject != null)
            {
                // Slight optimization: PyInteger has __index__ but we already know to get its index.
                var asPyInt = asPyObject as PyInteger;
                if(asPyInt != null)
                {
                    return (int)asPyInt.InternalValue;
                }
                else if(asPyObject.__dict__.ContainsKey("__index__"))
                {
                    var index_dunder = (IPyCallable)asPyObject.__dict__["__index__"];
                    var index = await index_dunder.Call(interpreter, context, new object[0]);
                    var asPyIndex = index as PyInteger;
                    if(asPyIndex == null)
                    {
                        context.CurrentException = new TypeError("TypeError: slice indices must be integers or None or have an __index__ method");
                        return 0;
                    }
                    else
                    {
                        return (int) asPyIndex.InternalValue;
                    }
                }
                else
                {
                    context.CurrentException = new TypeError("TypeError: slice indices must be integers or None or have an __index__ method");
                    return 0;
                }
            }
            else if(sliceIdx is BigInteger || sliceIdx is int)
            {
                return (int)sliceIdx;
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: slice indices must be integers or None or have an __index__ method");
                return 0;
            }
        }

        [ClassMember]
        public static async Task<object> __getitem__(IInterpreter interpreter, FrameContext context, PyList self, PyObject index_or_slice)
        {
            var asPyInt = index_or_slice as PyInteger;
            if(asPyInt != null)
            {
                try
                {
                    return self.list[(int)asPyInt.InternalValue];
                }
                catch (ArgumentOutOfRangeException)
                {
                    // TODO: Represent as a more natural Python exception;
                    throw new Exception("IndexError: list index out of range");
                }
            }

            var asPySlice = index_or_slice as PySlice;
            if(asPySlice != null)
            {
                int listLen = self.list.Count;
                int start = 0;
                int stop = listLen;
                int step = 1;
                if(asPySlice.Start != null && asPySlice.Start != NoneType.Instance)
                {
                    start = await castSliceIndex(interpreter, context, asPySlice.Start);
                }
                if (asPySlice.Stop != null && asPySlice.Stop != NoneType.Instance)
                {
                    stop = await castSliceIndex(interpreter, context, asPySlice.Stop);
                }
                if (asPySlice.Step != null && asPySlice.Step != NoneType.Instance)
                {
                    step = await castSliceIndex(interpreter, context, asPySlice.Step);
                }
                if (context.CurrentException != null)
                {
                    return null;                // castSliceIndex failed somewhere along the line.
                }

                // Adjust negative starts and stops based on array length.
                start = start >= 0 ? start : listLen + start;
                stop = stop >= 0 ? stop : listLen + stop;

                // Adjust really negative starts to just be at the beginning of the list. We don't do modulus or rollover or whatever.
                if (start < 0)
                {
                    start = 0;
                }

                var newList = new List<object>();
                for (int i = start; i < stop && i < self.list.Count && i >= 0; i += step)
                {
                    newList.Add(self.list[i]);
                }
                return PyList.Create(newList);
            }
            else
            {
                context.CurrentException = new PyException("__getitem__ requires an integer index or slice, received " + index_or_slice.GetType().Name);
                return null;
            }
        }

        // TODO: Manage slices
        [ClassMember]
        public static void __setitem__(PyList self, PyInteger i, PyObject value)
        {
            __setitem__(self, (int)i.InternalValue, value);
        }

        // TODO: Manage slices
        public static void __setitem__(PyList self, int i, PyObject value)
        {
            try
            {
                self.list[i] = value;
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("IndexError: list assignment index out of range");
            }
        }

        // TODO: Test
        [ClassMember]
        public static PyBool __eq__(PyList self, object other)
        {
            var otherList = other as PyList;
            if(otherList == null)
            {
                return PyBool.False;
            }

            if(otherList.list.Count != self.list.Count)
            {
                return PyBool.False;
            }

            for(int i = 0; i < self.list.Count; ++i)
            {
                var selfPyObject = self.list[i] as PyObject;
                var otherPyObject = otherList.list[i] as PyObject;

                if(selfPyObject == null || otherPyObject == null)
                {
                    return PyBool.False;
                }
                else if(selfPyObject != null)
                {
                    if(selfPyObject.__eq__(otherPyObject).InternalValue == false)
                    {
                        return PyBool.False;
                    }
                }
                else if(!self.list[i].Equals(otherList.list[i]))
                {
                    return PyBool.False;
                }
            }
            return PyBool.True;
        }

        [ClassMember]
        public static PyBool __ne__(PyList self, PyObject other)
        {
            return !__eq__(self, other);
        }

        [ClassMember]
        public static void clear(PyList self)
        {
            self.list.Clear();
        }

        [ClassMember]
        public static void append(PyList self, PyObject toAdd)
        {
            self.list.Add(toAdd);
        }

        public static void prepend(PyList self, PyObject toAdd)
        {
            self.list.Insert(0, toAdd);
        }

        [ClassMember]
        public static Task<PyString> __str__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            return __repr__(interpreter, context, self);
        }

        [ClassMember]
        public static async Task<PyString> __repr__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asList = (PyList)self;
            PyString retStr = PyString.Create("[");
            for(int i = 0; i < asList.list.Count; ++i)
            {
                var pyObj = asList.list[i] as PyObject;
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
                else if(asList.list[i] == null)
                {
                    retStr = (PyString)PyStringClass.__add__(context, retStr, PyString.Create("null"));
                }
                else
                {
                    retStr = (PyString)PyStringClass.__add__(context, retStr, PyString.Create(asList.list[i].ToString()));
                }

                // Appending commas except on last index
                if (i < asList.list.Count - 1)
                {
                    retStr = (PyString)PyStringClass.__add__(context, retStr, PyString.Create(", "));
                }
            }
            return (PyString) PyStringClass.__add__(context, retStr, PyString.Create("]"));
        }

        [ClassMember]
        public static PyInteger __len__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asList = (PyList)self;
            return PyInteger.Create(asList.list.Count);
        }
    }

    public class PyList : PyObject, IEnumerable
    {
        public List<object> list;
        public PyList()
        {
            list = new List<object>();
        }

        // TODO: [.NET PYCONTAINERS] Container types should be able to accept object type, not just PyObject.
        //       We could use .NET objects for a keys in a PyDict, for example.
        public PyList(List<object> existingList)
        {
            list = existingList;
        }

        public static PyList Create(List<object> existingList)
        {
            var pyList = PyTypeObject.DefaultNew<PyList>(PyListClass.Instance);
            pyList.list = existingList;
            return pyList;
        }

        public static PyList Create()
        {
            var pyList = PyTypeObject.DefaultNew<PyList>(PyListClass.Instance);
            return pyList;
        }

        public override bool Equals(object obj)
        {
            var asList = obj as PyList;
            if(asList == null)
            {
                return false;
            }
            else
            {
                return PyListClass.__eq__(this, asList).InternalValue;
            }
        }

        // Implemented mostly for the sake of using container assertions in NUnit.
        public IEnumerator GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public override int GetHashCode()
        {
            return list.GetHashCode();
        }

        public override string ToString()
        {
            return "PyList(contents are not yet displayed)";
        }

        public void SetList(List<object> newList)
        {
            list = newList;
        }
    }

    public class PyListIteratorClass : PyClass
    {
        private static PyListIteratorClass __instance;

        public static PyListIteratorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new PyListIteratorClass(null);
                }
                return __instance;
            }
        }

        public PyListIteratorClass(PyFunction __init__) :
            base("list_iterator", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyRangeIterator.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyListIterator>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        [ClassMember]
        public static async Task<object> __next__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asIterator = self as PyListIterator;
            if (asIterator.CurrentIdx >= asIterator.IteratedList.list.Count)
            {
                throw new StopIterationException();
            }
            else
            {
                asIterator.CurrentIdx += 1;
                return await PyListClass.__getitem__(interpreter, context, asIterator.IteratedList, PyInteger.Create(asIterator.CurrentIdx - 1));
            }
        }
    }

    public class PyListIterator : PyObject
    {
        public int CurrentIdx;
        public PyList IteratedList;

        public PyListIterator() : base(PyListClass.Instance)
        {

        }

        public static PyListIterator Create(PyList list)
        {
            var iterator = PyTypeObject.DefaultNew<PyListIterator>(PyListIteratorClass.Instance);
            iterator.CurrentIdx = 0;
            iterator.IteratedList = list;

            return iterator;
        }
    }
}
