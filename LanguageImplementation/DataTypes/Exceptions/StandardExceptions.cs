namespace LanguageImplementation.DataTypes.Exceptions
{
    public class AttributeError : PyException
    {
        public AttributeError(PyException self, string msg) : base(self, msg)
        {

        }
    }
}
