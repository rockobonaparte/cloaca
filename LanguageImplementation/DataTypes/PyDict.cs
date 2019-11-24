using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LanguageImplementation.DataTypes
{
    public class PyDictClass : PyClass
    {
        public PyDictClass(CodeObject __init__) :
            base("dict", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyDict.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyDict>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyDictClass __instance;
        public static PyDictClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyDictClass(null);
                }
                return __instance;
            }
        }

        [ClassMember]
        public static PyBool __contains__(PyDict self, PyObject k)
        {
            // Help on built-in function __contains__:
            //
            // __contains__(key, /) method of builtins.dict instance
            //     True if D has a key k, else False.
            if(self.dict.ContainsKey(k))
            {
                return new PyBool(true);
            }
            else
            {
                return new PyBool(false);
            }
        }

        [ClassMember]
        public static void __delitem__(PyDict self, PyObject k)
        {
            try
            {
                self.dict.Remove(k);
            }
            catch (KeyNotFoundException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("KeyError: " + k);
            }
        }

        [ClassMember]
        public static PyObject __getitem__(PyDict self, PyObject k)
        {
            try
            {
                return self.dict[k];
            }
            catch(KeyNotFoundException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("KeyError: " + k);
            }
        }

        [ClassMember]
        public static void __setitem__(PyDict self, PyObject k, PyObject value)
        {
            if(self.dict.ContainsKey(k))
            {
                self.dict[k] = value;
            }
            else
            {
                self.dict.Add(k, value);
            }
        }

        // TODO: Test
        [ClassMember]
        public static PyBool __eq__(PyDict self, PyObject other)
        {
            var otherDict = other as PyDict;
            if(otherDict == null)
            {
                return new PyBool(false);
            }

            if(otherDict.dict.Count != self.dict.Count)
            {
                return new PyBool(false);
            }

            foreach(var pair in self.dict)
            {
                if(!otherDict.dict.ContainsKey(pair.Key))
                {
                    return new PyBool(false);
                }

                var otherVal = otherDict.dict[pair.Key];
                if(pair.Value.__eq__(otherVal).boolean == false)
                {
                    return new PyBool(false);
                }
            }
            return new PyBool(true);
        }

        [ClassMember]
        public static PyBool __ne__(PyDict self, PyObject other)
        {
            return !__eq__(self, other);
        }

        [ClassMember]
        public static void clear(PyDict self)
        {
            throw new NotImplementedException();
        }

        [ClassMember]
        public static PyDict copy(PyDict self)
        {
            throw new NotImplementedException();
        }

        [ClassMember]
        public static PyDict fromkeys(PyDict self, PyObject iterable, PyObject value)
        {
            // Help on built-in function fromkeys:
            // 
            // fromkeys(iterable, value = None, /) method of builtins.type instance
            // Returns a new dict with keys from iterable and values equal to value.
            //
            throw new NotImplementedException();
        }


        [ClassMember]
        public static PyDict get(PyDict self, PyObject k, PyObject d)
        {
            // Help on built-in function get:
            // 
            // get(...) method of builtins.dict instance
            // D.get(k[, d])->D[k] if k in D, else d.d defaults to None.
            //
            // k is the key
            // d is the default if k isn't in dictionary
            throw new NotImplementedException();
        }

        // TODO: Should be return something like a PySet
        [ClassMember]
        public static PyObject items(PyDict self)
        {
            // Help on built-in function items:
            //
            // items(...) method of builtins.dict instance
            //     D.items() -> a set-like object providing a view on D's items
            throw new NotImplementedException();
        }

        // TODO: Should be return something like a PySet
        [ClassMember]
        public static PyObject keys(PyDict self)
        {
            // Help on built-in function keys:
            //
            // keys(...) method of builtins.dict instance
            //     D.keys() -> a set-like object providing a view on D's keys
            throw new NotImplementedException();
        }

        [ClassMember]
        public static PyDict pop(PyDict self, PyObject k, PyObject d)
        {
            // Help on built-in function pop:
            //
            // pop(...) method of builtins.dict instance
            //     D.pop(k[,d]) -> v, remove specified key and return the corresponding value.
            //     If key is not found, d is returned if given, otherwise KeyError is raised
            //
            // k is the key
            // d is the default if k isn't in dictionary
            throw new NotImplementedException();
        }

        [ClassMember]
        public static PyDict popitem(PyDict self, PyObject k)
        {
            // Help on built-in function popitem:
            //
            // popitem(...) method of builtins.dict instance
            //     D.popitem() -> (k, v), remove and return some (key, value) pair as a
            //     2-tuple; but raise KeyError if D is empty.
            //
            // k is the key
            throw new NotImplementedException();
        }

        [ClassMember]
        public static void setdefault(PyDict self, PyObject k, PyObject d)
        {
            // Help on built-in function setdefault:
            //
            // setdefault(...) method of builtins.dict instance
            //     D.setdefault(k[,d]) -> D.get(k,d), also set D[k]=d if k not in D
            //
            // If k is already in D, do nothing. Else, k => d
            throw new NotImplementedException();
        }

        [ClassMember]
        public static void update(PyDict self, PyObject k, PyObject d)
        {
            //Help on built-in function update:
            //
            //update(...) method of builtins.dict instance
            //    D.update([E, ]**F) -> None.  Update D from dict/iterable E and F.
            //    If E is present and has a .keys() method, then does:  for k in E: D[k] = E[k]
            //    If E is present and lacks a .keys() method, then does:  for k, v in E: D[k] = v
            //    In either case, this is followed by: for k in F:  D[k] = F[k]
            //
            // The update() method takes either a dictionary or an iterable object of key/value pairs (generally tuples).
            //
            // If update() is called without passing parameters, the dictionary remains unchanged.
            throw new NotImplementedException();
        }

        // TODO: Return PyList or similar
        [ClassMember]
        public static PyObject values(PyDict self)
        {
            // Help on built-in function values:
            //
            // values(...) method of builtins.dict instance
            //     D.values() -> an object providing a view on D's values
            throw new NotImplementedException();
        }

    }

    public class PyDict : PyObject, IEnumerable
    {
        internal Dictionary<PyObject, PyObject> dict;
        public PyDict()
        {
            dict = new Dictionary<PyObject, PyObject>();
        }

        public override bool Equals(object obj)
        {
            var asList = obj as PyDict;
            if (asList == null)
            {
                return false;
            }
            else
            {
                return PyDictClass.__eq__(this, asList).boolean;
            }
        }

        // Implemented mostly for the sake of using container assertions in NUnit.
        public IEnumerator GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        public override int GetHashCode()
        {
            return dict.GetHashCode();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
