namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// What happened when a save was written.
    /// </summary>
    public readonly struct SaveResult
    {
        private SaveResult(SaveStatus status, string message)
        {
            Status = status;
            Message = message ?? string.Empty;
        }

        public static SaveResult Success()
        {
            return new SaveResult(SaveStatus.Success, string.Empty);
        }

        public static SaveResult Failure(SaveStatus status, string message)
        {
            return new SaveResult(status, message);
        }

        public SaveStatus Status { get; }

        public string Message { get; }

        public bool Succeeded => Status == SaveStatus.Success;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Message)
                ? Status.ToString()
                : Status + ": " + Message;
        }
    }
}
