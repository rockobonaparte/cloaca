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

        [ClassMember]
        public static PyObject __add__(PyObject self, PyObject other)
        {
            var a = self as PyInteger;
            var b = other as PyInteger;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyInteger for addition");
            }
            if (b == null)
            {
                throw new Exception("Tried to add a PyInteger to a non-PyInteger");
            }

            var newPyInteger = new PyInteger(a.number + b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            var a = self as PyInteger;
            var b = other as PyInteger;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyInteger for multiplication");
            }
            if (b == null)
            {
                throw new Exception("Tried to multiply a PyInteger with a non-PyInteger");
            }

            var newPyInteger = new PyInteger(a.number * b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            var a = self as PyInteger;
            var b = other as PyInteger;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyInteger for subtraction");
            }
            if (b == null)
            {
                throw new Exception("Tried to subtract a PyInteger by a non-PyInteger");
            }

            var newPyInteger = new PyInteger(a.number - b.number);
            return newPyInteger;
        }

        [ClassMember]
        public static PyObject __div__(PyObject self, PyObject other)
        {
            var a = self as PyInteger;
            var b = other as PyInteger;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyInteger for division");
            }
            if (b == null)
            {
                throw new Exception("Tried to divide a PyInteger by a non-PyInteger");
            }

            var newPyInteger = new PyInteger(a.number / b.number);
            return newPyInteger;
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
    }

    public class ClassMember : System.Attribute
    {

    }
}
