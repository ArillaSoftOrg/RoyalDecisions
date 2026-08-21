namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The player's frame-rate preference. Replaces the old boolean "high frame rate cap" toggle
    /// with a three-way choice; see <see cref="GameSettings"/> for how it combines with battery
    /// saver.
    /// </summary>
    public enum FrameRateMode
    {
        /// <summary>Matches the pre-existing default (the old toggle's "on" state).</summary>
        Sixty = 0,
        Thirty = 1,

        /// <summary>
        /// No project-side cap: the platform's own default cadence is used. This is Unity's real
        /// "no target frame rate" mode, not a fabricated device-performance heuristic.
        /// </summary>
        Auto = 2
    }
}
