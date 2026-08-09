using RoyalDecisions.Domain;

namespace RoyalDecisions.Application
{
    /// <summary>
    /// The application's port onto run persistence.
    /// </summary>
    /// <remarks>
    /// The application owns this contract; a composition-root adapter implements it over the
    /// concrete save service. That is what keeps the application layer free of file I/O and of any
    /// Infrastructure type, and what lets flow tests run against an in-memory fake.
    ///
    /// No implementation may throw: every outcome is a returned value.
    /// </remarks>
    public interface IRunSaveStore
    {
        bool HasSave();

        RunLoadOutcome Load();

        SaveOutcome Save(RunState runState);

        SaveOutcome Delete();
    }
}
