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
                return (PyTraceback) internal_dict[NextName];
            }
            set
            {
                internal_dict[NextName] = value;
            }
        }

        public Frame Frame
        {
            get
            {
                return (Frame)internal_dict[FrameName];
            }
            set
            {
                internal_dict[FrameName] = value;
            }
        }

        public int LineNumber
        {
            get
            {
                return (int)internal_dict[LineNumberName];
            }
            set
            {
                internal_dict[LineNumberName] = value;
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
