using System;
using System.Collections.Generic;

using CloacaInterpreter;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

using Antlr4.Runtime;
using NUnit.Framework;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace CloacaTests
{
    [TestFixture]
    public class RunCodeTest
    {
        protected List<ExceptionDispatchInfo> escapedExceptions;
        protected TaskEventRecord receipt;

        protected void taskHadException(TaskEventRecord taskRecord, ExceptionDispatchInfo exc)
        {
            taskRecord.WhenTaskExceptionEscaped -= taskHadException;
            escapedExceptions.Add(exc);
        }

        protected void whenTaskScheduled(ScheduledTaskRecord scheduled)
        {
            scheduled.SubmitterReceipt.WhenTaskExceptionEscaped += taskHadException;
        }

        protected void whenTaskCompleted(ScheduledTaskRecord scheduled)
        {
            scheduled.SubmitterReceipt.WhenTaskExceptionEscaped -= taskHadException;
        }

        protected async Task<FrameContext> runProgram(string program, Dictionary<string, object> variablesIn, List<ISpecFinder> moduleSpecFinders, int expectedIterations, bool checkExceptions=true)
        {
            // TODO: This dependency association is kind of gross. It's almost circular and is broken by assigning
            // the interpreter reference to the schedular after its initial constructor.
            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            interpreter.DumpState = true;
            foreach (var finder in moduleSpecFinders)
            {
                interpreter.AddModuleFinder(finder);
            }
            scheduler.SetInterpreter(interpreter);
            scheduler.OnTaskScheduled += whenTaskScheduled;

            escapedExceptions = new List<ExceptionDispatchInfo>();
            PyFunction compiledFunction = null;
            Task<PyFunction> compiledTask = null;
            try
            {
                // This is awaitable now but relies on the scheduler. We'll tick the scheduler
                // awhile until this resolves.
                var globals = new Dictionary<string, object>(variablesIn);
                compiledTask = ByteCodeCompiler.Compile(program, globals, scheduler.Interpreter.GetBuiltins(), scheduler);
            }
            catch (CloacaParseException parseFailed)
            {
                Assert.Fail(parseFailed.Message);
            }

            for (int tries = 1; tries < 1000 && !compiledTask.IsCompleted && escapedExceptions.Count == 0; ++tries)
            {
                await scheduler.Tick();
            }

            if (!compiledTask.IsCompleted)
            {
                Assert.Fail("Compilation did not finish with interpreter after 1,000 scheduler ticks");
            }
            else if (escapedExceptions.Count > 0)
            {
                escapedExceptions[0].Throw();
            }
            compiledFunction = await compiledTask;
            Dis.dis(compiledFunction.Code);

            receipt = scheduler.Schedule(compiledFunction, variablesIn);

            // Waiting on the task makes sure we get punched in the face by any exceptions it throws.
            // But they'll come rolling in as AggregateExceptions so we'll have to unpack them.
            var scheduler_task = scheduler.RunUntilDone();
            scheduler_task.Wait();
            Assert.That(receipt.Completed);

            if(checkExceptions)
            {
                AssertNoExceptions();
            }
            
            Assert.That(scheduler.TickCount, Is.EqualTo(expectedIterations));
            return receipt.Frame;
        }

        public void AssertNoDotNetExceptions()
        {
            // For now, just throw the topmost exception if we have one. It would mean if there are
            // multiple exceptions that we have a game of whack-a-mole going on as we sequentially
            // debug them.
            if (escapedExceptions.Count > 0)
            {
                escapedExceptions[0].Throw();
            }
        }

        public void AssertNoExceptions()
        {
            if(receipt.EscapedExceptionInfo != null)
            {
                receipt.EscapedExceptionInfo.Throw();
            }
            AssertNoDotNetExceptions();
        }


        protected async Task<FrameContext> runProgram(string program, Dictionary<string, object> variablesIn, int expectedIterations, bool checkExceptions=true)
        {
            FrameContext context = await runProgram(program, variablesIn, new List<ISpecFinder>(), expectedIterations, checkExceptions);
            return context;
        }

        protected async Task runBasicTest(string program, Dictionary<string, object> variablesIn, VariableMultimap expectedVariables, int expectedIterations,
            string[] ignoreVariables)
        {
            var context = await runProgram(program, variablesIn, expectedIterations);
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

        protected async Task runBasicTest(string program, Dictionary<string, object> variablesIn, VariableMultimap expectedVariables, int expectedIterations)
        {
            await runBasicTest(program, variablesIn, expectedVariables, expectedIterations, new string[0]);
        }


        protected async Task runBasicTest(string program, VariableMultimap expectedVariables, int expectedIterations)
        {
            await runBasicTest(program, new Dictionary<string, object>(), expectedVariables, expectedIterations, new string[0]);
        }

        protected async Task runBasicTest(string program, VariableMultimap expectedVariables, int expectedIterations, string[] ignoreVariables)
        {
            await runBasicTest(program, new Dictionary<string, object>(), expectedVariables, expectedIterations, ignoreVariables);
        }
    }
}
