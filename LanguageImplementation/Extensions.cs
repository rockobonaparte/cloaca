using System.Collections.Generic;

namespace LanguageImplementation
{
    public static class Extensions
    {
        public static int AddGetIndex<T>(this List<T> list, T item)
        {
            int idx = list.IndexOf(item);
            if (idx >= 0)
            {
                return idx;
            }

            list.Add(item);
            return list.Count - 1;
        }
    }
}
