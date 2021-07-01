using System.Collections.Generic;

using Antlr4.Runtime;
using Language;
using LanguageImplementation;

namespace CloacaInterpreter
{
    /// <summary>
    /// Builds Cloaca byte code from source text. This will generate the necessary code objects to give to the
    /// scheduler in order to run the script.
    /// </summary>
    public class ByteCodeCompiler
    {
        /// <summary>
        /// Build a Cloaca script from source to produce a CodeObject that you can run from the scheduler.
        /// </summary>
        /// <param name="program">The script text.</param>
        /// <param name="variablesIn">Variables referenced from the script that have to be externally injected. The
        /// dictionary maps the string key as the variable name and the value is the object to be referenced in the script.</param>
        /// <returns>The compiled code.</returns>
        /// <exception cref="CloacaParseException">There were errors trying to build the script into byte code.</exception>
        public static CodeObject Compile(string program, Dictionary<string, object> variablesIn, Scheduler scheduler)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.file_input();

            errorListener.AssertNoErrors();

            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(antlrVisitorContext);

            visitor.PostProcess(scheduler);

            CodeObject compiledProgram = visitor.RootProgram.Build();
            return compiledProgram;
        }
    }
}
