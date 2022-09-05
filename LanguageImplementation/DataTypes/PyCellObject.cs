namespace LanguageImplementation.DataTypes
{
    public class PyCellObject
    {
        public object ob_ref;

        public PyCellObject(object initial_value)
        {
            ob_ref = initial_value;
        }

        public PyCellObject()
        {
            ob_ref = null;
        }
    }
}
