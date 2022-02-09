using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageImplementation
{
    // Used for implementing printf-like functionality like:
    // https://docs.python.org/3/library/stdtypes.html#old-string-formatting
    public class PrintfHelper
    {
        public static string Format(string in_str, params object[] in_obj)
        {
            var builder = new StringBuilder();
            int prev_i, next_i;

            for(prev_i = 0, next_i = in_str.IndexOf('%'); 
                next_i < in_str.Length && next_i >= 0;
                prev_i = next_i, next_i = in_str.IndexOf('%', next_i + 1))
            {
                
            }

            builder.Append(in_str.Substring(prev_i));
            return builder.ToString();
        }
    }
}
