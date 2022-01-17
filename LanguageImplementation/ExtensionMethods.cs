using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LanguageImplementation
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Determine if a MethodBase is an extension method.
        /// 
        /// Note that it's only coincidental--if not ironic--that this call is itself an extension method!
        /// </summary>
        /// <param name="methodBase">MethodBase to test to see if it's an extension method.</param>
        public static bool IsExtensionMethod(this MethodBase methodBase)
        {
            return methodBase.IsDefined(typeof(ExtensionAttribute));
        }

        public static void AddOrSet<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        public static object GetDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            TValue value;
            return dict.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}
