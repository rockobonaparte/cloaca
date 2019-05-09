using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piksel.LibREPL
{
    partial class Repl
    {
        private void Dir(ArgumentType type)
        {
            Write("<");
            Write(type.ToString(), ConsoleColor.Magenta);
            Write(">");
        }

        public void Dir(object[] a)
        {
            var arrDecMax = a.Length.ToString().Length;
            for (int i = 0; i < a.Length; i++)
            {
                Write(" [");
                Write(i.ToString().PadLeft(arrDecMax, ' '), ConsoleColor.Cyan);
                Write("] => ");
                Write(a[i].ToString());
                NewLine();
            }
        }

        public void Dir(Dictionary<string, string> d)
        {
            var arrDecMax = 0;
            foreach (var key in d.Keys)
                if (key.Length > arrDecMax) arrDecMax = key.Length;

            foreach (var kvp in d)
            {
                Write(" [");
                Write(kvp.Key.PadLeft(arrDecMax, ' '), ConsoleColor.Cyan);
                Write("] => ");
                Write(kvp.Value.ToString());
                NewLine();
            }
        }

        internal void Dir(InputCommand cmd)
        {
            NewLine();
            Write(" (");
            Write(cmd.Target, ConsoleColor.Cyan);
            Write(") => ");
            var offset = new string(' ', cmd.Target.Length + 7);
            for (int i = 0; i < cmd.Arguments.Length; i++)
            {
                if (i > 0) Write(offset);
                Write("[", ConsoleColor.White);
                Write(i, ConsoleColor.Magenta);
                Write("] ");
                Write(cmd.Arguments[i] + "\n", ConsoleColor.Yellow);
            }
            if (cmd.Arguments.Length < 1)
                Write("[]\n");
            NewLine();
        }
    }
}
