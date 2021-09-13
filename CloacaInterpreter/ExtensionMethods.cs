using System;
using System.Collections.Generic;
using System.Reflection;

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

        public static MethodInfo[] GetNamedMethods(this Type t, string name)
        {
            var allMethods = t.GetMethods();
            var matched = new List<MethodInfo>();
            foreach(var methodInfo in allMethods)
            {
                if(methodInfo.Name == name)
                {
                    matched.Add(methodInfo);
                }
            }

            return matched.ToArray();
        }
    }
}
