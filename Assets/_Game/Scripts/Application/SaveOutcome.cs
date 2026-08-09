namespace RoyalDecisions.Application
{
    /// <summary>
    /// Whether a write reached the disk.
    /// </summary>
    public readonly struct SaveOutcome
    {
        private readonly string message;

        private SaveOutcome(bool succeeded, string message)
        {
            Succeeded = succeeded;
            this.message = message;
        }

        public static SaveOutcome Ok()
        {
            return new SaveOutcome(true, null);
        }

        public static SaveOutcome Failure(string message)
        {
            return new SaveOutcome(false, message);
        }

        public bool Succeeded { get; }

        public string Message => message ?? string.Empty;
    }
}
