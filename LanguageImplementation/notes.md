https://docs.python.org/3/tutorial/classes.html

When a class definition is entered, a new namespace is created, and used as the local scope — thus, all assignments to local variables go into this new namespace. In particular, function definitions bind the name of the new function here.

When a class definition is left normally (via the end), a class object is created. This is basically a wrapper around the contents of the namespace created by the class definition; we’ll learn more about class objects in the next section. The original local scope (the one in effect just before the class definition was entered) is reinstated, and the class object is bound here to the class name given in the class definition header (ClassName in the example).

_So we need to create a new namespace when we encounter a class and dump all the functions made during it in there_

Add test for attribute reference:
```
class Foo:
  a = 1
  def hehe(self):
    pass

Should be able to extract Foo.1 and Foo.hehe

Construction for Foo is not calling __init__, it's doing an _instantiation operation_

```
    >>> def def_constructor():
    ...   class Foo:
    ...     def __init__(self):
    ...       pass
    ...
    >>> dis(def_constructor)
      2           0 LOAD_BUILD_CLASS
                  2 LOAD_CONST               1 (<code object Foo at 0x0000021BD59175D0, file "<stdin>", line 2>)
                  4 LOAD_CONST               2 ('Foo')
                  6 MAKE_FUNCTION            0
                  8 LOAD_CONST               2 ('Foo')
                 10 CALL_FUNCTION            2
                 12 STORE_FAST               0 (Foo)
                 14 LOAD_CONST               0 (None)
                 16 RETURN_VALUE
    >>> x = ctypes.cast(0x0000021BD59175D0, ctypes.py_object).value
    >>> x
    <code object Foo at 0x0000021BD59175D0, file "<stdin>", line 2>
    >>> dis(x)
      2           0 LOAD_NAME                0 (__name__)
                  2 STORE_NAME               1 (__module__)
                  4 LOAD_CONST               0 ('def_constructor.<locals>.Foo')
                  6 STORE_NAME               2 (__qualname__)

      3           8 LOAD_CONST               1 (<code object __init__ at 0x0000021BD5908C00, file "<stdin>", line 3>)
                 10 LOAD_CONST               2 ('def_constructor.<locals>.Foo.__init__')
                 12 MAKE_FUNCTION            0
                 14 STORE_NAME               3 (__init__)
                 16 LOAD_CONST               3 (None)
                 18 RETURN_VALUE
```



def fasts_vs_names():
  class Foo:
    def __init__(self, a):
      self.a = a
    def some_method(self, b):
      self.a = b
  def inner_func(a):
    d = a+1
    return Foo(d)


fasts_vs_names root
  2           0 LOAD_BUILD_CLASS
              2 LOAD_CONST               1 (<code object Foo at 0x000002CE01F964B0, file "<stdin>", line 2>)
              4 LOAD_CONST               2 ('Foo')
              6 MAKE_FUNCTION            0
              8 LOAD_CONST               2 ('Foo')
             10 CALL_FUNCTION            2
             12 STORE_DEREF              0 (Foo)

  7          14 LOAD_CLOSURE             0 (Foo)
             16 BUILD_TUPLE              1
             18 LOAD_CONST               3 (<code object inner_func at 0x000002CE01F96390, file "<stdin>", line 7>)
             20 LOAD_CONST               4 ('fasts_vs_names.<locals>.inner_func')
             22 MAKE_FUNCTION            8
             24 STORE_FAST               0 (inner_func)
             26 LOAD_CONST               0 (None)
             28 RETURN_VALUE

Foo root
  2           0 LOAD_NAME                0 (__name__)
              2 STORE_NAME               1 (__module__)
              4 LOAD_CONST               0 ('fasts_vs_names.<locals>.Foo')
              6 STORE_NAME               2 (__qualname__)

  3           8 LOAD_CONST               1 (<code object __init__ at 0x000002CE01C93930, file "<stdin>", line 3>)
             10 LOAD_CONST               2 ('fasts_vs_names.<locals>.Foo.__init__')
             12 MAKE_FUNCTION            0
             14 STORE_NAME               3 (__init__)

  5          16 LOAD_CONST               3 (<code object some_method at 0x000002CE01F811E0, file "<stdin>", line 5>)
             18 LOAD_CONST               4 ('fasts_vs_names.<locals>.Foo.some_method')
             20 MAKE_FUNCTION            0
             22 STORE_NAME               4 (some_method)
             24 LOAD_CONST               5 (None)
             26 RETURN_VALUE

Foo.__init__
  4           0 LOAD_FAST                1 (a)
              2 LOAD_FAST                0 (self)
              4 STORE_ATTR               0 (a)
              6 LOAD_CONST               0 (None)
              8 RETURN_VALUE

Foo.some_method
>>> dis.dis(fasts_vs_names.__code__.co_consts[1].co_consts[3])
  6           0 LOAD_FAST                1 (b)
              2 LOAD_FAST                0 (self)
              4 STORE_ATTR               0 (a)
              6 LOAD_CONST               0 (None)
              8 RETURN_VALUE

inner_func
>>> dis.dis(fasts_vs_names.__code__.co_consts[3])
  8           0 LOAD_FAST                0 (a)
              2 LOAD_CONST               1 (1)
              4 BINARY_ADD
              6 STORE_FAST               1 (d)

  9           8 LOAD_DEREF               0 (Foo)
             10 LOAD_FAST                1 (d)
             12 CALL_FUNCTION            1
             14 RETURN_VALUE




    


```
    >>> def big_try():
    ...   try:
    ...     a = 1
    ...     raise Exception()
    ...   except Exception as e:
    ...     a = a + 10
    ...   else:
    ...     a = a + 100
    ...   finally:
    ...     a = a + 1000
    ...

    >>> dis.dis(big_try)
      2           0 SETUP_FINALLY           70 (to 72)
                  2 SETUP_EXCEPT            14 (to 18)

      3           4 LOAD_CONST               1 (1)
                  6 STORE_FAST               0 (a)

      4           8 LOAD_GLOBAL              0 (Exception)
                 10 CALL_FUNCTION            0
                 12 RAISE_VARARGS            1
                 14 POP_BLOCK
                 16 JUMP_FORWARD            42 (to 60)

      5     >>   18 DUP_TOP
                 20 LOAD_GLOBAL              0 (Exception)
                 22 COMPARE_OP              10 (exception match)
                 24 POP_JUMP_IF_FALSE       58
                 26 POP_TOP
                 28 STORE_FAST               1 (e)
                 30 POP_TOP
                 32 SETUP_FINALLY           14 (to 48)

      6          34 LOAD_FAST                0 (a)
                 36 LOAD_CONST               2 (10)
                 38 BINARY_ADD
                 40 STORE_FAST               0 (a)
                 42 POP_BLOCK
                 44 POP_EXCEPT
                 46 LOAD_CONST               0 (None)
            >>   48 LOAD_CONST               0 (None)
                 50 STORE_FAST               1 (e)
                 52 DELETE_FAST              1 (e)
                 54 END_FINALLY
                 56 JUMP_FORWARD            10 (to 68)
            >>   58 END_FINALLY

      8     >>   60 LOAD_FAST                0 (a)
                 62 LOAD_CONST               3 (100)
                 64 BINARY_ADD
                 66 STORE_FAST               0 (a)
            >>   68 POP_BLOCK
                 70 LOAD_CONST               0 (None)

     10     >>   72 LOAD_FAST                0 (a)
                 74 LOAD_CONST               4 (1000)
                 76 BINARY_ADD
                 78 STORE_FAST               0 (a)
                 80 END_FINALLY
                 82 LOAD_CONST               0 (None)
                 84 RETURN_VALUE

New opcodes:
 * DUP_TOP
 * POP_TOP
 * RAISE_VARARGS
 * SETUP_FINALLY
 * SETUP_EXCEPT
 * POP_EXCEPT
 * DELETE_FAST
 * END_FINALLY



```
    >>> def raises():
    ...   raise Exception("Hello, world!")
    ...
    >>> dis.dis(raises)
      2           0 LOAD_GLOBAL              0 (Exception)
                  2 LOAD_CONST               1 ('Hello, world!')
                  4 CALL_FUNCTION            1
                  6 RAISE_VARARGS            1
                  8 LOAD_CONST               0 (None)
                 10 RETURN_VALUE
```



```
    >>> def try_except():
    ...   try:
    ...     raise Exception("Hello, World!")
    ...   except:
    ...     return 10
    ...   return 1
    ...
    >>> dis.dis(try_except)
      2           0 SETUP_EXCEPT            12 (to 14)

      3           2 LOAD_GLOBAL              0 (Exception)
                  4 LOAD_CONST               1 ('Hello, World!')
                  6 CALL_FUNCTION            1
                  8 RAISE_VARARGS            1
                 10 POP_BLOCK
                 12 JUMP_FORWARD            10 (to 24)

      4     >>   14 POP_TOP
                 16 POP_TOP
                 18 POP_TOP

      5          20 LOAD_CONST               2 (10)
                 22 RETURN_VALUE

      6     >>   24 LOAD_CONST               3 (1)
                 26 RETURN_VALUE
```






Testing proper inheritance. How are symbols propagated to subclass?

```python
class A:
   def __init__(self):
      self.a = 0

class B(A):
   def __init__(self):
      self.b = self.a
```
Constructing B will fail since a isn't known.

```
>>> dis(B)
Disassembly of __init__:
  3           0 LOAD_FAST                0 (self)
              2 LOAD_ATTR                0 (a)
              4 LOAD_FAST                0 (self)
              6 STORE_ATTR               1 (b)
              8 LOAD_CONST               0 (None)
             10 RETURN_VALUE
```

Now to properly call superconstructor
```python
class A:
   def __init__(self):
      self.a = 0

class B(A):
   def __init__(self):
      super().__init__()
      self.b = self.a
```
>>> dis(B)
Disassembly of __init__:
  3           0 LOAD_GLOBAL              0 (super)
              2 CALL_FUNCTION            0
              4 LOAD_ATTR                1 (__init__)
              6 CALL_FUNCTION            0
              8 POP_TOP

  4          10 LOAD_FAST                0 (self)
             12 LOAD_ATTR                2 (a)
             14 LOAD_FAST                0 (self)
             16 STORE_ATTR               3 (b)
             18 LOAD_CONST               0 (None)
             20 RETURN_VALUE
```

Super internals:
https://www.python.org/dev/peps/pep-3135/
https://stackoverflow.com/questions/13126727/how-is-super-in-python-3-implemented

Asking for details in comp.lang.python on how super() manages internally to get a handle to self.

It looks in CPython that self is actually TOP(), which is the top of the stack. This
implies I put self on the top of the stack!