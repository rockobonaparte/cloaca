namespace LanguageImplementation.DataTypes
{
    public class NotImplemented
    {
        private static NotImplemented _instance;
        public static NotImplemented Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NotImplemented();
                }
                return _instance;
            }
        }

        private NotImplemented()
        {
        }
    }
}
