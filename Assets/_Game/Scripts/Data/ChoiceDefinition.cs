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

        public ChoiceDefinition()
        {
        }

        public ChoiceDefinition(
            string previewText,
            StatDeltas deltas,
            string[] flagsToAdd = null,
            string[] flagsToRemove = null,
            string forcedNextCardId = "",
            string audioEventId = "")
        {
            this.previewText = previewText ?? string.Empty;
            this.deltas = deltas;
            this.flagsToAdd = flagsToAdd ?? Array.Empty<string>();
            this.flagsToRemove = flagsToRemove ?? Array.Empty<string>();
            this.forcedNextCardId = forcedNextCardId ?? string.Empty;
            this.audioEventId = audioEventId ?? string.Empty;
        }

        public string PreviewText => previewText ?? string.Empty;

        public StatDeltas Deltas => deltas;

        public IReadOnlyList<string> FlagsToAdd => flagsToAdd ?? Array.Empty<string>();

        public IReadOnlyList<string> FlagsToRemove => flagsToRemove ?? Array.Empty<string>();

        public string ForcedNextCardId => forcedNextCardId ?? string.Empty;

        public string AudioEventId => audioEventId ?? string.Empty;

        public bool HasForcedNextCard => !string.IsNullOrEmpty(forcedNextCardId);

        public bool HasAudioEvent => !string.IsNullOrEmpty(audioEventId);
    }
}
