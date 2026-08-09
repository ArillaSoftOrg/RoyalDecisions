using RoyalDecisions.Domain;

namespace RoyalDecisions.Application
{
    /// <summary>
    /// The result of asking the save store for a run.
    /// </summary>
    public readonly struct RunLoadOutcome
    {
        private readonly string message;

        private RunLoadOutcome(RunLoadStatus status, RunState runState, string message)
        {
            Status = status;
            RunState = runState;
            this.message = message;
        }

        public static RunLoadOutcome Loaded(RunLoadStatus status, RunState runState)
        {
            return new RunLoadOutcome(status, runState, null);
        }

        public static RunLoadOutcome Failure(RunLoadStatus status, string message)
        {
            return new RunLoadOutcome(status, null, message);
        }

        public RunLoadStatus Status { get; }

        public RunState RunState { get; }

        public string Message => message ?? string.Empty;

        public bool HasRun => RunState != null;

        public bool Succeeded =>
            Status == RunLoadStatus.Success
            || Status == RunLoadStatus.SuccessAfterRepair
            || Status == RunLoadStatus.RecoveredFromBackup;

        /// <summary>Diagnostic only. A repaired save is never rewritten on load.</summary>
        public bool WasRepaired => Status == RunLoadStatus.SuccessAfterRepair;
    }
}
