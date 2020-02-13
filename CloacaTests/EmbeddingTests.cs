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
    public delegate void SimpleIntEvent(int something);
    class ReflectIntoPython
    {
        public int AnInteger;
        public string AString;
        public event SimpleIntEvent IntEvent;

        public int AnIntegerProperty
        {
            get { return AnInteger; }
            set { AnInteger = value; }
        }

        public int AnOverload()
        {
            return AnInteger;
        }

        public int AnOverload(int an_arg)
        {
            AnInteger += an_arg;
            return AnInteger;
        }

        // Two of these are defined so they can be subscribed separately as events and kept distinct.
        public void SubscribeSetAnInteger1(int newInteger)
        {
            AnInteger = newInteger;
        }

        public void SubscribeSetAnInteger2(int newInteger)
        {
            AnInteger += newInteger;
        }

        public int AnOverload(params int[] lots_of_ints)
        {
            foreach(int to_add in lots_of_ints)
            {
                AnInteger += to_add;
            }
            return AnInteger;
        }

        // Same as AnOverload that takes params, but it takes an int[] as a normal field.
        // We test if we can work with arrays that are not params. When this was added, we
        // hadn't implemented this yet.
        public int TakeIntArray(int[] ints_that_are_not_params)
        {
            foreach (int to_add in ints_that_are_not_params)
            {
                AnInteger += to_add;
            }
            return AnInteger;
        }

        // Same as TakeIntArray but we mutate it to show that we can alter the proxy.
        public int TakeIntArrayAndChange(int[] ints_that_are_not_params)
        {
            for (int i = 0; i < ints_that_are_not_params.Length; ++i)
            {
                AnInteger += ints_that_are_not_params[i];
                ints_that_are_not_params[i] += 1;
            }
            return AnInteger;
        }

        public object ReturnsNull()
        {
            return null;
        }

        public bool IsNull(object isItNull)
        {
            return isItNull == null;
        }

        public ReflectIntoPython RecursiveObject;

        public ReflectIntoPython(int intVal, string strVal)
        {
            AnInteger = intVal;
            AString = strVal;
        }

        public void SomeMethod()
        {

        }

        public void TriggerIntEvent(int intArg)
        {
            IntEvent(intArg);
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

        class EmbeddedBasicObject
        {
            public int aNumber;
            public EmbeddedBasicObject()
            {
                aNumber = 1111;
            }
        }

        [Test]
        public void BoxObject()
        {
            var pyObj = PyObjectBoxer.Box(new ReflectIntoPython(1, "Yay!"));
        }

        [Test]
        public void CallOverloads()
        {
            runBasicTest(
                "a = obj.AnOverload()\n" +
                "b = obj.AnOverload(raw_integer)\n",
                new Dictionary<string, object>()
            {
                { "obj", new ReflectIntoPython(0, "doesn't matter") },
                { "raw_integer", 1 },
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", 0 },
                { "b", 1 }
            }), 1);
        }

        [Test]
        public void AcceptsNoneAsNull()
        {
            runBasicTest(
                "a = obj.IsNull(None)\n" +
                "b = obj.IsNull(1)\n",
                new Dictionary<string, object>()
            {
                { "obj", new ReflectIntoPython(0, "doesn't matter") }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", true },
                { "b", false }
            }), 1);
        }

        [Test]
        public void NullEqualsNone()
        {
            runBasicTest(
                "a = obj.ReturnsNull() is None\n",
                new Dictionary<string, object>()
            {
                { "obj", new ReflectIntoPython(0, "doesn't matter") }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyBool.True }
            }), 1);
        }

        [Test]
        public void FollowTwoLevelsDeep()
        {
            var inner = new ReflectIntoPython(100, "inner");
            var outer = new ReflectIntoPython(0, "outer");
            outer.RecursiveObject = inner;

            runBasicTest(
                "a = obj.AnInteger\n" +
                "b = obj.RecursiveObject.AnInteger\n",
                new Dictionary<string, object>()
            {
                { "obj", outer }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", 0 },
                { "b", 100 }
            }), 1);
        }

        [Test]
        public void PassPyIntegerToInteger()
        {
            runBasicTest(
                "a = obj.AnOverload(15)\n",
                new Dictionary<string, object>()
            {
                { "obj", new ReflectIntoPython(0, "doesn't matter") }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", 15 }
            }), 1);
        }

        [Test]
        public void PassPyIntegerToParamsInteger()
        {
            runBasicTest(
                "a = obj.AnOverload(1, 2, 3, 4, 5)\n",
                new Dictionary<string, object>()
            {
                { "obj", new ReflectIntoPython(0, "doesn't matter") }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", 1 + 2 + 3 + 4 + 5 }
            }), 1);
        }

        [Test]
        public void ConstructFromClass()
        {
            runBasicTest(
                "obj = ReflectIntoPython(1337, 'I did it!')\n" +
                "a = obj.AnInteger\n" +
                "b = obj.AString\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", new WrappedCodeObject(typeof(ReflectIntoPython).GetConstructors()) }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", 1337 },
                { "b", "I did it!" }
            }), 1);
        }

        [Test]
        [Ignore("Event subscription/unsubscription doesn't work yet")]
        public void EventDotNet()
        {
            runBasicTest(
                "obj = ReflectIntoPython(1337, 'I did it!')\n" +
                "obj.IntEvent += obj.SubscribeSetAnInteger1\n" +
                "obj.IntEvent += obj.SubscribeSetAnInteger2\n" +
                "obj.TriggerIntEvent(111)\n" +          // Set 111 and then add 111 = 222
                "obj.IntEvent -= obj.SubscribeSetAnInteger1\n" + // Event #2 remains so next one will still += AnInteger
                "obj.TriggerIntEvent(111)\n" +          // 222 + 111 = 333
                "a = obj.AnInteger\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", new WrappedCodeObject(typeof(ReflectIntoPython).GetConstructors()) }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", 333 },
            }), 1);
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
                // Overloaded methods come up more than once, but the WrappedCodeObject takes care of it the first time.
                if (!po.__dict__.ContainsKey(method.Name))
                {
                    po.__dict__.Add(method.Name, new WrappedCodeObject(method.Name, method, genericObj));
                }
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
