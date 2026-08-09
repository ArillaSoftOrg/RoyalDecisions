namespace RoyalDecisions.Application
{
    /// <summary>
    /// What went wrong, and whether the session can come back from it.
    /// </summary>
    /// <remarks>
    /// The message getter coalesces rather than relying on the constructor, because
    /// <see cref="None"/> is <c>default</c> and never runs one.
    /// </remarks>
    public readonly struct SessionError
    {
        private readonly string message;

        private SessionError(SessionErrorCode code, string message, bool isRecoverable)
        {
            Code = code;
            this.message = message;
            IsRecoverable = isRecoverable;
        }

        public static SessionError None => default;

        /// <summary>The session can retry or continue once the cause is addressed.</summary>
        public static SessionError Recoverable(SessionErrorCode code, string message)
        {
            return new SessionError(code, message, true);
        }

        /// <summary>The run cannot continue. Only a restart escapes.</summary>
        public static SessionError Terminal(SessionErrorCode code, string message)
        {
            return new SessionError(code, message, false);
        }

        public SessionErrorCode Code { get; }

        public string Message => message ?? string.Empty;

        public bool IsRecoverable { get; }

        public bool HasError => Code != SessionErrorCode.None;

        public override string ToString()
        {
            return HasError ? Code + ": " + Message : "None";
        }
    }
}
