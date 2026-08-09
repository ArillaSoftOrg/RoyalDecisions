using System;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// An inclusive range a statistic must fall inside for a card to be eligible.
    /// Expresses conditions such as "people &lt;= 25" as a range of 0..25.
    /// </summary>
    /// <remarks>
    /// This is a class rather than a struct specifically so the field initialisers below run when
    /// an author adds an element in the Inspector. A struct would zero-fill, silently producing a
    /// 0..0 range that means "this stat must be exactly zero" instead of "unrestricted".
    /// </remarks>
    [Serializable]
    public class StatRange
    {
        [SerializeField] private StatType stat = StatType.Authority;
        [SerializeField] private int min = StatBounds.Min;
        [SerializeField] private int max = StatBounds.Max;

        public StatRange()
        {
        }

        public StatRange(StatType stat, int min, int max)
        {
            this.stat = stat;
            this.min = min;
            this.max = max;
        }

        public StatType Stat => stat;

        public int Min => min;

        public int Max => max;

        public bool Contains(int value)
        {
            return value >= min && value <= max;
        }

        public override string ToString()
        {
            return string.Format("{0} in {1}..{2}", stat, min, max);
        }
    }
}
