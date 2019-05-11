using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Piksel.LibREPL
{
    public partial class Repl
    {
        ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;
        ConsoleColor DefaultForegroundColor = ConsoleColor.Gray;

        string _headerTitle;
        public string HeaderTitle
        {
            get { return _headerTitle; }
            set {
                Console.Title = value;
                _headerTitle = value;
            }
        }
        public string HeaderSubTitle;
        public string Prompt;
        public ConsoleColor PromptColor = ConsoleColor.White;

        public SoftwareInfo Software = new SoftwareInfo();

        private string _spinnerEnd = " ";
        private int _spinnerPos = 0;
        private int _spinnerTop = 0;
        private BackgroundWorker _progressWorker;

        private bool _userQuit = false;

        public bool PrintInput = false;

        public Dictionary<string, Command> Commands = new Dictionary<string, Command>();

        public Repl() : this("") { }
        public Repl(string prompt)
        {
            Prompt = prompt;
            AddInternalCommands();
        }

        public void Start(bool showHeader = true)
        {
            if(showHeader)
                PrintHeader();

            InputCommand cmd;
            do
            {
                cmd = DoPrompt();

                if (PrintInput)
                    Dir(cmd);
                else
                    NewLine();

                ProcessCommand(cmd);

            } while (!_userQuit);
        }

        public void Reset(bool resetColors = true, bool resetCursor = true, bool clearScreen = true)
        {
            if (resetColors)
            {
                Console.BackgroundColor = DefaultBackgroundColor;
                Console.ForegroundColor = DefaultForegroundColor;
            }
            if(resetCursor)
            {
                Console.SetCursorPosition(0, 0);
            }
            if (clearScreen)
            {
                Console.Clear();
            }
        }

        public void ProcessCommand(InputCommand cmd)
        {

            if (Commands.ContainsKey(cmd.Target))
            {
                var cc = Commands[cmd.Target];
                if(cc.Progress == CommandProgressType.Spinner)
                {
                    Write(" "+cc.ProgressMessage);
                    StartSpinner();
                }
                Commands[cmd.Target].Action.Invoke(this, cc, cmd.Arguments);
                if (cc.Progress == CommandProgressType.Spinner)
                {
                    StopSpinner("Done!");
                }
            }
            else {
                Write(" Error", ConsoleColor.Red);
                Write(": Unknown command \"");
                Write(cmd.Target, ConsoleColor.Yellow);
                Write("\".\n");
            }

        }

        public void PrintHeader()
        {
            if (string.IsNullOrEmpty(HeaderTitle) && string.IsNullOrEmpty(HeaderSubTitle))
                return;

            NewLine();
            Write(" " + HeaderTitle + "\n", ConsoleColor.White);
            Write(" " + HeaderSubTitle + "\n");
            Hr();
        }

        public InputCommand DoPrompt()
        {
            Console.ForegroundColor = PromptColor;
            Console.Write("\n " + Prompt);
            Console.ForegroundColor = ConsoleColor.Gray;
            var raw = Console.ReadLine();
            return new InputCommand(raw);
        }



        public void Write(string s, ConsoleColor? color = null)
        {
            ConsoleColor orig = ConsoleColor.Gray;
            if (color.HasValue)
            {
                orig = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
            }
            Console.Write(s);
            if (color.HasValue)
                Console.ForegroundColor = orig;
        }

        public void WriteFormatted(string s, ConsoleColor? color = null)
        {
            ConsoleColor orig = ConsoleColor.Gray;
            if (color.HasValue)
            {
                orig = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
            }
            var matches = Regex.Matches(s, "([^_]*)_([^\\s]*)_([^_]*)");
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    Write(match.Groups[1].Value);
                    Write(match.Groups[2].Value, ConsoleColor.Magenta);
                    Write(match.Groups[3].Value);
                }
            }
            else
            {
                Write(s);
            }
            if (color.HasValue)
                Console.ForegroundColor = orig;
        }

        public void Write(int i, ConsoleColor? c = null)
        {
            Write(i.ToString(), c);
        }

        public void NewLine()
        {
            Console.WriteLine();
        }

        public void Hr(char lineChar = '-', ConsoleColor? color = null)
        {
            ConsoleColor orig = ConsoleColor.Gray;
            if (color.HasValue)
            {
                orig = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
            }
            Console.WriteLine(" " + new string(lineChar, Console.BufferWidth - 2));
            if (color.HasValue)
                Console.ForegroundColor = orig;
        }

        public void Clear()
        {
            Console.Clear();
        }

        public string GetPassword(string prompt = "")
        {
            if (!string.IsNullOrEmpty(prompt))
                Write("\n " + prompt);

            var sb = new StringBuilder();
            do
            {
                var key = Console.ReadKey(true);
                if ((key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.C)
                    || key.Key == ConsoleKey.Escape)
                {
                    Write("Aborted!\n", ConsoleColor.Red);
                    return string.Empty;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    NewLine();
                    return sb.ToString();
                }
                else
                {
                    sb.Append(key.KeyChar);
                }
            } while (true);
        }
    }
}
