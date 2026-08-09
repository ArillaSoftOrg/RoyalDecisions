using System;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Per-view configuration for what to show when content supplies no sprite.
    /// </summary>
    /// <remarks>
    /// Serialized rather than hard-coded so the team can swap placeholder treatment for final art
    /// without a code change — every placeholder card and ending currently ships with no image at
    /// all, so this is the path the game actually takes today.
    /// </remarks>
    [Serializable]
    public sealed class GraphicFallbackSettings
    {
        [Tooltip("Shown when the content has no sprite. Optional.")]
        [SerializeField] private Sprite fallbackSprite;

        [Tooltip("Flat colour shown when there is no sprite at all.")]
        [SerializeField] private Color fallbackColour = new Color(0.22f, 0.22f, 0.26f, 1f);

        [Tooltip("When off, a graphic with no sprite is disabled instead of filled with colour.")]
        [SerializeField] private bool useFallbackColour = true;

        public GraphicFallbackSettings()
        {
        }

        public GraphicFallbackSettings(Sprite fallbackSprite, Color fallbackColour, bool useFallbackColour)
        {
            this.fallbackSprite = fallbackSprite;
            this.fallbackColour = fallbackColour;
            this.useFallbackColour = useFallbackColour;
        }

        public Sprite FallbackSprite => fallbackSprite;

        public Color FallbackColour => fallbackColour;

        public bool UseFallbackColour => useFallbackColour;
    }
}
