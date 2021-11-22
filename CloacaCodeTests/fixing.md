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
```

```
{'__name__': '__main__', '__doc__': None, '__package__': None, '__loader__': <class '_frozen_importlib.BuiltinImporter'>, '__spec__': None, '__annotations__': {}, '__builtins__': <module 'builtins' (built-in)>, 'outer': <function outer at 0x0000022FBC03D790>}
{'x': 1, 'inner': <function outer.<locals>.inner at 0x0000022FBC03D700>}
```
So inner is a local wrt itself.

The trick is that all your functions so far have been at the module level so they'll be global. Anything beyond that needs to know that it's locally scoped. I am not sure how to detect that yet. I
am thinking that when I prepare the frame, I see if the function I am calling is in globals. If it isn't, I add it to locals. This check cannot be a basic name check because of name shadowing.