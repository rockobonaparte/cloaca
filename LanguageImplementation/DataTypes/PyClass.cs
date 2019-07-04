namespace LanguageImplementation.DataTypes
{
    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, IInterpreter interpreter, FrameContext context,
            PyClass[] bases) :
            base(name, __init__, interpreter, context)
        {
            __bases__ = bases;
            // __dict__ used to be set here but was moved upstream
        }
    }
}
