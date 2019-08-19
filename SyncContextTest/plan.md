# What We're Trying To Do

Currently, we are using IEnumerator coroutines. This has a few problems:
1. Pollution of function signatures to all return IEnumerators
2. Copy-paste of a common iteration loop over these calls to get them to complete, with intermediate results just getting percolated upwards.

Let's try something--anything--else. First, Tasks in general.

Let's consider a scenario similar to how we're using this interpreter. The interpreter is embedded into Unity as its own subsystem running on the main
thread. This subsystem is tracking how much time it's blowing in the current frame and will let off if it either is about to exceed that or all of its
tasks are blocked. The tasks that are blocked are usually blocked on getting results from outside of the interpreter's subsystem.

Let's create two tasks:
1. A long-running task that is just cooperative yielding. Note that the cooperative yielding may be coming from the interpreter itself, but we'll
   just mock that with explicit mock callouts to pause.
2. A second task waiting for the result of something that will run outside of the scope of the interpreter's scheduler. A lot of such subsystem calls
   might be satisfied immediately, but this one should wait. It should theoretically wait at least one rendering frame.

There is an implicit third task: The outer context that will represent an external subsystem that supplies results.

For maximum replication, the yielding should happen a few layers from the top-level call so we can see what it looks like to propagate blockages.

All of this should run cooperatively on one thread.

# Rough Sketch

## Long Running Task
```
Outer:
  Increase a counter
  yield
  Inner (this is called as a separate function so we can see what it's like to chain into independent, yielding code)
    Loop 5 Times:
       Increase a counter
       cooperative pause
    return some value
```

## Requesting Task
```
Loop 5 Times:
   Request something from external subsystems; the external subsystem should block for one iteration before supplying it
  (this task should then pause)
```
