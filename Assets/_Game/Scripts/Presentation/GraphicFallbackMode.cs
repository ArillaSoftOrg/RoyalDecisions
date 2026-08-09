namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// What a view should do about an image whose sprite may be missing.
    /// </summary>
    public enum GraphicFallbackMode
    {
        /// <summary>The content supplied a sprite; show it.</summary>
        UseSource = 0,

        /// <summary>No content sprite, but a fallback sprite is configured.</summary>
        UseFallbackSprite = 1,

        /// <summary>No sprite at all; show a flat configured colour so the slot stays visible.</summary>
        UseFallbackColour = 2,

        /// <summary>Nothing to show; disable the graphic entirely.</summary>
        HideGraphic = 3
    }
}
