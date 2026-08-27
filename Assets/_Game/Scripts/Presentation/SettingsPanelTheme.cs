using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Shared tab-tint colours for the Settings panel. Kept as one source of truth so the Editor
    /// scene authoring (initial state) and <see cref="SettingsPanelView"/> (runtime tab switching)
    /// never drift apart.
    /// </summary>
    /// <remarks>
    /// A warm gold/rust royal palette, scoped to MainMenu + Settings + About only — matching the
    /// game's own actual branding (see <c>GameUITheme</c>'s gold/navy palette in the Game scene)
    /// rather than a generic neutral theme, since "Royal Decisions" already has a visual identity
    /// worth being consistent with. <c>SceneSetupAutomation</c> still decouples every place that
    /// used to read these values by reference (<c>ButtonColour</c>, the tap-choice-button fill) so
    /// none of this bleeds into gameplay — the two palettes are similar in spirit but kept as
    /// separate constants on purpose.
    /// </remarks>
    public static class SettingsPanelTheme
    {
        /// <summary>The single accent colour — the CTA/active-state colour across MainMenu and Settings.</summary>
        public static readonly Color ActiveTabColour = new Color32(0xB5, 0x58, 0x1A, 0xFF);

        /// <summary>Warm dark-brown card/surface tone for inactive tabs and control tracks.</summary>
        public static readonly Color InactiveTabColour = new Color32(0x1E, 0x18, 0x12, 0xFF);

        /// <summary>Near-white cream text on the accent fill.</summary>
        public static readonly Color ActiveTabTextColour = new Color32(0xF2, 0xE7, 0xCF, 0xFF);

        /// <summary>Muted warm tan text on the dark inactive fill.</summary>
        public static readonly Color InactiveTabTextColour = new Color32(0xD9, 0xC9, 0xA8, 0xFF);

        /// <summary>
        /// Marks a destructive, irreversible action (Reset Progress) as visually distinct from the
        /// ordinary settings around it — a clean red rather than the accent colour used for every
        /// other button, so it reads as dangerous without introducing a second style.
        /// </summary>
        public static readonly Color DangerColour = new Color32(0x6B, 0x1A, 0x1A, 0xFF);

        /// <summary>Light text on the dark danger fill.</summary>
        public static readonly Color DangerTextColour = Color.white;

        /// <summary>
        /// A lighter, legible red for danger-tinted text sitting directly on the panel background
        /// (e.g. a destructive-action caption) — <see cref="DangerColour"/> is tuned as a button
        /// fill behind white text and reads as too dark/low-contrast on its own.
        /// </summary>
        public static readonly Color DangerAccentColour = new Color32(0xD9, 0x70, 0x5A, 0xFF);

        /// <summary>
        /// The gold ring colour used around the slider handle — the ornate-royal counterpart to the
        /// flat panels above.
        /// </summary>
        public static readonly Color BorderGoldColour = new Color32(0xB5, 0x8A, 0x4A, 0xFF);
    }
}
