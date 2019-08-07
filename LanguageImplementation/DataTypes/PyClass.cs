namespace LanguageImplementation.DataTypes
{
    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, PyClass[] bases) :
            base(name, __init__)
        {
            __bases__ = bases;
            // __dict__ used to be set here but was moved upstream
        }
    }
}
