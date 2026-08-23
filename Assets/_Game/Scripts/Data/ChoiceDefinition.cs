using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// One of the two outcomes of a card: what the player sees while dragging, and what the
    /// decision does to the run once confirmed.
    /// </summary>
    [Serializable]
    public class ChoiceDefinition
    {
        [Tooltip("Short label faded in while the player drags towards this side.")]
        [SerializeField] private string previewText = string.Empty;

        [SerializeField] private StatDeltas deltas;

        [SerializeField] private string[] flagsToAdd = Array.Empty<string>();

        [SerializeField] private string[] flagsToRemove = Array.Empty<string>();

        [Tooltip("Optional card ID drawn next, bypassing normal selection.")]
        [SerializeField] private string forcedNextCardId = string.Empty;

        [Tooltip("Optional audio event ID; missing or unmapped IDs fall back to silence.")]
        [SerializeField] private string audioEventId = string.Empty;

        [Tooltip("Counters this choice increments or decrements, such as an investigation level.")]
        [SerializeField] private CounterDelta[] counterDeltas = Array.Empty<CounterDelta>();

        [Tooltip("Optional. When set, this replaces Deltas with a branch on a numeric condition.")]
        [SerializeField] private ConditionalChoiceEffect conditionalEffect;

        [Tooltip("Optional. When set, one of several deltas is picked at random on top of Deltas.")]
        [SerializeField] private RandomStatOutcome randomOutcome;

        [Tooltip("Optional. When set, this side is unavailable unless these conditions hold.")]
        [SerializeField] private CardConditions availability;

        public ChoiceDefinition()
        {
        }

        public ChoiceDefinition(
            string previewText,
            StatDeltas deltas,
            string[] flagsToAdd = null,
            string[] flagsToRemove = null,
            string forcedNextCardId = "",
            string audioEventId = "",
            CounterDelta[] counterDeltas = null,
            ConditionalChoiceEffect conditionalEffect = null,
            RandomStatOutcome randomOutcome = null,
            CardConditions availability = null)
        {
            this.previewText = previewText ?? string.Empty;
            this.deltas = deltas;
            this.flagsToAdd = flagsToAdd ?? Array.Empty<string>();
            this.flagsToRemove = flagsToRemove ?? Array.Empty<string>();
            this.forcedNextCardId = forcedNextCardId ?? string.Empty;
            this.audioEventId = audioEventId ?? string.Empty;
            this.counterDeltas = counterDeltas ?? Array.Empty<CounterDelta>();
            this.conditionalEffect = conditionalEffect;
            this.randomOutcome = randomOutcome;
            this.availability = availability;
        }

        public string PreviewText => previewText ?? string.Empty;

        public StatDeltas Deltas => deltas;

        public IReadOnlyList<string> FlagsToAdd => flagsToAdd ?? Array.Empty<string>();

        public IReadOnlyList<string> FlagsToRemove => flagsToRemove ?? Array.Empty<string>();

        public string ForcedNextCardId => forcedNextCardId ?? string.Empty;

        public string AudioEventId => audioEventId ?? string.Empty;

        public IReadOnlyList<CounterDelta> CounterDeltas => counterDeltas ?? Array.Empty<CounterDelta>();

        public ConditionalChoiceEffect ConditionalEffect => conditionalEffect;

        public RandomStatOutcome RandomOutcome => randomOutcome;

        /// <summary>Null means always available.</summary>
        public CardConditions Availability => availability;

        public bool HasAvailabilityCondition => availability != null && !availability.IsEmpty;

        public bool HasForcedNextCard => !string.IsNullOrEmpty(forcedNextCardId);

        public bool HasAudioEvent => !string.IsNullOrEmpty(audioEventId);

        public bool HasConditionalEffect => conditionalEffect != null;

        public bool HasRandomOutcome => randomOutcome != null && randomOutcome.HasOptions;
    }
}
