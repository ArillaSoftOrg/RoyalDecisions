namespace RoyalDecisions.Application
{
    /// <summary>
    /// The outcome of a session command.
    /// </summary>
    /// <remarks>
    /// Commands that are invalid for the current state return <c>Accepted == false</c> rather than
    /// throwing, so "the event was ignored" is something a test can assert instead of something it
    /// has to infer from an absence.
    /// </remarks>
    public readonly struct SessionResult
    {
        private SessionResult(bool accepted, GameSessionState state, SessionError error)
        {
            Accepted = accepted;
            State = state;
            Error = error;
        }

        public static SessionResult Ok(GameSessionState state)
        {
            return new SessionResult(true, state, SessionError.None);
        }

        /// <summary>The command was not valid here; nothing changed.</summary>
        public static SessionResult Rejected(
            GameSessionState state,
            SessionErrorCode code,
            string message)
        {
            return new SessionResult(false, state, SessionError.Recoverable(code, message));
        }

        /// <summary>The command was valid but failed; the session moved to an error state.</summary>
        public static SessionResult Failed(GameSessionState state, SessionError error)
        {
            return new SessionResult(false, state, error);
        }

        public bool Accepted { get; }

        public GameSessionState State { get; }

        public SessionError Error { get; }

        public override string ToString()
        {
            return Accepted ? "Accepted (" + State + ")" : "Rejected (" + State + ") " + Error;
        }
    }
}
