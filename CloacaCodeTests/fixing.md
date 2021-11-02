Some priorities:

1. Cloaca should not escape exceptions with a crash. Print and exit without causing a report to Microsoft
2. Double check recursion
   1. Basic recursion
   2. Defining a recursing function inside another function
3. Implement multiplication on arrays to create extended versions of them. This accounts for a lot of errors too.
4. Implement array slicing. I don't support array slicing yet! Whoops!
5. Enable sherlock2 
6. Need to actually implement NotImplementedError, which is ironic!
7. Revisit from there

Also need to implemented:
* sorted
* min
* max


Subscripting notes
>>> def subscripting():
...   r = [0, 1, 2]
...   a = r[0:]
...   b = r[:1]
...   c = r[1:2]
...
>>> dis(subscripting)
  2           0 BUILD_LIST               0
              2 LOAD_CONST               1 ((0, 1, 2))
              4 LIST_EXTEND              1
              6 STORE_FAST               0 (r)

  3           8 LOAD_FAST                0 (r)
             10 LOAD_CONST               2 (0)
             12 LOAD_CONST               0 (None)
             14 BUILD_SLICE              2
             16 BINARY_SUBSCR
             18 STORE_FAST               1 (a)

  4          20 LOAD_FAST                0 (r)
             22 LOAD_CONST               0 (None)
             24 LOAD_CONST               3 (1)
             26 BUILD_SLICE              2
             28 BINARY_SUBSCR
             30 STORE_FAST               2 (b)

  5          32 LOAD_FAST                0 (r)
             34 LOAD_CONST               3 (1)
             36 LOAD_CONST               4 (2)
             38 BUILD_SLICE              2
             40 BINARY_SUBSCR
             42 STORE_FAST               3 (c)
             44 LOAD_CONST               0 (None)
             46 RETURN_VALUE
>>>
