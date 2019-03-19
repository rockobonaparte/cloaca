Cloaca TODO
===========

Current problem is that locals don't get propagated down in scope.

General
* Try to create a traverser to get down to atom_expr
  for assignments_
* Look into what happens to constants if overridden (two classes with same name?)
* A lot of the disassembly lines are off by one

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
  * Constructor
  * Access members
  * Call functions
  * Inheritance
  * Start to wrap data types as classes
* Scheduler controlling interpreter to switch programs when one waits
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
* Exception handling try-catch-finally (else?)
* Imports
  * clr library for .NET stuff
  * Import Unity stuff?
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

At this point you should start trying to embed it and suffer it.



Tech debt:
* Implement BYTES_LITERAL
* Implement full atom rule
  * Requires yield