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