Some priorities:

1. [DONE] Cloaca should not escape exceptions with a crash. Print and exit without causing a report to Microsoft
2. [DONE] Double check recursion
   1. [DONE] Basic recursion
   2. [DONE] Defining a recursing function inside another function
3. [DONE] Implement multiplication on arrays to create extended versions of them. This accounts for a lot of errors too.
4. [DONE] Implement array slicing. I don't support array slicing yet! Whoops!
5. [DONE] Enable sherlock2 
6. [DONE] Need to actually implement NotImplementedError, which is ironic!
7. [DONE] Implement list add
8. [DONE enough] Implement min
9. [DONE enough] Implement max
10. [DONE] Implement sorted()
12. [DONE] Double check that we can index into strings
12. [DONE] Implement find() for strings
11. [DONE] Rerun tests and refile this list


1. [DONE] Need to get tracebacks for exceptions created inside C# code. Specifically this would be for quicksort.
2. Implementing "not in" seems straightforward enough.
3. Figure out the problem with add_two_numbers and that __init__. 

The others look like a bit of a slog.
It would also be nice to get a more accurate traceback but that's going to be rough with ANTLR and all.


add_two_numbers: Some `__init__` was given 2 arguments when it expected 3
amazon-subscribers: "Not In" unimplemented
assembly_line: NoneType to PyObject
minheap: Still get an NPE trying to parse the code
quicksort: TypeError while running, but tracebacks are not working right yet so I can't say where.
sherlock2: 'int' object has no attribute named '__getitem__'
smallest_change: Unknown NPE
stock_span: Casting NoneType to an object for some reason