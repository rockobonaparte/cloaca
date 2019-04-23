namespace LanguageImplementation
{
    /// <summary>
    /// This is a base class to give a common return signature for all the re-entrant code
    /// between the interpreter and it's .NET embedded code. Wrapped by an IEnumerable when
    /// defined.
    /// </summary>
    public class SchedulingInfo
    {

    }

    /// <summary>
    /// Used to specifically signify to all schedulers to perform just a simple, single pass
    /// yield on this re-entrant code.
    /// </summary>
    public class YieldOnePass : SchedulingInfo
    {

    }

    /// <summary>
    /// Used to signify to schedulers that this re-entrant code is about to terminate and will
    /// return a value. This will contain the returned value. Subsequent calls to the IEnumerable
    /// should not run any additional code and simply run the yield break (or just go out of scope).
    /// </summary>
    public class ReturnValue : SchedulingInfo
    {
        public object Returned;

        public ReturnValue(object returned)
        {
            Returned = returned;
        }
    }
}
