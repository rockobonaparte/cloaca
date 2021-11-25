Some priorities:

1. [DONE] Cloaca should not escape exceptions with a crash. Print and exit without causing a report to Microsoft
2. [DONE] Double check recursion
   1. [DONE] Basic recursion
   2. [DONE] Defining a recursing function inside another function
3. [DONE] Implement multiplication on arrays to create extended versions of them. This accounts for a lot of errors too.
4. [DONE] Implement array slicing. I don't support array slicing yet! Whoops!
5. [DONE] Enable sherlock2 
6. Need to actually implement NotImplementedError, which is ironic!
7. [DONE] Implement list add
8. [DONE enough] Implement min
9. [DONE enough] Implement max
10. Implement sorted
11. Rerun tests and refile this list


Currently taking a diversion to fix inner recursive calls. Inner functions can't see, well, themselves.
```
def foo(...):
  def bar(...):
    bar(<-- this bar won't be resolved)
```

```
def outer():
  def inner(x):
    if x <= 1:
       print(globals())
       print(locals())
    else:
       inner(x-1)
  inner(2)

outer.__code__.co_cellvars

```
co_cellvars just contains ('inner',). Interesting. A regular function doesn't mention inner stuff like that.


```
{'__name__': '__main__', '__doc__': None, '__package__': None, '__loader__': <class '_frozen_importlib.BuiltinImporter'>, '__spec__': None, '__annotations__': {}, '__builtins__': <module 'builtins' (built-in)>, 'outer': <function outer at 0x0000022FBC03D790>}
{'x': 1, 'inner': <function outer.<locals>.inner at 0x0000022FBC03D700>}
```
So inner is a local wrt itself.

The trick is that all your functions so far have been at the module level so they'll be global. Anything beyond that needs to know that it's locally scoped. I am not sure how to detect that yet. I
am thinking that when I prepare the frame, I see if the function I am calling is in globals. If it isn't, I add it to locals. This check cannot be a basic name check because of name shadowing.
One problem with this is that I think we're using a LOAD_GLOBAL opcode to resolve it.

```
Disassembly of <code object inner at 0x0000022FBC03E2F0, file "<stdin>", line 2>:
  3           0 LOAD_FAST                0 (x)
              2 LOAD_CONST               1 (1)
              4 COMPARE_OP               1 (<=)
              6 POP_JUMP_IF_FALSE       30

  4           8 LOAD_GLOBAL              0 (print)
             10 LOAD_GLOBAL              1 (globals)
             12 CALL_FUNCTION            0
             14 CALL_FUNCTION            1
             16 POP_TOP

  5          18 LOAD_GLOBAL              0 (print)
             20 LOAD_GLOBAL              2 (locals)
             22 CALL_FUNCTION            0
             24 CALL_FUNCTION            1
             26 POP_TOP
             28 JUMP_FORWARD            12 (to 42)

  7     >>   30 LOAD_DEREF               0 (inner)
             32 LOAD_FAST                0 (x)
             34 LOAD_CONST               1 (1)
             36 BINARY_SUBTRACT
             38 CALL_FUNCTION            1
             40 POP_TOP
        >>   42 LOAD_CONST               0 (None)
             44 RETURN_VALUE
```

Woah what the fuck is LOAD_DEREF?! Looks like now I need to read up on cells.

```
LOAD_DEREF(i)
Loads the cell contained in slot i of the cell and free variable storage. Pushes a reference to the object the cell contains on the stack.
```

```
co_cellvars is a tuple containing the names of local variables that are referenced by nested functions.
```
So while generating code, it looks like we need to keep track of this stuff?

Probably also need to ensure I do cell variable stuff correctly. So don't just do an inner call but see what happens when I do an inner call of a function that
also has variables defined.

More screwing around. It looks like nonlocal will ensure a cell variable is created. I don't know why a bar cellvar wasn't made though
```
>>> def foo():
...   x = 0
...   def bar():
...      x += 1
...   return bar()
...
>>> foo.__code__.co_cellvars
()
>>> foo()
Traceback (most recent call last):
  File "<stdin>", line 1, in <module>
  File "<stdin>", line 5, in foo
  File "<stdin>", line 4, in bar
UnboundLocalError: local variable 'x' referenced before assignment
>>> def foo():
...   x = 0
...   def bar():
...      nonlocal x
...      x += 1
...   return bar()
...
>>> foo()
>>> foo.__code__.co_cellvars
('x',)
```

It looks like inner is _not_ a cellvar. It's a free variable:
```
>>> outer.__code__.co_consts[1].co_freevars
('inner',)austin haunt
```
https://gist.github.com/DmitrySoshnikov/700292
    All `free variables` (i.e. variables which are
    neither local vars, nor arguments) of "baz" funtion
    are saved in the internal "__closure__" property.
    Every function has this property, though, not every
    saves the content there (if not use free variables).

It looks like __closure__ isn't public?
https://stackoverflow.com/questions/14413946/what-exactly-is-contained-within-a-obj-closure