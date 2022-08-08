using System;
using System.Collections.Generic;
using System.IO;


using CloacaInterpreter;
using LanguageImplementation;
using CloacaInterpreter.ModuleImporting;
using LanguageImplementation.DataTypes;
using System.Threading.Tasks;

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

        static int ScheduleLoop(Scheduler scheduler, string scriptNameForErrors)
        {
            while (!scheduler.Done)
            {
                try
                {
                    scheduler.Tick().Wait();
                }
                catch (AggregateException wrappedEscapedException)
                {
                    Console.WriteLine("Cloaca exception thrown while running " + scriptNameForErrors);
                    Console.WriteLine(wrappedEscapedException.InnerExceptions[0]);
                    return 1;
                }

                if (scheduler.LastTasklet.EscapedDotNetException != null)
                {
                    Console.WriteLine(".NET exception thrown while running " + scriptNameForErrors);
                    Console.WriteLine(scheduler.LastTasklet.EscapedDotNetException);
                    return 1;
                }
            }
            return 0;
        }

        static async Task<int> Main(string[] cmdline_args)
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

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            interpreter.DumpState = true;

            // We're setting up some module import paths so we can test the standard library.
            var repoRoots = new List<string>();
            repoRoots.Add(@"C:\coding\cloaca_git\StandardPythonLibrary");
            interpreter.AddModuleFinder(new FileBasedModuleFinder(repoRoots, new FileBasedModuleLoader()));

            scheduler.SetInterpreter(interpreter);
            interpreter.AddBuiltin(new WrappedCodeObject("print", typeof(ConsoleApplication).GetMethod("print_func")));
            interpreter.StepMode = false;

            // TODO [VARIABLE RESOLUTION]: Pipe in builtins here separately.
            var compiledFunctionTask = ByteCodeCompiler.Compile(program,
                new Dictionary<string, object>(),
                new Dictionary<string, object>(),
                new Dictionary<string, object>(),
                scheduler);
            ScheduleLoop(scheduler, cmdline_args[0]);

            var compiledFunction = await compiledFunctionTask;
            var context = scheduler.Schedule(compiledFunction).Frame;

            ScheduleLoop(scheduler, cmdline_args[0]);

            return 0;
        }
    }
}
