using System;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// The change a single choice applies to each of the four statistics.
    /// </summary>
    /// <remarks>
    /// Modelled as four fixed fields rather than a list of (stat, amount) pairs so that a stat can
    /// never be duplicated or omitted by content authoring, and so the Inspector shows a stable
    /// four-row layout.
    /// </remarks>
    [Serializable]
    public struct StatDeltas
    {
        [SerializeField] private int authority;
        [SerializeField] private int people;
        [SerializeField] private int security;
        [SerializeField] private int wealth;

        public StatDeltas(int authority, int people, int security, int wealth)
        {
            this.authority = authority;
            this.people = people;
            this.security = security;
            this.wealth = wealth;
        }

        public int Authority => authority;

        public int People => people;

        public int Security => security;

        public int Wealth => wealth;

        public int this[StatType stat] => stat switch
        {
            StatType.Authority => authority,
            StatType.People => people,
            StatType.Security => security,
            StatType.Wealth => wealth,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "Unknown stat type.")
        };

        /// <summary>True when this choice leaves every statistic untouched.</summary>
        public bool IsEmpty => authority == 0 && people == 0 && security == 0 && wealth == 0;
    }
}
