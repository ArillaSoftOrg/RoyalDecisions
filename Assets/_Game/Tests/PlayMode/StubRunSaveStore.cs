using RoyalDecisions.Application;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// An in-memory run store for scene tests.
    /// </summary>
    /// <remarks>
    /// Injected so a PlayMode test never writes to the player's persistent data, and so
    /// <see cref="SaveCount"/> can prove how many times a swipe reached persistence.
    /// </remarks>
    public sealed class StubRunSaveStore : IRunSaveStore
    {
        private RunState stored;

        public int SaveCount { get; private set; }

        public int DeleteCount { get; private set; }

        public bool HasSave()
        {
            return stored != null;
        }

        public RunLoadOutcome Load()
        {
            return stored == null
                ? RunLoadOutcome.Failure(RunLoadStatus.NoSave, "nothing stored")
                : RunLoadOutcome.Loaded(RunLoadStatus.Success, stored);
        }

        public SaveOutcome Save(RunState runState)
        {
            SaveCount++;
            stored = runState;
            return SaveOutcome.Ok();
        }

        public SaveOutcome Delete()
        {
            DeleteCount++;
            stored = null;
            return SaveOutcome.Ok();
        }
    }
}
