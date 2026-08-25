using RoyalDecisions.Data;
using TMPro;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>Shared, replaceable visual configuration for the game scene.</summary>
    [CreateAssetMenu(menuName = "Royal Decisions/UI/Game UI Theme", fileName = "GameUITheme")]
    public sealed class GameUITheme : ScriptableObject
    {
        [Header("Palette")]
        [SerializeField] private Color overallBackground = new Color32(0x07, 0x11, 0x1B, 0xFF);
        [SerializeField] private Color uiSurface = new Color32(0x12, 0x16, 0x20, 0xFF);
        [SerializeField] private Color cardSurface = new Color32(0x21, 0x17, 0x1A, 0xFF);
        [SerializeField] private Color borderGold = new Color32(0xB5, 0x8A, 0x4A, 0xFF);
        [SerializeField] private Color highlightGold = new Color32(0xD9, 0xC2, 0x8B, 0xFF);
        [SerializeField] private Color primaryText = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
        [SerializeField] private Color secondaryText = new Color32(0xB9, 0xAA, 0x90, 0xFF);
        [SerializeField] private Color people = new Color32(0x8A, 0x41, 0x4B, 0xFF);
        [SerializeField] private Color security = new Color32(0x68, 0x70, 0x3D, 0xFF);
        [SerializeField] private Color authority = new Color32(0x3E, 0x56, 0x7D, 0xFF);
        [SerializeField] private Color wealth = new Color32(0xB3, 0x8A, 0x3D, 0xFF);
        [SerializeField] private Color emptyBar = new Color32(0x2A, 0x2F, 0x3A, 0xFF);
        [SerializeField] private Color leftChoice = new Color32(0x8A, 0x41, 0x4B, 0xFF);
        [SerializeField] private Color rightChoice = new Color32(0xB3, 0x8A, 0x3D, 0xFF);
        [SerializeField] private Color portraitFallbackBackground =
            new Color32(0x12, 0x16, 0x20, 0xFF);
        [SerializeField] private Color portraitFallbackForeground =
            new Color32(0xB9, 0xAA, 0x90, 0xFF);

        [Tooltip("Ink colour for text on the light parchment situation panel above the card.")]
        [SerializeField] private Color situationText = new Color32(0x2A, 0x1E, 0x14, 0xFF);

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        [Header("Stat icons")]
        [SerializeField] private Sprite peopleIcon;
        [SerializeField] private Sprite securityIcon;
        [SerializeField] private Sprite authorityIcon;
        [SerializeField] private Sprite wealthIcon;

        [Header("Optional art")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite vignetteSprite;
        [SerializeField] private Sprite cardFrameSprite;
        [SerializeField] private Sprite portraitFrameSprite;
        [SerializeField] private Sprite portraitMaskSprite;
        [SerializeField] private Sprite leftEdgeSprite;
        [SerializeField] private Sprite rightEdgeSprite;
        [SerializeField] private Sprite cornerDecorationSprite;
        [SerializeField] private Sprite sealSprite;
        [SerializeField] private Sprite nextCardFrameSprite;
        [Tooltip(
            "Optional. The parchment/situation-panel background above the card. Missing falls " +
            "back to the procedural rounded panel rather than failing.")]
        [SerializeField] private Sprite situationPanelSprite;

        [Header("Presentation text")]
        [SerializeField] private string peopleName = "People";
        [SerializeField] private string securityName = "Security";
        [SerializeField] private string authorityName = "Authority";
        [SerializeField] private string wealthName = "Wealth";
        [SerializeField] private string peopleFallbackSymbol = "P";
        [SerializeField] private string securityFallbackSymbol = "S";
        [SerializeField] private string authorityFallbackSymbol = "A";
        [SerializeField] private string wealthFallbackSymbol = "W";
        // "+"/"-", not the originally-intended ▲/▼ triangles: the project-owned static Turkish
        // TMP atlas (TurkishGlyphValidator) does not bake U+25B2/U+25BC, and its source .ttf is
        // not present in this repo to extend it, so those glyphs render as missing-glyph tofu in
        // the HUD. ASCII +/- is guaranteed to already be in the atlas (BuildCharacterSet bakes the
        // full printable ASCII range) and reads just as clearly as a signed stat delta.
        [SerializeField] private string positiveImpactGlyph = "+";
        [SerializeField] private string negativeImpactGlyph = "-";

        [Header("Presentation thresholds")]
        [Range(1, 49)]
        [SerializeField] private int criticalBoundary = 15;

        public Color OverallBackground => overallBackground;
        public Color UISurface => uiSurface;
        public Color CardSurface => cardSurface;
        public Color BorderGold => borderGold;
        public Color HighlightGold => highlightGold;
        public Color PrimaryText => primaryText;
        public Color SecondaryText => secondaryText;
        public Color EmptyBar => emptyBar;
        public Color LeftChoice => leftChoice;
        public Color RightChoice => rightChoice;
        public Color PortraitFallbackBackground => portraitFallbackBackground;
        public Color PortraitFallbackForeground => portraitFallbackForeground;
        public Color SituationText => situationText;
        public TMP_FontAsset TitleFont => titleFont;
        public TMP_FontAsset BodyFont => bodyFont;
        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite VignetteSprite => vignetteSprite;
        public Sprite CardFrameSprite => cardFrameSprite;
        public Sprite PortraitFrameSprite => portraitFrameSprite;
        public Sprite PortraitMaskSprite => portraitMaskSprite;
        public Sprite LeftEdgeSprite => leftEdgeSprite;
        public Sprite RightEdgeSprite => rightEdgeSprite;
        public Sprite CornerDecorationSprite => cornerDecorationSprite;
        public Sprite SealSprite => sealSprite;
        public Sprite NextCardFrameSprite => nextCardFrameSprite;
        public Sprite SituationPanelSprite => situationPanelSprite;
        public string PositiveImpactGlyph => string.IsNullOrEmpty(positiveImpactGlyph) ? "+" : positiveImpactGlyph;
        public string NegativeImpactGlyph => string.IsNullOrEmpty(negativeImpactGlyph) ? "-" : negativeImpactGlyph;
        public int CriticalBoundary => criticalBoundary;

        public Color GetStatColor(StatType stat)
        {
            return stat switch
            {
                StatType.People => people,
                StatType.Security => security,
                StatType.Authority => authority,
                StatType.Wealth => wealth,
                _ => primaryText
            };
        }

        public Sprite GetStatIcon(StatType stat)
        {
            return stat switch
            {
                StatType.People => peopleIcon,
                StatType.Security => securityIcon,
                StatType.Authority => authorityIcon,
                StatType.Wealth => wealthIcon,
                _ => null
            };
        }

        public string GetStatName(StatType stat)
        {
            return stat switch
            {
                StatType.People => peopleName,
                StatType.Security => securityName,
                StatType.Authority => authorityName,
                StatType.Wealth => wealthName,
                _ => stat.ToString()
            };
        }

        public string GetStatFallbackSymbol(StatType stat)
        {
            return stat switch
            {
                StatType.People => peopleFallbackSymbol,
                StatType.Security => securityFallbackSymbol,
                StatType.Authority => authorityFallbackSymbol,
                StatType.Wealth => wealthFallbackSymbol,
                _ => "?"
            };
        }
    }
}
