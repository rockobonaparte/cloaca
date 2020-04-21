using System;
using System.Collections.Generic;

using CloacaInterpreter;
using Language;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

using Antlr4.Runtime;
using NUnit.Framework;

namespace CloacaTests
{
    [TestFixture]
    public class RunCodeTest
    {
        protected void runProgram(string program, Dictionary<string, object> variablesIn, List<ISpecFinder> moduleSpecFinders, int expectedIterations, out FrameContext context)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.file_input();

            Assert.That(errorListener.Errors.Count, Is.Zero, "There were parse errors:\n" + errorListener.Report());

            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(antlrVisitorContext);

            // We'll do a disassembly here but won't assert against it. We just want to make sure it doesn't crash.
            CodeObject compiledProgram = visitor.RootProgram.Build();

            Dis.dis(compiledProgram);

            // TODO: This dependency association is kind of gross. It's almost circular and is broken by assigning
            // the interpreter reference to the schedular after its initial constructor.
            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            interpreter.DumpState = true;
            foreach(var finder in moduleSpecFinders)
            {
                interpreter.AddModuleFinder(finder);
            }
            scheduler.SetInterpreter(interpreter);

            var receipt = scheduler.Schedule(compiledProgram);
            context = receipt.Frame;
            foreach (string varName in variablesIn.Keys)
            {
                context.SetVariable(varName, variablesIn[varName]);
            }

            // Waiting on the task makes sure we get punched in the face by any exceptions it throws.
            // But they'll come rolling in as AggregateExceptions so we'll have to unpack them.
            var scheduler_task = scheduler.RunUntilDone();
            scheduler_task.Wait();
            Assert.That(receipt.Completed);
            if(receipt.EscapedExceptionInfo != null)
            {
                receipt.EscapedExceptionInfo.Throw();
            }

            Assert.That(scheduler.TickCount, Is.EqualTo(expectedIterations));
        }

        protected void runProgram(string program, Dictionary<string, object> variablesIn, int expectedIterations, out FrameContext context)
        {
            runProgram(program, variablesIn, new List<ISpecFinder>(), expectedIterations, out context);
        }

        protected FrameContext runProgram(string program, Dictionary<string, object> variablesIn, int expectedIterations)
        {
            FrameContext context;
            runProgram(program, variablesIn, new List<ISpecFinder>(), expectedIterations, out context);
            return context;
        }

        protected FrameContext runProgram(string program, Dictionary<string, object> variablesIn, List<ISpecFinder> modulesSpecFinders, int expectedIterations)
        {
            FrameContext context;
            runProgram(program, variablesIn, modulesSpecFinders, expectedIterations, out context);
            return context;
        }

        protected void runBasicTest(string program, Dictionary<string, object> variablesIn, VariableMultimap expectedVariables, int expectedIterations,
            string[] ignoreVariables)
        {
            var context = runProgram(program, variablesIn, expectedIterations);
            var variables = new VariableMultimap(context);
            try
            {
                variables.AssertSubsetEquals(expectedVariables);
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        protected void runBasicTest(string program, Dictionary<string, object> variablesIn, VariableMultimap expectedVariables, int expectedIterations)
        {
            runBasicTest(program, variablesIn, expectedVariables, expectedIterations, new string[0]);
        }


        protected void runBasicTest(string program, VariableMultimap expectedVariables, int expectedIterations)
        {
            runBasicTest(program, new Dictionary<string, object>(), expectedVariables, expectedIterations, new string[0]);
        }

        protected void runBasicTest(string program, VariableMultimap expectedVariables, int expectedIterations, string[] ignoreVariables)
        {
            runBasicTest(program, new Dictionary<string, object>(), expectedVariables, expectedIterations, ignoreVariables);
        }
    }
}
