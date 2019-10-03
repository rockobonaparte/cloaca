// For lack of a better place for now, this is given its own file in the interpreter project.

using System.Threading.Tasks;

namespace CloacaInterpreter
{
    /// <summary>
    /// This is used by the interpreter to run scheduled tasks within its own context instead of the
    /// SynchronizationContext.All awaiters used by the interpreter should implement this and interact
    /// with the interpreter.
    /// 
    /// The general sequence is:
    /// 1. Whatever tasklet/program is created that implement ISubscheduledContinuation
    /// 2. The scheduler will call AssignInterpreter to give the ISubscheduledContinuation a handle to
    ///    the interpreter. It can then call back to the scheduler via interpreter.Scheduler in order
    ///    to notify it when it is blocking. This is essential for properly setting aside the task and
    ///    resuming it later. This is the social contract.
    /// 3. The tasklet/program prepares to block. It class interpreter.Scheduler.NotifyBlocked()
    ///    and passes itself. It could call SetYielded() but that is a single-tick block handled by
    ///    YieldTick.
    /// 4. The tasklet/program associates itself to whatever subsystem is satisfying the synchronous,
    ///    blocking operation
    /// 5. The tasklet/program blocks on an await [SOME CERTAIN SPOT IN CODE].
    /// 6. (The scheduler runs other stuff)
    /// 7. The subsystem performing the synchronous task completes. It calls back to the tasklet/program
    ///    which will then call interpreter.Scheduler.NotifyUnblocked.
    /// 8. The scheduler will move the tasklet/program to the active queue.
    /// 9. The scheduler will restart the tasklet/program using ISubscheduledContinuation.Continue()
    /// 10. The tasklet/program will resume at [SOME CERTAIN SPOT IN CODE].
    /// 
    /// Getting the continuation means implementing a custom awaiter to scoop up the continuation from
    /// INotifyCompletion.OnCompleted().
    /// </summary>
    public interface ISubscheduledContinuation
    {
        /// <summary>
        /// Invoked by the scheduler when this continuation should start/resume.
        /// </summary>
        /// <returns>A Task because the started code might end up blocking some more!</returns>
        Task Continue();

        /// <summary>
        /// Gives the interpreter to this ISubscheduledContinuation. The scheduler will give this to
        /// this activity before it runs so that it can reach the scheduler (interpreter.Scheduler) to
        /// notify it that it is blocking.
        /// </summary>
        /// <param name="interpreter"></param>
        void AssignInterpreter(Interpreter interpreter);
    }
}