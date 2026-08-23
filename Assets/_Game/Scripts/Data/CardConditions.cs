using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// Eligibility requirements a run must satisfy before a card can be drawn.
    /// Evaluation lives in ConditionEvaluator; this type only stores the authored data.
    /// </summary>
    [Serializable]
    public class CardConditions
    {
        [Tooltip("Every flag listed here must be present on the run.")]
        [SerializeField] private string[] requiredFlags = Array.Empty<string>();

        [Tooltip("The card is ineligible if any flag listed here is present on the run.")]
        [SerializeField] private string[] forbiddenFlags = Array.Empty<string>();

        [Tooltip("Every range listed here must contain the run's current value for that stat.")]
        [SerializeField] private StatRange[] statRanges = Array.Empty<StatRange>();

        [Tooltip("Every numeric condition listed here (counters, leader health) must hold.")]
        [SerializeField] private NumericCondition[] numericConditions = Array.Empty<NumericCondition>();

        public CardConditions()
        {
        }

        public CardConditions(string[] requiredFlags, string[] forbiddenFlags, StatRange[] statRanges,
            NumericCondition[] numericConditions = null)
        {
            this.requiredFlags = requiredFlags ?? Array.Empty<string>();
            this.forbiddenFlags = forbiddenFlags ?? Array.Empty<string>();
            this.statRanges = statRanges ?? Array.Empty<StatRange>();
            this.numericConditions = numericConditions ?? Array.Empty<NumericCondition>();
        }

        public IReadOnlyList<string> RequiredFlags => requiredFlags ?? Array.Empty<string>();

        public IReadOnlyList<string> ForbiddenFlags => forbiddenFlags ?? Array.Empty<string>();

        public IReadOnlyList<StatRange> StatRanges => statRanges ?? Array.Empty<StatRange>();

        public IReadOnlyList<NumericCondition> NumericConditions =>
            numericConditions ?? Array.Empty<NumericCondition>();

        /// <summary>True when the card places no restriction on the run at all.</summary>
        public bool IsEmpty =>
            RequiredFlags.Count == 0 && ForbiddenFlags.Count == 0 && StatRanges.Count == 0
            && NumericConditions.Count == 0;
    }
}
