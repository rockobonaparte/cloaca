Notes for Supporting the Collections Module
===========================================

Python syntactic junk I have to understand:

* class __loader__(object) in _collections.py.
* Things in collection's __init__.py
  * __all__
  * __getattr__ (module level)

Extra syntax I'd need to support to support collections completely:
* Decorators
  * @classmethod
  * @property

Stuff I actually use right now from coding interview crap:
* deque
* defaultdict

Consider also throwing in heapq. Actually, it might just run? So maybe do this first?!


So the plan:
1. Try to create a standard library setup
2. Add heapq. Suffer.
3. Add bastardized collections for deque and defaultdict
4. [Get decorators going in general]
5. Come back to try to implement a more completion version of collections


## Keyword Arguments Notes
The default settings for some keyword arguments are in the function's __defaults__ as a set. We don't have sets yet so we're going to
have to hack around that for now (tuple?)

Setting **kwargs is distinct from just having named defaults. So you'd look at the number of arguments, the number of __defaults__, and
fill back from the end.

Yuck.

```python
>>> def all_in_my_mouth(a, *b, **c, d=1):
  File "<stdin>", line 1
    def all_in_my_mouth(a, *b, **c, d=1):
                                    ^
SyntaxError: invalid syntax
>>> def all_in_my_mouth(a, *b, d=1, **c):
...    pass
...
>>> def all_in_my_mouth(a, d=1, *b, **c):
...    pass
...
>>>
```


What does this even do?
def pyle(a, b=2, *c, **d):
  print(a)
  print(b)
  print(c)
  print(d)


## Current issues

I have been targeting heapq. I was missing vargs, defaults, and keyword arguments, but they have since been implemented. What I don't have are **kwargs and default-only.

heapq has `merge`:
`def merge(*iterables, key=None, reverse=False):`

We don't support defaults after *args right now.

List comprehensions are also missing.

Vomit from trying to import heapq
```
There were parse errors:
line 315:25 mismatched input '=' expecting ')'
line 315:45 mismatched input ')' expecting {';', NEWLINE}
line 316:4 extraneous input '    ' expecting {<EOF>, 'del', 'pass', 'break', 'continue', 'return', 'raise', 'from', 'import', '...', 'global', 'nonlocal', 'assert', 'if', 'while', 'for', 'try', 'with', 'lambda', 'not', '~', 'None', 'True', 'False', 'class', 'yield', STRING, NUMBER, 'wait', 'def', '*', '@', '+', '-', NAME, NEWLINE, '(', '[', '{', ASYNC, AWAIT}
line 393:0 extraneous input '\n' expecting {<EOF>, 'del', 'pass', 'break', 'continue', 'return', 'raise', 'from', 'import', '...', 'global', 'nonlocal', 'assert', 'if', 'while', 'for', 'try', 'with', 'lambda', 'not', '~', 'None', 'True', 'False', 'class', 'yield', STRING, NUMBER, 'wait', 'def', '*', '@', '+', '-', NAME, NEWLINE, '(', '[', '{', ASYNC, AWAIT}
line 462:30 mismatched input '=' expecting ')'
line 462:35 no viable alternative at input ')'
line 463:4 extraneous input '    ' expecting {<EOF>, 'del', 'pass', 'break', 'continue', 'return', 'raise', 'from', 'import', '...', 'global', 'nonlocal', 'assert', 'if', 'while', 'for', 'try', 'with', 'lambda', 'not', '~', 'None', 'True', 'False', 'class', 'yield', STRING, NUMBER, 'wait', 'def', '*', '@', '+', '-', NAME, NEWLINE, '(', '[', '{', ASYNC, AWAIT}
line 520:0 extraneous input '\n' expecting {<EOF>, 'del', 'pass', 'break', 'continue', 'return', 'raise', 'from', 'import', '...', 'global', 'nonlocal', 'assert', 'if', 'while', 'for', 'try', 'with', 'lambda', 'not', '~', 'None', 'True', 'False', 'class', 'yield', STRING, NUMBER, 'wait', 'def', '*', '@', '+', '-', NAME, NEWLINE, '(', '[', '{', ASYNC, AWAIT}
line 522:29 mismatched input '=' expecting ')'
line 522:34 no viable alternative at input ')'
line 523:4 extraneous input '    ' expecting {<EOF>, 'del', 'pass', 'break', 'continue', 'return', 'raise', 'from', 'import', '...', 'global', 'nonlocal', 'assert', 'if', 'while', 'for', 'try', 'with', 'lambda', 'not', '~', 'None', 'True', 'False', 'class', 'yield', STRING, NUMBER, 'wait', 'def', '*', '@', '+', '-', NAME, NEWLINE, '(', '[', '{', ASYNC, AWAIT}
line 578:0 extraneous input '\n' expecting {<EOF>, 'del', 'pass', 'break', 'continue', 'return', 'raise', 'from', 'import', '...', 'global', 'nonlocal', 'assert', 'if', 'while', 'for', 'try', 'with', 'lambda', 'not', '~', 'None', 'True', 'False', 'class', 'yield', STRING, NUMBER, 'wait', 'def', '*', '@', '+', '-', NAME, NEWLINE, '(', '[', '{', ASYNC, AWAIT}
```

* Line 315 has to do with \*iterables: `def merge(*iterables, key=None, reverse=False):`
* Line 393 has to do with yield from: `yield from next.__self__`
* Unsure about 462-463; might be noise from 393 which is previous line of actual code.
* Line 520 (and 578) is a list comprehension (oh boy): `return [elem for (k, order, elem) in result]`
* Lines 522-523 is probably spillover from 520
