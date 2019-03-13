namespace LanguageImplementation
{
    public enum ByteCodes
    {
        // Mnemonics = Opcode // (Operand length) Description
		STOP_CODE                  = 0x00, // (0) Indicates end-of-code to the compiler, not used by the interpreter. 
		POP_TOP                    = 0x01, // (0) Removes the top-of-stack (TOS) item.
		ROT_TWO                    = 0x02, // (0) Swaps the two top-most stack items.
		ROT_THREE                  = 0x03, // (0) Lifts second and third stack item one position up, moves top down to position three.
		DUP_TOP                    = 0x04, // (0) Duplicates the reference on top of the stack.
		ROT_FOUR                   = 0x05, // (0) Lifts second, third and forth stack item one position up, moves top down to position four.
		NOP                        = 0x09, // (0) Do nothing code. Used as a placeholder by the bytecode optimizer.
		UNARY_POSITIVE             = 0x0A, // (0) Implements TOS = +TOS.
		UNARY_NEGATIVE             = 0x0B, // (0) Implements TOS = -TOS.
		UNARY_NOT                  = 0x0C, // (0) Implements TOS = not TOS.
		UNARY_CONVERT              = 0x0D, // (0) Implements TOS = `TOS`.
		UNARY_INVERT               = 0x0F, // (0) Implements TOS = ~TOS.
		LIST_APPEND                = 0x12, // (0) Calls list.append(TOS1, TOS). Used to implement list comprehensions.
		BINARY_POWER               = 0x13, // (0) Implements TOS = TOS1 ** TOS. 
		BINARY_MULTIPLY            = 0x14, // (0) Implements TOS = TOS1 * TOS.
		BINARY_DIVIDE              = 0x15, // (0) Implements TOS = TOS1 / TOS when from __future__ import division is not in effect.
		BINARY_MODULO              = 0x16, // (0) Implements TOS = TOS1 % TOS.
		BINARY_ADD                 = 0x17, // (0) Implements TOS = TOS1 + TOS.
		BINARY_SUBTRACT            = 0x18, // (0) Implements TOS = TOS1 - TOS.
		BINARY_SUBSCR              = 0x19, // (0) Implements TOS = TOS1[TOS].
		BINARY_FLOOR_DIVIDE        = 0x1A, // (0) Implements TOS = TOS1 // TOS.
		BINARY_TRUE_DIVIDE         = 0x1B, // (0) Implements TOS = TOS1 / TOS when from __future__ import division is in effect.
		INPLACE_FLOOR_DIVIDE       = 0x1C, // (0) Implements in-place TOS = TOS1 // TOS.
		INPLACE_TRUE_DIVIDE        = 0x1D, // (0) Implements in-place TOS = TOS1 / TOS when from __future__ import division is in effect.
		SLICE                      = 0x1E, // (0) Implements TOS = TOS[:].
		SLICE_1                    = 0x1F, // (0) Implements TOS = TOS1[TOS:].
		SLICE_2                    = 0x20, // (0) Implements TOS = TOS1[:TOS].
		SLICE_3                    = 0x21, // (0) Implements TOS = TOS2[TOS1:TOS].
		STORE_SLICE                = 0x28, // (0) Implements TOS[:] = TOS1.
		STORE_SLICE_1              = 0x29, // (0) Implements TOS1[TOS:] = TOS2.
		STORE_SLICE_2              = 0x2A, // (0) Implements TOS1[:TOS] = TOS2.
		STORE_SLICE_3              = 0x2B, // (0) Implements TOS2[TOS1:TOS] = TOS3.
		DELETE_SLICE               = 0x32, // (0) Implements del TOS[:].
		DELETE_SLICE_1             = 0x33, // (0) Implements del TOS1[TOS:].
		DELETE_SLICE_2             = 0x34, // (0) Implements del TOS1[:TOS].
		DELETE_SLICE_3             = 0x35, // (0) Implements del TOS2[TOS1:TOS].
		INPLACE_ADD                = 0x37, // (0) Implements in-place TOS = TOS1 + TOS.
		INPLACE_SUBTRACT           = 0x38, // (0) Implements in-place TOS = TOS1 - TOS.
		INPLACE_MULTIPLY           = 0x39, // (0) Implements in-place TOS = TOS1 * TOS.
		INPLACE_DIVIDE             = 0x3A, // (0) Implements in-place TOS = TOS1 / TOS when from __future__ import division is not in effect.
		INPLACE_MODULO             = 0x3B, // (0) Implements in-place TOS = TOS1 % TOS.
		STORE_SUBSCR               = 0x3C, // (0) Implements TOS1[TOS] = TOS2.
		DELETE_SUBSCR              = 0x3D, // (0) Implements del TOS1[TOS]. 
		BINARY_LSHIFT              = 0x3E, // (0) Implements TOS = TOS1 << TOS. 
		BINARY_RSHIFT              = 0x3F, // (0) Implements TOS = TOS1 >> TOS.
		BINARY_AND                 = 0x40, // (0) Implements TOS = TOS1 & TOS.
		BINARY_XOR                 = 0x41, // (0) Implements TOS = TOS1 ^ TOS. 
		BINARY_OR                  = 0x42, // (0) Implements TOS = TOS1 | TOS.
		INPLACE_POWER              = 0x43, // (0) Implements in-place TOS = TOS1 ** TOS.
		GET_ITER                   = 0x44, // (0) Implements TOS = iter(TOS).
		PRINT_EXPR                 = 0x46, // (0) Implements the expression statement for the interactive mode. TOS is removed from the stack and printed. In non-interactive mode, an expression statement is terminated with POP_STACK.
		PRINT_ITEM                 = 0x47, // (0) Prints TOS to the file-like object bound to sys.stdout. There is one such instruction for each item in the print statement.
		PRINT_NEWLINE              = 0x48, // (0) Prints a new line on sys.stdout. This is generated as the last operation of a print statement, unless the statement ends with a comma.
		PRINT_ITEM_TO              = 0x49, // (0) Like PRINT_ITEM, but prints the item second from TOS to the file-like object at TOS. This is used by the extended print statement.
		PRINT_NEWLINE_TO           = 0x4A, // (0) Like PRINT_NEWLINE, but prints the new line on the file-like object on the TOS. This is used by the extended print statement.
		INPLACE_LSHIFT             = 0x4B, // (0) Implements in-place TOS = TOS1 << TOS.
		INPLACE_RSHIFT             = 0x4C, // (0) Implements in-place TOS = TOS1 >> TOS.
		INPLACE_AND                = 0x4D, // (0) Implements in-place TOS = TOS1 & TOS.
		INPLACE_XOR                = 0x4E, // (0) Implements in-place TOS = TOS1 ^ TOS.
		INPLACE_OR                 = 0x4F, // (0) Implements in-place TOS = TOS1 | TOS. 
		BREAK_LOOP                 = 0x50, // (0) Terminates a loop due to a break statement.
		WITH_CLEANUP               = 0x51, // (0) ???
		LOAD_LOCALS                = 0x52, // (0) Pushes a reference to the locals of the current scope on the stack. This is used in the code for a class definition: After the class body is evaluated, the locals are passed to the class definition.
		RETURN_VALUE               = 0x53, // (0) Returns with TOS to the caller of the function.
		IMPORT_STAR                = 0x54, // (0) Loads all symbols not starting with "_" directly from the module TOS to the local namespace. The module is popped after loading all names. This opcode implements from module import *.
		EXEC_STMT                  = 0x55, // (0) Implements exec TOS2,TOS1,TOS. The compiler fills missing optional parameters with None.
		YIELD_VALUE                = 0x56, // (0) Pops TOS and yields it from a generator.
		POP_BLOCK                  = 0x57, // (0) Removes one block from the block stack. Per frame, there is a stack of blocks, denoting nested loops, try statements, and such.
		END_FINALLY                = 0x58, // (0) Terminates a finally clause. The interpreter recalls whether the exception has to be re-raised, or whether the function returns, and continues with the outer-next block.
		BUILD_CLASS                = 0x59, // (0) Creates a new class object. TOS is the methods dictionary, TOS1 the tuple of the names of the base classes, and TOS2 the class name.
		STORE_NAME                 = 0x5A, // (2) Implements name = TOS. /namei/ is the index of name in the attribute co_names of the code object. The compiler tries to use STORE_LOCAL or STORE_GLOBAL if possible.
		DELETE_NAME                = 0x5B, // (2) Implements del name, where /namei/ is the index into co_names attribute of the code object.
		UNPACK_SEQUENCE            = 0x5C, // (2) Unpacks TOS into /count/ individual values, which are put onto the stack right-to-left.
		FOR_ITER                   = 0x5D, // (2) TOS is an iterator. Call its next() method. If this yields a new value, push it on the stack (leaving the iterator below it). If the iterator indicates it is exhausted TOS is popped, and the byte code counter is incremented by /delta/.
		STORE_ATTR                 = 0x5F, // (2) Implements TOS.name = TOS1, where /namei/ is the index of name in co_names.
		DELETE_ATTR                = 0x60, // (2) Implements del TOS.name, using /namei/ as index into co_names. 
		STORE_GLOBAL               = 0x61, // (2) Works as STORE_NAME(/namei/), but stores the name as a global.
		DELETE_GLOBAL              = 0x62, // (2) Works as DELETE_NAME(/namei/), but deletes a global name.
		DUP_TOPX                   = 0x63, // (None) Duplicate /count/ items, keeping them in the same order. Due to implementation limits, count should be between 1 and 5 inclusive.
		LOAD_CONST                 = 0x64, // (2) Pushes "co_consts[/consti/]" onto the stack.
		LOAD_NAME                  = 0x65, // (2) Pushes the value associated with "co_names[/namei/]" onto the stack.
		BUILD_TUPLE                = 0x66, // (2) Creates a tuple consuming /count/ items from the stack, and pushes the resulting tuple onto the stack.
		BUILD_LIST                 = 0x67, // (2) Works as BUILD_TUPLE(/count/), but creates a list.
		BUILD_MAP                  = 0x68, // (2) Pushes a new empty dictionary object onto the stack. The argument is ignored and set to /zero/ by the compiler.
		LOAD_ATTR                  = 0x69, // (2) Replaces TOS with getattr(TOS, co_names[/namei/]).
		COMPARE_OP                 = 0x6A, // (2) Performs a Boolean operation. The operation name can be found in cmp_op[/opname/].
		IMPORT_NAME                = 0x6B, // (2) Imports the module co_names[/namei/]. The module object is pushed onto the stack. The current namespace is not affected: for a proper import statement, a subsequent STORE_FAST instruction modifies the namespace.
		IMPORT_FROM                = 0x6C, // (2) Loads the attribute co_names[/namei/] from the module found in TOS. The resulting object is pushed onto the stack, to be subsequently stored by a STORE_FAST instruction. 
		JUMP_FORWARD               = 0x6E, // (2) Increments byte code counter by /delta/.
		JUMP_IF_FALSE              = 0x6F, // (2) If TOS is false, increment the byte code counter by /delta/. TOS is not changed.
		JUMP_IF_TRUE               = 0x70, // (2) If TOS is true, increment the byte code counter by /delta/. TOS is left on the stack.
		JUMP_ABSOLUTE              = 0x71, // (2) Set byte code counter to /target/.
		LOAD_GLOBAL                = 0x74, // (2) Loads the global named co_names[/namei/] onto the stack. 
		CONTINUE_LOOP              = 0x77, // (2) Continues a loop due to a continue statement. /target/ is the address to jump to (which should be a FOR_ITER instruction).
		SETUP_LOOP                 = 0x78, // (2) Pushes a block for a loop onto the block stack. The block spans from the current instruction with a size of /delta/ bytes.
		SETUP_EXCEPT               = 0x79, // (2) Pushes a try block from a try-except clause onto the block stack. /delta/ points to the first except block.
		SETUP_FINALLY              = 0x7A, // (2) Pushes a try block from a try-except clause onto the block stack. /delta/ points to the finally block.
		LOAD_FAST                  = 0x7C, // (2) Pushes a reference to the local co_varnames[/var_num/] onto the stack.
		STORE_FAST                 = 0x7D, // (2) Stores TOS into the local co_varnames[/var_num/].
		DELETE_FAST                = 0x7E, // (2) Deletes local co_varnames[/var_num/].
		RAISE_VARARGS              = 0x82, // (2) Raises an exception. /argc/ indicates the number of parameters to the raise statement, ranging from 0 to 3. The handler will find the traceback as TOS2, the parameter as TOS1, and the exception as TOS.
		CALL_FUNCTION              = 0x83, // (2) Calls a function. The low byte of /argc/ indicates the number of positional parameters, the high byte the number of keyword parameters. On the stack, the opcode finds the keyword parameters first. For each keyword argument, the value is on top of the key. Below the keyword parameters, the positional parameters are on the stack, with the right-most parameter on top. Below the parameters, the function object to call is on the stack. 
		MAKE_FUNCTION              = 0x84, // (2) Pushes a new function object on the stack. TOS is the code associated with the function. The function object is defined to have /argc/ default parameters, which are found below TOS.
		BUILD_SLICE                = 0x85, // (2) Pushes a slice object on the stack. /argc/ must be 2 or 3. If it is 2, slice(TOS1, TOS) is pushed; if it is 3, slice(TOS2, TOS1, TOS) is pushed. See the slice() built-in function for more information.
		MAKE_CLOSURE               = 0x86, // (2) Creates a new function object, sets its func_closure slot, and pushes it on the stack. TOS is the code associated with the function. If the code object has N free variables, the next N items on the stack are the cells for these variables. The function also has /argc/ default parameters, where are found before the cells.
		LOAD_CLOSURE               = 0x87, // (2) Pushes a reference to the cell contained in slot /i/ of the cell and free variable storage. The name of the variable is co_cellvars[i] if i is less than the length of co_cellvars. Otherwise it is co_freevars[i - len(co_cellvars)].
		LOAD_DEREF                 = 0x88, // (2) Loads the cell contained in slot /i/ of the cell and free variable storage. Pushes a reference to the object the cell contains on the stack.
		STORE_DEREF                = 0x89, // (2) Stores TOS into the cell contained in slot /i/ of the cell and free variable storage.
		CALL_FUNCTION_VAR          = 0x8C, // (2) Calls a function. /argc/ is interpreted as in CALL_FUNCTION. The top element on the stack contains the variable argument list, followed by keyword and positional arguments.
		CALL_FUNCTION_KW           = 0x8D, // (2) Calls a function. /argc/ is interpreted as in CALL_FUNCTION. The top element on the stack contains the keyword arguments dictionary, followed by explicit keyword and positional arguments.
		CALL_FUNCTION_VAR_KW       = 0x8E, // (2) Calls a function. /argc/ is interpreted as in CALL_FUNCTION. The top element on the stack contains the keyword arguments dictionary, followed by the variable-arguments tuple, followed by explicit keyword and positional arguments.
		EXTENDED_ARG               = 0x8F, // (2) Support for opargs more than 16 bits long.

        // TODO: Figure out the actual opcode byte for this.
        BUILD_CONST_KEY_MAP        = 0x9C, // (2) The version of BUILD_MAP specialized for constant keys. count values are consumed from the stack. The top element on the stack contains a tuple of keys.

        WAIT                       = 0xA0, // (0) Custom Cloaca green thread yield
    }

    // Temporary method of establishing comparison operators. It looks like we need a cmp_op table--probably due to overrides of comparisons--but
    // while testing with integers, we'll do whatever we want. :p
    public enum CompareOps
    {
        Eq = 0,
        Ne = 1,
        Lt = 3,
        Gt = 4,
        Lte = 5,
        Gte = 6,
        In = 7,
        Is = 8,         // This constant is accurate to Python disassembly
        IsNot = 9,      // This constant is accurate to Python disassembly
        NotIn = 10,
        LtGt = 11
    }
}