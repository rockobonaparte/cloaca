using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Antlr4.Runtime;
using LanguageImplementation.DataTypes;

namespace LanguageImplementation
{
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
        //   co_flags	        bitmap of CO_* flags
        //   co_lnotab	        encoded mapping of line numbers to bytecode indices
        //   co_freevars	    tuple of names of free variables (referenced via a function’s closure)
        //   co_kwonlyargcount	number of keyword only arguments (not including ** arg)
        //   co_name	        name with which this code object was defined
        //   co_names	        tuple of names of local variables
        //   co_nlocals	        number of local variables
        //   co_stacksize	    virtual machine stack space required
        //   co_varnames	    tuple of names of arguments and local variables
        //
        // ArgCount here maps to co_argcount. This tracks the number of positional arguments, which will include defaults
        // that don't follow a variable/keyword argument(s).
        // >>> def derp(first, second, third=3, fourth=4):
        // ...   pass
        // ...
        // >>> derp.__code__.co_argcount
        // 4
        // >>> def derp(first, second, *args, third=3, fourth=4):
        // ...   pass
        // ...
        // >>> derp.__code__.co_argcount
        // 2
        public int ArgCount;

        public List<string> VarNames;   // co_varnames (not really; this should be a tuple used by LOAD_FAST/STORE_FAST
        public List<string> ArgVarNames;// This will collapse into co_varnames when we start using LOAD_FAST/STORE_FAST
        public List<string> Names;      // co_names. Referenced by LOAD/STORE_NAME, LOAD/STORE_ATTR and globals.

        public CodeByteArray Code;      // co_code
        public string Filename;         // co_filename
        public string Name;             // co_name

        public List<object> Constants;     // co_constants

        public byte[] lnotab;
        public int firstlineno;
        public int Flags;
        public List<object> Defaults;

        // co_flag settings
        // The following flag bits are defined for co_flags: bit 0x04 is set if the function uses the *arguments syntax to
        // accept an arbitrary number of positional arguments; bit 0x08 is set if the function uses the **keywords syntax
        // to accept arbitrary keyword arguments; bit 0x20 is set if the function is a generator
        //
        // Future feature declarations(from __future__ import division) also use bits in co_flags to indicate whether a
        // code object was compiled with a particular feature enabled: bit 0x2000 is set if the function was compiled with
        // future division enabled; bits 0x10 and 0x1000 were used in earlier versions of Python.
        public const int CO_FLAGS_VARGS = 0x04;
        public const int CO_FLAGS_KWARGS = 0x08;
        public const int CO_FLAGS_GENERATOR = 0x20;

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
            Flags = 0;
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
            newCodeObj.Flags = Flags;
            newCodeObj.Defaults = Defaults != null ? Defaults : new List<object>();

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
