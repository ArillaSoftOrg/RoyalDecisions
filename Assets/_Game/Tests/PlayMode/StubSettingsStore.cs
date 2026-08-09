using RoyalDecisions.Application;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>An in-memory settings store for scene tests.</summary>
    public sealed class StubSettingsStore : ISettingsStore
    {
        private GameSettings stored;

        public GameSettings Load()
        {
            return stored ?? GameSettings.CreateDefault();
        }

        public SaveOutcome Save(GameSettings settings)
        {
            stored = settings;
            return SaveOutcome.Ok();
        }
    }
}
