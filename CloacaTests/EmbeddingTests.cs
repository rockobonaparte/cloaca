using System;
using System.Numerics;
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
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    class MockBlockedReturnValue : INotifyCompletion, ISubscheduledContinuation, IPyCallable
    {
        private Interpreter interpreter;
        private Action continuation;

        public void AssignInterpreter(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            var task = wrappedMethodBody(interpreter);
            return task;
        }

        // This is the actual payload.
        public async Task<object> wrappedMethodBody(IInterpreter interpreter)
        {
            // We're just having it wait off one tick as a pause since we don't actually have something on the
            // other end of this that will block.
            this.interpreter = (Interpreter) interpreter;
            await new YieldTick(this.interpreter);
            return new PyInteger(1);                // TODO: Helpers to box/unbox between .NET and Python types.
        }

        // Needed by ISubscheduledContinuation
        public Task Continue()
        {
            // We only yield once so we're good.
            continuation?.Invoke();
            return Task.FromResult(true);
        }

        // Needed by INotifyCompletion. Gives us the continuation that we must run when we're done with our blocking section
        // in order to continue execution wherever we were halted.
        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
        }
    }

    [TestFixture]
    public class EmbeddingTests : RunCodeTest
    {
        // This is suboptimal due to some sequential coupling, but it's an experiment for starters.
        int calledCount = 0;
        void Meow()
        {
            calledCount += 1;
        }


        [Test]
        public void EmbeddedVoid()
        {            
            calledCount = 0;
            Expression<Action<EmbeddingTests>> expr = instance => Meow();
            var methodInfo = ((MethodCallExpression)expr.Body).Method;

            var meowCode = new WrappedCodeObject("meow", methodInfo, this);
            
            var interpreter = runProgram("meow()\n", new Dictionary<string, object>()
            {
                { "meow", meowCode }
            }, 1);
            Assert.That(calledCount, Is.EqualTo(1));
        }

        [Test]
        public void YieldForResult()
        {
            var blockedReturnMock = new MockBlockedReturnValue();

            runBasicTest("a = blocking_call()\n", new Dictionary<string, object>()
            {
                { "blocking_call", blockedReturnMock }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", new PyInteger(1) }
            }), 2);
        }
    }
}
