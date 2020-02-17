using LanguageImplementation;
using LanguageImplementation.DataTypes;

using NUnit.Framework;


namespace CloacaTests
{

    class TestPythonClass : PyClass
    {
        public TestPythonClass(CodeObject __init__) :
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
        [Ignore("Just prototyped here for now to show the existence of the problem. We need to get the self pointer associated with the WrappedCodeObject")]
        public void InvokingWrappedMethod()
        {
            var wrapper = new WrappedCodeObject("hello_void", typeof(TestPythonClass).GetMethod("hello_void"), this);
            var instance = new TestPythonObject();
            wrapper.Call(null, null, new object[0]);
        }
    }
}
