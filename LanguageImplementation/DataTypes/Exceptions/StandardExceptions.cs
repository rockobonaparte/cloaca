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
}
