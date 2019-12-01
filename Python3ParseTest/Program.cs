using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;

using OriginalPythonLanguage;

namespace Python3ParseTest
{
    public class TestErrorListener : IParserErrorListener
    {
        public List<string> Errors;

        public TestErrorListener()
        {
            Errors = new List<string>();
        }

        public void ReportAmbiguity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, bool exact, [Nullable] BitSet ambigAlts, [NotNull] ATNConfigSet configs)
        {
            // Ignore
        }

        public void ReportAttemptingFullContext([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, [Nullable] BitSet conflictingAlts, [NotNull] SimulatorState conflictState)
        {
            // Ignore
        }

        public void ReportContextSensitivity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, int prediction, [NotNull] SimulatorState acceptState)
        {
            // Ignore
        }

        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            Errors.Add("line " + line + ":" + charPositionInLine + " " + msg);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //var program = "a = [\n" +
            //              "\n" +
            //              "# Look at me, I'm a riot!\n" +
            //              "1\n" +
            //              "]\n";

            // Use with:
            //var antlrVisitorContext = parser.file_input();

            var program = "if True:\n" +
                          "   a = 1\n" +
                          "   b = 2\n" +
                          "\n";

            var inputStream = new AntlrInputStream(program);
            var lexer = new Python3Lexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var parser = new Python3Parser(commonTokenStream);

            var antlrVisitorContext = parser.single_input();
            Console.ReadLine();
        }
    }
}
