namespace RoyalDecisions.Data
{
    /// <summary>
    /// Which end of a statistic's range triggers an ending.
    /// </summary>
    /// <remarks>
    /// Serialised by integer value in ending assets — append new members only. See
    /// <see cref="StatType"/> for the same constraint.
    /// </remarks>
    public enum StatBoundary
    {
        Min = 0,
        Max = 1
    }
}
