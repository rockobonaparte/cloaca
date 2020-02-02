using System;
using System.Collections.Generic;
using System.Numerics;

using LanguageImplementation.DataTypes;

namespace LanguageImplementation
{
    public class PyNetConverter
    {
        // This will lead to some problems if some asshat decides to subclass the base types. We won't be able to look it up this way.
        // At this point, I'm willing to dismiss that. Famous last words.
        private static Dictionary<ValueTuple<Type, Type>, Func<object, object>> converters = new Dictionary<ValueTuple<Type, Type>, Func<object, object>>
        {
            { ValueTuple.Create(typeof(int), typeof(PyInteger)), (as_int) => { return new PyInteger((int)as_int); } },
            { ValueTuple.Create(typeof(short), typeof(PyInteger)), (as_short) => { return new PyInteger((short)as_short); } },
            { ValueTuple.Create(typeof(long), typeof(PyInteger)), (as_long) => { return new PyInteger((long)as_long); } },
            { ValueTuple.Create(typeof(BigInteger), typeof(PyInteger)), (as_bi) => { return new PyInteger((BigInteger)as_bi); } },
            { ValueTuple.Create(typeof(PyInteger), typeof(int)), (as_pi) => { return (int) ((PyInteger)as_pi).number; } },
            { ValueTuple.Create(typeof(PyInteger), typeof(short)), (as_pi) => { return (short) ((PyInteger)as_pi).number; } },
            { ValueTuple.Create(typeof(PyInteger), typeof(long)), (as_pi) => { return (long) ((PyInteger)as_pi).number; } },
            { ValueTuple.Create(typeof(PyInteger), typeof(BigInteger)), (as_pi) => { return (BigInteger) ((PyInteger)as_pi).number; } },
        };

        // We'll reuse a ValueTuple instead of constantly creating new ones.
        private static ValueTuple<Type, Type> cachedKey = ValueTuple.Create<Type, Type>(null, null);

        public static object Convert(object fromObj, Type toType)
        {
            cachedKey.Item1 = fromObj.GetType();
            cachedKey.Item2 = toType;
            if (converters.ContainsKey(cachedKey))
            {
                return converters[cachedKey].Invoke(fromObj);
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
            return converters.ContainsKey(cachedKey);
        }
    }
}
