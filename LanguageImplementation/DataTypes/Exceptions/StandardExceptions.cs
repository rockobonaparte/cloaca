using System;

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

    public class AttributeError : PyException
    {
        public AttributeError(string msg) : base(msg)
        {

        }
    }

    public class TypeError : PyException
    {
        public TypeError(string msg) : base(msg)
        {

        }
    }

    public class ValueError : PyException
    {
        public ValueError(string msg) : base(msg)
        {

        }
    }

    public class NotImplemented : PyException
    {
        public NotImplemented(string msg) : base(msg)
        {

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
            base("ImportError", null, new PyClass[] { new PyExceptionClass() })
        {

        }

        public ImportErrorClass(string classname, PyFunction __init__, PyClass[] bases) : base(classname, __init__, bases)
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
            base("ModuleNotFoundError", null, new PyClass[] { new ImportErrorClass() })
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
