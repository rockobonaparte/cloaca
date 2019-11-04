using System;
using System.Linq;
using System.Numerics;

namespace LanguageImplementation.DataTypes
{
    public class PyIntegerClass : PyTypeObject
    {
        public PyIntegerClass(CodeObject __init__) :
            base("int", __init__)
        {
            var classMembers = GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(ClassMember), false).Length > 0).ToArray();

            foreach (var classMember in classMembers)
            {
                this.__dict__[classMember.Name] = new WrappedCodeObject(classMember.Name, classMember);
            }

            __instance = this;
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
            var newPyInteger = new PyInteger(a.number + b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "multiplication");
            var newPyInteger = new PyInteger(a.number * b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "subtraction");
            var newPyInteger = new PyInteger(a.number - b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __div__(PyObject self, PyObject other)
        {
            PyInteger a, b;
            castOperands(self, other, out a, out b, "division");
            var newPyInteger = new PyInteger(a.number / b.number);
            return newPyInteger;
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

    public class ClassMember : System.Attribute
    {

    }
}
