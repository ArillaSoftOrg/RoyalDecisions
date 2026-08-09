using RoyalDecisions.Domain;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// What happened when a run save was read, and the run itself when there is one.
    /// </summary>
    public readonly struct RunLoadResult
    {
        private RunLoadResult(LoadStatus status, RunState runState, string message)
        {
            Status = status;
            RunState = runState;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// A run that has already been sanitized. Callers cannot obtain an unrepaired state.
        /// </summary>
        public static RunLoadResult Loaded(LoadStatus status, RunState runState)
        {
            return new RunLoadResult(status, runState, string.Empty);
        }

        public static RunLoadResult Failure(LoadStatus status, string message)
        {
            return new RunLoadResult(status, null, message);
        }

        public LoadStatus Status { get; }

        public RunState RunState { get; }

        public string Message { get; }

        public bool HasRun => RunState != null;

        public bool Succeeded =>
            Status == LoadStatus.Success
            || Status == LoadStatus.SuccessAfterRepair
            || Status == LoadStatus.RecoveredFromBackup;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Message)
                ? Status.ToString()
                : Status + ": " + Message;
        }
    }
}
