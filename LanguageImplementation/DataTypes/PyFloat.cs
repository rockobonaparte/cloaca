using System;
using System.Linq.Expressions;
using System.Numerics;

namespace LanguageImplementation.DataTypes
{
    public class PyFloatClass : PyClass
    {
        public PyFloatClass(CodeObject __init__) :
            base("float", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyFloat.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyFloat>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyFloatClass __instance;
        public static PyFloatClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyFloatClass(null);
                }
                return __instance;
            }
        }

        public static decimal ExtractAsDecimal(PyObject var)
        {
            var rightFloat = var as PyFloat;
            if (rightFloat != null)
            {
                return rightFloat.InternalValue;
            }

            var rightInt = var as PyInteger;
            if (rightInt != null)
            {
                return (decimal) rightInt.InternalValue;
            }

            var rightBool = var as PyBool;
            if (rightBool != null)
            {
                return rightBool.InternalValue ? (decimal) 1.0 : (decimal) 0.0;
            }

            var rightStr = var as PyString;
            if (rightStr != null)
            {
                return Decimal.Parse(rightStr.InternalValue);
            }

            else
            {
                throw new InvalidCastException("TypeError: could not convert " + var + " to a floating-point type.");
            }
        }

        private static void castOperands(PyObject self, PyObject other, out PyFloat selfOut, out PyFloat otherOut, string operation)
        {
            selfOut = self as PyFloat;
            if (selfOut == null)
            {
                throw new Exception("Tried to use a non-PyFloat for lvalue of: " + operation);
            }

            var rightFloat = other as PyFloat;
            if (rightFloat != null)
            {
                otherOut = rightFloat;
                return;
            }

            var rightInt = other as PyInteger;
            if (rightInt != null)
            {
                otherOut = PyFloat.Create((decimal)rightInt.InternalValue);
                return;
            }

            var rightBool = other as PyBool;
            if (rightBool != null)
            {
                otherOut = PyFloat.Create(rightBool.InternalValue ? 1.0 : 0.0);
                return;
            }

            var rightStr = other as PyString;
            if (rightStr != null)
            {
                otherOut = PyFloat.Create(Decimal.Parse(rightStr.InternalValue));
                return;
            }

            else
            {
                throw new Exception("TypeError: unsupported operand(s) for " + operation + " 'float' and '" + other.__class__.Name + "'");
            }
        }

        [ClassMember]
        public static PyObject __add__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "addition");
            var newPyFloat = PyFloat.Create(a.InternalValue + b.InternalValue);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "multiplication");
            var newPyFloat = PyFloat.Create(a.InternalValue * b.InternalValue);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "subtract");
            var newPyFloat = PyFloat.Create(a.InternalValue - b.InternalValue);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __truediv__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "division");
            var newPyFloat = PyFloat.Create(a.InternalValue / b.InternalValue);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __floordiv__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "floor division");
            var newPyInteger = PyFloat.Create(Math.Floor(a.InternalValue / b.InternalValue));
            return newPyInteger;
        }
        
        [ClassMember]
        public static PyObject __mod__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "modulo");
            var newPyInteger = PyFloat.Create(a.InternalValue % b.InternalValue);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __pow__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "exponent");
            double doubleExp = Math.Pow((double)a.InternalValue, (double)b.InternalValue);
            var newPyFloat = PyFloat.Create(doubleExp);
            return newPyFloat;
        }

        [ClassMember]
        public static PyBool __lt__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "less-than");
            return a.InternalValue < b.InternalValue;
        }

        [ClassMember]
        public static PyBool __gt__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "greater-than");
            return a.InternalValue > b.InternalValue;
        }

        [ClassMember]
        public static PyBool __le__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "less-than-equal");
            return a.InternalValue <= b.InternalValue;
        }

        [ClassMember]
        public static PyBool __ge__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "greater-than-equal");
            return a.InternalValue >= b.InternalValue;
        }

        [ClassMember]
        public static PyBool __eq__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "equality");
            return a.InternalValue == b.InternalValue;
        }

        [ClassMember]
        public static PyBool __ne__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "non-equality");
            return a.InternalValue != b.InternalValue;
        }

        [ClassMember]
        public static PyBool __ltgt__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "less-than-greater-than");
            return a.InternalValue < b.InternalValue && a.InternalValue > b.InternalValue;
        }

        [ClassMember]
        public static new PyString __str__(PyObject self)
        {
            return __repr__(self);
        }

        [ClassMember]
        public static new PyString __repr__(PyObject self)
        {
            return PyString.Create(((PyFloat)self).ToString());
        }
    }

    public class PyFloat : PyObject
    {
        public Decimal InternalValue;
        public PyFloat(Decimal num) : base(PyFloatClass.Instance)
        {
            InternalValue = num;
        }

        public PyFloat(double num) : base(PyFloatClass.Instance)
        {
            InternalValue = new Decimal(num);
        }

        public PyFloat()
        {
            InternalValue = 0;
        }

        public static PyFloat Create()
        {
            return PyTypeObject.DefaultNew<PyFloat>(PyFloatClass.Instance);
        }

        public static PyFloat Create(Decimal value)
        {
            var pyFloat = PyTypeObject.DefaultNew<PyFloat>(PyFloatClass.Instance);
            pyFloat.InternalValue = value;
            return pyFloat;
        }

        public static PyFloat Create(double value)
        {
            var pyFloat = PyTypeObject.DefaultNew<PyFloat>(PyFloatClass.Instance);
            pyFloat.InternalValue = new Decimal(value);
            return pyFloat;
        }

        public override bool Equals(object obj)
        {
            var asPyFloat = obj as PyFloat;
            if(asPyFloat == null)
            {
                return false;
            }
            else
            {
                return asPyFloat.InternalValue == InternalValue;
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

        #region Cast Conversions
        public static explicit operator sbyte(PyFloat pyfloat) => (sbyte)pyfloat.InternalValue;
        public static explicit operator byte(PyFloat pyfloat) => (byte)pyfloat.InternalValue;
        public static explicit operator short(PyFloat pyfloat) => (short)pyfloat.InternalValue;
        public static explicit operator ushort(PyFloat pyfloat) => (ushort)pyfloat.InternalValue;
        public static explicit operator int(PyFloat pyfloat) => (int)pyfloat.InternalValue;
        public static explicit operator uint(PyFloat pyfloat) => (uint)pyfloat.InternalValue;
        public static explicit operator long(PyFloat pyfloat) => (long)pyfloat.InternalValue;
        public static explicit operator ulong(PyFloat pyfloat) => (ulong)pyfloat.InternalValue;
        public static explicit operator BigInteger(PyFloat pyfloat) => (BigInteger)pyfloat.InternalValue;
        public static explicit operator float(PyFloat pyfloat) => (float)pyfloat.InternalValue;
        public static explicit operator double(PyFloat pyfloat) => (double)pyfloat.InternalValue;
        public static explicit operator decimal(PyFloat pyfloat) => pyfloat.InternalValue;
        public static explicit operator bool(PyFloat pyfloat)
        {
            if (pyfloat.InternalValue == 1.0m)
            {
                return true;
            }
            else if (pyfloat.InternalValue == 0.0m)
            {
                return false;
            }
            else
            {
                throw new InvalidCastException("cannot convert PyFloat value of " + pyfloat.InternalValue + " to a boolean 1.0 or 0.0.");
            }
        }
        #endregion Cast Conversions

    }
}
