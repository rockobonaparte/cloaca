using System;
using System.Collections.Generic;
using System.Reflection;

namespace LanguageImplementation
{
    /// <summary>
    /// Represents callable code outside of the scope of the interpreter.
    /// </summary>
    public class WrappedCodeObject
    {
        public MethodInfo MethodInfo
        {
            get;
            private set;
        }

        private object instance;
        private FrameContext context;

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

        public IEnumerable<SchedulingInfo> Call(object[] args)
        {
            object[] finalArgs;
         
            // Auto-curry the FrameContext if we were given one (non-null) when this WrappedCodeObject was created.
            // We have to do this so we don't publish a function to the interpreter that is asking for this zany 
            // FrameContext object thing.
            if(context != null)
            {
                finalArgs = new object[args.Length + 1];
                finalArgs[0] = context;
                Array.Copy(args, 0, finalArgs, 1, args.Length);
            }
            else
            {
                finalArgs = args;
            }

            if(MethodInfo.ReturnType == typeof(IEnumerable<SchedulingInfo>))
            {
                foreach(var continuation in MethodInfo.Invoke(instance, finalArgs) as IEnumerable<SchedulingInfo>)
                {
                    yield return continuation;
                }
            }
            else
            {
                yield return new ReturnValue(MethodInfo.Invoke(instance, finalArgs));
            }
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
            Name = nameInsideInterpreter;
            instance = null;
            this.context = context;
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo methodInfo) : this(null, nameInsideInterpreter, methodInfo)
        {
        }

        public WrappedCodeObject(FrameContext context, MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            instance = null;
            this.context = context;
        }

        public WrappedCodeObject(MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            instance = null;
            this.context = null;
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo methodInfo, object instance)
        {
            this.MethodInfo = methodInfo;
            Name = nameInsideInterpreter;
            this.instance = instance;
            this.context = context;
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo methodInfo, object instance) : this(null, nameInsideInterpreter, methodInfo, instance)
        {
        }

        public WrappedCodeObject(FrameContext context, MethodInfo methodInfo, object instance)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            this.instance = instance;
            this.context = context;
        }

        public WrappedCodeObject(MethodInfo methodInfo, object instance)
        {
            this.MethodInfo = methodInfo;
            Name = methodInfo.Name;
            this.instance = instance;
            this.context = null;
        }
    }

    public class CodeObject
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
    }

    /// <summary>
    /// Used by the ANTLR visitor to build up the code. Instead of having an array, it'll use a CodeBuilder,
    /// which is based off of a System.Collections.List<byte>.
    /// </summary>
    public class CodeObjectBuilder : CodeObject
    {
        public new CodeBuilder Code;

        public CodeObjectBuilder() : base(null)
        {
            Code = new CodeBuilder();
        }

        /// <summary>
        /// Add an instruction to the end of the active program.
        /// </summary>
        /// <param name="opcode">The instruction opcode</param>
        /// <param name="data">Opcode data.</param>
        /// <returns>The index of the NEXT instruction in the program.</returns>
        public int AddInstruction(ByteCodes opcode, int data)
        {
            Code.AddByte((byte)opcode);
            Code.AddUShort(data);
            return Code.Count;
        }

        /// <summary>
        /// Add an instruction to the end of the active program.
        /// </summary>
        /// <param name="opcode">The instruction opcode</param>
        /// <returns>The index of the NEXT instruction in the program.</returns>
        public int AddInstruction(ByteCodes opcode)
        {
            Code.AddByte((byte)opcode);
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

            return newCodeObj;
        }
    }
}
