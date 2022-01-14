using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaTests
{

    class TestPythonClass : PyClass
    {
        public TestPythonClass(PyFunction __init__) :
            base("test_python_class", __init__, new PyClass[0])
        {
        }

        private static TestPythonClass __instance;
        public static TestPythonClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new TestPythonClass(null);
                }
                return __instance;
            }
        }
        [ClassMember]
        public static void hello_void(TestPythonObject self)
        {
            self.TestInt += 1;
        }
    }

    class TestPythonObject : PyObject
    { 
        public int TestInt;

        public TestPythonObject() : base(TestPythonClass.Instance)
        {
            TestInt = 0;
        }
    }

    [TestFixture]
    public class WrappedCodeObjectTests
    {
        [Test]
        public void InvokingWrappedMethod()
        {
            var mockScheduler = new Scheduler();
            var mockInterpreter = new Interpreter(mockScheduler);
            var mockFrame = new FrameContext();
            var instance = PyTypeObject.DefaultNew<TestPythonObject>(TestPythonClass.Instance);
            var method = instance.__getattribute__("hello_void") as IPyCallable;
            method.Call(mockInterpreter, mockFrame, new object[0]);
        }
    }
}
