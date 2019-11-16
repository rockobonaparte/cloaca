﻿using System;
using System.Linq;
using System.Numerics;

namespace LanguageImplementation.DataTypes
{
    public class PyBoolClass : PyTypeObject
    {
        public PyBoolClass(CodeObject __init__) :
            base("bool", __init__)
        {
            var classMembers = GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(ClassMember), false).Length > 0).ToArray();

            foreach (var classMember in classMembers)
            {
                this.__dict__[classMember.Name] = new WrappedCodeObject(classMember.Name, classMember);
            }

            __instance = this;
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

        private static BigInteger extractInt(PyObject a)
        {
            if(a is PyBool)
            {
                return ((PyBool) a).boolean ? 1 : 0;
            }
            else if(a is PyInteger)
            {
                return ((PyInteger)a).number;
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
            return new PyInteger(extractInt(self) + extractInt(other));
        }

        [ClassMember]
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            return new PyInteger(extractInt(self) * extractInt(other));
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            return new PyInteger(extractInt(self) - extractInt(other));
        }

        [ClassMember]
        public static PyObject __div__(PyObject self, PyObject other)
        {
            return new PyInteger(extractInt(self) / extractInt(other));
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
    }

    public class PyBool : PyObject
    {
        public bool boolean;
        public PyBool(bool boolean) : base(PyBoolClass.Instance)
        {
            this.boolean = boolean;
        }

        public PyBool()
        {
            this.boolean = false;
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
                return asPyBool.boolean == boolean;
            }
        }

        public override int GetHashCode()
        {
            return boolean.GetHashCode();
        }

        public override string ToString()
        {
            return boolean.ToString();
        }

        public static implicit operator PyBool(bool rhs)
        {
            return new PyBool(rhs);
        }

        public static implicit operator bool(PyBool rhs)
        {
            return rhs.boolean;
        }
    }
}
