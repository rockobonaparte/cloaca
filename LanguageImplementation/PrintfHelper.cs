using System;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;

namespace LanguageImplementation
{
    public enum TextParseMode
    {
        WidthMode,
        PrecisionMode,
        MappingMode,
    }

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

        public ConversionSpecifier()
        {
            // Probably redundant but paranoid.
            Clear();
        }

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

        public static bool StartsConversionSpecifier(char c)
        {
            return c == '#' ||
                   c == '+' ||
                   c == '-' ||
                   c == ' ' ||
                   (c >= '0' && c <= '9');
        }

        public int ParseFromString(string format_str, int start_idx, out object error_out)
        {
            TextParseMode mode = TextParseMode.WidthMode;
            StringBuilder numBuilder = new StringBuilder();
            int idx = start_idx;
            int dotcount = 0;
            bool done = false;
            while(idx < format_str.Length && !done)
            {
                switch(format_str[idx])
                {
                    case '#':
                        AlternateForm = true;
                        break;
                    case '+':
                        SignPosAndNeg = true;
                        break;
                    case '-':
                        LeftAdjusted = true;
                        break;
                    case ' ':
                        SpaceBeforePos = true;
                        break;
                    case '(':
                        if(mode == TextParseMode.MappingMode)
                        {
                            error_out = ValueErrorClass.Create("ValueError: incomplete format key");
                            return start_idx;
                        }
                        else if(numBuilder.Length > 0)
                        {
                            error_out = ValueErrorClass.Create("ValueError: unsupported format character '('(0x28) at index " + idx);
                            return start_idx;
                        }
                        mode = TextParseMode.MappingMode;
                        break;
                    case ')':
                        if(mode != TextParseMode.MappingMode)
                        {
                            error_out = ValueErrorClass.Create("ValueError: unsupported format character ')'(0x29) at index " + idx);
                            return start_idx;
                        }
                        else
                        {
                            MappingKey = numBuilder.ToString();
                            mode = TextParseMode.WidthMode;
                        }
                        break;
                    case '.':
                        dotcount += 1;
                        if(dotcount > 1)
                        {
                            error_out = ValueErrorClass.Create("ValueError: unsupported format character '.'(0x2e) at index " + idx);
                            return start_idx;
                        }
                        else
                        {
                            // Can't Int32.Parse and empty string.
                            if(numBuilder.Length > 0)
                            {
                                Width = Int32.Parse(numBuilder.ToString());
                            }
                            mode = TextParseMode.PrecisionMode;
                            numBuilder.Clear();
                        }
                        break;
                    default:
                        {
                            if (format_str[idx] >= '0' && format_str[idx] <= '9')
                            {
                                if(numBuilder.Length == 0 && format_str[idx] == '0')
                                {
                                    ZeroPadded = true;
                                }
                                else
                                {
                                    numBuilder.Append(format_str[idx]);
                                }
                            }
                            else
                            {
                                done = true;
                            }
                        }
                        break;
                }

                if(!done)
                {
                    idx += 1;
                }
            }

            if(numBuilder.Length > 0)
            {
                if(mode == TextParseMode.PrecisionMode)
                {
                    Precision = Int32.Parse(numBuilder.ToString());
                }
                else
                {
                    Width = Int32.Parse(numBuilder.ToString());
                }
            }
            error_out = null;
            return idx;
        }
    }

    // Used for implementing printf-like functionality like:
    // https://docs.python.org/3/library/stdtypes.html#old-string-formatting
    public class PrintfHelper
    {
        private static string FormatStringFromPrecision(int precision, object insert, out object error_out)
        {
            string insert_str = insert as string;
            if (insert_str == null)
            {
                insert_str = insert.ToString();
            }
            error_out = null;

            return insert_str.PadRight(precision, '0');
        }

        private static string formatString(ConversionSpecifier spec, object insert, out object error_out)
        {
            string insert_str = insert as string;
            if(insert_str == null)
            {
                insert_str = insert.ToString();
            }
            error_out = null;

            if(spec.Width > 0 && spec.Width > insert_str.Length)
            {
                if(spec.LeftAdjusted)
                {
                    insert_str = insert_str.PadRight(spec.Width);
                }
                else
                {
                    insert_str = insert_str.PadLeft(spec.Width);
                }
            }
            return insert_str;
        }

        // Allows going out of bounds. Returns negative one.
        private static int nextPercentSign(string in_str, int next_i)
        {
            if(next_i >= in_str.Length)
            {
                return -1;
            }
            else
            {
                return in_str.IndexOf('%', next_i);
            }
        }

        public static string Format(string in_str, out object error_out, params object[] in_obj)
        {
            error_out = null;
            var conversion_spec = new ConversionSpecifier();
            var builder = new StringBuilder();
            int prev_i, next_i, param_i = 0;

            for(prev_i = 0, next_i = in_str.IndexOf('%'); 
                next_i < in_str.Length && next_i >= 0;
                prev_i = next_i, next_i = nextPercentSign(in_str, next_i + 1))
            {
                // These might be conversion flag characters: [#0-+ ]
                // OR it might be another % sign which means that it's being escaped.
                if(in_str[next_i+1] == '%')
                {
                    builder.Append(in_str.Substring(prev_i, 1 + next_i - prev_i));
                    next_i += 2;
                    continue;
                } 

                if (next_i + 1 >= in_str.Length - 1)
                {
                    error_out = ValueErrorClass.Create("ValueError: incomplete format");
                    return null;
                }

                builder.Append(in_str.Substring(prev_i, next_i - prev_i));

                if (ConversionSpecifier.StartsConversionSpecifier(in_str[next_i + 1]))
                {
                    next_i = conversion_spec.ParseFromString(in_str, next_i + 1, out error_out);
                    if(error_out != null)
                    {
                        return null;
                    }
                }
                else
                {
                    next_i += 1;
                }

                switch(in_str[next_i])
                {
                    case 's':
                        builder.Append(formatString(conversion_spec, in_obj[param_i], out error_out));
                        next_i += 1;
                        break;
                    case 'd':
                        var formatted = formatString(conversion_spec, in_obj[param_i], out error_out);
                        if (conversion_spec.ZeroPadded)
                        {
                            formatted = Regex.Replace(formatted, @"^\s", "0");
                        }
                        builder.Append(formatted);
                        next_i += 1;
                        break;
                    case 'f':
                        {
                            var p = in_obj[param_i];
                            string s;

                            if(p is PyBool)
                            {
                                var asPyBool = p as PyBool;
                                s = asPyBool.InternalValue ? "1.0" : "0.0";
                            }
                            else if(p is bool)
                            {
                                var asBool = (bool) p;
                                s = asBool ? "1.0" : "0.0";
                            }
                            else if (p is PyInteger || 
                                p is int ||
                                p is long ||
                                p is ulong ||
                                p is uint ||
                                p is BigInteger)
                            {
                                s = p.ToString() + ".0";
                            }
                            else if(p is PyFloat)
                            {
                                s = Math.Round(((PyFloat) p).InternalValue, conversion_spec.Precision, MidpointRounding.AwayFromZero).ToString();
                            }
                            else if (
                                p is float ||
                                p is decimal ||
                                p is double
                                )
                            {
                                s = Math.Round((decimal) p, conversion_spec.Precision, MidpointRounding.AwayFromZero).ToString();
                            }
                            else
                            {
                                error_out = TypeErrorClass.Create("TypeError: must be real number, not " + p.GetType().Name);
                                return null;
                            }

                            var splitArr = s.Split('.');
                            if(splitArr.Length > 2)
                            {
                                error_out = ValueErrorClass.Create("ValueError: unsupported floating-point conversion: " + s);
                                return null;
                            }
                            var intPart = splitArr[0];
                            var fractPart = splitArr.Length == 2 ? splitArr[1] : "";
                            if(error_out != null)
                            {
                                return null;
                            }
                            fractPart = FormatStringFromPrecision(conversion_spec.Precision, fractPart, out error_out);
                            if (error_out != null)
                            {
                                return null;
                            }

                            var combined = intPart + "." + fractPart;
                            combined = formatString(conversion_spec, combined, out error_out);
                            if (error_out != null)
                            {
                                return null;
                            }
                            return combined;
                        }
                        break;
                    default:
                        error_out = ValueErrorClass.Create("ValueError: unsupported format character '"
                            + in_str[next_i] +"' (0x" + Convert.ToByte(in_str[next_i]) + ") at index " + next_i + 1);
                        return null;
                }
                if(error_out != null)
                {
                    return null;
                }
                param_i += 1;
            }

            builder.Append(in_str.Substring(prev_i));
            return builder.ToString();
        }
    }
}
