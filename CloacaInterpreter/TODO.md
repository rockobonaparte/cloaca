Cloaca TODO
===========

There is a bit of a circular dependency chain between the scheduler and the interpreter. Currently, we start the scheduler
without a reference to the interpreter and then fill it in afterwards.

Embedding Notes:
Consider updating DefaultNew so we can pass constructor args in one step to objects we're creating on-the-fly if it isn't a huge pain.
Add Create() calls to all the other Python types even if we don't directly construct them (yet). Just get in the habit of having them. Make it part of interface?

Work that has boiled to the top while working on Unity embedding:
1. [DONE] Current plan is to extend WrappedCodeObject to take multiple method candidates:
   1. [DONE] Accept multiple MethodInfos
   2. [DONE] Still have defaults for just a single one
   3. [DONE] Determine which one is the best fit given incoming arguments
   4. [DONE] Prove one test where a pair of overloads is correctly invoked based on which arguments were actually given.
2. [DONE] Start trying to invoke these methods from types generated in Python code and suffer type conversion hell
3. [DONE] Then deal with embedded class constructors!
4. [WIP?] Events
   1. [DONE] Implement necessary operators
      * [DONE] INPLACE_ADD
      * [DONE] INPLACE_SUBTRACT
   2. [DONE] Try to subscribe C# event handler to C# event
   3. Try to subscribe Cloaca function to C# event (maybe move to hardening)
5. [DONE] Assignment operator bonus round!
    * [DONE] INPLACE_MULTIPLY
    * [DONE] INPLACE_TRUE_DIVIDE
    * [DONE] INPLACE_MODULO
    * [DONE] INPLACE_FLOOR_DIVIDE
    * [DONE] INPLACE_POWER
    * [DONE] INPLACE_AND
    * [DONE] INPLACE_OR
    * [DONE] INPLACE_XOR
    * [DONE] INPLACE_RSHIFT
    * [DONE] INPLACE_LSHIFT
   3. Add the __i*__ calls to applicable classes
6. Subscripting
    * Lists
	* Dictionaries
7. [DONE] Invoke a generic where the generic parameter isn't given! This might require bending the language to be able to do Foo<Generic>(parameter)
8. Advanced overload: Check if there could be multiple applicable overloads
   * consider an error if this collision is a real possibility, or else resolve it in the typical .NET way if there is a
     typical way to manage this.
9. More dunders to add to objects (PyInteger, PyFloat, PyBool...). Just do a dir on them beforehand to see if they fit in.
   * __xor__
   * __rshift__
   * __lshift__
   * __truediv__
   * __floordiv__
   * __pow__
   * __mod__
10. Need to make sure that we can check and convert Python args in params field
11. Embed a helper to create basic .NET types from Python types. This would be necessary to index into .NET dictionaries using
    a specific key type.
   * We can try to use the same conversion helpers that the wrapped code object uses to transform types if the dictionary is
     a generic with specific types.

Part 2: Unity embedding. See how practical this is to use in Unity.
* First Unity embed!
  * [DONE] Experiment in demo how it we would expose a subsystem in REPL. This will probably cause a lot of TODOs!
  * [DONE] Toss REPL into Unity!
  * [DONE] Add support for invoking generic methods (so you can call GetComponent<T>)
  * Final exam: start a script that works on a gameObject to do something like change its color with a delay in a loop
     * Expose scheduler in order to execute scripts in a non-blocking way
	 * Expose GameObject finding code in Unity
	 * Manipulate GameObject code
	 * Embed scene hierarchy into Unity
* [IN-PROGRESS] Expand basic opcodes to work with .NET types when possible (add, subtract... etc)
* Serializing script state: dabble in trying to serialize a single, non-blocking script's state.


Part 3: Hardening
* NoneType needs to be formalized as an object and type.
* Switch to wordcode. I thought I had already done this! Wow!
* Read up on the CPython data model: https://docs.python.org/3/reference/datamodel.html
* Integration with parent runtime
  * Call Python function through interpreter
  * Primitive Boxing/unboxing
     * Int
	 * BigInt
	 * Float
	 * Decimal types
	 * strings
	 * bools
  * Object wrapping. Start with wrapping a generic object. All fields should also get boxed/unboxed which will
    likely mess around with how primitive boxing/unboxing is done.
  * PyTuple trial: Creating native types needs to be simplified. Returning a PyTuple of other PyObject types is really tedious to do correctly due
    to needing to call the class to properly create the objects.
	  * This may be as simple as writing a factory.
	  * Need to be able to use the proper PyTuple constructor to pass in a list. Right now, invoking the class with a the tuple contents doesn't
	    cause the right constructor to get invoked.
  * Passing PyInteger where PyFloat is needed--and vice versa--shouldn't fail to invoke the wrapper
* Python number type
  * Integer
     * Hex
     * Binary
  * Floating point
     * Exponential
  * Imaginary
  * Longs
* Lists
    * Slices
  * Sets
* Strings (part 2)
  * str() function for various numeric/bool/other data types
  * Concatenate strings
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
  * Serialization
  * Any test that can wait should automatically be run 
    * All the way through as usual
    * Then with state saved and reloaded each time the interpreter stops
  * __class__.__name__
     * Add to class definitions
     * Use in error messages
* Flush out other major operators
  * += etc
  * Logic operators
* Imports
  * clr library for .NET stuff
  * Import Unity stuff?
* Scripting serialization of blocking code. Use that Reissue() idea for custom awaiters to resume loaded script state blocked on subsystems.
* Functions
  * Implement co_flags
* Exceptions
  * assert statement (it is a statement, not a function! Parentheses implies passing a tuple, which evaluates to true)
    * AssertionError
  * Message should by PyString, not string
  * raise from (exception chaining)
  * Improve exception creation process (need class to construct self pointer. Can I be more direct?)
     * It has something to do with the two-part __new__ and __init__ process. I am not currently handling this
       in the most proper manner but rather kind of encapsulating the self pointer (has-a instead of is-a)
  * Wrap .NET exceptions
* REPL and REPL demo
  * implement help() with a proof-of-concept implementation
  * [May skip...] Improve GUI interaction
	 * Can still select text outside this area
	 * History with arrow keys
	 * Verify copy-paste of multiline statements
  * REPL tweak: strings should have single quotes on them but none of the other results. This is odd because we are getting
    a PyString back to process and we work with a lot of PyStrings, so everything gets wrapped in quotes if we force it in PyString.
  * Robustness from bad programs/awaits/embedding
	* Figure out how to make the scheduler more robust after hangs coming from assuming the last scheduled task--which was null--was the
	  one we cared about when notifying that we unblocked.
    * Add a thread guard in the scheduler that detects if tasks are starting to run from different threads.  
  * Having a FutureAwaiter immediately set a result without blocking in the scheduler causes problems, but it shouldn't...
     * Probably just want to document this since it comes down to notifying the scheduler when the result is set, and I'm not 
       keen on making this check less strict.
* Code bytes should generally take just one extra byte as an argument instead of two bytes. This apparently changed in 3.6: https://stackoverflow.com/questions/50806427/how-to-get-size-of-python-opcode. Question posted online in comp.lang.python.
  * All instructions are two bytes (wordcode) in 3.6+ and that includes ones that normally don't need arguments. Use the EXTENDED_ARG opcode to stage wider data. They are ordered most significant bytes to list; the first EXTENDED_ARG byte is the
    most-significant byte in the series and the number of them determines how far we go. So the interpreter should just assume to fetch into an int and just shift that value over and continue fetching when it seeds EXTENDED_ARG. This makes things
	prettier too since we don't have to worry about moving the cursor forward a mixed amount of times based on opcode. It's always two.
* More .NET integration
  * Generic class support.
  * Implement .NET interface as a Python class and be able to pass to .NET methods requiring interface.
* Fixed 'and' 'or': BINARY_AND and BINARY_OR are being used for 'and' and 'or' tests but they should be used for '&' and '|'. For the logical tests, I guess we
  do some cute jump opcode logic to mimick them.
* Need to implement __hash__ and use it in our data types.
* Need to implement __getattr__ properly as the alternative to __getattribute__


Tech debt:
* Implement BYTES_LITERAL
* Implement full atom rule
  * Requires yield
* Class and objects -- particularly stuff with __new__ and __init__ -- are a mess.
  Look at how CPython is managing them and try to reconcile
* Reimplement WAIT
  * Using async-await.
  * Make it a function instead of a keyword
* Reconcile CodeObject and WrappedCodeObject
  * Create a default __init__ once (and only once) to use in the class builder instead of stubbing a default constructor
* Argumentlist Call() should not require passing in new object[0]


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



Useful bits:
Dump a code object that comes up in a disassembly
>>> import ctypes
>>> c = ctypes.cast(0x10cabda50, ctypes.py_object).value



PySuperType requires more work than I have right now. I'm kind of faking it with the overridden __getattribute__
1. It should implement __getattr__ although it won't report as such if you looked. getattr on the super type will give the parent's methods
2. ...which implies a custom __dir__
3. Also how does this work with multiple bases?! Right now we just hard-code for the first base

```
class Parent:
    def __init__(self):
        print("Parent __init__!")
        self.a = 1

    def stuff(self):
        print("Parent stuff!")


class Child(Parent):
    def __init__(self):
        super().__init__()
        print("Child __init__!")
        self.b = 2
        self.super_instance = super()

    def stuff(self):
        print("Child stuff!")

    def only_in_child(self):
        print("Only in child!")


c = Child()
c.super_instance.__init__()
c.stuff()
c.super_instance.stuff()
print(c.__init__)
print(c.super_instance.__init__)
print(c.__getattribute__)
print(c.super_instance.__getattribute__)
print(dir(c))
print(dir(c.super_instance))
print(c.__dict__ == c.super_instance.__dict__)
print(getattr(c, "__init__"))
print(getattr(c.super_instance, "__init__"))
print(c.__getattribute__("__init__"))
print(c.super_instance.__getattribute__("__init__"))


Parent __init__!
Child __init__!
Parent __init__!
Child stuff!
Parent stuff!
<bound method Child.__init__ of <__main__.Child object at 0x000001FC5AFF9828>>
<bound method Parent.__init__ of <__main__.Child object at 0x000001FC5AFF9828>>
<method-wrapper '__getattribute__' of Child object at 0x000001FC5AFF9828>
<method-wrapper '__getattribute__' of Child object at 0x000001FC5AFF9828>
['__class__', '__delattr__', '__dict__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__getattribute__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__le__', '__lt__', '__module__', '__ne__', '__new__', '__reduce__', '__reduce_ex__', '__repr__', '__setattr__', '__sizeof__', '__str__', '__subclasshook__', '__weakref__', 'a', 'b', 'only_in_child', 'stuff', 'super_instance']
['__class__', '__delattr__', '__dir__', '__doc__', '__eq__', '__format__', '__ge__', '__get__', '__getattribute__', '__gt__', '__hash__', '__init__', '__init_subclass__', '__le__', '__lt__', '__ne__', '__new__', '__reduce__', '__reduce_ex__', '__repr__', '__self__', '__self_class__', '__setattr__', '__sizeof__', '__str__', '__subclasshook__', '__thisclass__', 'a', 'b', 'super_instance']
True
<bound method Child.__init__ of <__main__.Child object at 0x000001FC5AFF9828>>
<bound method Parent.__init__ of <__main__.Child object at 0x000001FC5AFF9828>>
<bound method Child.__init__ of <__main__.Child object at 0x000001FC5AFF9828>>
<bound method Child.__init__ of <__main__.Child object at 0x000001FC5AFF9828>>
```

We'll have to worry about staticmethod at some point. Currently, we don't create a method if PyClass' __getattribute__ is trying
to pull out __call__. We will probably have to expand from there when we try to support static methods.
