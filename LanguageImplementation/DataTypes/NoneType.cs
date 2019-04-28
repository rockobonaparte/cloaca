namespace LanguageImplementation.DataTypes
{
    public class NoneType
    {
        private static NoneType _instance;
        public static NoneType Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NoneType();
                }
                return _instance;
            }
        }

        private NoneType()
        {
        }
    }
}
