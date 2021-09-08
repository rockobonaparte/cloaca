# Ratings
* -1: Might as well be sabouaging my own efforts to support it.
* 0: No support; aware of it and I don't have anything for it.
* ?: No support; kind of oblivious to what it even is.
* 1: Infrastructural support; probably can't use but wouldn't be too hard to add.
* 2: Marginal support; might not still be usable but scaffolding is there for it.
* 3: Supported; works at least to the level I personally test it.


# Keywords Support

| Keyword      | Rating | Notes                                                                                                 |
| -----        | ------ | ---------------                                                                                       |
| async/await  | 1      | Keywords are not supported, but the whole goal of the project should make this "reasonably possible." |
| assert       | 1      | Straightforward to add an has TODO items for it.                                                      |
| and          | 3      |
| as           | 3      | Particularly tested for exceptions                                                                    |
| break        | 3      |
| class        | 3      | Metaclasses are not technically supported but use a default metaclass in object instantiation.        |
| continue     | 3      |
| def          | 3      | Normal usage will work just fine, but we haven't tested functions-in-functions.                       |
| del          | 3      | Should use appropriate \_\_delitem__ dunders and work on some .NET containers.                        |
| for          | 3      | Should also be able to use it on enumerable .NET containers.                                          |
| from         | 3      | Particularly in relation to imports (from foo import bar)                                             |
| global       | 3      |
| if-elif-else | 3      |                                                                                                       |
| import	   | 3      | The import system is very similar to Python's own system. It was modelled after notes from [https://www.python.org/dev/peps/pep-0451/](PEP-451). |
| is           | 3      |
| in           | 3      | Should also work on some .NET containers
| lambda       | 1      | We use code objects like Python and nothing is really stopping us from actually supporting it but time |
| or           | 3      |
| None         | 3      |
| nonlocal     | 1      | We do support global but we never went all the way and added nonlocal too                             |
| not          | 3      |
| return       | 3      |
| True/False   | 3      |                                                                                                       |
| try-except-finally | 3 | A fairly robust implementation that includes "raise from." Interoperates with .NET exceptions        |
| with         | 0      | Context managers have not been started with all. \_\_enter__ and \_\_exit__ are not used.             |
| while        | 3      |
| yield        | 3      | Generators are supported. Specifically \_\_next__ is supported, along with range().                   |


# Built-In Functions Support

When it comes to the built-in functions, support is much lower:

| Function     | Rating | Notes                                                                                                 |
| -----        | ------ | ---------------                                                                                       |
| \_\_import__ | ?      | Need to see how easy this would be to implement with the import system.                               |
| abs          | 0      |                                                                                                       |
| all          | 0      |
| ascii        | 0
| any          | 0      |
| bin          | 0      | Not demonstrably parsing binary data in the first place.                                              |
| bool         | 3      |
| bytearray    | 0      | very low priority since any scripting doing this will probably use the host runtime's own data types. |
| bytes        |        | very low priority since any scripting doing this will probably use the host runtime's own data types. |
| breakpoint   | -1     | A major foul in an embedded interpreter, but we'll probably give an interpreter user hook to react to this. |
| callable     | 2      | Doesn't exist but appears trivial to support.                                                         |
| chr          | 0
| classmethod  | -1     | Would rather focus on class method decorator                                                          |
| compile      | -1     | It's bad enough to use an embedded interpreter but arbitrary code execution is even worse. Probably create a callback hook for how to react with reference on how to compile if that's what you want to do!. |
| complex      | 0      | No complex number support at all.                                                                     |
| dict         | 2      | I don't know how much we aggressively have tested its casting and arguments. Basic calls work.        |
| dir          | 3      | Should even behave consistently to Python's own dir() when it comes to inheritance.                   |
| divmod       | 0
| delattr      | 2      | Uses the \_\_delattr__ dunder and should work on most .NET containers, but function not implemented   |
| enumerate    | 0      | _I should use this function more in general._                                                         |
| eval         | -1     | It's bad enough to use an embedded interpreter but arbitrary code execution is even worse. Probably create a callback hook for how to react. |
| filter       | 0
| float        | 2      | Can cast and create floats but I expect technicalities would screw up like NaN, infinity and such.    |
| format       | 0      | Not too keen on fulling supporting this, and I'd rather go in on f-strings.                           |
| frozenset    | 0      | Frozen sets have not be implemented at all.                                                           |
| getattr      | 1      | The \_\_getattr__ dunder is supported but we haven't implemented this actual function.
| globals      | 0
| hash         | 2      | We should use an object-ID-based hash like Python's default and we support hashing, but didn't make this function yet. |
| hasattr      | 0      | Unlike lots of other \*attr functions, we don't seem to have any plumbing for hasattr.                |
| help         | 0
| hex          | 0
| id           | 2      | Object ID is implemented, but this function is not.                                                   |
| input        | -1     | The embedded interpreter would need to decide how to handle this call.                                |
| int          | 3
| isinstance   | 3      | A lot of pain and suffering went here and it should behave like Python's.                             |
| issubclass   | 3      | A lot of blood and sweat went into this and it should behave like Python's.                           |
| iter         | 1      | Iterators are a thing but next() isn't nor is this function itself.                                   |
| len          | 3      | Should also work on .NET containers.                                                                  |
| locals       | 0
| list         | 3      | Seems to behave at least in typical usage.
| map          | 0
| memoryview   | ?      | Given the focus on C with memoryview, I am not sure what we'd actually do to support this.            |
| max          | 0
| min          | 0
| next         | 2      | The function isn't implemented itself, but all of the generator mechanisms for it are there           |
| object       | 1      | The function itself isn't implemented at all and I don't know about the technicalities of using it directly |
| oct          | 0      | I don't know if we're even parsing octal numbers yet.                                                 |
| open         | ?      | We're not sure what we really want to do with file I/O. It might be restricted. We also don't yet support context managers nor Python file I/O in general |
| ord          | 0      | Except lots of inconsistencies over Unicode.                                                          |
| pow          | 0
| print        | -1     | Printing is peculiar in an embedded runtime and this will probably defer to a user-specified callback |
| property     | 0      | Will probably require a lot of scrutiny to ensure it works in the typical cases                       |
| range        | 3
| repr         | 2      | The \_\_repr__ dunde is supported but we haven't actually implemented this function.                  |
| reversed     | 2      | It works for Python types but has not been enabled for most .NET types                                |  
| round        | 0
| set          | 0
| setattr      | 2      | The \_\_setattr__ dunder is supported but we haven't implemented this actual function.                |
| slice        | 0
| sorted       | ?      | There's some decisionmaking that has to happen first. Do we defer to .NET sorting? Mimic TimSort?     |
| staticmethod | 0      | We don't support static methods yet and this could ruin things if we did. Class methods are supported. |
| str          | 2      | You have to throw five million asterisks on this for Unicode alone. Strings generally work though.    |
| sum          | 0
| super        | 3      | Implemented like Python3's: horrifically. It's amazing. You should look it up.                        |
| tuple        | 2      | You can do it but it will probably fail on technicalities. Tuples generally are supported             |
| type         | 2      | Exists but unconfident in fullness of implementation. This gets ugly.                                 |
| vars         | 0
| zip          | 2      | Needs to iterate .NET types too
