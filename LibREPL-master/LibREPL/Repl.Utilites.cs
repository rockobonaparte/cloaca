using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piksel.LibREPL
{
    partial class Repl
    {
        public static bool? ParseBoolean(string input)
        {
            switch(input.ToLower())
            {
                case "true":
                case "yes":
                case "enable":
                case "1":
                case "on":
                    return true;

                case "false":
                case "no":
                case "disable":
                case "0":
                case "off":
                    return false;

                default:
                    return null;
            }
        }

        public static bool TryParseBoolean(string input, out bool result)
        {
            var b = ParseBoolean(input);
            result = b.GetValueOrDefault();
            return b.HasValue;
        }
    }
}
