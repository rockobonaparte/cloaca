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
            /*
                >>> def raise_stuff():
                ...   raise Exception("Derp")
                ...
                >>> raise_stuff()
                Traceback (most recent call last):
                  File "<stdin>", line 1, in <module>
                  File "<stdin>", line 2, in raise_stuff
                Exception: Derp
            */
            string currentTrace = "\tline " + Frame.Program.GetCodeLine(Frame.Cursor) + ", in ";
            currentTrace += Frame.Program.Name;
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
