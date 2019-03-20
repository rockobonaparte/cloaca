Cloaca TODO
===========

Current problem is that locals don't get propagated down in scope.
For now locals will be carried down through all scopes. This isn't quite right and will defintely 
have to be changed when I get to modules, as well as for this:

```
>>> def both_use_x():
...    x = 1
...    y = 2
...    def first():
...       x = 3
...       return x
...    def second():
...       y = 4
...       return y
...    return first() + second() + x + y
>>> both_use_x()
10
>>> dis.dis(both_use_x)
  2           0 LOAD_CONST               1 (1)
              2 STORE_FAST               0 (x)

  3           4 LOAD_CONST               2 (2)
              6 STORE_FAST               1 (y)

  4           8 LOAD_CONST               3 (<code object first at 0x000001FD4F3E8B70, file "<stdin>", line 4>)
             10 LOAD_CONST               4 ('both_use_x.<locals>.first')
             12 MAKE_FUNCTION            0
             14 STORE_FAST               2 (first)

  7          16 LOAD_CONST               5 (<code object second at 0x000001FD4F3E8A50, file "<stdin>", line 7>)
             18 LOAD_CONST               6 ('both_use_x.<locals>.second')
             20 MAKE_FUNCTION            0
             22 STORE_FAST               3 (second)

 10          24 LOAD_FAST                2 (first)
             26 CALL_FUNCTION            0
             28 LOAD_FAST                3 (second)
             30 CALL_FUNCTION            0
             32 BINARY_ADD
             34 LOAD_FAST                0 (x)
             36 BINARY_ADD
             38 LOAD_FAST                1 (y)
             40 BINARY_ADD
             42 RETURN_VALUE
>>> c = ctypes.cast(0x000001FD4F3E8A50, ctypes.py_object).value
>>> dis.dis(c)
  8           0 LOAD_CONST               1 (4)
              2 STORE_FAST               0 (x)

  9           4 LOAD_FAST                0 (x)
              6 RETURN_VALUE
>>> c = ctypes.cast(0x000001FD4F3E8B70, ctypes.py_object).value
>>> dis.dis(c)
  5           0 LOAD_CONST               1 (3)
              2 STORE_FAST               0 (x)

  6           4 LOAD_FAST                0 (x)
              6 RETURN_VALUE
```

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