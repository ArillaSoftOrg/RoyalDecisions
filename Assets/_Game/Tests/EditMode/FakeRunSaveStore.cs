using System.Collections.Generic;
using RoyalDecisions.Application;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// An in-memory run store that counts writes and can be told to fail or to return any status.
    /// </summary>
    /// <remarks>
    /// The write counters are what let a test prove a corrupt save was never overwritten: the
    /// assertion is <c>SaveCount == 0</c>, not the absence of a log line.
    /// </remarks>
    public sealed class FakeRunSaveStore : IRunSaveStore
    {
        private RunState stored;

        public int SaveCount { get; private set; }

        public int DeleteCount { get; private set; }

        public int LoadCount { get; private set; }

        public bool FailSaves { get; set; }

        /// <summary>When set, overrides whatever is stored.</summary>
        public RunLoadStatus? ForcedLoadStatus { get; set; }

        public RunState LastSaved { get; private set; }

        public List<string> Calls { get; } = new List<string>();

        public void Seed(RunState runState)
        {
            stored = runState;
        }

        public bool HasSave()
        {
            return ForcedLoadStatus.HasValue || stored != null;
        }

        public RunLoadOutcome Load()
        {
            LoadCount++;
            Calls.Add("Load");

            if (ForcedLoadStatus.HasValue)
            {
                RunLoadStatus forced = ForcedLoadStatus.Value;

                bool usable = forced == RunLoadStatus.Success
                    || forced == RunLoadStatus.SuccessAfterRepair
                    || forced == RunLoadStatus.RecoveredFromBackup;

                return usable && stored != null
                    ? RunLoadOutcome.Loaded(forced, stored)
                    : RunLoadOutcome.Failure(forced, forced.ToString());
            }

            return stored == null
                ? RunLoadOutcome.Failure(RunLoadStatus.NoSave, "nothing stored")
                : RunLoadOutcome.Loaded(RunLoadStatus.Success, stored);
        }

        public SaveOutcome Save(RunState runState)
        {
            SaveCount++;
            Calls.Add("Save");

            if (FailSaves)
            {
                return SaveOutcome.Failure("the fake store was told to fail");
            }

            stored = runState;
            LastSaved = runState;
            return SaveOutcome.Ok();
        }

        public SaveOutcome Delete()
        {
            DeleteCount++;
            Calls.Add("Delete");
            stored = null;
            return SaveOutcome.Ok();
        }
    }
}
