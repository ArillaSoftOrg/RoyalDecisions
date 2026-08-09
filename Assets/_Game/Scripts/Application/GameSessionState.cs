namespace RoyalDecisions.Application
{
    /// <summary>
    /// Where a play session is in its lifecycle.
    /// </summary>
    /// <remarks>
    /// The transient states — <see cref="Loading"/>, <see cref="PresentingCard"/> and
    /// <see cref="ResolvingDecision"/> — accept no commands at all. That is deliberate: a callback
    /// that fires while one of them is active lands somewhere that rejects it, which is what stops
    /// re-entrant restarts, duplicate saves and overlapping selections.
    /// </remarks>
    public enum GameSessionState
    {
        /// <summary>No run yet. Accepts StartNewGame and Resume.</summary>
        Uninitialized = 0,

        /// <summary>Creating or restoring a run. Transient.</summary>
        Loading = 1,

        /// <summary>Selecting and rendering a card. Transient.</summary>
        PresentingCard = 2,

        /// <summary>A card is armed and input is live.</summary>
        AwaitingDecision = 3,

        /// <summary>Applying a decision, evaluating the ending, and saving. Transient.</summary>
        ResolvingDecision = 4,

        /// <summary>Gameplay is resolved and persisted; the card is still leaving the screen.</summary>
        WaitingForCardExit = 5,

        /// <summary>The run has ended and its ending is on screen.</summary>
        ShowingGameOver = 6,

        /// <summary>A save failed. Recoverable through RetrySave.</summary>
        PersistenceError = 7,

        /// <summary>Content is unusable for this run. Only Restart escapes.</summary>
        ContentError = 8
    }
}
