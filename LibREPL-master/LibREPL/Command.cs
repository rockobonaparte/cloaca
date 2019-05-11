using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piksel.LibREPL
{
    public class Command
    {
        public CommandAction Action;
        public CommandProgressType Progress = CommandProgressType.None;
        public List<Argument> Arguments = new List<Argument>();

        public Dictionary<string, string> GetArgumentDictionary(string[] args) {
            var ad = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++) {
                ad[(i >= Arguments.Count) ? "?" : Arguments[i].Keyword] = args[i];
            }
            return ad;
        } 

        public string Description { get; set; }
        public string ProgressMessage { get; set; }
        public string ProgressCompleted { get; set; }

        internal bool GetBoolArgumentOrDefault(string[] args, int index)
        {
            // TODO: Perhaps a bit to "clever" of a function? -NM 2016-02-14
            var value = GetArgumentOrDefault(args, index);

            bool parsed;
            return (Repl.TryParseBoolean(value, out parsed) || 
                (value != Arguments[index].Default &&
                Repl.TryParseBoolean(Arguments[index].Default, out parsed)))
                ? parsed : false;
        }

        internal decimal GetNumberArgumentOrDefault(string[] args, int index)
        {
            // TODO: Perhaps a bit to "clever" of a function? -NM 2016-02-14
            var value = GetArgumentOrDefault(args, index);

            decimal parsed;
            return (decimal.TryParse(value, out parsed) ||
                (value != Arguments[index].Default &&
                decimal.TryParse(Arguments[index].Default, out parsed)))
                ? parsed : default(decimal);
        }

        internal string GetArgumentOrDefault(string[] args, int index)
        {
            return index < args.Length ? args[index] : Arguments[index].Default;
        }
    }

    public class Argument
    {
        public Argument(string keyword = "", string description = "")
        {
            Keyword = keyword;
            Description = description;
        }
        public string Description;
        public string Keyword;
        public bool Required = false;
        public ArgumentType Type = ArgumentType.String;
        public string Default;
    }

    public enum ArgumentType
    {
        String, Boolean, Number
    }

    public enum CommandProgressType
    {
        None, Progressbar, Spinner
    }

    public delegate void CommandAction(Repl repl, Command c, string[] args);
}
