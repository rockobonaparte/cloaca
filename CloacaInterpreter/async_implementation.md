﻿Coroutines are an essential part of the Cloaca interpreter. The whole point of going through all of this pain in writing a custom
interpreter is to have coroutines. Here are some of the motivations for having one:

* NPC interactions
* Standalone loops

### NPC Interactions
The player wishes to have a conversation with an NPC. The conversation prompts a dialog. This dialog displays some text and may
prompt the player for a response. At a minimum, it will block until the player acknowledged the dialog. A separate subsystem will
draw a box on the screen and display the dialog text. If the script literally blocked for this response, we would deadlock! This
is because the script would block the thread that would prevent the dialog GUI to draw and the controller subsystem to respond
to input to acknowledge the dialog afterwards. Many frames will elapse between the dialog request and it's acknowledgement by the
player.

We could instead:
1. Use a state machine. Each dialog is its own state and we move on when the state moves on. The problem is that I would be turning
   a series of basically print statements into their own states. It's not how I would be wanting to script this. The state machine
   would turn into an intermediate layer between the game's script and how the game can actually work with it.
2. A purely embedded language that itself can pause the interpreter for the request. I'd then be dealing with that custom language
   and having to hard-code around the coroutine problem. I would basically be having to do everything I have to do here but without
   being able to scour Python stuff for ideas.
3. Shove all this into C#. I plan to be dialog-heavy, so there would actually be a lot of boilerplate to this. I would likely also
   have to use the await statement on just about every line of these "scripts."

### Standalone Loops
If you have the ability to write coroutines then you have the ability to describe a lot of game logic as loops changing various
state. This is the kind of thing usually better off doing with something like a statemachine since you usually end up with something
like a while-switch loop. Consider instead a situation like described in the Maniac Mansion post-mortem at GDC 2017:
https://youtu.be/WD64ExGHBWE?t=1225

They describe a script in SCUMM that controls a grandfather clock. The script controls the pendulum and ticks the clock. It's a
simple script to write as an infinitely-running program. There are situations where having it that way is just the easiest to
express, and it's nice to do that without having to fling it into a thread.

## Implementation
We use custom awaiters to coordinate with Cloaca's scheduler. The custom awaiter ferrets the Action generated by INotifyCompletion
over to the scheduler, which will tuck it away until the scheduler is ready to run it.

Let's say the script is meant to implicitly block. The Python code itself doesn't have an await statement or anything else that
explicitly declares it will stop at that line until some condition is met. The block happens under the hood. In the dialog example,
the block happens while we wait for dialog to display and the user to acknowledge it. The script won't continue until this happens,
but everything else continues running.

To embed this blocking call, we need to create a callable for it so the interpreter knows to invoke it. This callable will accept
the scheduler as an argument as part of its interface. The awaiter implementation will probably grab this reference to use to 
notify when to start and stop blocking this call. The Run() method is async. It can return whatever you want, but make sure to
return a Task if you otherwise don't return anything! However, it internally will create an instance of a custom awaiter. This is
something that implements both INotifyCompletion and ISubscheduledContinuation.

Before awaiting on the awaiter, the scheduler's NotifyBlocked method is called and passed the awaiter. Technically, the scheduler
only needs to see an ISubscheduledContinuation; it just so happens that tying it to the customer awaiter tends to make for easier
plumbing. The scheduler will take the object and store it in a queue of blocked tasks. This will send the active script to pasture
and probably have the scheduler pick up on something else.

The .NET runtime will then react to the await statement by calling the unfortunately-named OnCompleted() with the context to resume
when whatever your awaiter is waiting on finishes. The method's name is bad because it sounds like it's called _when_ whatever is
unblocked, not basically immediately after awaiting. Anyways, you stash this Action and instead invoke it when the scheduler decides
to call the ISubscheduledContinuation.Continue() call to unblock.

The embedded call probably also has other hooks into this custom awaiter to notify it when things have finished. In the dialog
example, the dialog subsystem has a handle to the awaiter to notify it when it has finished the request. This would be when the
user has acknowledged the result. This notification call should call the scheduler's NotifyUnblocked() method, which will then
stage this code to continue. Soon afterwards, you can expect the scheduler to invoke Continue() and then you have to resume that
Action you stashed away. That'll kick things back off where we left off in the Python script.

## Justification
This sounds like a real pain in the ass! Well, it comes down to something like a Future and will probably be wrapped as one with
some more refinement. Still, you now know that's happening under the hood. Still, why bother?

Consider a producer-consumer setup of different coroutines bouncing off of each other. Without the scheduler stepping in, these two
coroutines could just hog the thread infinitely if they aren't being pre-empted by something else. We want to have a scheduler 
managing the scripts generally so the system doesn't get starved by a terribly-written script. We also want the scheduler to warn us
when this is happening. Good debugging!

Alternately, we could use IEnumerables and a lot of yielding. This was the first implementation. The problem is it could be __a lot
of yielding__. A pretty hideous antipattern errupted where we had to tick through IEnumerables from IEnumerables from IEnumerable.
Consider a case where we might recursively call something that eventually internally has a yield. Each of those callables is presenting
an IEnumerable to the interpreter, and we have to keep yielding up and up and up even though only the bottom-most code is actually
blocking on anything. The notation for this is not so bad in itself, but it has to be copied and pasted *every place* that a callable
is getting invoked. Imagine then changing that pasted code some day! Ewww!

Note that F# apparently has a `yield!` keyword that would exhaust all the yielding in one statement. This mostly works for us except
if we have to actually return something and intercept the last yield to use it. Also, Cloaca isn't written in F# (yet :p). There really
isn't a nice way to express this process in C#. Even if there was, we'd still be internally yielding and yielding and yielding up
possibly large stacks. The async-await approach pops out that frame right at the awaiter and that's that.

```
    public interface IPyCallable
    {
        IEnumerable<SchedulingInfo> Call(IInterpreter interpreter, FrameContext context, object[] args);
    }
```

First we're not dealing with IPyCallable anymore so we should change its signature:
```
    public interface IPyCallable
    {
        object Call(IInterpreter interpreter, FrameContext context, object[] args);
    }
```
It looks like a void call might just return null anyways, but I am finding that scary, so I'd recommend a sentinel.

Now extend the interface if the call can be reissued:
```
    public interface IPyReissuable : IPyCallable
    {
        object Reissue(int someMagicToken, IInterpreter interpreter, FrameContext context, object[] args);
    }
```
That token is something to identify this particular call against other blocked calls in the scheduler. Presumably it will be given 
during serialization so it can be saved. It would also have to be accessible to the subsystem executing this call so it can tell that it
was in-progress to service X, and be able to reconnect the reissued call to X.

So the current flow is sketched like:
1. Call() is invoked from the interpreter. The callable involves an async call.
2. The implementation of the callable sets up the yielding operation and passes the ISubscheduledContinuation awaiter to the interpreter.
3. The interpreter will pass this along to the scheduler, and cause the current script to pause.
4. (other scripts can run and the Cloaca runtime itself can be left and re-entered without the context getting lost)

Let's say nothing fancy happens and the call finishes.
5. The awaiter it triggered awake.
6. The scheduler stages the ISubscheduledContinuation to resume
7. (some more scheduled scripts might run)
8. The callable is resumed.
9. Call() finishes and returns the result--if any.

Let's say instead we try to save:
5. The blocking subsystem should know a continuation was blocked on it. That continuation has handle XYZ.
6. Cloaca sets up serialization data. The interpreter state in the scheduler is saved. It notes it was blocked on at a specific
   call with handle XYZ.
7. All of that is written to disk.

Upon resumption:
8. The scheduler notes that it has to reconnect to a subsystem. It's blocked on something with handle XYZ. The associated callable is
  an IPyReissuable.
9. It invokes Reissue, passing XYZ and the original arguments.
10. The subsystem doesn't start a new request with this command. Instead, it finds XYZ, pairs whatever callback it needs for when that
  is finally done.
11. The reissue blocks the interpreter just like with call, but the resulting ISubscheduledContinuation is set to wake up when the in-flight
  request ultimately finishes.

There's some handwaving over this handle that needs to be understood.