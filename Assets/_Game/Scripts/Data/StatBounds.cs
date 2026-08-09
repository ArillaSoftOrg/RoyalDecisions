namespace RoyalDecisions.Data
{
    /// <summary>
    /// Legal range of every statistic, shared by content authoring and by runtime clamping.
    /// </summary>
    /// <remarks>
    /// These live in the Data layer rather than the Domain layer because the dependency direction
    /// is Domain -> Data: content conditions authored against these bounds cannot reach upwards
    /// into Domain for them.
    /// </remarks>
    public static class StatBounds
    {
        public const int Min = 0;
        public const int Max = 100;

        /// <summary>Value every statistic starts a new run at.</summary>
        public const int Initial = 50;
    }
}
