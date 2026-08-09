using System;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The four current statistic values of a run, always inside
    /// <see cref="StatBounds.Min"/>..<see cref="StatBounds.Max"/>.
    /// </summary>
    /// <remarks>
    /// Immutable: every operation returns a new value rather than mutating in place, so a view or
    /// controller holding a copy can never change the run. StatSystem owns transitions and change
    /// events.
    /// </remarks>
    [Serializable]
    public struct StatValues
    {
        [SerializeField] private int authority;
        [SerializeField] private int people;
        [SerializeField] private int security;
        [SerializeField] private int wealth;

        public StatValues(int authority, int people, int security, int wealth)
        {
            this.authority = Clamp(authority);
            this.people = Clamp(people);
            this.security = Clamp(security);
            this.wealth = Clamp(wealth);
        }

        /// <summary>The starting position of every new run: all four stats at the midpoint.</summary>
        public static StatValues CreateInitial()
        {
            return new StatValues(
                StatBounds.Initial,
                StatBounds.Initial,
                StatBounds.Initial,
                StatBounds.Initial);
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

        /// <summary>Applies a choice's deltas, clamping each stat to its legal range.</summary>
        public StatValues WithDelta(StatDeltas deltas)
        {
            return new StatValues(
                authority + deltas.Authority,
                people + deltas.People,
                security + deltas.Security,
                wealth + deltas.Wealth);
        }

        /// <summary>Replaces a single stat, clamping it to its legal range.</summary>
        public StatValues With(StatType stat, int value) => stat switch
        {
            StatType.Authority => new StatValues(value, people, security, wealth),
            StatType.People => new StatValues(authority, value, security, wealth),
            StatType.Security => new StatValues(authority, people, value, wealth),
            StatType.Wealth => new StatValues(authority, people, security, value),
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "Unknown stat type.")
        };

        public bool IsAtMin(StatType stat)
        {
            return this[stat] <= StatBounds.Min;
        }

        public bool IsAtMax(StatType stat)
        {
            return this[stat] >= StatBounds.Max;
        }

        /// <summary>
        /// Re-clamps every stat. JSON deserialisation writes the backing fields directly and so
        /// bypasses the constructor; SaveService calls this on load to contain a hand-edited or
        /// corrupted file rather than letting an out-of-range stat reach gameplay.
        /// </summary>
        public StatValues Sanitized()
        {
            return new StatValues(authority, people, security, wealth);
        }

        public override string ToString()
        {
            return string.Format(
                "A:{0} P:{1} S:{2} W:{3}",
                authority,
                people,
                security,
                wealth);
        }

        private static int Clamp(int value)
        {
            return Mathf.Clamp(value, StatBounds.Min, StatBounds.Max);
        }
    }
}
