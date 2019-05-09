namespace Piksel.LibREPL
{
    internal class AliasCommand : Command
    {
        private InputCommand _inCmd;

        public AliasCommand(string command)
        {
            _inCmd = new InputCommand(command);
            Description = $"Alias of _{command}_.";
            Action = (repl, cmd, args) =>
            {
                repl.ProcessCommand((args.Length > 0) ? _inCmd.AppendArguments(args) : _inCmd);
            };
        }
    }
}