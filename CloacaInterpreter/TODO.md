Cloaca TODO
===========


There is a bit of a circular dependency chain between the scheduler and the interpreter. Currently, we start the scheduler
without a reference to the interpreter and then fill it in afterwards.

Next, focusing on turning basic types into PyObjects. The interpreter loop and code generation need to
get rid of their schizophrenic ways ot managing these data types. Some known issues:
1. Array subscripts are inconsistent.
2. Some attempts to consolidate lookups are messy.
3. Where consolidation wasn't done, it's even messier!
4. There amount of foreachs on continuations that happen now is obnoxious and it's time to investigate async-await
5. PyFloat has not been implemented anywhere as near as PyInteger
6. PyInteger itself isn't even finished!
7. What's the dunder supposed to be for <>?

Following that, serialization of tasks.

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
  * [DONE] Inheritance
  * Wrap data types as classes
     *  [In-progress] Integer
     *  [In-progress] Floating-Point
     *  Boolean
     *  String
     *  List
     *  Dict
  * [DONE] Classes as objects (I think that's how it's done, right?). Need it for except classes (Exception as e, CompareException operator)
* [DONE] Scheduler controlling interpreter to switch programs when one waits
* Integration with parent runtime
  * [DONE] Call embedded C# function from script
  * Call Python function through interpreter
  * [DONE-ish] Manage async call with waiting in interpreter
  * Primitive Boxing/unboxing
     * Int
	 * BigInt
	 * Float
	 * Decimal types
	 * strings
	 * bools
  * Object wrapping. Start with wrapping a generic object. All fields should also get boxed/unboxed which will
    likely mess around with how primitive boxing/unboxing is done.
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
  * [DONE] try-catch-finally (else?)
  * [DONE-ish] Getting accurate stack trace (formatting doesn't yet have to be consistent with Python)
  * assert statement (it is a statement, not a function! Parentheses implies passing a tuple, which evaluates to true)
    * AssertionError
  * raise from (exception chaining)
  * Improve exception creation process (need class to construct self pointer. Can I be more direct?)
     * It has something to do with the two-part __new__ and __init__ process. I am not currently handling this
       in the most proper manner but rather kind of encapsulating the self pointer (has-a instead of is-a)
  * [DONE] Raise from class
  * Wrap .NET exceptions
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


Getting line numbers
https://late.am/post/2012/03/26/exploring-python-code-objects.html
co_firstlineno is the first line number in the file (in case there is white space beforehand)_
co_lnotab is a byte dump of alternately:
1. The number of code bytes for the current line of code
2. how far ahead the next line of code is from the current line

https://svn.python.org/projects/python/branches/pep-0384/Objects/lnotab_notes.txt



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
* Start to use dependency injection to manage CodeObject, FrameContext, and the like for WrappedCodeObjects instead of your chicken bits.
  * This wasn't done at the time because I was dabbling in seeing if super() could be implemented with the frame context.
  * I realized trying to pass the frame context too early in the process would be impossible for global built-ins because the
    context will be constantly changing under them.
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





Current notes on embedding
Need to cast int to PyInteger. Generally need box/unbox helpers.
All numeric types--include bool--need to be able to handle having math done with them against the other types. So
int + float + bool has to work.