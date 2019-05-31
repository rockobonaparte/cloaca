using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LanguageImplementation;

namespace CloacaInterpreter
{
    /// <summary>
    /// Helper for jump fixups in the byte code generator. This takes care of the offset
    /// calculations, calling out fixups instead of just having magic integer offsets that
    /// mingle in multiple places during code generation.
    /// 
    /// General usage:
    /// var fixup = new JumpOpcodeFixer(AddInstruction(ByteCodes.JUMP_FORWARD, -1));
    /// 
    /// (emit a pile of byte code)
    /// 
    /// fixup.Fixup(ActiveProgram.Code.Count)
    /// (emit the byte code that the JUMP_FORWARD would go to)
    /// 
    /// This is encapsulating some quirks. The constructor is getting the location of the
    /// opcode AFTER the one to patch. All the opcodes that cause jumps are consistent sizes
    /// we can figure which byte is the one we meant.
    /// </summary>
    public class JumpOpcodeFixer
    {
        private int fixupByteOffset;
        private CodeBuilder builder;
        public JumpOpcodeFixer(CodeBuilder builder, int codeByteIndexAfterInstruction)
        {
            this.builder = builder;
            fixupByteOffset = codeByteIndexAfterInstruction - 2;
        }

        public void Fixup(int jumpPoint)
        {
            builder.SetUShort(fixupByteOffset, jumpPoint - fixupByteOffset);
        }
    }
}
