using System;
using System.Linq;
using System.Numerics;

namespace LanguageImplementation.DataTypes
{
    public class PyIntegerClass : PyTypeObject
    {
        public PyIntegerClass(CodeObject __init__, IInterpreter interpreter, FrameContext context,
            PyClass[] bases) :
            base("int", __init__, interpreter, context)
        {
            __bases__ = bases;

            var classMembers = GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(ClassMember), false).Length > 0).ToArray();

            foreach (var classMember in classMembers)
            {
                this.__dict__[classMember.Name] = new WrappedCodeObject(classMember.Name, classMember);
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
    }

    public class PyInteger : PyObject
    {
        public BigInteger number;
        public PyInteger(BigInteger num)
        {
            number = num;
        }

        public PyInteger()
        {
            number = 0;
        }
    }

    public class ClassMember : System.Attribute
    {

    }
}
