using System.Threading.Tasks;
using LanguageImplementation;

namespace LanguageImplementation
{
    public interface IScheduler
    {
        bool Done { get; }
        FrameContext LastTasklet { get; }

        void NotifyBlocked(ISubscheduledContinuation continuation);
        void NotifyUnblocked(ISubscheduledContinuation continuation);
        Task RunUntilDone();
        FrameContext Schedule(CodeObject program);
        void SetInterpreter(IInterpreter interpreter);
        void SetYielded(ISubscheduledContinuation continuation);
        Task Tick();
    }
}
