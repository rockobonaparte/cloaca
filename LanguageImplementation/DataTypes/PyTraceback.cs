namespace LanguageImplementation.DataTypes
{
    public class PyTraceback : PyObject
    {
        public PyTraceback Next;
        public Frame Frame;
        public int LineNumber;

        public PyTraceback(PyTraceback next, Frame frame)
        {
            this.Next = next;
            this.Frame = frame;
        }

        public string DumpStack()
        {
            string currentTrace = "Line " + Frame.Program.GetCodeLine(Frame.Cursor);
            if(Next != null)
            {
                return Next.DumpStack() + "\n" + currentTrace;
            }
            else
            {
                return currentTrace;
            }
        }
    }
}
