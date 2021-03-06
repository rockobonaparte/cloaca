﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using LanguageImplementation.DataTypes;

namespace LanguageImplementation
{
    public class PyNetConverter
    {
        /// <summary>
        /// Adaptation of Boolean.Parse that factors in some of the strings we might expect from Python to turn into a bool. Namely, "1" and "0" should
        /// become booleans too.
        /// </summary>
        /// <param name="text_in">Input text to parse as a boolean</param>
        /// <returns>Parsed boolean value</returns>
        private static bool extendedBoolString(string text_in)
        {
            if(text_in == "1" || text_in == "1.0")
            {
                return true;
            }
            else if(text_in == "0" || text_in == "0.0")
            {
                return false;
            }
            else
            {
                return Boolean.Parse(text_in);
            }
        }

        // This will lead to some problems if some asshat decides to subclass the base types. We won't be able to look it up this way.
        // At this point, I'm willing to dismiss that. Famous last words.
        private static Dictionary<ValueTuple<Type, Type>, Func<object, object>> converters = new Dictionary<ValueTuple<Type, Type>, Func<object, object>>
        {
            // Integer-Integer conversions
            { ValueTuple.Create(typeof(int), typeof(PyInteger)), (as_int) => { return PyInteger.Create((int)as_int); } },
            { ValueTuple.Create(typeof(short), typeof(PyInteger)), (as_short) => { return PyInteger.Create((short)as_short); } },
            { ValueTuple.Create(typeof(long), typeof(PyInteger)), (as_long) => { return PyInteger.Create((long)as_long); } },
            { ValueTuple.Create(typeof(BigInteger), typeof(PyInteger)), (as_bi) => { return PyInteger.Create((BigInteger)as_bi); } },
            { ValueTuple.Create(typeof(PyInteger), typeof(int)), (as_pi) => { return (int) ((PyInteger)as_pi).InternalValue; } },
            { ValueTuple.Create(typeof(PyInteger), typeof(short)), (as_pi) => { return (short) ((PyInteger)as_pi).InternalValue; } },
            { ValueTuple.Create(typeof(PyInteger), typeof(long)), (as_pi) => { return (long) ((PyInteger)as_pi).InternalValue; } },
            { ValueTuple.Create(typeof(PyInteger), typeof(BigInteger)), (as_pi) => { return (BigInteger) ((PyInteger)as_pi).InternalValue; } },
            { ValueTuple.Create(typeof(BigInteger), typeof(int)), (as_bi) => { return (int) ((BigInteger)as_bi); } },

            // Float-Float conversions
            { ValueTuple.Create(typeof(float), typeof(PyFloat)), (as_float) => { return PyFloat.Create((float)as_float); } },
            { ValueTuple.Create(typeof(double), typeof(PyFloat)), (as_double) => { return PyFloat.Create((double)as_double); } },
            { ValueTuple.Create(typeof(Decimal), typeof(PyFloat)), (as_Decimal) => { return PyFloat.Create((Decimal)as_Decimal); } },
            { ValueTuple.Create(typeof(PyFloat), typeof(float)), (as_pf) => { return (float) ((PyFloat)as_pf).InternalValue; } },
            { ValueTuple.Create(typeof(PyFloat), typeof(double)), (as_pf) => { return (double) ((PyFloat)as_pf).InternalValue; } },
            { ValueTuple.Create(typeof(PyFloat), typeof(Decimal)), (as_pf) => { return (Decimal) ((PyFloat)as_pf).InternalValue; } },

            // Integer-Float conversions
            { ValueTuple.Create(typeof(PyInteger), typeof(PyFloat)), (as_pi) => { return (PyFloat) PyFloat.Create((decimal) ((PyInteger)as_pi).InternalValue); } },

            // Float-Integer conversions
            { ValueTuple.Create(typeof(PyFloat), typeof(PyInteger)), (as_pf) => { return (PyInteger) PyInteger.Create((BigInteger) ((PyFloat)as_pf).InternalValue); } },

            // Bool-Bool conversions
            { ValueTuple.Create(typeof(bool), typeof(PyBool)), (as_bool) => { return new PyBool((bool)as_bool); } },
            { ValueTuple.Create(typeof(PyBool), typeof(bool)), (as_pb) => { return ((PyBool)as_pb).InternalValue; } },

            // String-other conversions
            { ValueTuple.Create(typeof(PyString), typeof(PyInteger)), (as_text) => { return (PyInteger) PyInteger.Create(BigInteger.Parse(((PyString) as_text).InternalValue)); } },
            { ValueTuple.Create(typeof(string), typeof(PyInteger)), (as_text) => { return (PyInteger) PyInteger.Create(BigInteger.Parse((string) as_text)); } },
            { ValueTuple.Create(typeof(PyString), typeof(PyFloat)), (as_text) => { return (PyFloat) PyFloat.Create(decimal.Parse(((PyString) as_text).InternalValue)); } },
            { ValueTuple.Create(typeof(string), typeof(PyFloat)), (as_text) => { return (PyFloat) PyFloat.Create(decimal.Parse((string) as_text)); } },
            { ValueTuple.Create(typeof(PyString), typeof(PyBool)), (as_text) => { return (PyBool) PyBool.Create(extendedBoolString(((PyString) as_text).InternalValue)); } },
            { ValueTuple.Create(typeof(string), typeof(PyBool)), (as_text) => { return (PyBool) PyBool.Create(extendedBoolString((string) as_text)); } },

            // We don't write out to-string conversions. If the toType is a string, we'll just use ToString(). (famous last words)
        };

        // We'll reuse a ValueTuple instead of constantly creating new ones.
        private static ValueTuple<Type, Type> cachedKey = ValueTuple.Create<Type, Type>(null, null);

        public static object Convert(object fromObj, Type toType)
        {
            cachedKey.Item1 = fromObj.GetType();
            cachedKey.Item2 = toType;

            if(fromObj == NoneType.Instance)
            {
                return null;
            }
            else if (toType.IsAssignableFrom(cachedKey.Item1))
            {
                return fromObj;
            }
            else if (toType == typeof(string))
            {
                return fromObj.ToString();
            }
            else if (toType == typeof(PyString))
            {
                return PyString.Create(fromObj.ToString());
            }
            else if (converters.ContainsKey(cachedKey))
            {
                return converters[cachedKey].Invoke(fromObj);
            }
            else if(fromObj is PyDotNetClassProxy)
            {
                return ((PyDotNetClassProxy)fromObj).__getattribute__(PyDotNetClassProxy.__dotnettype__);
            }
            else
            {
                return fromObj;
            }
        }

        public static bool CanConvert(Type fromType, Type toType)
        {
            cachedKey.Item1 = fromType;
            cachedKey.Item2 = toType;

            if(toType.IsAssignableFrom(fromType))
            {
                return true;
            }
            else if(toType == typeof(string))
            {
                return true;
            }
            else if(toType == typeof(PyString))
            {
                return true;
            }
            return converters.ContainsKey(cachedKey);
        }
    }
}
