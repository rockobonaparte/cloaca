Cloaca TODO
===========

Need to implement LOAD_NAME/STORE_NAME

Good stuff about LOAD_NAME and reference ordering
https://stackoverflow.com/questions/20246523/how-references-to-variables-are-resolved-in-python_

DeclareClassMember unit test shows the Names property in the interpreter having ["self", "self"] while
ActiveProgram.Names is ["a"] (which is what I expected). Figure out what's up with that.

* Strings (part 1)
  * [DONE] Parse strings
  * [DONE] Single quote
  * [DONE] Double quote
  * Concatenate strings
  * [DONE] Strings in functions
    * String as function argument
* [DONE] Switch to a raw byte stream (one byte for opcode, two bytes for operand)
  * Verify .pyc syntax using dis.dis and hex editor
* NoneType
  * [DONE] Base value
  * [DONE] is None
  * [DONE] is not None
  * Can't assign to None
* Compound data structures
  * Lists
    * [DONE] Parse list
    * [DONE] Subscript read
    * [DONE] Subscript write
    * Subscript with variable
  * Tuples
    * [DONE] Parse tuple
    * Single element (trailing comma) tuple
  * Sets
  * Dictionary
    * [DONE] Parse dictionary
    * [DONE] Set value
    * [DONE] get value
    * [DONE] Implement default using BUILD_CONST_KEY_MAP (even if not used)
    * [DONE] Implement default using BUILD_MAP
      * You have to use this when the names are dynamically generated
    * Subscript with variable
* [DONE] Booleans
  * [DONE] Basic True/false
  * Is true/false
  * Is Not true/false
* Python number type
  * Integer
     * Hex
     * Binary
  * Floating point
     * [DONE] Decimal point
     * Exponential
  * Imaginary
  * Longs
* Classes
  * [DONE] Constructor
  * [DONE] Access members
  * [DONE] Call functions
  * Inheritance
  * Start to wrap data types as classes
  * Classes as objects (I think that's how it's done, right?). Need it for except classes (Exception as e, CompareException operator)
* [DONE] Scheduler controlling interpreter to switch programs when one waits
* Integration with parent runtime
  * [DONE] Call embedded C# function from script
  * Call Python function through interpreter
  * Manage async call with waiting in interpreter
* Serialization
  * Any test that can wait should automatically be run 
    * All the way through as usual
    * Then with state saved and reloaded each time the interpreter stops
* Flush out other major operators
  * += etc
  * Logic operators
* Imports
  * clr library for .NET stuff
  * Import Unity stuff?
* Exceptions
  * AssertionError
  * try-catch-finally (else?)
  * Getting accurate stack trace (formatting doesn't yet have to be consistent with Python)
  * assert function
  * raise from (exception chaining)
  * Improve exception creation process (need class to construct self pointer. Can I be more direct?)
     * It has something to do with the two-part __new__ and __init__ process. I am not currently handling this
       in the most proper manner but rather kind of encapsulating the self pointer (has-a instead of is-a)
* See if you can use that REPL helper module directly.


Part 2: First, harden the code, but keep some of this in mind while doing that.


* Strings (part 2)
  * % operator
  * format command
  * unicode
  * literals
  * f-strings
  * docstrings (long strings)
  * Unit test that spins through all the major variations
* Classes
  * metaclasses
  * Multiple inheritance

* Functions
  * Implement co_flags

https://docs.python.org/3/library/inspect.html
Notes on co_flags
inspect.CO_OPTIMIZED
The code object is optimized, using fast locals.

inspect.CO_NEWLOCALS
If set, a new dict will be created for the frame’s f_locals when the code object is executed.

inspect.CO_VARARGS
The code object has a variable positional parameter (*args-like).

inspect.CO_VARKEYWORDS
The code object has a variable keyword parameter (**kwargs-like).

inspect.CO_NESTED
The flag is set when the code object is a nested function.

inspect.CO_GENERATOR
The flag is set when the code object is a generator function, i.e. a generator object is returned when the code object is executed.

inspect.CO_NOFREE
The flag is set if there are no free or cell variables.

inspect.CO_COROUTINE
The flag is set when the code object is a coroutine function. When the code object is executed it returns a coroutine object. See PEP 492 for more details.

New in version 3.5.

inspect.CO_ITERABLE_COROUTINE
The flag is used to transform generators into generator-based coroutines. Generator objects with this flag can be used in await expression, and can yield from coroutine objects. See PEP 492 for more details.

New in version 3.5.

inspect.CO_ASYNC_GENERATOR
The flag is set when the code object is an asynchronous generator function. When the code object is executed it returns an asynchronous generator object. See PEP 525 for more details._


At this point you should start trying to embed it and suffer it.



Tech debt:
* Implement BYTES_LITERAL
* Implement full atom rule
  * Requires yield
* Class and objects -- particularly stuff with __new__ and __init__ -- are a mess.
  Look at how CPython is managing them and try to reconcile
* Reimplement WAIT--probably using async-await.  
* Reconcile CodeObject and WrappedCodeObject
  * Create a default __init__ once (and only once) to use in the class builder instead of stubbing a default constructor

Useful bits:
Dump a code object that comes up in a disassembly
>>> import ctypes
>>> c = ctypes.cast(0x10cabda50, ctypes.py_object).value