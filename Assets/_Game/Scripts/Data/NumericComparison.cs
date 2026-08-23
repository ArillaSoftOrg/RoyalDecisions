namespace RoyalDecisions.Data
{
    /// <summary>How a <see cref="NumericCondition"/> compares its source value to its threshold.</summary>
    public enum NumericComparison
    {
        /// <summary>Unconditional. The threshold and source are ignored.</summary>
        Always = 0,

        LessThan = 1,
        LessOrEqual = 2,
        GreaterThan = 3,
        GreaterOrEqual = 4
    }
}
