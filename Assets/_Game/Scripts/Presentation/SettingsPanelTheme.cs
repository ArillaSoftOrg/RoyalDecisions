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

        /// <summary>
        /// Marks a destructive, irreversible action (Reset Progress) as visually distinct from the
        /// ordinary settings around it — a muted red rather than the gold used for every other
        /// button, so it reads as dangerous without introducing a whole second button style.
        /// </summary>
        public static readonly Color DangerColour = new Color32(0x7A, 0x22, 0x22, 0xFF);

        /// <summary>Light text on the dark danger fill.</summary>
        public static readonly Color DangerTextColour = Color.white;

        /// <summary>
        /// A lighter, legible red for danger-tinted text sitting directly on the panel background
        /// (e.g. the "Tehlikeli İşlemler" section caption) — <see cref="DangerColour"/> is tuned as
        /// a button fill behind white text and reads as too dark/low-contrast on its own.
        /// </summary>
        public static readonly Color DangerAccentColour = new Color32(0xE0, 0x7A, 0x7A, 0xFF);
    }
}
