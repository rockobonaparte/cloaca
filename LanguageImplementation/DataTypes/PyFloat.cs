using System;
using System.Linq;

namespace LanguageImplementation.DataTypes
{
    public class PyFloatClass : PyTypeObject
    {
        public PyFloatClass(CodeObject __init__) :
            base("int", __init__)
        {
            var classMembers = GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(ClassMember), false).Length > 0).ToArray();

            foreach (var classMember in classMembers)
            {
                this.__dict__[classMember.Name] = new WrappedCodeObject(classMember.Name, classMember);
            }

            __instance = this;
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

        [ClassMember]
        public static PyObject __add__(PyObject self, PyObject other)
        {
            var a = self as PyFloat;
            var b = other as PyFloat;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyFloat for addition");
            }
            if (b == null)
            {
                throw new Exception("Tried to add a PyFloat to a non-PyFloat");
            }

            var newPyFloat = new PyFloat(a.number + b.number);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            var a = self as PyFloat;
            var b = other as PyFloat;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyFloat for multiplication");
            }
            if (b == null)
            {
                throw new Exception("Tried to multiply a PyFloat with a non-PyFloat");
            }

            var newPyFloat = new PyFloat(a.number * b.number);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            var a = self as PyFloat;
            var b = other as PyFloat;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyFloat for subtraction");
            }
            if (b == null)
            {
                throw new Exception("Tried to subtract a PyFloat by a non-PyFloat");
            }

            var newPyFloat = new PyFloat(a.number - b.number);
            return newPyFloat;
        }

        [ClassMember]
        public static PyObject __div__(PyObject self, PyObject other)
        {
            var a = self as PyFloat;
            var b = other as PyFloat;
            if (a == null)
            {
                throw new Exception("Tried to use a non-PyFloat for division");
            }
            if (b == null)
            {
                throw new Exception("Tried to divide a PyFloat by a non-PyFloat");
            }

            var newPyFloat = new PyFloat(a.number / b.number);
            return newPyFloat;
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
