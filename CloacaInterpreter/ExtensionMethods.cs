using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CloacaInterpreter
{
    public static class ExtensionMethods
    {
        public static void AddOrSet<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if(dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }
    }
}
