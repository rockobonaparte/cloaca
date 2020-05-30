using System;
using System.Linq.Expressions;
using System.Numerics;

namespace LanguageImplementation.DataTypes
{
    public class PyBoolClass : PyClass
    {
        public PyBoolClass(CodeObject __init__) :
            base("bool", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyBool.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyBool>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyBoolClass __instance;
        public static PyBoolClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyBoolClass(null);
                }
                return __instance;
            }
        }

        public static BigInteger extractInt(PyObject a)
        {
            if(a is PyBool)
            {
                return ((PyBool) a).InternalValue ? 1 : 0;
            }
            else if(a is PyInteger)
            {
                return ((PyInteger)a).InternalValue;
            }
            else
            {
                // TODO: Handle at least FLOAT
                throw new Exception("boolean type can currently only do math with int and bool types");
            }
        }

        [ClassMember]
        public static PyObject __add__(PyObject self, PyObject other)
        {
            if (other is PyInteger)
            {
                return PyInteger.Create(extractInt(self) + extractInt(other));
            }
            else
            {
                return PyFloat.Create(PyFloatClass.ExtractAsDecimal(self) + PyFloatClass.ExtractAsDecimal(other));
            }
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            if (other is PyInteger)
            {
                return PyInteger.Create(extractInt(self) * extractInt(other));
            }
            else
            {
                return PyFloat.Create(PyFloatClass.ExtractAsDecimal(self) * PyFloatClass.ExtractAsDecimal(other));
            }
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            if (other is PyInteger)
            {
                return PyInteger.Create(extractInt(self) - extractInt(other));
            }
            else
            {
                return PyFloat.Create(PyFloatClass.ExtractAsDecimal(self) - PyFloatClass.ExtractAsDecimal(other));
            }
        }

        [ClassMember]
        public static PyObject __truediv__(PyObject self, PyObject other)
        {
            var newPyFloat = PyFloat.Create((decimal) extractInt(self) / PyFloatClass.ExtractAsDecimal(other));
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __floordiv__(PyObject self, PyObject other)
        {
            var newPyInteger = PyInteger.Create((BigInteger) ((decimal)extractInt(self) / PyFloatClass.ExtractAsDecimal(other)));
            return newPyInteger;
        }
                
        [ClassMember]
        public static PyObject __and__(PyObject self, PyObject other)
        {
            var anded = extractInt(self) & extractInt(other);
            return anded > 0 ? PyBool.True : PyBool.False;
        }

        [ClassMember]
        public static PyObject __or__(PyObject self, PyObject other)
        {
            var orded = extractInt(self) | extractInt(other);
            return orded > 0 ? PyBool.True : PyBool.False;
        }

        [ClassMember]
        public static PyObject __xor__(PyObject self, PyObject other)
        {
            var orded = extractInt(self) ^ extractInt(other);
            return orded > 0 ? PyBool.True : PyBool.False;
        }

        [ClassMember]
        public static PyBool __lt__(PyObject self, PyObject other)
        {
            return extractInt(self) < extractInt(other);
        }

        [ClassMember]
        public static PyBool __gt__(PyObject self, PyObject other)
        {
            return extractInt(self) > extractInt(other);
        }

        [ClassMember]
        public static PyBool __le__(PyObject self, PyObject other)
        {
            return extractInt(self) <= extractInt(other);
        }

        [ClassMember]
        public static PyBool __ge__(PyObject self, PyObject other)
        {
            return extractInt(self) >= extractInt(other);
        }

        [ClassMember]
        public static PyBool __eq__(PyObject self, PyObject other)
        {
            return extractInt(self) == extractInt(other);
        }

        [ClassMember]
        public static PyBool __ne__(PyObject self, PyObject other)
        {
            return extractInt(self) != extractInt(other);
        }

        [ClassMember]
        public static PyBool __ltgt__(PyObject self, PyObject other)
        {
            var a = extractInt(self);
            var b = extractInt(other);
            return a < b && a > b;
        }

        [ClassMember]
        public static PyObject __rshift__(PyObject self, PyObject other)
        {
            var orded = (int)PyBoolClass.extractInt(self) >> (int)PyBoolClass.extractInt(other);
            return PyInteger.Create(orded);
        }

        [ClassMember]
        public static PyObject __lshift__(PyObject self, PyObject other)
        {
            var orded = (int)PyBoolClass.extractInt(self) << (int)PyBoolClass.extractInt(other);
            return PyInteger.Create(orded);
        }

        [ClassMember]
        public static new PyString __str__(PyObject self)
        {
            return __repr__(self);
        }

        [ClassMember]
        public static new PyString __repr__(PyObject self)
        {
            return PyString.Create(((PyBool)self).ToString());
        }
    }

    public class PyBool : PyObject
    {
        public bool InternalValue
        {
            get; private set;
        }

        public PyBool(bool boolean) : base(PyBoolClass.Instance)
        {
            this.InternalValue = boolean;
        }

        public PyBool()
        {
            this.InternalValue = false;
        }

        public override bool Equals(object obj)
        {
            var asPyBool = obj as PyBool;
            if(asPyBool == null)
            {
                return false;
            }
            else
            {
                return asPyBool.InternalValue == InternalValue;
            }
        }

        public override int GetHashCode()
        {
            return InternalValue.GetHashCode();
        }

        public override string ToString()
        {
            return InternalValue.ToString();
        }

        public static implicit operator PyBool(bool rhs)
        {
            if(rhs == true)
            {
                return PyBool.True;
            }
            else
            {
                return PyBool.False;
            }
        }

        private static PyBool _makeInstance(bool value)
        {
            var instance = PyTypeObject.DefaultNew<PyBool>(PyBoolClass.Instance);
            instance.InternalValue = value;
            return instance;
        }
        
        public static readonly PyBool True = _makeInstance(true);
        public static readonly PyBool False = _makeInstance(false);

        public static PyBool Create(bool fromBool)
        {
            if(fromBool)
            {
                return PyBool.True;
            }
            else
            {
                return PyBool.False;
            }
        }

        #region Cast Conversions
        public static explicit operator sbyte(PyBool pybool) => (sbyte) (pybool.InternalValue ? 1 : 0);
        public static explicit operator byte(PyBool pybool) => (byte)(pybool.InternalValue ? 1 : 0);
        public static explicit operator short(PyBool pybool) => (short)(pybool.InternalValue ? 1 : 0);
        public static explicit operator ushort(PyBool pybool) => (ushort)(pybool.InternalValue ? 1 : 0);
        public static explicit operator int(PyBool pybool) => (int)(pybool.InternalValue ? 1 : 0);
        public static explicit operator uint(PyBool pybool) => (uint)(pybool.InternalValue ? 1 : 0);
        public static explicit operator long(PyBool pybool) => (long)(pybool.InternalValue ? 1 : 0);
        public static explicit operator ulong(PyBool pybool) => (ulong)(pybool.InternalValue ? 1 : 0);
        public static explicit operator BigInteger(PyBool pybool) => (BigInteger)(pybool.InternalValue ? 1 : 0);
        public static explicit operator float(PyBool pybool) => (float)(pybool.InternalValue ? 1 : 0);
        public static explicit operator double(PyBool pybool) => (double)(pybool.InternalValue ? 1 : 0);
        public static explicit operator decimal(PyBool pybool) => (decimal)(pybool.InternalValue ? 1 : 0);
        public static implicit operator bool(PyBool rhs)
        {
            return rhs.InternalValue;
        }
        #endregion Cast Conversions
    }
}
