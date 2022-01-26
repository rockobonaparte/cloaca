using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var asSet = self as PySet;
            return PySetIterator.Create(asSet);
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

            return self.set.SetEquals(otherSet.set) ? PyBool.True : PyBool.False;
        }

        [ClassMember]
        public static PyBool __ne__(PySet self, PyObject other)
        {
            return !__eq__(self, other);
        }

        [ClassMember]
        //
        // clear(...) method of builtins.set instance
        //    Remove all elements from this set.
        public static void clear(PySet self)
        {
            self.set.Clear();
        }

        [ClassMember]
        //
        // add(...) method of builtins.set instance
        //    Add an element to a set.
        //
        // This has no effect if the element is already present.
        public static void add(PySet self, PyObject toAdd)
        {
            self.set.Add(toAdd);
        }

        [ClassMember]
        // difference(...) method of builtins.set instance
        //    Return the difference of two or more sets as a new set.
        //
        // (i.e.all elements that are in this set but not the others.)
        public static PySet difference(PySet self, params PySet[] others)
        {
            var enumerable = (IEnumerable<object>) self.set;
            foreach(var otherSet in others)
            {
                enumerable = enumerable.Except(otherSet.set);
            }
            var retSet = PySet.Create(enumerable.ToHashSet<object>());
            return retSet;
        }

        [ClassMember]
        // difference_update(...) method of builtins.set instance
        //    Remove all elements of another set from this set.
        public static void difference_update(PySet self, params PySet[] others)
        {
            foreach(var otherSet in others)
            {
                self.set.ExceptWith(otherSet.set);
            }
        }

        [ClassMember]
        // discard(...) method of builtins.set instance
        //    Remove an element from a set if it is a member.
        //
        // If the element is not a member, do nothing.
        public static void discard(PySet self, object element)
        {
            self.set.Remove(element);
        }

        [ClassMember]
        // intersection(...) method of builtins.set instance
        //    Return the intersection of two sets as a new set.
        //
        // (i.e.all elements that are in both sets.)
        public static PySet intersection(PySet self, PySet other)
        {
            var intersection = new HashSet<object>(self.set);
            intersection.IntersectWith(other.set);
            return PySet.Create(intersection);
        }

        [ClassMember]
        // intersection_update(...) method of builtins.set instance
        //    Update a set with the intersection of itself and another.
        public static void intersection_update(PySet self, PySet other)
        {
            self.set.IntersectWith(other.set);
        }

        [ClassMember]
        // isdisjoint(...) method of builtins.set instance
        //    Return True if two sets have a null intersection.
        public static PyBool isdisjoint(PySet self, PySet other)
        {
            return PyBool.Create(!self.set.Overlaps(other.set));
        }

        [ClassMember]
        // issubset(...) method of builtins.set instance
        //    Report whether another set contains this set.
        public static PyBool issuperset(PySet self, PySet other)
        {
            return PyBool.Create(self.set.IsSupersetOf(other.set));
        }

        [ClassMember]
        // pop(...) method of builtins.set instance
        //    Remove and return an arbitrary set element.
        //    Raises KeyError if the set is empty.
        public static object pop(PySet self, FrameContext context)
        {
            if(self.set.Count <= 0)
            {
                context.CurrentException = new PyException("KeyError: 'pop from an empty set'");
                return null;
            }
            var toReturn = self.set.FirstOrDefault();
            self.set.Remove(toReturn);
            return toReturn;
        }

        [ClassMember]
        // remove(...) method of builtins.set instance
        //    Remove an element from a set; it must be a member.
        //    If the element is not a member, raise a KeyError.
        public static void remove(PySet self, PyObject to_remove, FrameContext context)
        {
            if(!self.set.Contains(to_remove))
            {
                context.CurrentException = new PyException("KeyError: " + to_remove.ToString());
            }
            else
            {
                self.set.Remove(to_remove);
            }
        }

        [ClassMember]
        // symmetric_difference(...) method of builtins.set instance
        //    Return the symmetric difference of two sets as a new set.
        //
        //   (i.e.all elements that are in exactly one of the sets.)
        public static PySet symmetric_difference(PySet self, PySet other)
        {
            var newSet = new HashSet<object>(self.set);
            newSet.SymmetricExceptWith(other.set);
            return PySet.Create(newSet);
        }

        [ClassMember]
        // symmetric_difference_update(...) method of builtins.set instance
        //    Update a set with the symmetric difference of itself and another.
        public static void symmetric_difference_update(PySet self, PySet other)
        {
            self.set.SymmetricExceptWith(other.set);
        }

        [ClassMember]
        // union(...) method of builtins.set instance
        //    Return the union of sets as a new set.
        //
        //    (i.e.all elements that are in either set.)
        public static PySet union(PySet self, params PySet[] others)
        {
            var newSet = new HashSet<object>(self.set);
            foreach(var otherSet in others)
            {
                newSet.UnionWith(otherSet.set);
            }
            return PySet.Create(newSet);
        }

        [ClassMember]
        // update(...) method of builtins.set instance
        //    Update a set with the union of itself and others.
        public static void update(PySet self, params PySet[] others)
        {
            foreach (var otherSet in others)
            {
                self.set.UnionWith(otherSet.set);
            }
        }

        [ClassMember]
        public static PyObject __sub__(PySet self, object other, FrameContext context)
        {
            if (other is PySet)
            {
                var otherSet = other as PySet;
                return PySetClass.difference(self, otherSet);
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: unsupported operand type(s) for -: 'set' and '" + other.GetType().Name + "'");
                return null;
            }
        }

        [ClassMember]
        public static PyObject __and__(PySet self, object other, FrameContext context)
        {
            if (other is PySet)
            {
                var otherSet = other as PySet;
                return PySetClass.intersection(self, otherSet);
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: unsupported operand type(s) for &: 'set' and '" + other.GetType().Name + "'");
                return null;
            }
        }

        [ClassMember]
        public static PyObject __or__(PySet self, object other, FrameContext context)
        {
            if (other is PySet)
            {
                var otherSet = other as PySet;
                return PySetClass.union(self, otherSet);
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: unsupported operand type(s) for |: 'set' and '" + other.GetType().Name + "'");
                return null;
            }
        }

        [ClassMember]
        public static PyObject __xor__(PySet self, object other, FrameContext context)
        {
            if (other is PySet)
            {
                var otherSet = other as PySet;
                return PySetClass.symmetric_difference(self, otherSet);
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: unsupported operand type(s) for ^: 'set' and '" + other.GetType().Name + "'");
                return null;
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
            var asSet = (PySet)self;
            return PyInteger.Create(asSet.set.Count);
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

        public static PySet Create(IEnumerable<object> iterable)
        {
            var inner_set = new HashSet<object>(); 
            foreach(var element in iterable)
            {
                inner_set.Add(element);
            }
            var pySet = PyTypeObject.DefaultNew<PySet>(PySetClass.Instance);
            pySet.set = inner_set;
            return pySet;
        }

        public static PySet Create()
        {
            var pySet = PyTypeObject.DefaultNew<PySet>(PySetClass.Instance);
            return pySet;
        }

        public override bool Equals(object obj)
        {
            var asSet = obj as PySet;
            if(asSet == null)
            {
                return false;
            }
            else
            {
                return PySetClass.__eq__(this, asSet).InternalValue;
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

        public void SetSet(HashSet<object> newSet)
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
