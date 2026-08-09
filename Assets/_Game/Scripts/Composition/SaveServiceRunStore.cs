using System;
using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Adapts the concrete save service to the application's persistence port.
    /// </summary>
    /// <remarks>
    /// This mapping is the price of keeping the application layer free of Infrastructure types, and
    /// it is deliberately the only place the two vocabularies meet. Infrastructure itself is
    /// untouched by Phase 7.
    /// </remarks>
    public sealed class SaveServiceRunStore : IRunSaveStore
    {
        private readonly SaveService saveService;

        public SaveServiceRunStore(SaveService saveService)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public bool HasSave()
        {
            return saveService.HasSave();
        }

        public RunLoadOutcome Load()
        {
            RunLoadResult result = saveService.Load();
            RunLoadStatus status = Map(result.Status);

            if (result.HasRun)
            {
                return RunLoadOutcome.Loaded(status, result.RunState);
            }

            return RunLoadOutcome.Failure(status, result.Message);
        }

        public SaveOutcome Save(RunState runState)
        {
            SaveResult result = saveService.Save(runState);

            return result.Succeeded
                ? SaveOutcome.Ok()
                : SaveOutcome.Failure(result.Status + ": " + result.Message);
        }

        public SaveOutcome Delete()
        {
            SaveResult result = saveService.Delete();

            return result.Succeeded
                ? SaveOutcome.Ok()
                : SaveOutcome.Failure(result.Status + ": " + result.Message);
        }

        /// <summary>
        /// An empty file maps to Corrupt rather than to "no save": it exists, it is unusable, and
        /// it must not be deleted on the player's behalf.
        /// </summary>
        private static RunLoadStatus Map(LoadStatus status)
        {
            switch (status)
            {
                case LoadStatus.Success:
                    return RunLoadStatus.Success;

                case LoadStatus.SuccessAfterRepair:
                    return RunLoadStatus.SuccessAfterRepair;

                case LoadStatus.RecoveredFromBackup:
                    return RunLoadStatus.RecoveredFromBackup;

                case LoadStatus.NoSaveFile:
                    return RunLoadStatus.NoSave;

                case LoadStatus.UnsupportedVersion:
                    return RunLoadStatus.UnsupportedVersion;

                case LoadStatus.ReadFailed:
                    return RunLoadStatus.ReadFailed;

                default:
                    return RunLoadStatus.Corrupt;
            }
        }
    }
}
