using System.Collections.Generic;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>Applies additive accessibility preferences to explicitly wired views.</summary>
    public sealed class AccessibilityPresentationController : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] scalableText = System.Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] secondaryText = System.Array.Empty<TMP_Text>();
        [SerializeField] private CardSwipeController swipeController;
        [SerializeField] private StatItemView[] statItems = System.Array.Empty<StatItemView>();

        [Tooltip("Every panel/tab-crossfade/transition-overlay fade in this scene — Reduced Motion "
            + "shortens each one's duration via PanelFadeAnimator.SetReducedMotion, the same way it "
            + "already shortens the card swipe and stat-bar animations above.")]
        [SerializeField] private PanelFadeAnimator[] panelAnimators = System.Array.Empty<PanelFadeAnimator>();

        [Tooltip("The CRT overlay flicker on Settings/About — Reduced Motion calms it down (longer "
            + "gaps, shorter/softer bursts) via CrtFlickerAnimator.SetReducedMotion, same shape as "
            + "the panel fades above.")]
        [SerializeField] private CrtFlickerAnimator[] crtFlickerAnimators =
            System.Array.Empty<CrtFlickerAnimator>();

        private readonly Dictionary<TMP_Text, Vector2> baseSizes =
            new Dictionary<TMP_Text, Vector2>();

        /// <summary>Small trims the default down; Large matches the old "larger text" toggle exactly.</summary>
        private const float SmallTextScale = 0.9f;
        private const float NormalTextScale = 1f;
        private const float LargeTextScale = 1.15f;

        public void Apply(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            float scale = TextScaleFor(settings.TextSizeMode);
            for (int i = 0; i < scalableText.Length; i++)
            {
                TMP_Text text = scalableText[i];
                if (text == null)
                {
                    continue;
                }
                if (!baseSizes.TryGetValue(text, out Vector2 sizes))
                {
                    sizes = new Vector2(text.fontSizeMin, text.fontSizeMax);
                    baseSizes.Add(text, sizes);
                }
                text.fontSizeMin = sizes.x * scale;
                text.fontSizeMax = sizes.y * scale;
            }
            Color secondary = settings.HighContrast
                ? new Color32(0xF3, 0xE8, 0xC8, 0xFF)
                : new Color32(0xB9, 0xAA, 0x90, 0xFF);
            for (int i = 0; i < secondaryText.Length; i++)
            {
                if (secondaryText[i] != null)
                {
                    secondaryText[i].color = secondary;
                }
            }
            swipeController?.SetReducedMotion(settings.ReducedMotion);
            for (int i = 0; i < statItems.Length; i++)
            {
                statItems[i]?.SetReducedMotion(settings.ReducedMotion);
            }
            for (int i = 0; i < panelAnimators.Length; i++)
            {
                panelAnimators[i]?.SetReducedMotion(settings.ReducedMotion);
            }
            for (int i = 0; i < crtFlickerAnimators.Length; i++)
            {
                crtFlickerAnimators[i]?.SetReducedMotion(settings.ReducedMotion);
            }
        }

        private static float TextScaleFor(TextSizeMode mode)
        {
            switch (mode)
            {
                case TextSizeMode.Small: return SmallTextScale;
                case TextSizeMode.Large: return LargeTextScale;
                default: return NormalTextScale;
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            TMP_Text[] text,
            TMP_Text[] secondary,
            CardSwipeController swipe,
            StatItemView[] stats,
            PanelFadeAnimator[] panels = null,
            CrtFlickerAnimator[] crtFlicker = null)
        {
            scalableText = text ?? System.Array.Empty<TMP_Text>();
            secondaryText = secondary ?? System.Array.Empty<TMP_Text>();
            swipeController = swipe;
            statItems = stats ?? System.Array.Empty<StatItemView>();
            panelAnimators = panels ?? System.Array.Empty<PanelFadeAnimator>();
            crtFlickerAnimators = crtFlicker ?? System.Array.Empty<CrtFlickerAnimator>();
        }
#endif
    }
}
