# Serializing Coroutines

So let's say you've found some new kind of psychoactive drug that makes you want to save the states of your scripts while they're still running. Okay, *pure* scripts are not 
hard at all with an interpreter. You have the current stack, some variables, and the trace of your call stack. Just dump that all out and load it all back in. The problem is if
you're blocked on something on the C# side that's been paused.

An approximation of what has to happen:
1. A script has to call something that will await internally.
2. A custom awaiter (implementing ISubscheduledContinuation) will start up that the interpreter will grab and pass along to the scheduler.
3. This awaiter is stashed in the scheduler and that script stops running.

... eventually, the awaiter would awaken, call back to the scheduler (using ISubscheduledContinuation), and the scheduler will start
the script back up with the result--if any. But let's say we want to save state here. We have enough information to know what line
we are at, that we made the call, and that we're waiting for the result. We don't have the awaiter anymore.

Somebody has tried really hard to actually serialize these continuations (https://github.com/ljw1004/blog). I couldn't get it to work, but
I didn't run it into the ground trying either. It's clearly a hack--admitted as such--of trying to save the async state machine.

So we have to recreate the awaiter, which implies blocking on the same request. We could rerun the call but we might have been partially
through it. There might be artifacts of that call that are in-place that would become orphaned. So we need a way to _reconnect_.

So on top of calling a blocked call as usual, I am propose a _reissue_ capability. A call will submit a blocking request as usual. A reissue
attempts to rebind to something a subsystem that was called previously was already doing. We need something unique to disambiguate the caller
(like some kind of ID) that can be determined to belong to a saved request.

A simple sleep scenario can test how this might go. Suppose we add sleep() cooperative. This would have to go to some underlying subsystem
that can serialize how much time has already been slept, and reconstruct call chain so that it finishes waiting the appropriate time.

(show normal call chain--sequence diagram?)

(show reissued call chain)

When first phase of loading has happened:
* The timing subsystem knows it is currently fulfilling a delay request that calls back to some object XYZ.
* The interpreter knows it has a script blowing on a scheduled operation that has to be reissued.
   * The reissued operation also has a reference to object XYZ.

The suspended script should reissue the call. Since the operation was saved knowing it was waiting on XYZ, it will pass that to the timing
subsystem and reattach.

...or something...