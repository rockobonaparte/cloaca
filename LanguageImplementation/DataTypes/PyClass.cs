﻿using LanguageImplementation.DataTypes.Exceptions;
using System;

namespace LanguageImplementation.DataTypes
{
    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, PyClass[] bases) :
            base(name, __init__)
        {
            __bases__ = bases;
            // __dict__ used to be set here but was moved upstream
        }

        public const string __REPR__ = "__repr__";
        public const string __STR__ = "__str__";

        [ClassMember]
        public static void __delattr__(PyObject self, string name)
        {
            throw new NotImplementedException();
        }

        // I don't fully understand the difference between __getattribute__ and __getattr__
        // yet, but I believe I default to __getattribute__
        [ClassMember]
        public static object __getattr__(PyObject self, string name)
        {
            throw new NotImplementedException();
        }

        [ClassMember]
        public static object __getattribute__(PyObject self, string name)
        {
            if (!self.__dict__.ContainsKey(name))
            {
                var className = self.__class__ != null ? self.__class__.Name : "(Null Class!)";
                throw new EscapedPyException(new AttributeError("'" + className + "' object has no attribute named '" + name + "'"));
            }
            return self.__dict__[name];
        }

        [ClassMember]
        public static void __setattr__(PyObject self, string name, object value)
        {
            if (self.__dict__.ContainsKey(name))
            {
                self.__dict__[name] = value;
            }
            else
            {
                self.__dict__.Add(name, value);
            }
        }

        [ClassMember]
        public static PyBool __eq__(PyObject self, PyObject other)
        {
            return self.Equals(other);
        }

        [ClassMember]
        public static PyBool __ne__(PyObject self, PyObject other)
        {
            return !Equals(self, other);
        }

        [ClassMember]
        public static PyString __repr__(PyObject self)
        {
            // Default __repr__
            // TODO: Switch to __class__.__name__
            // TODO: Switch to an internal Python object ID (probably when doing something like object subpooling)
            // Using the hashcode is definitely not the same as what Python is using.
            return PyString.Create("<" + self.GetType().Name + " object at " + self.GetHashCode() + ">");
        }

        [ClassMember]
        public static PyString __str__(PyObject self)
        {
            // Default for __str__ is same as __repr__
            return __repr__(self);
        }
    }
}
