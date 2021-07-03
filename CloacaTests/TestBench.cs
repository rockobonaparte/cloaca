﻿using System;
using System.Collections.Generic;

using CloacaInterpreter;
using Language;
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

        protected async Task<FrameContext> runProgram(string program, Dictionary<string, object> variablesIn, List<ISpecFinder> moduleSpecFinders, int expectedIterations)
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
            CodeObject compiledProgram = null;
            try
            {
                compiledProgram = await ByteCodeCompiler.Compile(program, variablesIn, scheduler);
            }
            catch(CloacaParseException parseFailed)
            {
                Assert.Fail(parseFailed.Message);
            }

            Dis.dis(compiledProgram);

            var receipt = scheduler.Schedule(compiledProgram);
            FrameContext context = receipt.Frame;
            foreach (string varName in variablesIn.Keys)
            {
                context.SetVariable(varName, variablesIn[varName]);
            }

            // Waiting on the task makes sure we get punched in the face by any exceptions it throws.
            // But they'll come rolling in as AggregateExceptions so we'll have to unpack them.
            var scheduler_task = scheduler.RunUntilDone();
            scheduler_task.Wait();
            Assert.That(receipt.Completed);

            //if(receipt.EscapedExceptionInfo != null)
            //{
            //    receipt.EscapedExceptionInfo.Throw();
            //}

            // For now, just throw the topmost exception if we have one. It would mean if there are
            // multiple exceptions that we have a game of whack-a-mole going on as we sequentially
            // debug them.
            //if(escapedExceptions.Count > 0)
            //{
            //    escapedExceptions[0].Throw();
            //}

            Assert.That(scheduler.TickCount, Is.EqualTo(expectedIterations));
            return context;
        }

        public void AssertNoDotNetExceptions()
        {
            if(escapedExceptions.Count > 0)
            {
                escapedExceptions[0].Throw();
            }
        }


        protected async Task<FrameContext> runProgram(string program, Dictionary<string, object> variablesIn, int expectedIterations)
        {
            FrameContext context = await runProgram(program, variablesIn, new List<ISpecFinder>(), expectedIterations);
            return context;
        }

        protected async void runBasicTest(string program, Dictionary<string, object> variablesIn, VariableMultimap expectedVariables, int expectedIterations,
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
