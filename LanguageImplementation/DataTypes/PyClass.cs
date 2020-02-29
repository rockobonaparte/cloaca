﻿using LanguageImplementation.DataTypes.Exceptions;
using System;
using System.Collections.Generic;

namespace LanguageImplementation.DataTypes
{
    public class PyClass : PyTypeObject
    {
        public Dictionary<string, object> __dict__;

        public PyClass(string name, CodeObject __init__, PyClass[] bases) :
            base(name, __init__)
        {
            __bases__ = bases;
            __dict__ = new Dictionary<string, object>();
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
        //
        // Apparently you fall back on __getattr__ if the attribute is not found. For now, I'm
        // not implementing it at all and I'll come back to it after I have attributes working
        // in at least the "normal" way.
        [ClassMember]
        public static object __getattr__(PyObject self, string name)
        {
            throw new NotImplementedException();
        }

        private static object __getattribute__(PyClass testClass, string name, out bool found)
        {
            found = false;
            if(testClass.__dict__.ContainsKey(name))
            {
                found = true;
                return testClass.__dict__[name];
            }
            else
            {
                foreach(var parentClass in testClass.__bases__)
                {
                    var parentResult = __getattribute__(parentClass, name, out found);
                    if(found)
                    {
                        return parentResult;
                    }
                }
            }
            return null;
        }

        [ClassMember]
        public static object __getattribute__(PyObject self, string name)
        {
            if (!self.internal_dict.ContainsKey(name))
            {
                bool found = false;
                var fromClasses = __getattribute__(self.__class__, name, out found);
                if (!found)
                {
                    var className = self.__class__ != null ? self.__class__.Name : "(Null Class!)";
                    throw new EscapedPyException(new AttributeError("'" + className + "' object has no attribute named '" + name + "'"));
                }
                else
                {
                    return fromClasses;
                }
            }
            return self.internal_dict[name];
        }

        [ClassMember]
        public static void __setattr__(PyObject self, string name, object value)
        {
            if (self.internal_dict.ContainsKey(name))
            {
                self.internal_dict[name] = value;
            }
            else
            {
                self.internal_dict.Add(name, value);
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
