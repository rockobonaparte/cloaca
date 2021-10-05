using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// <param name="globals">Global variable look up that this code object (and any subordinated code objects if this is
        /// something like a module) would end up using when run.</param>
        /// <param name="scheduler">The scheduler to use to run any code required to completely compile the code. It's used
        /// particularly to fill in default values in function declarations; that involves running some code ahead of time
        /// that then gets assigned as defaults.</param>
        /// <returns>The compiled code.</returns>
        /// <exception cref="CloacaParseException">There were errors trying to build the script into byte code.</exception>
        public static async Task<CodeObject> Compile(string program, Dictionary<string, object> variablesIn, Dictionary<string, object> globals, 
            IScheduler scheduler)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.file_input();

            errorListener.AssertNoErrors();

            var visitor = new CloacaBytecodeVisitor(variablesIn, globals);
            visitor.Visit(antlrVisitorContext);

            await visitor.PostProcess(scheduler);

            CodeObject compiledProgram = visitor.RootProgram.Build(globals);
            return compiledProgram;
        }
    }
}
