using System;
using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Adapts the concrete settings service to the application's settings port.
    /// </summary>
    public sealed class SettingsServiceStore : ISettingsStore
    {
        private readonly SettingsSaveService settingsService;

        public SettingsServiceStore(SettingsSaveService settingsService)
        {
            this.settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
        }

        /// <summary>Never fails: unreadable preferences resolve to defaults.</summary>
        public GameSettings Load()
        {
            return settingsService.Load();
        }

        public SaveOutcome Save(GameSettings settings)
        {
            SaveResult result = settingsService.Save(settings);

            return result.Succeeded
                ? SaveOutcome.Ok()
                : SaveOutcome.Failure(result.Status + ": " + result.Message);
        }
    }
}
