using System;
using System.Linq.Expressions;

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
                otherOut = new PyFloat((decimal)rightInt.number);
                return;
            }

            var rightBool = other as PyBool;
            if (rightBool != null)
            {
                otherOut = new PyFloat(rightBool.boolean ? 1.0 : 0.0);
                return;
            }

            var rightStr = other as PyString;
            if (rightStr != null)
            {
                otherOut = new PyFloat(Decimal.Parse(rightStr.str));
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
            var newPyFloat = new PyFloat(a.number + b.number);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "multiplication");
            var newPyFloat = new PyFloat(a.number * b.number);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "subtract");
            var newPyFloat = new PyFloat(a.number - b.number);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __truediv__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "division");
            var newPyFloat = new PyFloat(a.number / b.number);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __floordiv__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "floor division");
            var newPyInteger = new PyFloat(Math.Floor(a.number / b.number));
            return newPyInteger;
        }
        
        [ClassMember]
        public static PyObject __mod__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "modulo");
            var newPyInteger = new PyFloat(a.number % b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyBool __lt__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "less-than");
            return a.number < b.number;
        }

        [ClassMember]
        public static PyBool __gt__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "greater-than");
            return a.number > b.number;
        }

        [ClassMember]
        public static PyBool __le__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "less-than-equal");
            return a.number <= b.number;
        }

        [ClassMember]
        public static PyBool __ge__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "greater-than-equal");
            return a.number >= b.number;
        }

        [ClassMember]
        public static PyBool __eq__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "equality");
            return a.number == b.number;
        }

        [ClassMember]
        public static PyBool __ne__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "non-equality");
            return a.number != b.number;
        }

        [ClassMember]
        public static PyBool __ltgt__(PyObject self, PyObject other)
        {
            PyFloat a, b;
            castOperands(self, other, out a, out b, "less-than-greater-than");
            return a.number < b.number && a.number > b.number;
        }

        [ClassMember]
        public static new PyString __str__(PyObject self)
        {
            return __repr__(self);
        }

        [ClassMember]
        public static new PyString __repr__(PyObject self)
        {
            return new PyString(((PyFloat)self).ToString());
        }
    }

    public class PyFloat : PyObject
    {
        public Decimal number;
        public PyFloat(Decimal num) : base(PyFloatClass.Instance)
        {
            number = num;
        }

        public PyFloat(double num) : base(PyFloatClass.Instance)
        {
            number = new Decimal(num);
        }

        public PyFloat()
        {
            number = 0;
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
                return asPyFloat.number == number;
            }
        }

        public override int GetHashCode()
        {
            return number.GetHashCode();
        }

        public override string ToString()
        {
            return number.ToString();
        }
    }
}
