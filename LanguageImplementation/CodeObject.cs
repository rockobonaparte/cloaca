using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace LanguageImplementation
{
    /// <summary>
    /// Represents callable code outside of the scope of the interpreter.
    /// </summary>
    public class WrappedCodeObject : IPyCallable
    {
        public MethodInfo MethodInfo
        {
            get;
            private set;
        }

        private object instance;

        public int ArgCount
        {
            get
            {
                return MethodInfo.GetParameters().Length;
            }
        }

        public List<string> ArgVarNames
        {
            get
            {
                List<string> variableNames = new List<string>();
                foreach(var paramInfo in MethodInfo.GetParameters())
                {
                    variableNames.Add(paramInfo.Name);
                }
                return variableNames;
            }
        }

        public string Name
        {
            get; protected set;
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            var injector = new Injector(interpreter, context);
            var final_args = injector.Inject(MethodInfo, args);

            // Little convenience here. We'll convert a non-task Task<object> type to a task.
            if (MethodInfo.ReturnType.IsGenericType && MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // The little bit of bizarre testing above gets us this far but we still need a Task<object>.
                // The cast will fail if they're returning something else. At least now it'll error in a way
                // more related to the real problem. We had a method return Task<PyClass> and it would blow
                // up from this section when it was written more naively.
                return (Task<object>)MethodInfo.Invoke(instance, final_args);
            }
            else
            {
                return Task.FromResult(MethodInfo.Invoke(instance, final_args));
            }
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
            Name = nameInsideInterpreter;
            instance = null;
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo methodInfo) : this(null, nameInsideInterpreter, methodInfo)
        {
        }

        public WrappedCodeObject(FrameContext context, MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            instance = null;
        }

        public WrappedCodeObject(MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            instance = null;
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo methodInfo, object instance)
        {
            this.MethodInfo = methodInfo;
            Name = nameInsideInterpreter;
            this.instance = instance;
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo methodInfo, object instance) : this(null, nameInsideInterpreter, methodInfo, instance)
        {
        }

        public WrappedCodeObject(FrameContext context, MethodInfo methodInfo, object instance)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            this.instance = instance;
        }

        public WrappedCodeObject(MethodInfo methodInfo, object instance)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            this.instance = instance;
        }
    }

    public class CodeObject : IPyCallable
    {
        //'co_argcount', 'co_cellvars', 'co_code', 'co_consts', 'co_filename',
        // 'co_firstlineno', 'co_flags', 'co_freevars', 'co_lnotab', 'co_name',
        // 'co_names', 'co_nlocals', 'co_stacksize', 'co_varnames'

        // According to inspect:
        //   co_argcount	    number of arguments (not including keyword only arguments, * or ** args)
        //   co_code	        string of raw compiled bytecode
        //   co_cellvars	    tuple of names of cell variables (referenced by containing scopes)
        //   co_consts	        tuple of constants used in the bytecode
        //   co_filename	    name of file in which this code object was created
        //   co_firstlineno	    number of first line in Python source code
        //   co_flags	        bitmap of CO_* flags, read more here
        //   co_lnotab	        encoded mapping of line numbers to bytecode indices
        //   co_freevars	    tuple of names of free variables (referenced via a function’s closure)
        //   co_kwonlyargcount	number of keyword only arguments (not including ** arg)
        //   co_name	        name with which this code object was defined
        //   co_names	        tuple of names of local variables
        //   co_nlocals	        number of local variables
        //   co_stacksize	    virtual machine stack space required
        //   co_varnames	    tuple of names of arguments and local variables
        //
        public int ArgCount;            // co_argcount
        public List<string> VarNames;   // co_varnames (not really; this should be a tuple used by LOAD_FAST/STORE_FAST
        public List<string> ArgVarNames;// This will collapse into co_varnames when we start using LOAD_FAST/STORE_FAST
        public List<string> Names;      // co_names. Referenced by LOAD/STORE_NAME, LOAD/STORE_ATTR and globals.

        public CodeByteArray Code;      // co_code
        public string Filename;         // co_filename
        public string Name;             // co_name

        public List<object> Constants;     // co_constants

        public byte[] lnotab;
        public int firstlineno;

        public CodeObject(byte[] code)
        {
            ArgCount = 0;
            Filename = "<string>";
            Name = "<module>";
            Code = new CodeByteArray(code);
            VarNames = new List<string>();
            Constants = new List<object>();
            ArgVarNames = new List<string>();
            Names = new List<string>();
        }


        /// <summary>
        /// Gets line of code corresponding to the given position in byte code
        /// </summary>
        /// <param name="bytePosition">The position in the byte code to translate to a source file line.</param>
        /// <returns>The source line corresponding to the given byte code position, or -1 if no
        /// mapping is available.</returns>
        public int GetCodeLine(int bytePosition)
        {
            if(lnotab.Length == 0)
            {
                return -1;
            }
            int currentLine = firstlineno;
            int byteCount = 0;
            for(int i = 0; i < lnotab.Length - 1; i += 2)
            {
                if(byteCount >= bytePosition)
                {
                    return currentLine;
                }
                byteCount += lnotab[i];
                currentLine += lnotab[i + 1];
            }

            // Fallback, last byte code of program. Return current line now.
            return currentLine;
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            return interpreter.CallInto(context, this, args);
        }
    }

    /// <summary>
    /// Used by the ANTLR visitor to build up the code. Instead of having an array, it'll use a CodeBuilder,
    /// which is based off of a System.Collections.List<byte>.
    /// </summary>
    public class CodeObjectBuilder : CodeObject
    {
        public new CodeBuilder Code;
        private int firstLine;
        private int trackingLine;
        private List<byte> lnotab_builder;

        // If true, will raise runtime exception for AddInstruction calls used that don't take context as argument.
        // Sanity check for normal code building.
        public bool AssertContextGiven;

        public CodeObjectBuilder() : base(null)
        {
            Code = new CodeBuilder();
            lnotab_builder = new List<byte>();
            firstLine = -1;
            trackingLine = -1;
            AssertContextGiven = true;
        }

        private void advanceLineCounts(byte bytesAdded, int line_no)
        {
            if (firstLine == -1)
            {
                firstLine = line_no;
                trackingLine = firstLine;
                lnotab_builder.Add(bytesAdded);
            }
            else if(line_no < trackingLine)
            {
                // Somehow we went backwards on lines. This can happen when doing stuff
                // like building loops. While I thought this was alarming, Python seems to
                // just assume these advance lines are part of the current line. So I'll do
                // the same until it becomes a problem.

                // Same logic as else clause, for now. We're coding it special
                // and separate so we can insert a blurb.
                // throw new Exception("Told to advanced backwards in source line count");
                lnotab_builder[lnotab_builder.Count - 1] += bytesAdded;
            }
            else if (trackingLine != line_no)
            {
                int lineDiff = line_no - trackingLine;

                while (lineDiff > 255)
                {
                    // Check out this jackass that has over 255 lines of nothing between
                    // lines of code.
                    // (that must be a hell of a comment block... I am going to assume)
                    lnotab_builder.Add(255);
                    lnotab_builder.Add(0);
                    lineDiff -= 255;
                }
                lnotab_builder.Add((byte)lineDiff);
                lnotab_builder.Add(bytesAdded);
                trackingLine = line_no;
            }
            else
            {
                lnotab_builder[lnotab_builder.Count - 1] += bytesAdded;
            }
        }

        private void advanceLineCounts(byte bytesAdded, ParserRuleContext context)
        {
            int line_no = context.Start.Line;
            advanceLineCounts(bytesAdded, line_no);
        }

        /// <summary>
        /// Add an instruction to the end of the active program. It is assumed to not
        /// correspond to actual user code and won't have line number matching.
        /// </summary>
        /// <param name="opcode">The instruction opcode</param>
        /// <param name="data">Opcode data.</param>
        /// <returns>The index of the NEXT instruction in the program.</returns>
        public int AddInstruction(ByteCodes opcode, int data)
        {
            if(AssertContextGiven)
            {
                throw new Exception("AddInstruction called without context");
            }
            Code.AddByte((byte)opcode);
            Code.AddUShort(data);
            return Code.Count;
        }

        /// <summary>
        /// Add an instruction to the end of the active program.
        /// </summary>
        /// <param name="opcode">The instruction opcode</param>
        /// <param name="data">Opcode data.</param>
        /// <param name="context">Parser context from which to infer line count information
        /// from source</param>
        /// <returns>The index of the NEXT instruction in the program.</returns>
        public int AddInstruction(ByteCodes opcode, int data, ParserRuleContext context)
        {
            Code.AddByte((byte)opcode);
            Code.AddUShort(data);

            advanceLineCounts(3, context);
            return Code.Count;
        }

        /// <summary>
        /// Add a single-byte instruction to the end of the active program. It is assumed
        /// to not correspond to actual user code and won't have line number matching.
        /// </summary>
        /// <param name="opcode">The instruction opcode</param>
        /// <returns>The index of the NEXT instruction in the program.</returns>
        public int AddInstruction(ByteCodes opcode)
        {
            if (AssertContextGiven)
            {
                throw new Exception("AddInstruction called without context");
            }
            Code.AddByte((byte)opcode);
            return Code.Count;
        }

        /// <summary>
        /// Add a single-byte instruction to the end of the active program. 
        /// </summary>
        /// <param name="opcode">The instruction opcode</param>
        /// <param name="context">Parser context from which to infer line count information
        /// from source</param>
        /// <returns>The index of the NEXT instruction in the program.</returns>
        public int AddInstruction(ByteCodes opcode, ParserRuleContext context)
        {
            Code.AddByte((byte)opcode);
            advanceLineCounts(1, context);
            return Code.Count;
        }

        // Converts into a regular code object using byte arrays.
        // Functions in constants will also get converted to regular CodeObjects            
        public CodeObject Build()
        {
            var newCodeObj = new CodeObject(Code.ToArray());
            newCodeObj.ArgCount = ArgCount;
            newCodeObj.Filename = Filename;
            newCodeObj.Name = Name;
            newCodeObj.VarNames = VarNames;
            newCodeObj.Constants = Constants;
            newCodeObj.ArgVarNames = ArgVarNames;
            newCodeObj.Names = Names;

            for (int i = 0; i < newCodeObj.Constants.Count; ++i)
            {
                if(newCodeObj.Constants[i] is CodeObjectBuilder)
                {
                    var asBuilder = newCodeObj.Constants[i] as CodeObjectBuilder;
                    newCodeObj.Constants[i] = asBuilder.Build();
                }
            }

            newCodeObj.firstlineno = firstLine;
            newCodeObj.lnotab = lnotab_builder.ToArray();

            return newCodeObj;
        }
    }
}
