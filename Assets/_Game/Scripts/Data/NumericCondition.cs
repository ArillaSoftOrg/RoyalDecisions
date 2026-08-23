using System;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// A single numeric comparison against a stat, a story counter, or leader health.
    /// </summary>
    /// <remarks>
    /// Data only: evaluating a condition against a run requires <c>RunState</c>, which the Data
    /// layer cannot depend on. See <c>ConditionEvaluator.EvaluateNumeric</c> in the Domain layer.
    /// </remarks>
    [Serializable]
    public sealed class NumericCondition
    {
        [SerializeField] private NumericSource source = NumericSource.Stat;
        [SerializeField] private StatType stat = StatType.Authority;
        [SerializeField] private string counterId = string.Empty;
        [SerializeField] private NumericComparison comparison = NumericComparison.Always;
        [SerializeField] private int threshold;

        public NumericCondition()
        {
        }

        public NumericCondition(NumericSource source, NumericComparison comparison, int threshold,
            StatType stat = StatType.Authority, string counterId = "")
        {
            this.source = source;
            this.comparison = comparison;
            this.threshold = threshold;
            this.stat = stat;
            this.counterId = counterId ?? string.Empty;
        }

        /// <summary>Builds an always-true condition, used where an effect has no real gate.</summary>
        public static NumericCondition Always()
        {
            return new NumericCondition(NumericSource.Stat, NumericComparison.Always, 0);
        }

        public NumericSource Source => source;

        public StatType Stat => stat;

        public string CounterId => counterId ?? string.Empty;

        public NumericComparison Comparison => comparison;

        public int Threshold => threshold;
    }
}
