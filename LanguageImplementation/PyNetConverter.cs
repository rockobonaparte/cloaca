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
        private static Dictionary<Tuple<Type, Type>, Func<object, object>> converters = new Dictionary<Tuple<Type, Type>, Func<object, object>>
        {
            { new Tuple<Type, Type>(typeof(int), typeof(PyInteger)), (as_int) => { return new PyInteger((int)as_int); } },
            { new Tuple<Type, Type>(typeof(short), typeof(PyInteger)), (as_short) => { return new PyInteger((short)as_short); } },
            { new Tuple<Type, Type>(typeof(long), typeof(PyInteger)), (as_long) => { return new PyInteger((long)as_long); } },
            { new Tuple<Type, Type>(typeof(BigInteger), typeof(PyInteger)), (as_bi) => { return new PyInteger((BigInteger)as_bi); } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(int)), (as_pi) => { return (int) ((PyInteger)as_pi).number; } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(short)), (as_pi) => { return (short) ((PyInteger)as_pi).number; } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(long)), (as_pi) => { return (long) ((PyInteger)as_pi).number; } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(BigInteger)), (as_pi) => { return (BigInteger) ((PyInteger)as_pi).number; } },
        };

        public static object Convert(object fromObj, Type toType)
        {
            var convert_rule = new Tuple<Type, Type>(fromObj.GetType(), toType);
            if (converters.ContainsKey(convert_rule))
            {
                return converters[convert_rule].Invoke(fromObj);
            }
            else
            {
                return fromObj;
            }
        }

        public static bool CanConvert(Type fromType, Type toType)
        {
            var convert_rule = new Tuple<Type, Type>(fromType, toType);
            return converters.ContainsKey(convert_rule);
        }
    }
}
