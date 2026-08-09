using System;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// One audio event ID paired with the clip it plays.
    /// </summary>
    [Serializable]
    public sealed class AudioCue
    {
        [Tooltip("Matches ChoiceDefinition.audioEventId exactly, compared ordinally.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Optional. An empty slot is a silent cue, not an error.")]
        [SerializeField] private AudioClip clip;

        public AudioCue()
        {
        }

        public AudioCue(string id, AudioClip clip)
        {
            this.id = id ?? string.Empty;
            this.clip = clip;
        }

        public string Id => id ?? string.Empty;

        public AudioClip Clip => clip;
    }
}
