using System;
using System.Numerics;
using System.Collections.Generic;

using CloacaInterpreter;
using LanguageImplementation;

using NUnit.Framework;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using LanguageImplementation.DataTypes;

namespace CloacaTests
{
    public delegate void SimpleIntEvent(int something);
    public delegate int ReturnIntEvent(int something);

    // These mocks were created to test:
    // mesh_renderer.material.color = new_color
    // It was just loading all the attributes and not actually storing anything, so
    // I was pretty sure parsing was screwed up.
    public class MockMaterial
    {
        public int color;
    }

    public class MockMeshRenderer
    {
        public MockMaterial material = new MockMaterial();
    }

    class ReflectIntoPython
    {
        public int AnInteger;
        public string AString;
        public event SimpleIntEvent IntEvent;
        public event ReturnIntEvent ReturnIntTakeIntEvent;

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

        /// <summary>
        /// Used to test a static call that takes a parameter.
        /// </summary>
        /// <param name="test_input">The input parameter as a number to be tested as one or not</param>
        /// <returns>True if the input is 1, false otherwise.</returns>
        public static bool IsOne(int test_input)
        {
            return test_input == 1;
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

        public int SubscribeReturnInteger(int newInteger)
        {
            AnInteger += newInteger;
            return AnInteger;
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

        public int TriggerReturningIntEvent(int intArg)
        {
            return ReturnIntTakeIntEvent(intArg);
        }

        public T GenericMethod<T>(T arg)
        {
            return arg;
        }

        public int Kwargs(int first=1, int second=10)
        {
            return first + 10 * second;
        }
    }

    static class ReflectIntoPythonExtensions
    {
        public static int AnExtensionMethod(this ReflectIntoPython reflectThis)
        {
            return reflectThis.AnInteger;
        }

        public static int AnExtensionMethodWithArgs(this ReflectIntoPython reflectThis, int number)
        {
            reflectThis.AnInteger += number;
            return reflectThis.AnInteger;
        }

        public static T AGenericExtensionMethod<T>(this ReflectIntoPython reflectThis, T arg)
        {
            return reflectThis.GenericMethod<T>(arg);
        }

        public static bool AGenericExtensionMethodNoArgs<T>(this ReflectIntoPython reflectThis)
        {
            return true;
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
            return PyInteger.Create(1);                // TODO: Helpers to box/unbox between .NET and Python types.
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
        public void MultilevelAttribute()
        {
            var mesh_renderer = new MockMeshRenderer();
            var interpreter = runProgram("mesh_renderer.material.color = three\n", new Dictionary<string, object>()
            {
                { "mesh_renderer", mesh_renderer},
                { "three", 3 }
            }, 1);
            Assert.That(mesh_renderer.material.color, Is.EqualTo(3));
        }

        [Test]
        public void MultilevelAttributeDotNet()
        {
            var mesh_renderer = new MockMeshRenderer();
            var interpreter = runProgram("mesh_renderer.material.color = 3\n", new Dictionary<string, object>()
            {
                { "mesh_renderer", mesh_renderer},
            }, 1);
            Assert.That(mesh_renderer.material.color, Is.EqualTo(3));
        }

        [Test]
        public void MultilevelInplaceAttribute()
        {
            var mesh_renderer = new MockMeshRenderer();
            mesh_renderer.material.color = 1;
            var interpreter = runProgram("mesh_renderer.material.color += three\n", new Dictionary<string, object>()
            {
                { "mesh_renderer", mesh_renderer},
                { "three", 3 }
            }, 1);
            Assert.That(mesh_renderer.material.color, Is.EqualTo(4));
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
                { "a", PyInteger.Create(1) }
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
        public void StaticCall()
        {
            runBasicTest(
                "a = ReflectIntoPython.IsOne(1)\n" +
                "b = ReflectIntoPython.IsOne(2)\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", typeof(ReflectIntoPython) }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", true },
                { "b", false },
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
                { "ReflectIntoPython", new PyDotNetClassProxy(typeof(ReflectIntoPython)) }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", 1337 },
                { "b", "I did it!" }
            }), 1);
        }

        [Test]
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

        // Test coverage TODO:
        // 1. Call .NET event directly
        // 2. Attach/detach IPyCallable
        // 3. Maybe see what happens when an exception happens during event invocation.
        // 4. Get mad if you try to attach an event to another event using +=/-=. It's stupid but you did it once and the null pointer
        //    exception isn't enough to call it out.
        [Test]
        public async Task ReturningEventDotNet()
        {
            string failNote = "Attempted to bind a callable to an event that requires a return type. We don't support this type of binding.  " +
                    "All our callables have to be async, and that meddles with signature of basic return values. Why are you using an event with " +
                    "a return type anyways?";
            FrameContext runContext = await runProgram
                        ("obj = ReflectIntoPython(1337, 'I did it!')\n" +
                         "obj.ReturnIntTakeIntEvent += obj.SubscribeReturnInteger\n" +
                         "obj.TriggerReturningIntEvent(111)\n" +
                         "obj.ReturnIntTakeIntEvent -= obj.SubscribeReturnInteger\n" +
                         "a = obj.AnInteger\n", new Dictionary<string, object>()
                         {
                            { "ReflectIntoPython", new WrappedCodeObject(typeof(ReflectIntoPython).GetConstructors()) }
                         }, 1);
            Assert.NotNull(runContext.EscapedDotNetException);
            Assert.That(runContext.EscapedDotNetException.Message, Is.EqualTo(failNote));
        }

        [Test]
        public async Task CallGenericMethod()
        {
            FrameContext runContext = await runProgram(
                "obj = ReflectIntoPython(1337, 'Generic test!')\n" +
                "a = obj.GenericMethod(ReflectIntoPython, obj)\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", new PyDotNetClassProxy(typeof(ReflectIntoPython)) }
            }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var aInstance = (ReflectIntoPython)variables["a"];
            Assert.That(aInstance.AnInteger, Is.EqualTo(1337));
            Assert.That(aInstance.AString, Is.EqualTo("Generic test!"));
        }

        [Test]
        public async Task CallGenericExtensionMethod()
        {
            FrameContext runContext = await runProgram(
                "obj = ReflectIntoPython(1337, 'Generic test!')\n" +
                "b = MockMaterial()\n" +
                "b.color = 333\n" +
                "a = obj.AGenericExtensionMethod(MockMaterial, b)\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", new PyDotNetClassProxy(typeof(ReflectIntoPython)) },
                { "MockMaterial", typeof(MockMaterial) }
            }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var aInstance = (MockMaterial)variables["a"];
            Assert.That(aInstance.color, Is.EqualTo(333));
        }

        [Test]
        public async Task CallGenericExtensionMethodNoArgs()
        {
            FrameContext runContext = await runProgram(
                "obj = ReflectIntoPython(1337, 'Generic test!')\n" +
                "a = obj.AGenericExtensionMethodNoArgs(ReflectIntoPython)\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", new PyDotNetClassProxy(typeof(ReflectIntoPython)) }
            }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var aInstance = (bool)variables["a"];
            Assert.That(aInstance, Is.True);
        }

        [Test]
        public async Task CallExtensionMethod()
        {
            FrameContext runContext = await runProgram(
                "obj = ReflectIntoPython(1337, 'Generic test!')\n" +
                "a = obj.AnExtensionMethod()\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", new PyDotNetClassProxy(typeof(ReflectIntoPython)) }
            }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var a = (int)variables["a"];
            Assert.That(a, Is.EqualTo(1337));
        }

        [Test]
        public async Task CallExtensionMethodWithArgs()
        {
            FrameContext runContext = await runProgram(
                "obj = ReflectIntoPython(1337, 'Generic test!')\n" +
                "a = obj.AnExtensionMethodWithArgs(1)\n",
                new Dictionary<string, object>()
            {
                { "ReflectIntoPython", new PyDotNetClassProxy(typeof(ReflectIntoPython)) }
            }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var a = (int)variables["a"];
            Assert.That(a, Is.EqualTo(1338));
        }

        [Test]
        public async Task CallDotNetOptionalsImplicit()
        {
            FrameContext runContext = await runProgram(
                "a = obj.Kwargs()\n",
                new Dictionary<string, object>()
                {
                    { "obj", new ReflectIntoPython(1337, "Kwargs test!") }
                }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var a = (int)variables["a"];
            Assert.That(a, Is.EqualTo(101));
        }

        [Test]
        public async Task CallDotNetOptionalsImplicitInOrder()
        {
            FrameContext runContext = await runProgram(
                "a = obj.Kwargs(2, 20)\n",
                new Dictionary<string, object>()
                {
                    { "obj", new ReflectIntoPython(1337, "Kwargs test!") }
                }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var a = (int)variables["a"];
            Assert.That(a, Is.EqualTo(202));
        }


        [Test]
        [Ignore("Cannot support this until we support kwargs")]
        public async Task CallDotNetOptionalsExplicitInOrder()
        {
            FrameContext runContext = await runProgram(
                "a = obj.Kwargs(first=2, second=20)\n",
                new Dictionary<string, object>()
                {
                    { "obj", new ReflectIntoPython(1337, "Kwargs test!") }
                }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var a = (int)variables["a"];
            Assert.That(a, Is.EqualTo(202));
        }

        [Test]
        [Ignore("Cannot support this until we support kwargs.")]
        public async Task CallDotNetOptionalsExplicitOutOfOrder()
        {
            FrameContext runContext = await runProgram(
                "a = obj.Kwargs(second=20, first=2)\n",
                new Dictionary<string, object>()
                {
                    { "obj", new ReflectIntoPython(1337, "Kwargs test!") }
                }, 1);
            var variables = runContext.DumpVariables();
            Assert.That(variables.ContainsKey("a"));
            var a = (int)variables["a"];
            Assert.That(a, Is.EqualTo(202));
        }

        // Cousin to Basics.ComprehensiveArithmeticOperators. This tests with a .NET integer!
        [Test]
        public void ComprehensiveArithmeticOperators()
        {
            runBasicTest(
                "a = x + 2\n" +
                "b = x - 2\n" +
                "c = x * 2\n" +
                "d = x / 2\n" +
                "e = x % 9\n" +
                "f = x // 3\n" +
                "g = x ** 2\n" +
                "h = x & 2\n" +
                "i = x | 14\n" +
                "j = x ^ 2\n" +
                "k = x >> 2\n" +
                "l = x << 2\n", 
            new Dictionary<string, object>()
            {
                { "x", 10 }
            }, new VariableMultimap(new TupleList<string, object>
            {
                { "a", (BigInteger) 12 },
                { "b", (BigInteger) 8 },
                { "c", (BigInteger) 20 },
                { "d", (decimal) 5.0 },
                { "e", (BigInteger) 1 },
                { "f", (BigInteger) 3 },
                { "g", 100.0 },
                { "h", (BigInteger) 2 },
                { "i", (BigInteger) 14 },
                { "j", (BigInteger) 8 },
                { "k", (int) 2 },
                { "l", (int) 40 }
            }), 1);
        }

        // Cousin to Basics.AssignmentOperators. This tests with a .NET integer!
        [Test]
        public void AssignmentOperators()
        {
            // https://www.w3schools.com/python/python_operators.asp
            // +=	x += 3	x = x + 3	
            // -=	x -= 3	x = x - 3	
            // *=	x *= 3	x = x * 3	
            // /=	x /= 3	x = x / 3	
            // %=	x %= 3	x = x % 3	
            // //=	x //= 3	x = x // 3	
            // **=	x **= 3	x = x ** 3	
            // &=	x &= 3	x = x & 3	
            // |=	x |= 3	x = x | 3	
            // ^=	x ^= 3	x = x ^ 3	
            // >>=	x >>= 3	x = x >> 3	
            // <<=	x <<= 3	x = x << 3
            runBasicTest(
                "a += 2\n" +
                "b -= 2\n" +
                "c *= 2\n" +
                "d /= 2\n" +
                "e %= 9\n" +
                "f //= 3\n" +
                "g **= 2\n" +
                "h &= 2\n" +
                "i |= 14\n" +
                "j ^= 2\n" +
                "k >>= 2\n" +
                "l <<= 2\n",
                new Dictionary<string, object>()
                {
                    { "a", 10 },
                    { "b", 10 },
                    { "c", 10 },
                    { "d", 10 },
                    { "e", 10 },
                    { "f", 10 },
                    { "g", 10 },
                    { "h", 10 },
                    { "i", 10 },
                    { "j", 10 },
                    { "k", 10 },
                    { "l", 10 },
                }, new VariableMultimap(new TupleList<string, object>
                {
                { "a", (BigInteger) 12 },
                { "b", (BigInteger) 8 },
                { "c", (BigInteger) 20 },
                { "d", (decimal) 5.0 },
                { "e", (BigInteger) 1 },
                { "f", (BigInteger) 3 },
                { "g", 100.0 },
                { "h", (BigInteger) 2 },
                { "i", (BigInteger) 14 },
                { "j", (BigInteger) 8 },
                { "k", 2 },
                { "l", 40 }
            }), 1);
        }

        // Cousin to DataStructureTests.ListReadWrite
        [Test]
        public async Task ListReadWrite()
        {
            var context = await runProgram(
                "b = a[0]\n" +
                "a[1] = twohundred\n", new Dictionary<string, object>()
                {
                    { "a", new int[] {1, 2} },
                    { "twohundred", 200 }
                }, 1);
            var variables = context.DumpVariables();
            Assert.That(variables["b"], Is.EqualTo(1));
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new int[] { 1, 200 }));
            Assert.That(variables.ContainsKey("b"));
        }

        // Just like ListReadWrite, but we're assigning a PyInteger to a .NET integer
        [Test]
        public async Task ListReadWriteDoNetInt()
        {
            var context = await runProgram(
                "b = a[0]\n" +
                "a[1] = 200\n", new Dictionary<string, object>()
                {
                    { "a", new int[] {1, 2} },
                }, 1);
            var variables = context.DumpVariables();
            Assert.That(variables["b"], Is.EqualTo(1));
            Assert.That(variables.ContainsKey("a"));
            Assert.That(variables["a"], Is.EquivalentTo(new int[] { 1, 200 }));
            Assert.That(variables.ContainsKey("b"));
        }
    }
}
