namespace LanguageImplementation.DataTypes
{
    public class PyTraceback : PyObject
    {
        public const string NextName = "tb_next";
        public const string FrameName = "tb_frame";
        public const string LineNumberName = "tb_lineno";

        public PyTraceback Next
        {
            get
            {
                return (PyTraceback) __dict__[NextName];
            }
            set
            {
                __dict__[NextName] = value;
            }
        }

        public Frame Frame
        {
            get
            {
                return (Frame)__dict__[FrameName];
            }
            set
            {
                __dict__[FrameName] = value;
            }
        }

        public int LineNumber
        {
            get
            {
                return (int)__dict__[LineNumberName];
            }
            set
            {
                __dict__[LineNumberName] = value;
            }
        }

        public PyTraceback(PyTraceback next, Frame frame, int line_number)
        {
            this.Next = next;
            this.Frame = frame;
            this.LineNumber = line_number;
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
