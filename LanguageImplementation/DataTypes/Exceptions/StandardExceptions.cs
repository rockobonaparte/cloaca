using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes.Exceptions
{
    public class AssertionError : PyException
    {
        public AssertionError(string msg) : base(msg)
        {

        }

        public AssertionError() : base()
        {

        }
    }

    public class AssertionErrorClass : PyExceptionClass
    {
        public AssertionErrorClass() :
            base("AssertionError", null, new PyClass[] { PyExceptionClass.Instance })
        {

        }

        private static AssertionErrorClass __instance;
        public static new AssertionErrorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new AssertionErrorClass();
                }
                return __instance;
            }
        }

        public static AssertionError Create(string message)
        {
            var exc = PyTypeObject.DefaultNew<AssertionError>(AssertionErrorClass.Instance);
            exc.Message = message;
            return exc;
        }

        public override async Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args,
                                 Dictionary<string, object> defaultOverrides = null,
                                 KwargsDict kwargsDict = null)
        {
            if(args.Length == 0)
            {
                return AssertionErrorClass.Create("");
            }
            else if(args.Length == 1)
            {
                return AssertionErrorClass.Create(args[0].ToString());
            }
            else
            {
                StringBuilder msg = new StringBuilder();
                msg.Append("(");
                for(int arg_i = 0; arg_i < args.Length; ++arg_i)
                {
                    msg.Append(args[arg_i].ToString());
                    if(arg_i < args.Length-1)
                    {
                        msg.Append(", ");
                    }
                }
                msg.Append(")");
                return AssertionErrorClass.Create(msg.ToString());
            }
        }
    }

    public class AttributeError : PyException
    {
        public AttributeError(string msg) : base(msg)
        {

        }
    }

    public class NotImplementedError : PyException
    {
        public NotImplementedError() : base()
        {
            // Parameterless constructor for DefaultNew() and Create()
        }

        public NotImplementedError(string msg) : base(msg)
        {

        }

        public NotImplementedError(PyClass baseClass) : base(baseClass)
        {

        }
    }

    public class NotImplementedErrorClass : PyExceptionClass
    {
        public NotImplementedErrorClass() :
            base("NotImplementedError", null, new PyClass[] { PyExceptionClass.Instance })
        {

        }

        public NotImplementedErrorClass(string classname, PyFunction __init__, PyClass[] bases) : base(classname, __init__, new PyClass[] { PyExceptionClass.Instance })
        {

        }

        private static NotImplementedErrorClass instance = null;
        public static new NotImplementedErrorClass Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NotImplementedErrorClass();
                }
                return instance;
            }
        }

        public static NotImplementedError Create(string message)
        {
            var exc = PyTypeObject.DefaultNew<NotImplementedError>(NotImplementedErrorClass.Instance);
            exc.Message = message;
            return exc;
        }

    }

    public class TypeError : PyException
    {
        public TypeError() : base()
        {
            // Default parameterless constructor for Create()
        }

        public TypeError(string msg) : base(msg)
        {

        }
    }

    public class TypeErrorClass : PyExceptionClass
    {
        public TypeErrorClass() :
            base("TypeError", null, new PyClass[] { PyExceptionClass.Instance })
        {

        }

        private static TypeErrorClass __instance;
        public static new TypeErrorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new TypeErrorClass();
                }
                return __instance;
            }
        }

        public static TypeError Create(string message)
        {
            var exc = PyTypeObject.DefaultNew<TypeError>(TypeErrorClass.Instance);
            exc.Message = message;
            return exc;
        }
    }

    public class IndexError : PyException
    {
        public IndexError() : base()
        {

        }

        public IndexError(string msg) : base(msg)
        {

        }

    }

    public class IndexErrorClass : PyExceptionClass
    {
        public IndexErrorClass() :
            base("IndexError", null, new PyClass[] { PyExceptionClass.Instance })
        {

        }

        private static IndexErrorClass __instance;
        public static new IndexErrorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new IndexErrorClass();
                }
                return __instance;
            }
        }

        public static IndexError Create(string message)
        {
            var exc = PyTypeObject.DefaultNew<IndexError>(IndexErrorClass.Instance);
            exc.Message = message;
            return exc;
        }
    }


    public class ValueError : PyException
    {
        public ValueError() : base()
        {

        }

        public ValueError(string msg) : base(msg)
        {

        }

    }

    public class ValueErrorClass : PyExceptionClass
    {
        public ValueErrorClass() :
            base("ValueError", null, new PyClass[] { PyExceptionClass.Instance })
        {

        }

        private static ValueErrorClass __instance;
        public static new ValueErrorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new ValueErrorClass();
                }
                return __instance;
            }
        }

        public static ValueError Create(string message)
        {
            var exc = PyTypeObject.DefaultNew<ValueError>(ValueErrorClass.Instance);
            exc.Message = message;
            return exc;
        }
    }

    public class ImportError : PyException
    {
        public ImportError(string msg) : base(msg)
        {

        }

        public ImportError(PyClass baseClass): base(baseClass)
        {

        }
    }

    public class ImportErrorClass : PyExceptionClass
    {
        public ImportErrorClass() :
            base("ImportError", null, new PyClass[] { PyExceptionClass.Instance })
        {

        }

        public ImportErrorClass(string classname, PyFunction __init__, PyClass[] bases) : base(classname, __init__, new PyClass[] { PyExceptionClass.Instance })
        {

        }

    }

    public class ModuleNotFoundError : PyException
    {
        public ModuleNotFoundError() : base(ModuleNotFoundErrorClass.Instance)
        {
            // Default parameterless constructor to appease DefaultNew.
        }
    }

    public class ModuleNotFoundErrorClass : ImportErrorClass
    {
        public ModuleNotFoundErrorClass() :
            base("ModuleNotFoundError", null, new PyClass[] { ImportErrorClass.Instance })
        {

        }

        private static ModuleNotFoundErrorClass __instance;
        public static new ModuleNotFoundErrorClass Instance
        {
            get
            {
                if (__instance == null)
                {
                    __instance = new ModuleNotFoundErrorClass();
                }
                return __instance;
            }
        }

        public static ModuleNotFoundError Create(string message)
        {
            var exc = PyTypeObject.DefaultNew<ModuleNotFoundError>(ModuleNotFoundErrorClass.Instance);
            exc.Message = message;
            return exc;
        }
    }

    public class StopIteration : PyException
    {
        public StopIteration() : base()
        {

        }
    }

    /// <summary>
    /// Version of StopIteration thrown from .NET code.
    /// </summary>
    public class StopIterationException : Exception
    {

    }
}
