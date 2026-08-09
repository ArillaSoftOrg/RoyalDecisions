using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Describes a single statistic moving from one value to another.
    /// </summary>
    /// <remarks>
    /// A struct payload rather than loose event arguments so a subscriber cannot transpose
    /// "previous" and "current" at the call site.
    /// </remarks>
    public readonly struct StatChange
    {
        public StatChange(StatType stat, int previous, int current)
        {
            Stat = stat;
            Previous = previous;
            Current = current;
        }

        public StatType Stat { get; }

        public int Previous { get; }

        public int Current { get; }

        /// <summary>The realised change, after clamping — not the delta the choice asked for.</summary>
        public int Delta => Current - Previous;

        public override string ToString()
        {
            return string.Format("{0} {1} -> {2}", Stat, Previous, Current);
        }
    }
}
