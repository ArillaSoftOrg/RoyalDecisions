namespace RoyalDecisions.Data
{
    /// <summary>
    /// Legal range of leader health: a fifth, narrower measure tracked alongside the four core
    /// statistics for story content that models a "reign" a leader can die out of.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="StatBounds"/> and <see cref="StatType"/> rather than added as
    /// a fifth core stat: the four core stats are explicitly locked by CLAUDE.md, and leader health
    /// resets on succession independently of them (see <c>RunState.ReignNumber</c>).
    /// </remarks>
    public static class LeaderHealthBounds
    {
        public const int Min = 0;
        public const int Max = 10;

        /// <summary>Value a new leader's health starts, and resets to, at.</summary>
        public const int Initial = 10;

        /// <summary>
        /// Below this, a "leader risk" choice that would normally cost health instead ends the
        /// reign outright.
        /// </summary>
        public const int CriticalThreshold = 5;
    }
}
