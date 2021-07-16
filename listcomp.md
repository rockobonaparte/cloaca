# List Comprehension Notes

These are some notes on working with list comprehensions and particularly how to generate code for them.

## Reference

Using a base loop that just assigns each element to the new list like a copy.

Actually coding the for-loop:
```
>>> def copy_list(x):
...   dot0 = []
...   for y in x:
...     dot0.append(y)
...   return dot0
...
>>> dis(copy_list)
  2           0 BUILD_LIST               0
              2 STORE_FAST               1 (dot0)

  3           4 LOAD_FAST                0 (x)
              6 GET_ITER
        >>    8 FOR_ITER                14 (to 24)
             10 STORE_FAST               2 (y)

  4          12 LOAD_FAST                1 (dot0)
             14 LOAD_METHOD              0 (append)
             16 LOAD_FAST                2 (y)
             18 CALL_METHOD              1
             20 POP_TOP
             22 JUMP_ABSOLUTE            8

  5     >>   24 LOAD_FAST                1 (dot0)
             26 RETURN_VALUE
```

## Basic list comprehension
Basic list comprehension:
```
>>> def listcomp(x):
...   return [y for y in x]
...
>>> dis(listcomp)
  2           0 LOAD_CONST               1 (<code object <listcomp> at 0x0000014A3E13EF50, file "<stdin>", line 2>)
              2 LOAD_CONST               2 ('listcomp.<locals>.<listcomp>')
              4 MAKE_FUNCTION            0
              6 LOAD_FAST                0 (x)
              8 GET_ITER
             10 CALL_FUNCTION            1
             12 RETURN_VALUE

Disassembly of <code object <listcomp> at 0x0000014A3E13EF50, file "<stdin>", line 2>:
  2           0 BUILD_LIST               0
              2 LOAD_FAST                0 (.0)
        >>    4 FOR_ITER                 8 (to 14)
              6 STORE_FAST               1 (y)
              8 LOAD_FAST                1 (y)
             10 LIST_APPEND              2
             12 JUMP_ABSOLUTE            4
        >>   14 RETURN_VALUE
```

So what's this .0 name and can I get my hands on it?

```
>>> listcomp.__code__.co_consts
(None, <code object <listcomp> at 0x0000014A3E13EF50, file "<stdin>", line 2>, 'listcomp.<locals>.<listcomp>')
>>> listcomp.__code__.co_consts[1].co_varnames
('.0', 'y')
```

So the .0 is a thing it just generates, and it looks like there won't be a .1 or anything. You can just slap that sucker in and call it a day (?). 
Probably worth looking at the Python C source code to see if '.0' shows up anywhere.

## More than one list comprehension

### Two serial

```
>>> def two_comp(x):
...   return [y for y in x] + [z+1 for z in x]
...
>>> dis(two_comp)
  2           0 LOAD_CONST               1 (<code object <listcomp> at 0x0000014A3E2B0240, file "<stdin>", line 2>)
              2 LOAD_CONST               2 ('two_comp.<locals>.<listcomp>')
              4 MAKE_FUNCTION            0
              6 LOAD_FAST                0 (x)
              8 GET_ITER
             10 CALL_FUNCTION            1
             12 LOAD_CONST               3 (<code object <listcomp> at 0x0000014A3E2B03A0, file "<stdin>", line 2>)
             14 LOAD_CONST               2 ('two_comp.<locals>.<listcomp>')
             16 MAKE_FUNCTION            0
             18 LOAD_FAST                0 (x)
             20 GET_ITER
             22 CALL_FUNCTION            1
             24 BINARY_ADD
             26 RETURN_VALUE

Disassembly of <code object <listcomp> at 0x0000014A3E2B0240, file "<stdin>", line 2>:
  2           0 BUILD_LIST               0
              2 LOAD_FAST                0 (.0)
        >>    4 FOR_ITER                 8 (to 14)
              6 STORE_FAST               1 (y)
              8 LOAD_FAST                1 (y)
             10 LIST_APPEND              2
             12 JUMP_ABSOLUTE            4
        >>   14 RETURN_VALUE

Disassembly of <code object <listcomp> at 0x0000014A3E2B03A0, file "<stdin>", line 2>:
  2           0 BUILD_LIST               0
              2 LOAD_FAST                0 (.0)
        >>    4 FOR_ITER                12 (to 18)
              6 STORE_FAST               1 (z)
              8 LOAD_FAST                1 (z)
             10 LOAD_CONST               0 (1)
             12 BINARY_ADD
             14 LIST_APPEND              2
             16 JUMP_ABSOLUTE            4
        >>   18 RETURN_VALUE
```

### Two at the same time

Double list comprehension is a thing!
```
>>> text = [["Hello", "World!"], ["Lets", "Eat!"]]
>>> def double_comp(t):
...   return [word for words in t for word in words]
...
>>> double_comp(text)
['Hello', 'World!', 'Lets', 'Eat!']
>>> dis(double_comp)
  2           0 LOAD_CONST               1 (<code object <listcomp> at 0x0000014A3E2B6F50, file "<stdin>", line 2>)
              2 LOAD_CONST               2 ('double_comp.<locals>.<listcomp>')
              4 MAKE_FUNCTION            0
              6 LOAD_FAST                0 (t)
              8 GET_ITER
             10 CALL_FUNCTION            1
             12 RETURN_VALUE

Disassembly of <code object <listcomp> at 0x0000014A3E2B6F50, file "<stdin>", line 2>:
  2           0 BUILD_LIST               0
              2 LOAD_FAST                0 (.0)
        >>    4 FOR_ITER                18 (to 24)
              6 STORE_FAST               1 (words)
              8 LOAD_FAST                1 (words)
             10 GET_ITER
        >>   12 FOR_ITER                 8 (to 22)
             14 STORE_FAST               2 (word)
             16 LOAD_FAST                2 (word)
             18 LIST_APPEND              3
             20 JUMP_ABSOLUTE           12
        >>   22 JUMP_ABSOLUTE            4
        >>   24 RETURN_VALUE
>>>
```
