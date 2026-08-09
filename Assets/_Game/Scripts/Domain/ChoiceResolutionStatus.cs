namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Outcome of asking <see cref="ChoiceResolver"/> to apply a choice.
    /// </summary>
    public enum ChoiceResolutionStatus
    {
        /// <summary>The choice was applied to the run exactly once.</summary>
        Applied = 0,

        /// <summary>The card was null or carried no ID.</summary>
        InvalidCard = 1,

        /// <summary>The run has already ended.</summary>
        RunNotActive = 2,

        /// <summary>
        /// No card is awaiting a decision. This is what a second resolve of the same card returns,
        /// because a successful resolve clears the token.
        /// </summary>
        NoActiveCard = 3,

        /// <summary>A card is awaiting a decision, but not the one that was passed in.</summary>
        CardMismatch = 4
    }
}
