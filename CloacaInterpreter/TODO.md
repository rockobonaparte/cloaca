Cloaca TODO
===========

Part 3: Hardening
* Things that leaked out while implementing defaults parameters/keyword arguments:
  * [INTEGERS WITH FLOATS] Handle mixing of PyFloat and PyInteger (PyBool?) with basic arithmetic operators; 2 - 1.0 shouldn't fail.
  * Try to connect to TaskScheduler.UnobservedTaskException now that even compilation spawns tasks that
    like to throw suppressed exceptions
  * Combine the CALL_FUNCTION implementations_
  * Deal with the situation where some monster uses * or / in their parameters to force a switch to keyword-only.
    * Add unit test and put this todo there
  * [ARGPARAMMATCHER ERRORS] Generate errors when input arguments don't match requirements of code object.
  * [**kwargs] Support kwargs
  * [KEYWORD-POSITIONAL-ONLY] Implement positional-only (/) and keyword-only (*) arguments
  * [DEFAULTS SCOPE] Pass outer scope to defaults
  * [PARAM MATCHER ERRORS] Cast to actual TypeError
* FAANG Python coding interview obsessions with Python modules
  * collections
    * See what you can pull from Python's own source code for this
    * deque
      * appendleft
      * extendleft
      * count
      * pop
      * popleft
      * rotate
    * defaultdict
    * OrderedDict
      * Because somebody dumped this one on me randomly in an interview
    * Counter
  * heapq
    * See what you can pull from Python's own source code for this
    * heappish
    * heappop
    * heapify
* Documentation 
  * Document import process in official documentation
    * Explain how to embed your own modules
    * Explain how to use the file-based importer
  * Explain how generics are done. IronPython uses a special[T, K](arg1, arg2) convention.
* General architecture
  * There is a bit of a circular dependency chain between the scheduler and the interpreter. Currently, we start the scheduler
    without a reference to the interpreter and then fill it in afterwards. Can we simplify this?
  * Simplify some of the boilerplate between PyClass and PyObject without relying on dynamic dictionary lookups.
  * Figure out a one-liner to embed methods without having to use GetMethod(methodName). My hope is to retain compile-time checking of the binding.
  * Try to move CloacaBytecodeVisitor from CloacaInterpreter to LanguageImplementation. I spent a few minutes trying to find it again after
    a hiatus in LanguageImplementation, and was actually shocked to find it in CloacaInterpreter.
  * Unit test configuration builder. The multiple overloads for runProgram and runBasicTest are just too overwhelming at this point.
  * Read up on the CPython data model: https://docs.python.org/3/reference/datamodel.html
* Standard exceptions
  * Create some helpers to assist in creating all the standard Python error types as builtins with the appropriate chain of command.
    Look at how ModuleNotFoundError goes to ImportError goes to a generic PyException. It's pretty gross right now. 
  * Make sure traceback connects with the standard exceptions when we create them internally
    * ```
      >>> import butt
          Traceback (most recent call list):
          No traceback available. This exception was probably created outside of a running program.
          'ModuleNotFoundError' object has no attribute named 'tb'
      ```
* Extended argument types
  * `*args`
    * [DONE] Implement
    * Add to range() so you can do range(10) or range(0, 10, 1), or range(0, 10)...
  * keyword arguments and `**kwargs`
    * defaults based on code: `def foo(default=3+some_other_call())`
      * [MUTABLE DEFAULTS] Parse and then evaluate the defaults as the function is defined and then fill the defaults in. This is how Python does it.
      * We generally will need this to handle even the most basic types without writing a bunch of duplicate logic for creating the types. This is pretty high priority too since these can be pretty diverse.
    * Pure-Python kwargs
    * Calling .NET functions with optionals as if they were kwargs
    * Embedding C# functions that have defaults and can take kwargs
* Cleanup WrappedCode object. Consolidate everything added across the different method lookup conditions into streamlined calls.
  * Cleanup findBestMethodMatch
  * Cleanup injector
  * Cleanup invocation
  * Misc cleanup
  * Document the behavior
  * Try to diagram the behavior of the scenarios
* Add more PyNetConverter rules across the different data types (int-float, float-int, and bool for good measure)
* NoneType needs to be formalized as an object and type.
  * Create PyClass
  * Add a __repr__
* __repr__
  * Create a universal __repr__ helper for nested types
  * __repr__ helper seemlessly works with .NET types
  * Implement __repr__ for iterators
  * Implement ToString() for containers using the __repr__ helpers
* Switch to wordcode. I thought I had already done this! Wow!
* Integration with parent runtime
  * Call Python function through interpreter
  * PyTuple trial: Creating native types needs to be simplified. Returning a PyTuple of other PyObject types is really tedious to do correctly due
    to needing to call the class to properly create the objects.
	  * This may be as simple as writing a factory.
	  * Need to be able to use the proper PyTuple constructor to pass in a list. Right now, invoking the class with a the tuple contents doesn't
	    cause the right constructor to get invoked.     
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
* Tuples
  * Incorporate UNPACK_SEQUENCE into general lvalue expressions and not just for-loops. In fact, try to use a general lvalue assigner that
    can handle UNPACK_SEQUENCE take care of it in general.
  * Support grammar for UNPACK_EX (partial unpack).
  * [TUPLE DUNDERS] Supporting remaining tuple features by implementing the remaining dunders.
  * [TUPLE OBJECT] Support regular objects in tuples along with dunders like __eq__
* Sets
  * Create from literals
  * Main implementation
  * Iterator
  * Interoperation with sets (some notes about needing them there?)
* Dictionary missing methods
  * clear
  * copy
  * fromkeys
  * get
  * pop
  * popitem
  * setdefault
  * update
  * values
  * Update keys and items to use the set() type.
* Packing and unpacking operators
  * [CALL_FUNCTION_EX] use CALL_FUNCTION_EX when calling a function taking vargs that's being fed unpacked data
  * Consult: https://stackabuse.com/unpacking-in-python-beyond-parallel-assignment
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
* Scripting serialization
  * Scripting serialization of blocking code. Use that Reissue() idea for custom awaiters to resume loaded script state blocked on subsystems.
* Functions
  * Implement co_flags
  * Need to make sure that we can check and convert Python args in params field from wrapped calls
  * List/enumerable functional helpers:
    * map
	* filter
	* reduce
* Generators
  * yield statement
* Exceptions
  * Show call stack for scripts that failed in .NET code
  * Full call stack when mixing between interpreted and .NET code.
  * Make sure you can catch exceptions thrown in interpreter and helpers from .NET helper code (SubscriptHelpers as reference)
  * assert statement (it is a statement, not a function! Parentheses implies passing a tuple, which evaluates to true)
    * AssertionError
  * Message should by PyString, not string
  * raise from (exception chaining)
  * Improve exception creation process (need class to construct self pointer. Can I be more direct?)
     * It has something to do with the two-part __new__ and __init__ process. I am not currently handling this
       in the most proper manner but rather kind of encapsulating the self pointer (has-a instead of is-a)
  * Wrap .NET exceptions
  * Cloaca code running as a .NET event receiver that has an exception doesn't report the error. It just kind of disappears.
  * Need better management when trying to reference null .NET values that aren't our fault.
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
* More .NET integration
  * [.NET PYCONTAINERS] Container types should be able to accept object type, not just PyObject. We could use .NET objects for a keys in a PyDict, for example.
  * [UNPACK .NET] Unpack more .NET container types. This was put in TODO because there isn't a real silver bullet for this. We should handle most collection<T> and collection types
  * Verify expected type conversions. This first came up adding a PyInteger with an int. The result was a BigInteger when it maybe should be PyInteger?
  * Implement .NET interface as a Python class and be able to pass to .NET methods requiring interface.
  * .NET container support for
    * Enumerating
	* 'in' test
	* Interoperation with any container methods implemented like map, reduce, filter.
  * Cache extension method lookups in ObjectResolver because the current lookup method is horribly slow. We have to
    trawl all assemblies EACH time we call an extension method.
  * Advanced overload: Check if there could be multiple applicable overloads
    * consider an error if this collision is a real possibility, or else resolve it in the typical .NET way if there is a
     typical way to manage this.
  * Try to box array types (if possible). Came from wanting to support this: `GlobalState.Instance.DialogSubsystem.Prompt(prompt_text, ["No", "Yes"])` where the array
    argument is actually a .NET array.
  * Imports
    * [DISAMBIGUATE GENERIC IMPORTS] Replace clr generic type import with a more robust system that can manage Type<T> and Type<T, K>
* Fixed 'and' 'or': BINARY_AND and BINARY_OR are being used for 'and' and 'or' tests but they should be used for '&' and '|'. For the logical tests, I guess we
  do some cute jump opcode logic to mimick them.
* Need to implement __hash__ and use it in our data types.
* Need to implement __getattr__ properly as the alternative to __getattribute__
* Modules
  * Module isolation: Finders and loaders (and their state) are being kept in the interpreter, not the context, so all scripts
    are sharing that state. We might not want that--although it could be convenient to do that for most scripts.
  * Figure out how to embed methods into a module without necessarily needing to pass PyModule as the first argument.
  * [MODULE_INIT] Start running __init__.py files when importing.
  * [PRECOMPILE_LOAD] Precompile modules and load the precompiled versions
  * Implementing import
     * Relative imports. I can't even get help on this. It looks like it's very obscure and I might just declare I don't support it.
       * Follow-up https://groups.google.com/forum/#!topic/comp.lang.python/AnFJbDMsKAo
  * Helper to create custom .NET PyModules (investigate)
* Task schedule hardening
  * ScheduledTaskRecord additions
    * Cancel(): Cancel this task no matter current state
    * ScheduleCancel(): "Soft" cancel. Next time this task stops, cancel it. Still kind of crappy, but if the task is in a yield polling loop,
      this may be a perfectly fine way to stop them without having to communicate to them.
    * RunNow(): Stop current task and run the one given by the current task record
  * [SYS.SCHEDULE - RETURN TASK] sys.schedule should return the task record or a similar handle that the caller can manage
    * [SYS.SCHEDULE - RETURN TASK - CODEOBJECT] Handle for normal code case
    * [SYS.SCHEDULE - RETURN TASK - PYCALLABLE] Handle for all callables (WrappedCodeObject)
  * Scheduler additions 
    * YieldNow()
    * Scheduler should accept labels for input scripts so stack traces can show their origination.
  * SysModule wrapper should be easier to represent
    * Shouldn't need so much boilerplate
    * Should be able to create PyLists instead of C# arrays.
* Scope keywords: global, nonlocal
  * Exception checks for parsing cases where nonlocal, global are defined after variable is defined.
  * External global declaration.
  * Exception checks for parsing external global declarations that then use that name for function parameters.
  * Nonlocal keyword.
* Stackless Python channels for cross-coroutine communication.
* Ordinal type: ord(). PyOrdinal?


Tech debt:
* [Escaped StopIteration] StopIteration (and other Python exceptions thrown in .NET should be caught as regular Python exceptions)* Implement BYTES_LITERAL
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


## Notes for supporting __main__
What we need to actually schedule are not programs per se but modules containing programs. By default, we should
create a module called "__main__" and that name should then become part of the module's underpinnings. Then the question
is how to figure out how to get to __name__ from inside the module correctly.

Main comes into globals, which then becomes the first-level's locals. Then what happens when something lower down tries to references it?
1. Figure out how __name__ is loaded in a root module by stepping through the interpreter after assigning something to __name__
2. Contrast to how it's done in a function
3. Compare to how you're currently doing it
4. (work will naturally follow from the comparison)

If I do in the REPL:
a = \_\_name\_\_

CPython will do a LOAD_NAME, so that's what I should generate (not a LOAD_FAST). It's an "implicit global" in CPython code generation.

Cloaca also does LOAD_NAME now, so it's down to getting it to work!

I think LOAD_FAST and STORE_FAST have been rendered moot in current implementation. Is this were localsplus would come in?_