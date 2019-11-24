using System;
using System.Collections.Generic;

namespace LanguageImplementation.DataTypes
{
    public class PyTupleClass : PyTypeObject
    {
        public PyTupleClass(CodeObject __init__) :
            base("tuple", __init__)
        {
            __instance = this;
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
            if (Array.IndexOf(self.values, v) >= 0)
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
                return self.values[(int)i.number];
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

            if (otherList.values.Length != self.values.Length)
            {
                return PyBool.False;
            }

            for (int i = 0; i < self.values.Length; ++i)
            {
                if (self.values[i].__eq__(otherList.values[i]).boolean == false)
                {
                    return PyBool.False;
                }
            }
            return PyBool.True;
        }

        [ClassMember]
        public static PyBool __ne__(PyTuple self, PyObject other)
        {
            return !__eq__(self, other);
        }
    }

    public class PyTuple : PyObject
    {
        public PyObject[] values;
        public PyTuple(List<PyObject> values)
        {
            this.values = values.ToArray();
        }

        public PyTuple(PyObject[] values)
        {
            this.values = values;
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
                return PyTupleClass.__eq__(this, asTuple).boolean;
            }
        }

        public override int GetHashCode()
        {
            return values.GetHashCode();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }

}
