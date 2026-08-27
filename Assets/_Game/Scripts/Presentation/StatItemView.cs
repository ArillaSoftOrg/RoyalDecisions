using RoyalDecisions.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// A single statistic display: icon, optional label, and a fill bar.
    /// </summary>
    /// <remarks>
    /// Reusable by design — the HUD holds four of these, each carrying its own
    /// <see cref="StatType"/>, so adding or restyling a statistic is prefab work rather than code.
    ///
    /// Nothing here can write to a run: it receives a normalised float and shows it.
    /// </remarks>
    public sealed class StatItemView : MonoBehaviour
    {
        [SerializeField] private StatType stat = StatType.Authority;

        [Tooltip("Image with Type = Filled. Its fillAmount is what moves.")]
        [SerializeField] private Image fillImage;

        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private GraphicFallbackSettings iconFallback = new GraphicFallbackSettings();

        [SerializeField] private TMP_Text label;

        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text iconFallbackLabel;
        [SerializeField] private TMP_Text impactLabel;
        [SerializeField] private CanvasGroup impactGroup;
        [SerializeField] private TMP_Text criticalLabel;

        [Tooltip("Fill units per second when animating. Zero snaps instantly.")]
        [Min(0f)]
        [SerializeField] private float animationSpeed = 2.5f;

        [Tooltip("Seconds for the icon's glow to fade back to normal after a value change.")]
        [Min(0f)]
        [SerializeField] private float glowDuration = 0.6f;

        private float targetFill;
        private bool animating;
        private int criticalBoundary = 15;
        private Color iconBaseColor = Color.white;
        private Color iconGlowColor = Color.white;
        private float glowTimer;
        private int lastImpactDelta = int.MinValue;
        // "+"/"-" pre-ApplyTheme default, matching GameUITheme's own default — see that class for
        // why the originally-intended ▲/▼ triangles are not used.
        private string positiveImpactGlyph = "+";
        private string negativeImpactGlyph = "-";
        private Color positiveImpactColor = new Color32(0xD9, 0xC2, 0x8B, 0xFF);
        private Color negativeImpactColor = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
        private bool reducedMotion;

        public StatType Stat => stat;

        /// <summary>What is actually on screen right now, not the requested target.</summary>
        public float DisplayedFill => fillImage != null ? fillImage.fillAmount : 0f;

        public float TargetFill => targetFill;

        public bool IsAnimating => animating;

        public GraphicFallbackMode IconMode { get; private set; } = GraphicFallbackMode.HideGraphic;

        public string DisplayedValue => valueText != null ? valueText.text : string.Empty;

        public string ImpactText => impactLabel != null ? impactLabel.text : string.Empty;

        public bool IsCritical => criticalLabel != null && criticalLabel.gameObject.activeSelf;

        private void Awake()
        {
            RefreshIcon();
        }

        /// <summary>Sets the bar immediately, cancelling any animation in flight.</summary>
        public void SetFill(float normalized)
        {
            targetFill = Mathf.Clamp01(normalized);
            animating = false;
            ApplyFill(targetFill);
        }

        /// <summary>
        /// Eases the bar towards a value. Purely presentational — the domain value changed the
        /// instant the choice resolved, whatever the bar is doing.
        /// </summary>
        public void SetFillAnimated(float normalized)
        {
            targetFill = Mathf.Clamp01(normalized);

            if (animationSpeed <= 0f)
            {
                SetFill(targetFill);
                return;
            }

            animating = !Mathf.Approximately(DisplayedFill, targetFill);
        }

        public void SetLabel(string text)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }

        public void SetValue(int value, bool animated = false)
        {
            if (valueText != null)
            {
                valueText.SetText("{0}", value);
            }

            if (criticalLabel != null)
            {
                criticalLabel.gameObject.SetActive(
                    value <= criticalBoundary || value >= StatBounds.Max - criticalBoundary);
            }

            if (animated && !reducedMotion)
            {
                SetFillAnimated(StatDisplayMath.ToFill(value));
            }
            else
            {
                SetFill(StatDisplayMath.ToFill(value));
            }
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            if (enabled && animating)
            {
                SetFill(targetFill);
            }
            if (enabled && glowTimer > 0f)
            {
                glowTimer = 0f;
                ApplyGlow(0f);
            }
        }

        /// <summary>
        /// Briefly tints the icon towards this stat's own colour, then fades it back — a quick
        /// "this is the one that just changed" cue, distinct from <see cref="ShowImpact"/>'s
        /// drag-preview badge. A no-op under reduced motion, matching how <see cref="SetValue"/>
        /// already skips the fill-bar animation there.
        /// </summary>
        public void TriggerValueChangeGlow()
        {
            if (reducedMotion)
            {
                return;
            }

            glowTimer = glowDuration;
        }

        public void ShowImpact(int delta, float strength)
        {
            int level = ChoiceImpactMath.MagnitudeLevel(delta);
            if (impactLabel != null && delta != lastImpactDelta)
            {
                impactLabel.text = ChoiceImpactMath.Format(
                    delta, positiveImpactGlyph, negativeImpactGlyph);
                impactLabel.color = delta >= 0 ? positiveImpactColor : negativeImpactColor;
            }

            lastImpactDelta = delta;
            if (impactGroup != null)
            {
                impactGroup.alpha = level == 0 ? 0f : Mathf.Clamp01(strength);
            }

            if (impactLabel != null)
            {
                impactLabel.gameObject.SetActive(level > 0);
            }
        }

        public void ClearImpact()
        {
            lastImpactDelta = int.MinValue;
            if (impactLabel != null)
            {
                impactLabel.text = string.Empty;
                impactLabel.gameObject.SetActive(false);
            }

            if (impactGroup != null)
            {
                impactGroup.alpha = 0f;
            }
        }

        public void ApplyTheme(GameUITheme theme)
        {
            if (theme == null)
            {
                return;
            }

            iconSprite = theme.GetStatIcon(stat);
            criticalBoundary = theme.CriticalBoundary;
            positiveImpactGlyph = theme.PositiveImpactGlyph;
            negativeImpactGlyph = theme.NegativeImpactGlyph;
            positiveImpactColor = theme.HighlightGold;
            negativeImpactColor = theme.PrimaryText;

            if (fillImage != null)
            {
                fillImage.color = theme.GetStatColor(stat);
            }

            if (label == null || string.IsNullOrEmpty(label.text))
            {
                SetLabel(theme.GetStatName(stat));
            }

            ConfigureText(label, theme.PrimaryText, theme.BodyFont);
            ConfigureText(valueText, theme.PrimaryText, theme.BodyFont);
            ConfigureText(iconFallbackLabel, theme.PrimaryText, theme.TitleFont);
            ConfigureText(criticalLabel, theme.HighlightGold, theme.TitleFont);

            iconBaseColor = theme.PrimaryText;
            // A brightened tint of the stat's own bar colour, not the raw colour: several of the
            // stat colours (e.g. people's dark maroon) are darker than the cream icon tint, so
            // using them at full strength would make a "glow" read as the icon dimming instead.
            iconGlowColor = Color.Lerp(theme.GetStatColor(stat), Color.white, 0.55f);

            bool hasIcon = iconSprite != null;
            if (iconImage != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.color = iconBaseColor;
                iconImage.raycastTarget = false;
                iconImage.enabled = hasIcon;
            }

            IconMode = hasIcon ? GraphicFallbackMode.UseSource : GraphicFallbackMode.HideGraphic;
            if (iconFallbackLabel != null)
            {
                iconFallbackLabel.text = theme.GetStatFallbackSymbol(stat);
                iconFallbackLabel.gameObject.SetActive(!hasIcon);
            }
        }

        public void RefreshIcon()
        {
            IconMode = GraphicFallback.Apply(iconImage, iconSprite, iconFallback);
        }

        private static void ConfigureText(TMP_Text target, Color color, TMP_FontAsset font)
        {
            if (target == null)
            {
                return;
            }

            target.color = color;
            target.raycastTarget = false;
            if (font != null)
            {
                target.font = font;
            }
        }

        private void Update()
        {
            // Returns on the first line for every frame both effects are at rest, which is nearly
            // all of them. No allocation on either path.
            if (!animating && glowTimer <= 0f)
            {
                return;
            }

            if (animating)
            {
                float next = Mathf.MoveTowards(DisplayedFill, targetFill, animationSpeed * Time.deltaTime);
                ApplyFill(next);

                if (Mathf.Approximately(next, targetFill))
                {
                    ApplyFill(targetFill);
                    animating = false;
                }
            }

            if (glowTimer > 0f)
            {
                glowTimer = Mathf.Max(0f, glowTimer - Time.deltaTime);
                ApplyGlow(glowTimer / glowDuration);
            }
        }

        private void ApplyGlow(float strength)
        {
            if (iconImage != null)
            {
                iconImage.color = Color.Lerp(iconBaseColor, iconGlowColor, strength);
            }
        }

        private void ApplyFill(float value)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = value;
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by prefab setup and tests.</summary>
        public void SetAuthoringReferences(
            StatType statType,
            Image fill,
            Image icon = null,
            TMP_Text statLabel = null,
            Sprite sprite = null,
            GraphicFallbackSettings fallback = null,
            float speed = 2.5f,
            TMP_Text statValue = null,
            TMP_Text fallbackLabel = null,
            TMP_Text impact = null,
            CanvasGroup impactCanvasGroup = null,
            TMP_Text critical = null)
        {
            stat = statType;
            fillImage = fill;
            iconImage = icon;
            label = statLabel;
            valueText = statValue;
            iconFallbackLabel = fallbackLabel;
            impactLabel = impact;
            impactGroup = impactCanvasGroup;
            criticalLabel = critical;
            iconSprite = sprite;
            animationSpeed = speed;

            if (fallback != null)
            {
                iconFallback = fallback;
            }
        }
#endif
    }
}
