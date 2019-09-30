// For lack of a better place for now, this is given its own file in the interpreter project.

namespace CloacaInterpreter
{
    // This is used by the interpreter to run scheduled tasks within its own context instead of the
    // SynchronizationContext. All awaiters used by the interpreter should implement this and interact
    // with the interpreter.
    public interface ISubscheduledContinuation
    {
        void Continue();
        void AssignInterpreter(Interpreter interpreter);        // Used on deserialization to get new interpreter handle
    }
}