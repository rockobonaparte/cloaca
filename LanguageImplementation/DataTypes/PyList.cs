﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyListClass : PyClass
    {
        public PyListClass(CodeObject __init__) :
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
            catch (IndexOutOfRangeException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("IndexError: list assignment index out of range");
            }
        }

        [ClassMember]
        public static PyObject __getitem__(PyList self, PyInteger i)
        {
            try
            {
                return self.list[(int) i.InternalValue];
            }
            catch(IndexOutOfRangeException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("IndexError: list index out of range");
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
            catch (IndexOutOfRangeException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("IndexError: list assignment index out of range");
            }
        }

        // TODO: Test
        [ClassMember]
        public static PyBool __eq__(PyList self, PyObject other)
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
                if(self.list[i].__eq__(otherList.list[i]).InternalValue == false)
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
                var pyObj = asList.list[i];

                var __repr__ = pyObj.__getattribute__(PyClass.__REPR__);
                var functionToRun = __repr__ as IPyCallable;

                var returned = await functionToRun.Call(interpreter, context, new object[0]);
                if (returned != null)
                {
                    var asPyString = (PyString)returned;
                    retStr = (PyString)PyStringClass.__add__(retStr, asPyString);
                }

                // Appending commas except on last index
                if (i < asList.list.Count - 1)
                {
                    retStr = (PyString)PyStringClass.__add__(retStr, PyString.Create(", "));
                }
            }
            return (PyString) PyStringClass.__add__(retStr, PyString.Create("]"));
        }
    }

    public class PyList : PyObject, IEnumerable
    {
        public List<PyObject> list;
        public PyList()
        {
            list = new List<PyObject>();
        }
        public PyList(List<PyObject> existingList)
        {
            list = existingList;
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

        public void SetList(List<PyObject> newList)
        {
            list = newList;
        }
    }
}
