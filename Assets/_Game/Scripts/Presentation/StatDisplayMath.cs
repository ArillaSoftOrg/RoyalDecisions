using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Turns statistic values into bar fill amounts.
    /// </summary>
    /// <remarks>
    /// Read-only by construction: it takes values and returns floats, so a display can never nudge a
    /// domain number on its way to the screen.
    /// </remarks>
    public static class StatDisplayMath
    {
        private const float Range = StatBounds.Max - StatBounds.Min;

        /// <summary>Maps a statistic value onto <c>0..1</c>, clamping anything out of range.</summary>
        public static float ToFill(int value)
        {
            return Mathf.Clamp01((value - StatBounds.Min) / Range);
        }

        public static float ToFill(StatValues values, StatType stat)
        {
            return ToFill(values[stat]);
        }

        /// <summary>
        /// How close a statistic is to either end of its range, as <c>0..1</c>: the midpoint gives
        /// <c>0</c> and both boundaries give <c>1</c>. Useful for warning colours; nothing in
        /// Phase 5 requires it to mean more than that.
        /// </summary>
        public static float BoundaryProximity(int value)
        {
            return Mathf.Abs(ToFill(value) - 0.5f) * 2f;
        }
    }
}
