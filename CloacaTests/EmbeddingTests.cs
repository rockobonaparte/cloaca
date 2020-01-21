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
    class ReflectIntoPython
    {
        public int AnInteger;
        public string AString;

        public int AnIntegerProperty
        {
            get { return AnInteger; }
            set { AnInteger = value; }
        }

        public ReflectIntoPython(int intVal, string strVal)
        {
            AnInteger = intVal;
            AString = strVal;
        }

        public void SomeMethod()
        {

        }
    }

    class MockBlockedReturnValue : INotifyCompletion, ISubscheduledContinuation, IPyCallable
    {
        private IScheduler scheduler;
        private FrameContext context;
        private Action continuation;

        public void AssignScheduler(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            this.context = context;
            var task = wrappedMethodBody(interpreter);
            return task;
        }

        // This is the actual payload.
        public async Task<object> wrappedMethodBody(IInterpreter interpreter)
        {
            // We're just having it wait off one tick as a pause since we don't actually have something on the
            // other end of this that will block.
            await new YieldTick(((Interpreter) interpreter).Scheduler, context);
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

        [Test]
        [Ignore("Primitive boxing not yet implemented")]
        public void EmbeddingBasicTypes()
        {
            runBasicTest(
                "dest_int = src_int\n" +
                "dest_float = src_float\n" +
                "dest_double = src_double\n" +
                "dest_string = src_string\n" +
                "dest_bool = src_bool\n",
                new Dictionary<string, object>()
            {
                { "src_int", 1 },        // Note that it's going in as a .NET integer, not PyInteger. It should get boxed.
                { "src_float", 2.0f },
                { "src_double", 3.0 },
                { "src_string", "4" },
                { "src_bool", true }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "dest_int", new PyInteger(1) },     // Note that it *should* be a PyInteger!
                { "dest_float", new PyFloat(2.0f) },
                { "dest_double", new PyFloat(3.0) },
                { "dest_string", new PyString("4") },
                { "dest_bool", PyBool.True },
            }), 1);
        }

        class EmbeddedBasicObject
        {
            public int aNumber;
            public EmbeddedBasicObject()
            {
                aNumber = 1111;
            }
        }

        [Test]
        [Ignore("Object boxing not yet implemented")]
        public void EmbedBasicObjectRead()
        {
            var embeddedInstance = new EmbeddedBasicObject();
            runBasicTest("a = basic_object.aNumber\n", new Dictionary<string, object>()
            {
                { "blocking_call", embeddedInstance }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", new PyInteger(1111) }
            }), 1);
        }

        [Test]
        public void BoxObject()
        {
            var pyObj = PyObjectBoxer.Box(new ReflectIntoPython(1, "Yay!"));
        }
    }

    // Just putting this in the same place as the test for now while we explore where we all have the worry about doing this kind of thing.
    public class PyObjectBoxer
    {
        public static PyInteger Box(BigInteger num)
        {
            return new PyInteger(num);
        }

        public static PyInteger Box(int num)
        {
            return new PyInteger(num);
        }

        public static PyFloat Box(float num)
        {
            return new PyFloat(num);
        }

        public static PyFloat Box(double num)
        {
            return new PyFloat(num);
        }

        public static PyFloat Box(decimal num)
        {
            return new PyFloat(num);
        }

        public static PyBool Box(bool boolean)
        {
            return PyBool.Create(boolean);
        }

        public static PyString Box(string str)
        {
            return new PyString(str);
        }

        public static PyObject Box(object genericObj)
        {
            Type objType = genericObj.GetType();
            var po = new PyObject();
            po.__dict__.Add("__name__", objType.Name);
            po.__dict__.Add("__module__", null);
            po.__dict__.Add("__qualname__", null);

            // TODO: Autoconvert stuff like HashCode, Equals, and ToString to Python equivalents.
            foreach(var method in objType.GetMethods())
            {
                // TODO: Have to deal with overloading
                po.__dict__.Add(method.Name, new WrappedCodeObject(method.Name, method, genericObj));
            }

            foreach(var field in objType.GetFields())
            {
                if(field.FieldType == typeof(int))
                {
                    po.__dict__.Add(field.Name, new PyInteger((int)field.GetValue(genericObj)));
                }
                else if(field.FieldType == typeof(string))
                {
                    po.__dict__.Add(field.Name, new PyString((string)field.GetValue(genericObj)));
                }
            }

            foreach(var eventInfo in objType.GetEvents())
            {
                po.__dict__.Add(eventInfo.Name, null);
            }

            return po;
        }
    }
}
