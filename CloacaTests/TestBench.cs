﻿using System;
using System.Collections.Generic;

using CloacaInterpreter;
using Language;
using LanguageImplementation;

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;
using NUnit.Framework;

namespace CloacaTests
{
    [TestFixture]
    public class RunCodeTest
    {
        protected FrameContext runProgram(string program, Dictionary<string, object> variablesIn, int expectedIterations)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.program();

            Assert.That(errorListener.Errors.Count, Is.Zero, "There were parse errors:\n" + errorListener.Report());

            var visitor = new CloacaBytecodeVisitor(variablesIn);
            visitor.Visit(antlrVisitorContext);

            // We'll do a disassembly here but won't assert against it. We just want to make sure it doesn't crash.
            CodeObject compiledProgram = visitor.RootProgram.Build();

            Dis.dis(compiledProgram);

            var interpreter = new Interpreter();
            interpreter.DumpState = true;
            var scheduler = new Scheduler(interpreter);

            var context = scheduler.Schedule(compiledProgram);
            foreach (string varName in variablesIn.Keys)
            {
                context.SetVariable(varName, variablesIn[varName]);
            }

            scheduler.RunUntilDone();

            var variables = new VariableMultimap(context);

            Assert.That(scheduler.TickCount, Is.EqualTo(expectedIterations));
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
