using System;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// One full-screen illustration and subtitle in a <see cref="PrologueSequenceData"/>. Static
    /// content only — nothing here changes at runtime.
    /// </summary>
    [Serializable]
    public sealed class PrologueSlideData
    {
        [Tooltip("Optional. A missing illustration falls back to a plain dark frame rather than "
            + "blocking the slide.")]
        [SerializeField] private Sprite illustration;

        [TextArea(2, 6)]
        [SerializeField] private string subtitle = string.Empty;

        [SerializeField] private PrologueSlideMotion motion = PrologueSlideMotion.Zoom;

        [Tooltip("Optional. Seconds after this slide is fully shown before it auto-advances. Zero "
            + "(the default) disables auto-advance — the player must tap to continue.")]
        [SerializeField] private float autoAdvanceSeconds;

        [Tooltip("Optional. Matches an AudioCueLibrary entry exactly, compared ordinally — same "
            + "convention as ChoiceDefinition.audioEventId. Plays once, exactly when this slide "
            + "becomes visible. An empty ID is a silent slide, not an error.")]
        [SerializeField] private string accentCueId = string.Empty;

        public PrologueSlideData()
        {
        }

        public PrologueSlideData(
            Sprite illustration,
            string subtitle,
            PrologueSlideMotion motion = PrologueSlideMotion.Zoom,
            float autoAdvanceSeconds = 0f,
            string accentCueId = null)
        {
            this.illustration = illustration;
            this.subtitle = subtitle ?? string.Empty;
            this.motion = motion;
            this.autoAdvanceSeconds = autoAdvanceSeconds;
            this.accentCueId = accentCueId ?? string.Empty;
        }

        public Sprite Illustration => illustration;

        public string Subtitle => subtitle ?? string.Empty;

        public PrologueSlideMotion Motion => motion;

        public float AutoAdvanceSeconds => Mathf.Max(0f, autoAdvanceSeconds);

        public bool HasAutoAdvance => AutoAdvanceSeconds > 0f;

        public string AccentCueId => accentCueId ?? string.Empty;

        public bool HasAccentCue => !string.IsNullOrEmpty(accentCueId);

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by <c>PrologueSequenceSetup</c> to swap in real artwork
        /// (or sync a not-yet-hand-tuned motion style) on an already-existing slide without
        /// disturbing its subtitle or any other field. Compiled out of player builds, same as
        /// <see cref="RoyalDecisions.Data.CardDefinition.SetAuthoringData"/>.
        /// </summary>
        public void SetIllustration(Sprite newIllustration)
        {
            illustration = newIllustration;
        }

        public void SetMotion(PrologueSlideMotion newMotion)
        {
            motion = newMotion;
        }

        public void SetAccentCueId(string newAccentCueId)
        {
            accentCueId = newAccentCueId ?? string.Empty;
        }
#endif
    }
}
