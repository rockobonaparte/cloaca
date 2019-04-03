using System.Collections.Generic;

namespace LanguageImplementation
{
    public static class Extensions
    {
        public static int AddGetIndex<T>(this List<T> list, T item)
        {
            list.Add(item);
            return list.Count - 1;
        }
    }
}
