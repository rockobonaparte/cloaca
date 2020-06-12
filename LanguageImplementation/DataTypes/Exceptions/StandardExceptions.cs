using System;

namespace LanguageImplementation.DataTypes.Exceptions
{
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

    public class ModuleNotFoundError : PyException
    { 
        public ModuleNotFoundError(string msg) : base(msg)
        {

        }
    }

    public class NotImplemented : PyException
    {
        public NotImplemented(string msg) : base(msg)
        {

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
