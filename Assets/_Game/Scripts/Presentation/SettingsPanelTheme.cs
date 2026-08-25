using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Shared tab-tint colours for the Settings panel. Kept as one source of truth so the Editor
    /// scene authoring (initial state) and <see cref="SettingsPanelView"/> (runtime tab switching)
    /// never drift apart.
    /// </summary>
    /// <remarks>
    /// Zombie/post-apocalypse re-theme, scoped to MainMenu + Settings + About only. The Game/Card
    /// scene keeps its original gold/navy palette — <c>SceneSetupAutomation</c> decouples every
    /// place that used to read these values by reference (<c>ButtonColour</c>, the tap-choice-button
    /// fill) so none of this bleeds into gameplay. Deliberately mixes several wasteland tones rather
    /// than one colour repeated everywhere: rust for actions, ash for neutral surfaces, dried blood
    /// for danger — <c>SceneSetupAutomation.MenuToxicAccentColour</c> carries the one distinctly
    /// "zombie" toxic-green note, kept to slider fills only so it reads as a highlight, not the base.
    /// </remarks>
    public static class SettingsPanelTheme
    {
        /// <summary>Oxidised/rusted metal — the CTA colour across MainMenu and Settings.</summary>
        public static readonly Color ActiveTabColour = new Color32(0xA8, 0x5A, 0x26, 0xFF);

        /// <summary>Dust/ash-grey tone for inactive tabs and control tracks.</summary>
        public static readonly Color InactiveTabColour = new Color32(0x33, 0x2F, 0x29, 0xFF);

        /// <summary>Near-black text on the rust fill — white-on-rust reads as low-contrast.</summary>
        public static readonly Color ActiveTabTextColour = new Color32(0x1A, 0x0F, 0x06, 0xFF);

        /// <summary>Bone/ash white text on the dark inactive fill.</summary>
        public static readonly Color InactiveTabTextColour = new Color32(0xD9, 0xD3, 0xC4, 0xFF);

        /// <summary>
        /// Marks a destructive, irreversible action (Reset Progress) as visually distinct from the
        /// ordinary settings around it — a dried-blood red rather than the rust used for every other
        /// button, so it reads as dangerous without introducing a whole second style.
        /// </summary>
        public static readonly Color DangerColour = new Color32(0x5C, 0x14, 0x14, 0xFF);

        /// <summary>Light text on the dark danger fill.</summary>
        public static readonly Color DangerTextColour = Color.white;

        /// <summary>
        /// A lighter, legible blood-red for danger-tinted text sitting directly on the panel
        /// background (e.g. a destructive-action caption) — <see cref="DangerColour"/> is tuned as
        /// a button fill behind white text and reads as too dark/low-contrast on its own.
        /// </summary>
        public static readonly Color DangerAccentColour = new Color32(0xC2, 0x68, 0x52, 0xFF);
    }
}
