namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Run-level constants. Stat bounds deliberately live in
    /// <see cref="RoyalDecisions.Data.StatBounds"/> instead, because content authoring needs them
    /// and the Data layer cannot reference Domain.
    /// </summary>
    public static class GameConstants
    {
        /// <summary>
        /// Schema version stamped into every <see cref="RunState"/>. Increment whenever the
        /// serialised shape changes; SaveService rejects versions it does not understand.
        /// </summary>
        public const int CurrentSaveVersion = 1;

        /// <summary>Turn number of a new run, before its first decision is resolved.</summary>
        public const int FirstTurn = 0;

        /// <summary>The first leader of a new run.</summary>
        public const int FirstReignNumber = 1;

        /// <summary>
        /// The value a statistic is reset to when it triggers a reign succession rather than a
        /// game-over ending. Fixed by design, not authored per card: see
        /// <see cref="RoyalDecisions.Data.ConditionalChoiceEffect"/>.
        /// </summary>
        public const int ReignSuccessionResetStatValue = 3;
    }
}
