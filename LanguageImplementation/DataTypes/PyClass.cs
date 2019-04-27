namespace LanguageImplementation.DataTypes
{
    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, IInterpreter interpreter, FrameContext context) :
            base(name, __init__, interpreter, context)
        {
            // __dict__ used to be set here but was moved upstream
        }
    }
}
