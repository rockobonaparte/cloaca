namespace LanguageImplementation.DataTypes
{
    public class NoneType
    {
        private static NonePyObject _instance;
        public static NonePyObject Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NonePyObject();
                }
                return _instance;
            }
        }

        private NoneType()
        {
        }
    }

    public class NonePyObject : PyObject
    {

    }
}
