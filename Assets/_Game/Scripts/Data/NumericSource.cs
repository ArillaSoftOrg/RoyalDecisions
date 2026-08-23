namespace RoyalDecisions.Data
{
    /// <summary>
    /// What value a <see cref="NumericCondition"/> reads before comparing it to a threshold.
    /// </summary>
    public enum NumericSource
    {
        /// <summary>One of the four core statistics, named by <see cref="StatType"/>.</summary>
        Stat = 0,

        /// <summary>A named story counter, such as an investigation level. Zero when never touched.</summary>
        Counter = 1,

        /// <summary>The current leader's health (see <see cref="LeaderHealthBounds"/>).</summary>
        LeaderHealth = 2
    }
}
