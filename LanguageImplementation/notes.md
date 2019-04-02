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

