using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.IO;

using Antlr4.Runtime;

using CloacaInterpreter;
using Language;
using LanguageImplementation;
using CloacaInterpreter.ModuleImporting;
using LanguageImplementation.DataTypes;


namespace ConsoleApplication
{
    class ConsoleApplication
    {
        public async static void print_func(IInterpreter interpreter, FrameContext context, object to_print)
        {
            string finalStr = null;
            if (to_print is PyObject)
            {
                var str_func = (IPyCallable)((PyObject)to_print).__getattribute__(PyClass.__STR__);

                var returned = await str_func.Call(interpreter, context, new object[0]);
                if (returned != null)
                {
                    finalStr = ((PyString)returned).ToString();
                }
            }
            else if (to_print != null)
            {
                finalStr = to_print.ToString();
            }
            Console.WriteLine(finalStr);
        }

        static int Main(string[] cmdline_args)
        {
            if (cmdline_args.Length != 1)
            {
                Console.WriteLine("One argument required: path to script to compile and run.");
                Console.WriteLine("The Cloaca console application doesn't support a REPL yet.");
                return 1;
            }
            string program = null;
            using (var inFile = new StreamReader(cmdline_args[0]))
            {
                program = inFile.ReadToEnd();
            }

            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);
            if (errorListener.Errors.Count > 0)
            {
                Console.WriteLine("There were errors trying to compile the script. We cannot run it.");
                return 1;
            }

            var antlrVisitorContext = parser.file_input();

            var variablesIn = new Dictionary<string, object>();
            var visitor = new CloacaBytecodeVisitor(variablesIn, variablesIn);
            visitor.Visit(antlrVisitorContext);

            PyFunction compiledFunction = visitor.RootProgram.Build(variablesIn);

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            interpreter.DumpState = true;

            // We're setting up some module import paths so we can test the standard library.
            var repoRoots = new List<string>();
            repoRoots.Add(@"C:\coding\cloaca_git\StandardPythonLibrary");
            interpreter.AddModuleFinder(new FileBasedModuleFinder(repoRoots, new FileBasedModuleLoader()));

            scheduler.SetInterpreter(interpreter);
            interpreter.AddBuiltin(new WrappedCodeObject("print", typeof(ConsoleApplication).GetMethod("print_func")));

            var context = scheduler.Schedule(compiledFunction).Frame;

            interpreter.StepMode = false;
            while (!scheduler.Done)
            {
                try
                {
                    scheduler.Tick().Wait();
                }
                catch (AggregateException wrappedEscapedException)
                {
                    // Given the nature of exception handling, we should normally only have one of these!
                    ExceptionDispatchInfo.Capture(wrappedEscapedException.InnerExceptions[0]).Throw();
                }

                if (scheduler.LastTasklet.EscapedDotNetException != null)
                {
                    ExceptionDispatchInfo.Capture(scheduler.LastTasklet.EscapedDotNetException).Throw();
                }
            }

            return 0;
        }
    }
}
