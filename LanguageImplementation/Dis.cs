using System;

namespace LanguageImplementation
{
    public class Dis
    {
        private static string disassembleLine(int lastLineNumber, int currentLineNumber, int bytecode_idx, string opcode_str, int? data, string hint)
        {
            string disassembly = currentLineNumber != lastLineNumber ? string.Format("\n{0,3}", currentLineNumber) : "   ";
            disassembly += string.Format(" {0,11} ", bytecode_idx);
            disassembly += string.Format(" {0,-15}", opcode_str);
            disassembly += data != null ? string.Format(" {0,9}", data) : "          ";
            disassembly += hint != null ? " " + hint : "";
            disassembly += "\n";
            return disassembly;
        }

        public static string dis(CodeObject codeObject, int startIdx=0, int count=-1)
        {
            // General format of output:
            //   3           8 LOAD_CONST               2 (1)
            //              10 STORE_FAST               0 (x)
            //              12 JUMP_FORWARD            18 (to 32)
            string disassembly = "";
            var code = codeObject.Code;

            int lastLineNumber = 0;
            int currentLineNumber = codeObject.firstlineno;
            int bytesIntoLine = 0;

            int lnotab_i = 0;

            int cursor = startIdx;
            int lineCount = 0;

            while (cursor < code.Bytes.Length)
            {
                if(lineCount == count)
                {
                    return disassembly;
                }
                ++lineCount;

                var cursorBefore = cursor;
                switch ((ByteCodes)code[cursor])
                {
                    case ByteCodes.UNARY_NOT:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "UNARY_NOT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_ADD:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_ADD", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_AND:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_AND", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_OR:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_OR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_XOR:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_XOR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_SUBTRACT:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_SUB", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_MULTIPLY:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_MUL", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_DIVIDE:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_DIV", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_TRUE_DIVIDE:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_TRUE_DIV", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_FLOOR_DIVIDE:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_FLOOR_DIVIDE", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_MODULO:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_MODULO", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_POWER:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_POWER", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_RSHIFT:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_RSHIFT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_LSHIFT:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_LSHIFT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_ADD:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_ADD", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_SUBTRACT:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_SUBTRACT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_MULTIPLY:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_MULTIPLY", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_TRUE_DIVIDE:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_TRUE_DIVIDE", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_MODULO:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_MODULO", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_FLOOR_DIVIDE:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_FLOOR_DIVIDE", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_POWER:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_POWER", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_AND:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_AND", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_OR:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_OR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_XOR:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_XOR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_RSHIFT:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_RSHIFT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.INPLACE_LSHIFT:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "INPLACE_LSHIFT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.LOAD_CONST:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "LOAD_CONST", code.GetUShort(cursor), string.Format("({0})", codeObject.Constants[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_NAME:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "LOAD_NAME", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_FAST:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "LOAD_FAST", code.GetUShort(cursor), string.Format("({0})", codeObject.VarNames[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_GLOBAL:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "LOAD_GLOBAL", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_ATTR:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "LOAD_ATTR", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_NAME:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "STORE_NAME", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_FAST:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "STORE_FAST", code.GetUShort(cursor), string.Format("({0})", codeObject.VarNames[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_ATTR:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "STORE_ATTR", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_GLOBAL:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "STORE_GLOBAL", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.COMPARE_OP:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "COMPARE_OP", code.GetUShort(cursor), string.Format("({0})", code[cursor]));
                        cursor += 2;
                        break;
                    case ByteCodes.JUMP_IF_FALSE:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "JUMP_IF_FALSE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.JUMP_IF_TRUE:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "JUMP_IF_TRUE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.POP_JUMP_IF_FALSE:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "POP_JUMP_IF_FALSE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.POP_JUMP_IF_TRUE:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "POP_JUMP_IF_TRUE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.SETUP_LOOP:
                        {
                            cursor += 1;
                            var offset = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "SETUP_LOOP", offset, string.Format("(to {0})", cursor + 2 + offset));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.GET_ITER:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "GET_ITER", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.FOR_ITER:
                        cursor += 1;
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "FOR_ITER", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.POP_BLOCK:
                        // TODO: Block targets should have a >> cursor next to them.
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "POP_BLOCK", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.POP_TOP:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "POP_TOP", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.DUP_TOP:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "DUP_TOP", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.JUMP_ABSOLUTE:
                        {
                            cursor += 1;
                            var target = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "JUMP_ABSOLUTE", target, string.Format("(to {0})", target));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.JUMP_FORWARD:
                        {
                            cursor += 1;
                            var offset = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "JUMP_FORWARD", offset, string.Format("(to {0})", cursor + offset + 2));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BREAK_LOOP:
                        {
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BREAK_LOOP", null, null);
                            cursor += 1;
                            break;
                        }
                    case ByteCodes.WAIT:
                        // TODO: Block targets should have a >> cursor next to them.
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "WAIT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.MAKE_FUNCTION:
                        {
                            cursor += 1;
                            var opcode = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "MAKE_FUNCTION", opcode, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.CALL_FUNCTION:
                        {
                            cursor += 1;
                            var argCount = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "CALL_FUNCTION", argCount, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.RETURN_VALUE:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "RETURN_VALUE", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BUILD_TUPLE:
                        {
                            cursor += 1;
                            var tuple_size = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "BUILD_TUPLE", tuple_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BUILD_MAP:
                        {
                            cursor += 1;
                            var dict_size = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "BUILD_MAP", dict_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BUILD_CONST_KEY_MAP:
                        {
                            cursor += 1;
                            var dict_size = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "BUILD_CONST_KEY_MAP", dict_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BUILD_LIST:
                        {
                            cursor += 1;
                            var list_size = code.GetUShort(cursor-1);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BUILD_LIST", list_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BINARY_SUBSCR:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "BINARY_SUBSCR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.STORE_SUBSCR:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "STORE_SUBSCR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BUILD_CLASS:
                        disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "LOAD_BUILD_CLASS", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.SETUP_EXCEPT:
                        {
                            cursor += 1;
                            var offset = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "SETUP_EXCEPT", offset, string.Format("(to {0})", cursor + 2 + offset));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.SETUP_FINALLY:
                        {
                            cursor += 1;
                            var offset = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "SETUP_FINALLY", offset, string.Format("(to {0})", cursor + 2 + offset));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.RAISE_VARARGS:
                        {
                            cursor += 1;
                            var opcode = code.GetUShort(cursor);
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor-1, "RAISE_VARARGS", opcode, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.END_FINALLY:
                        {
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor, "END_FINALLY", null, null);
                            cursor += 1;
                            break;
                        }
                    case ByteCodes.IMPORT_NAME:
                        {
                            cursor += 1;
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "IMPORT_NAME", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                            cursor += 2;
                            break;
                        }
                    case ByteCodes.IMPORT_FROM:
                        {
                            cursor += 1;
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "IMPORT_FROM", code.GetUShort(cursor), string.Format("({0})", codeObject.Constants[code.GetUShort(cursor)]));
                            cursor += 2;
                            break;
                        }
                    case ByteCodes.IMPORT_STAR:
                        {
                            cursor += 1;
                            disassembly += disassembleLine(lastLineNumber, currentLineNumber, cursor - 1, "IMPORT_STAR", null, null);
                            break;
                        }
                    default:
                        throw new Exception("Unexpected opcode to disassemble: " + code[cursor] + " (0x" + Convert.ToString(code[cursor], 16) + ")");
                }
                lastLineNumber = currentLineNumber;
                bytesIntoLine += cursor - cursorBefore;
                if(lnotab_i < codeObject.lnotab.Length - 1 && codeObject.lnotab[lnotab_i] <= bytesIntoLine)
                {
                    lnotab_i += 1;
                    currentLineNumber += codeObject.lnotab[lnotab_i];
                    lnotab_i += 1;
                    bytesIntoLine = 0;

                    // Fast-forward through huge whitespace blocks
                    while (lnotab_i < codeObject.lnotab.Length - 1 && codeObject.lnotab[lnotab_i] == 0)
                    {
                        lnotab_i += 1;
                        currentLineNumber += codeObject.lnotab[lnotab_i];
                        lnotab_i += 1;
                    }
                }
            }
            return disassembly;
        }
    }
}
