using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Shared colour tokens for the Settings panel, the Main Menu and the About panel. Kept as one
    /// source of truth so the Editor scene authoring (initial state) and
    /// <see cref="SettingsPanelView"/> (runtime tab switching) never drift apart.
    /// </summary>
    /// <remarks>
    /// The post-apocalyptic retro palette from CLAUDE.md's UI/UX section: aged bronze and rusty
    /// gold frames with an amber highlight, over semi-transparent near-black panels, and a rusted
    /// red for anything negative or destructive. Scoped to MainMenu + Settings + About only —
    /// <c>GameUITheme</c>'s gold/navy gameplay palette is deliberately kept separate so a menu
    /// re-theme can never bleed into the card/HUD visuals.
    /// </remarks>
    public static class SettingsPanelTheme
    {
        /// <summary>Rusty gold — the accent/CTA colour: active tab fill and primary menu buttons.</summary>
        public static readonly Color ActiveTabColour = new Color32(0xD4, 0xAF, 0x37, 0xFF);

        /// <summary>
        /// Bright amber. The active tab's outer glow and the Apply button's fill — the one colour
        /// that reads as "lit" against the otherwise weathered, desaturated palette.
        /// </summary>
        public static readonly Color AmberGlowColour = new Color32(0xFF, 0xB8, 0x30, 0xFF);

        /// <summary>Apply/confirm fill. Aliases <see cref="AmberGlowColour"/> by intent, not by
        /// coincidence: the affirmative action is the lit one.</summary>
        public static readonly Color ApplyColour = AmberGlowColour;

        /// <summary>
        /// Semi-transparent near-black card/surface tone for inactive tabs, group panels and
        /// control tracks. The alpha (0xE0 ≈ 88%) is what lets the weathered grain layer behind it
        /// show through, so panels read as sitting *on* the surface rather than replacing it.
        /// </summary>
        public static readonly Color InactiveTabColour = new Color32(0x12, 0x12, 0x14, 0xE0);

        /// <summary>The same semi-transparent surface, named for its use as a grouped-card fill.</summary>
        public static readonly Color PanelSurfaceColour = InactiveTabColour;

        /// <summary>Opaque screen ground behind the panels — the base the grain layer is drawn on.</summary>
        public static readonly Color ScreenBackgroundColour = new Color32(0x12, 0x12, 0x14, 0xFF);

        /// <summary>
        /// Dark ink for text sitting on the <see cref="ActiveTabColour"/> / <see cref="ApplyColour"/>
        /// fills. Deliberately not the cream used elsewhere: cream on rusty gold falls under the
        /// 4.5:1 normal-text floor, whereas this clears it comfortably (see
        /// <see cref="UIContrastMath"/> and the palette contrast tests).
        /// </summary>
        public static readonly Color ActiveTabTextColour = new Color32(0x1A, 0x14, 0x10, 0xFF);

        /// <summary>Faded bronze text on the dark inactive fill.</summary>
        public static readonly Color InactiveTabTextColour = new Color32(0xC9, 0xB4, 0x87, 0xFF);

        /// <summary>
        /// Rusted red-brown. Marks anything negative or irreversible — the Cancel action, and the
        /// destructive Reset Progress button — as visually distinct from the amber/gold affirmative
        /// side of the palette.
        /// </summary>
        public static readonly Color DangerColour = new Color32(0x8B, 0x25, 0x00, 0xFF);

        /// <summary>Light text on the dark danger fill.</summary>
        public static readonly Color DangerTextColour = Color.white;

        /// <summary>
        /// A lighter, legible red for danger-tinted text sitting directly on the panel background
        /// (e.g. a destructive-action caption) — <see cref="DangerColour"/> is tuned as a button
        /// fill behind white text and reads as too dark/low-contrast on its own.
        /// </summary>
        public static readonly Color DangerAccentColour = new Color32(0xD9, 0x70, 0x5A, 0xFF);

        /// <summary>
        /// Aged bronze. Every frame and border in the menus: group-card outlines, the header's
        /// corner brackets, inactive tab edges and the slider handle's ring.
        /// </summary>
        public static readonly Color BorderGoldColour = new Color32(0x8C, 0x6D, 0x37, 0xFF);
    }
}
