using System;
using System.Linq.Expressions;
using System.Numerics;

namespace LanguageImplementation.DataTypes
{
    public class PyIntegerClass : PyClass
    {
        public PyIntegerClass(CodeObject __init__) :
            base("int", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyInteger.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyInteger>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyIntegerClass __instance;
        public static PyIntegerClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyIntegerClass(null);
                }
                return __instance;
            }
        }

        private static void castOperands(PyObject self, PyObject other, out PyInteger selfOut, out PyInteger otherOut, string operation)
        {
            selfOut = self as PyInteger;
            otherOut = other as PyInteger;
            if (selfOut == null)
            {
                throw new Exception("Tried to use a non-PyInteger for lvalue of: " + operation);
            }
            if (otherOut == null)
            {
                throw new Exception("Tried to use a non-PyInteger for rvalue of: " + operation);
            }
        }

        [ClassMember]
        public static PyObject __add__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "addition");
            var newPyInteger = PyInteger.Create(a.number + b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "multiplication");
            var newPyInteger = PyInteger.Create(a.number * b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __pow__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "exponent");
            double doubleExp = Math.Pow((double) a.number, (double) b.number);
            var newPyInteger = PyInteger.Create((BigInteger) doubleExp);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "subtraction");
            var newPyInteger = PyInteger.Create(a.number - b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __truediv__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "true division");
            var newPyFloat = PyFloat.Create(((Decimal) a.number) / ((Decimal) b.number));
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __floordiv__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "floor division");
            var newPyInteger = PyInteger.Create(a.number / b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __mod__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "modulo");
            var newPyInteger = PyInteger.Create(a.number % b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __and__(PyObject self, PyObject other)
        {
            var anded = PyBoolClass.extractInt(self) & PyBoolClass.extractInt(other);
            return PyInteger.Create(anded);
        }

        [ClassMember]
        public static PyObject __or__(PyObject self, PyObject other)
        {
            var orded = PyBoolClass.extractInt(self) | PyBoolClass.extractInt(other);
            return PyInteger.Create(orded);
        }

        [ClassMember]
        public static PyObject __xor__(PyObject self, PyObject other)
        {
            var orded = PyBoolClass.extractInt(self) ^ PyBoolClass.extractInt(other);
            return PyInteger.Create(orded);
        }

        // TODO: Might need to come up with a larger numeric type to handle shifting larger numbers.
        [ClassMember]
        public static PyObject __rshift__(PyObject self, PyObject other)
        {
            var orded = (int) PyBoolClass.extractInt(self) >> (int) PyBoolClass.extractInt(other);
            return PyInteger.Create(orded);
        }

        [ClassMember]
        public static PyObject __lshift__(PyObject self, PyObject other)
        {
            var orded = (int)PyBoolClass.extractInt(self) << (int)PyBoolClass.extractInt(other);
            return PyInteger.Create(orded);
        }


        [ClassMember]
        public static PyBool __lt__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "less-than");
            return a.number < b.number;
        }

        [ClassMember]
        public static PyBool __gt__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "greater-than");
            return a.number > b.number;
        }

        [ClassMember]
        public static PyBool __le__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "less-than-equal");
            return a.number <= b.number;
        }

        [ClassMember]
        public static PyBool __ge__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "greater-than-equal");
            return a.number >= b.number;
        }

        [ClassMember]
        public static PyBool __eq__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "equality");
            return a.number == b.number;
        }

        [ClassMember]
        public static PyBool __ne__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "non-equality");
            return a.number != b.number;
        }

        [ClassMember]
        public static PyBool __ltgt__(PyObject self, PyObject other)
        {
            PyInteger a, b;
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
            return PyString.Create(self.ToString());
        }
    }

    public class PyInteger : PyObject
    {
        public BigInteger number;
        public PyInteger(BigInteger num) : base(PyIntegerClass.Instance)
        {
            number = num;
        }

        public PyInteger()
        {
            number = 0;
        }

        public static PyInteger Create()
        {
            return PyTypeObject.DefaultNew<PyInteger>(PyIntegerClass.Instance);
        }

        public static PyInteger Create(BigInteger value)
        {
            var pyInt = PyTypeObject.DefaultNew<PyInteger>(PyIntegerClass.Instance);
            pyInt.number = value;
            return pyInt;
        }

        public override bool Equals(object obj)
        {
            var asPyInt = obj as PyInteger;
            if(asPyInt == null)
            {
                return false;
            }
            else
            {
                return asPyInt.number == number;
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
