using RoyalDecisions.Application;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>An in-memory settings store that never fails.</summary>
    public sealed class FakeSettingsStore : ISettingsStore
    {
        private GameSettings stored;

        public int SaveCount { get; private set; }

        public GameSettings Load()
        {
            return stored ?? GameSettings.CreateDefault();
        }

        public SaveOutcome Save(GameSettings settings)
        {
            SaveCount++;
            stored = settings;
            return SaveOutcome.Ok();
        }
    }
}
