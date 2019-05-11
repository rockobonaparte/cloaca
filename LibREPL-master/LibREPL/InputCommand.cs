using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piksel.LibREPL
{
    public class InputCommand
    {
        public string Target;
        public string[] Arguments;
        public InputCommand(string raw)
        {
            var qsep = (char)31;
            var quoted = raw.Split('"');
            StringBuilder unquoted = new StringBuilder();
            for (int i = 0; i < quoted.Length; i++)
            {
                var uq = ((i & 1) == 0) ? quoted[i] : quoted[i].Replace(' ', qsep);
                unquoted.Append(uq);
            }
            var parts = unquoted.ToString().Split(' ');
            Target = parts[0];
            Arguments = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                Arguments[i - 1] = parts[i].Replace(qsep, ' ');
            }
        }

        public InputCommand() { }

        public InputCommand AppendArguments(string[] arguments)
        {
            var ic = new InputCommand();
            ic.Arguments = Arguments.Concat(arguments).ToArray();
            ic.Target = Target;
            return ic;
        }
    }
}
