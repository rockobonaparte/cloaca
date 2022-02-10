using System;
using System.Text;

using LanguageImplementation.DataTypes.Exceptions;

namespace LanguageImplementation
{
    // Used for implementing printf-like functionality like:
    // https://docs.python.org/3/library/stdtypes.html#old-string-formatting
    public class PrintfHelper
    {
        public static string Format(string in_str, out object error_out, params object[] in_obj)
        {
            var builder = new StringBuilder();
            int prev_i, next_i, param_i = 0;

            for(prev_i = 0, next_i = in_str.IndexOf('%'); 
                next_i < in_str.Length && next_i >= 0;
                prev_i = next_i, next_i = in_str.IndexOf('%', next_i + 1))
            {
                if(next_i >= in_str.Length-1)
                {
                    error_out = ValueErrorClass.Create("ValueError: incomplete format");
                    return null;
                }

                builder.Append(in_str.Substring(prev_i, next_i - prev_i));

                switch(in_str[next_i + 1])
                {
                    case 's':
                        builder.Append(in_obj[param_i]);
                        next_i += 2;
                        break;
                    case 'd':
                        builder.Append(in_obj[param_i]);
                        next_i += 2;
                        break;
                    default:
                        error_out = ValueErrorClass.Create("ValueError: unsupported format character '"
                            + in_str[next_i] +"' (0x" + Convert.ToByte(in_str[next_i]) + ") at index " + next_i + 1);
                        return null;
                }
                param_i += 1;
            }

            builder.Append(in_str.Substring(prev_i));
            error_out = null;
            return builder.ToString();
        }
    }
}
