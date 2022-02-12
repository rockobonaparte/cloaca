using System;
using System.Text;

using LanguageImplementation.DataTypes.Exceptions;

namespace LanguageImplementation
{
    // https://docs.python.org/3/library/stdtypes.html#printf-style-string-formatting
    //
    // A conversion specifier contains two or more characters and has the following components, which must occur in this order:
    //
    // 1. The '%' character, which marks the start of the specifier.
    // 2. Mapping key(optional), consisting of a parenthesised sequence of characters(for example, (somename)).
    // 3. Conversion flags(optional), which affect the result of some conversion types.
    // 4. Minimum field width (optional). If specified as an '*' (asterisk), the actual width is read from the next element of the tuple in values, and the object to convert comes after the minimum field width and optional precision.
    // 5. Precision (optional), given as a '.' (dot) followed by the precision.If specified as '*' (an asterisk), the actual precision is read from the next element of the tuple in values, and the value to convert comes after the precision.
    // 6. Length modifier (optional).   [We will ignore this. It's not used in Python]
    // 7. Conversion type.
    public class ConversionSpecifier
    {
        public string MappingKey;       // used if the printf formatting is given a dictionary
        public int Width;
        public int Precision;
        public bool AlternateForm;
        public bool LeftAdjusted;
        public bool SignPosAndNeg;
        public bool ZeroPadded;
        public bool SpaceBeforePos;

        // Clear a ConversionSpecifier. Use to reuse this object for another run.
        public void Clear()
        {
            MappingKey = null;
            Width = 0;
            Precision = 0;
            AlternateForm = false;
            LeftAdjusted = false;
            SignPosAndNeg = false;
            ZeroPadded = false;
            SpaceBeforePos = false;
        }

        public static void ParseFromString(string format_str, int start_idx)
        {
            // TODO Here: parse and fill in.
        }
    }

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
                // These might be conversion flag characters: [#0-+ ]
                // OR it might be another % sign which means that it's being escaped.
                string conversion_flag = null;
                int conversion_flag_start = next_i;
                if(in_str[next_i+1] == '%')
                {
                    builder.Append(in_str.Substring(prev_i, 1 + next_i - prev_i));
                    next_i += 2;
                    continue;
                }

                if (next_i >= in_str.Length - 1)
                {
                    error_out = ValueErrorClass.Create("ValueError: incomplete format");
                    return null;
                }
                else
                {
                    builder.Append(in_str.Substring(prev_i, next_i - prev_i));
                    // break out if it's a conversion type or invalid. Otherwise, keep reading to eat up
                    // a conversion flag.
                    next_i += 1;
                    prev_i = next_i;
                    while(next_i < in_str.Length && (
                        in_str[next_i] == '#' ||
                        in_str[next_i] == '+' ||
                        in_str[next_i] == '-' ||
                        in_str[next_i] == ' ' ||
                        (in_str[next_i] >= '0' && in_str[next_i] <= '9')
                        ))
                    {
                        next_i += 1;
                    }
                }

                conversion_flag = in_str.Substring(conversion_flag_start, next_i - conversion_flag_start);
                builder.Append(in_str.Substring(prev_i, next_i - prev_i));

                switch(in_str[next_i])
                {
                    case 's':
                        builder.Append(in_obj[param_i]);
                        next_i += 1;
                        break;
                    case 'd':
                        builder.Append(in_obj[param_i]);
                        next_i += 1;
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
