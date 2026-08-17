namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// How strongly a haptic pulse should read to the player. Lets <see cref="IHapticService"/>
    /// give a light drag-threshold tick a different feel from a critical-stat or game-over event
    /// instead of firing the same buzz for everything.
    /// </summary>
    public enum HapticFeedbackLevel
    {
        /// <summary>Subtle UI feedback, e.g. a drag crossing the confirmation threshold.</summary>
        Light,

        /// <summary>Ordinary gameplay feedback, e.g. a decision being confirmed.</summary>
        Standard,

        /// <summary>A stat entering its critical range, or the run ending.</summary>
        Critical
    }
}
