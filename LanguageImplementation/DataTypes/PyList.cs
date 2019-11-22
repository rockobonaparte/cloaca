using System;
using System.Collections.Generic;

namespace LanguageImplementation.DataTypes
{
    public class PyListClass : PyTypeObject
    {
        public PyListClass(CodeObject __init__) :
            base("list", __init__)
        {
            __instance = this;
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
                return new PyBool(true);
            }
            else
            {
                return new PyBool(false);
            }
        }

        [ClassMember]
        public static void __delitem__(PyList self, PyInteger i)
        {
            try
            {
                self.list.RemoveAt((int) i.number);
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
                return self.list[(int) i.number];
            }
            catch(IndexOutOfRangeException)
            {
                // TODO: Represent as a more natural Python exception;
                throw new Exception("IndexError: list index out of range");
            }
        }

        [ClassMember]
        public static void __setitem__(PyList self, PyInteger i, PyObject value)
        {
            try
            {
                self.list[(int)i.number] = value;
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
                return new PyBool(false);
            }

            if(otherList.list.Count != self.list.Count)
            {
                return new PyBool(false);
            }

            for(int i = 0; i < self.list.Count; ++i)
            {
                if(self.list[i].__eq__(otherList.list[i]).boolean == false)
                {
                    return new PyBool(false);
                }
            }
            return new PyBool(true);
        }

        [ClassMember]
        public static PyBool __ne__(PyList self, PyObject other)
        {
            return !__eq__(self, other);
        }

        [ClassMember]
        public static void clear(PyList self)
        {
            throw new NotImplementedException();
        }

    }

    public class PyList : PyObject
    {
        internal List<PyObject> list;
        public PyList()
        {
            list = new List<PyObject>();
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
                return PyListClass.__eq__(this, asList).boolean;
            }
        }

        public override int GetHashCode()
        {
            return list.GetHashCode();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
