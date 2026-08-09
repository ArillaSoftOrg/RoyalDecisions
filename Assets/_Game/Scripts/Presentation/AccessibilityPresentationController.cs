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

        private readonly Dictionary<TMP_Text, Vector2> baseSizes =
            new Dictionary<TMP_Text, Vector2>();

        public void Apply(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            float scale = settings.LargerText ? 1.15f : 1f;
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
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            TMP_Text[] text,
            TMP_Text[] secondary,
            CardSwipeController swipe,
            StatItemView[] stats)
        {
            scalableText = text ?? System.Array.Empty<TMP_Text>();
            secondaryText = secondary ?? System.Array.Empty<TMP_Text>();
            swipeController = swipe;
            statItems = stats ?? System.Array.Empty<StatItemView>();
        }
#endif
    }
}
