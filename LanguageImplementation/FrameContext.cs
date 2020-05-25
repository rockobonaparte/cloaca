using System;
using System.Collections.Generic;
using LanguageImplementation.DataTypes;

namespace LanguageImplementation
{
    public class FrameContext
    {
        /// <summary>
        /// Mimicking the global exception structure I believe used in Python. We put one in the frame and react to it if it's
        /// present. If it's null, we don't have an exception and we run as usual.
        /// </summary>
        public PyObject CurrentException;

        // TODO: Start wrapping these up as PyExceptions
        public Exception EscapedDotNetException;

        public Stack<Frame> callStack;

        public FrameContext() : this(new Stack<Frame>())
        {
        }

        public FrameContext(Stack<Frame> callStack)
        {
            StartDepth = callStack.Count;
            this.callStack = callStack;
            SysModules = new Dictionary<string, PyModule>();
        }

        /// <summary>
        /// A subcontext carries the variable state of the parent FrameContext but runs different
        /// code with it. This is used for things like functions defined inside functions.
        /// </summary>
        /// <param name="subFrames">The frame stack to use for the subcontext.</param>
        /// <returns>A new FrameContext that has this FrameContext's variable state but the
        /// new callstack based on rootFrame.</returns>
        public FrameContext CreateSubcontext(Stack<Frame> subFrames)
        {
            Stack<Frame> reverseStack = new Stack<Frame>();
            foreach (var parentFrame in callStack)
            {
                reverseStack.Push(parentFrame);
            }
            foreach (var childFrame in subFrames)
            {
                reverseStack.Push(childFrame);
            }

            int subStartDepth = callStack.Count;
            FrameContext newContext = new FrameContext(reverseStack);
            newContext.StartDepth = subStartDepth;
            return newContext;
        }

        /// <summary>
        /// This is the call stack depth when the frame context was created. If our depth is less
        /// than this, then this context has finished.
        /// 
        /// A fresh frame context will likely start with a depth of zero. Subcontexts such as
        /// functions in functions will start with a non-zero depth.
        /// </summary>
        public int StartDepth
        {
            get; private set;
        }

        public int Cursor
        {
            get
            {
                return callStack.Peek().Cursor;
            }
            set
            {
                callStack.Peek().Cursor = value;
            }
        }

        public Stack<Object> DataStack
        {
            get
            {
                return callStack.Peek().DataStack;
            }
        }

        public Stack<Block> BlockStack
        {
            get
            {
                return callStack.Peek().BlockStack;
            }
        }

        public CodeObject Program
        {
            get
            {
                return callStack.Peek().Program;
            }
        }

        public byte[] Code
        {
            get
            {
                return callStack.Peek().Program.Code.Bytes;
            }
        }

        public CodeByteArray CodeBytes
        {
            get
            {
                return callStack.Peek().Program.Code;
            }
        }

        public List<object> Locals
        {
            get
            {
                return callStack.Peek().Locals;
            }
        }

        public List<string> Names
        {
            get
            {
                return callStack.Peek().Names;
            }
        }

        public List<string> LocalNames
        {
            get
            {
                return callStack.Peek().LocalNames;
            }
        }

        /// <summary>
        /// A representation of sys.modules. Each context gets its own since it's possibly
        /// importing different things.
        /// </summary>
        public Dictionary<string, PyModule> SysModules;

        // This is like doing a LOAD_NAME without pushing it on the stack.
        public object GetVariable(string name)
        {
            // Try to resolve locally, then globally, and then in our built-in namespace
            foreach (var stackFrame in callStack)
            {
                // Unlike LOAD_GLOBAL, the current frame is fair game. In fact, we search it first!
                var nameIdx = stackFrame.LocalNames.IndexOf(name);
                if (nameIdx >= 0)
                {
                    return stackFrame.Locals[nameIdx];
                }
            }

            throw new Exception("'" + name + "' not found in local or global namespaces, and we don't resolve built-ins yet.");
        }

        public void AddVariable(string name, object value)
        {
            var nameIdx = LocalNames.IndexOf(name);
            if (nameIdx == -1)
            {
                LocalNames.Add(name);
                Locals.Add(value);
            }
            else
            {
                Locals[nameIdx] = value;
            }
        }

        public void SetVariable(string name, object value)
        {
            int varIdx = LocalNames.IndexOf(name);
            if (varIdx < 0)
            {
                throw new KeyNotFoundException("Could not find variable in locals named " + name);
            }
            Locals[varIdx] = value;
        }

        public bool HasVariable(string name)
        {
            foreach (var stackFrame in callStack)
            {
                // Unlike LOAD_GLOBAL, the current frame is fair game. In fact, we search it first!
                var nameIdx = stackFrame.LocalNames.IndexOf(name);
                if (nameIdx >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public Dictionary<string, object> DumpVariables()
        {
            var variables = new Dictionary<string, object>();
            for (int i = 0; i < LocalNames.Count; ++i)
            {
                variables.Add(LocalNames[i], Locals[i]);
            }
            return variables;
        }
    }
}
