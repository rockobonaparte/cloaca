using System;

namespace LanguageImplementation
{
    public class Dis
    {
        private static string disassembleLine(int? lineNumber, int bytecode_idx, string opcode_str, int? data, string hint)
        {
            string disassembly = lineNumber != null ? string.Format("{0,3}", lineNumber) : "   ";
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

            int cursor = startIdx;
            int lineCount = 0;
            while (cursor < code.Bytes.Length)
            {
                if(lineCount == count)
                {
                    return disassembly;
                }
                ++lineCount;

                switch ((ByteCodes)code[cursor])
                {
                    case ByteCodes.BINARY_ADD:
                        disassembly += disassembleLine(null, cursor, "BINARY_ADD", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_SUBTRACT:
                        disassembly += disassembleLine(null, cursor, "BINARY_SUB", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_MULTIPLY:
                        disassembly += disassembleLine(null, cursor, "BINARY_MUL", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BINARY_DIVIDE:
                        disassembly += disassembleLine(null, cursor, "BINARY_DIV", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.LOAD_CONST:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "LOAD_CONST", code.GetUShort(cursor), string.Format("({0})", codeObject.Constants[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_NAME:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "LOAD_NAME", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_FAST:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "LOAD_FAST", code.GetUShort(cursor), string.Format("({0})", codeObject.VarNames[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_GLOBAL:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "LOAD_GLOBAL", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.LOAD_ATTR:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "LOAD_ATTR", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_NAME:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "STORE_NAME", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_FAST:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "STORE_FAST", code.GetUShort(cursor), string.Format("({0})", codeObject.VarNames[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_ATTR:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "STORE_ATTR", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.STORE_GLOBAL:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "STORE_GLOBAL", code.GetUShort(cursor), string.Format("({0})", codeObject.Names[code.GetUShort(cursor)]));
                        cursor += 2;
                        break;
                    case ByteCodes.COMPARE_OP:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "COMPARE_OP", code.GetUShort(cursor), string.Format("({0})", code[cursor]));
                        cursor += 2;
                        break;
                    case ByteCodes.JUMP_IF_FALSE:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "JUMP_IF_FALSE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.JUMP_IF_TRUE:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor-1, "JUMP_IF_TRUE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.POP_JUMP_IF_FALSE:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor - 1, "POP_JUMP_IF_FALSE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.POP_JUMP_IF_TRUE:
                        cursor += 1;
                        disassembly += disassembleLine(null, cursor - 1, "POP_JUMP_IF_TRUE", code.GetUShort(cursor), null);
                        cursor += 2;
                        break;
                    case ByteCodes.SETUP_LOOP:
                        {
                            cursor += 1;
                            var offset = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "SETUP_LOOP", offset, string.Format("(to {0})", cursor + 1 + offset));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.POP_BLOCK:
                        // TODO: Block targets should have a >> cursor next to them.
                        disassembly += disassembleLine(null, cursor, "POP_BLOCK", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.POP_TOP:
                        disassembly += disassembleLine(null, cursor, "POP_TOP", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.JUMP_ABSOLUTE:
                        {
                            cursor += 1;
                            var target = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "JUMP_ABSOLUTE", target, string.Format("(to {0})", target));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.JUMP_FORWARD:
                        {
                            cursor += 1;
                            var offset = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "JUMP_FORWARD", offset, string.Format("(to {0})", cursor + 1 + offset));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.WAIT:
                        // TODO: Block targets should have a >> cursor next to them.
                        disassembly += disassembleLine(null, cursor, "WAIT", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.MAKE_FUNCTION:
                        {
                            cursor += 1;
                            var opcode = code.GetUShort(cursor-1);
                            disassembly += disassembleLine(null, cursor, "MAKE_FUNCTION", opcode, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.CALL_FUNCTION:
                        {
                            cursor += 1;
                            var argCount = code.GetUShort(cursor-1);
                            disassembly += disassembleLine(null, cursor, "CALL_FUNCTION", argCount, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.RETURN_VALUE:
                        disassembly += disassembleLine(null, cursor, "RETURN_VALUE", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BUILD_TUPLE:
                        {
                            cursor += 1;
                            var tuple_size = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "BUILD_TUPLE", tuple_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BUILD_MAP:
                        {
                            cursor += 1;
                            var dict_size = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "BUILD_MAP", dict_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BUILD_CONST_KEY_MAP:
                        {
                            cursor += 1;
                            var dict_size = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "BUILD_CONST_KEY_MAP", dict_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BUILD_LIST:
                        {
                            cursor += 1;
                            var list_size = code.GetUShort(cursor-1);
                            disassembly += disassembleLine(null, cursor, "BUILD_LIST", list_size, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.BINARY_SUBSCR:
                        disassembly += disassembleLine(null, cursor, "BINARY_SUBSCR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.STORE_SUBSCR:
                        disassembly += disassembleLine(null, cursor, "STORE_SUBSCR", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.BUILD_CLASS:
                        disassembly += disassembleLine(null, cursor, "LOAD_BUILD_CLASS", null, null);
                        cursor += 1;
                        break;
                    case ByteCodes.SETUP_EXCEPT:
                        {
                            cursor += 1;
                            var offset = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "SETUP_EXCEPT", offset, string.Format("(to {0})", cursor + 1 + offset));
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.RAISE_VARARGS:
                        {
                            cursor += 1;
                            var opcode = code.GetUShort(cursor);
                            disassembly += disassembleLine(null, cursor-1, "RAISE_VARARGS", opcode, null);
                            cursor += 2;
                        }
                        break;
                    case ByteCodes.END_FINALLY:
                        {
                            disassembly += disassembleLine(null, cursor, "END_FINALLY", null, null);
                            cursor += 1;
                            break;
                        }
                    default:
                        throw new Exception("Unexpected opcode to disassemble: " + code[cursor]);
                }
            }
            return disassembly;
        }
    }
}
