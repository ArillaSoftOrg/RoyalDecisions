using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Shared tab-tint colours for the Settings panel. Kept as one source of truth so the Editor
    /// scene authoring (initial state) and <see cref="SettingsPanelView"/> (runtime tab switching)
    /// never drift apart.
    /// </summary>
    public static class SettingsPanelTheme
    {
        /// <summary>Matches the existing CTA/button gold used across MainMenu and Settings.</summary>
        public static readonly Color ActiveTabColour = new Color(0.78f, 0.58f, 0.18f, 1f);

        /// <summary>Matches the existing HUD stat-background dark tone.</summary>
        public static readonly Color InactiveTabColour = new Color32(0x2A, 0x2F, 0x3A, 0xFF);

        /// <summary>Dark text on the gold fill — white-on-gold reads as low-contrast.</summary>
        public static readonly Color ActiveTabTextColour = new Color32(0x2A, 0x1C, 0x08, 0xFF);

        /// <summary>Light text on the dark inactive fill.</summary>
        public static readonly Color InactiveTabTextColour = Color.white;
    }
}
