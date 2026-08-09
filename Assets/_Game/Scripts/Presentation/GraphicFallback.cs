using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Decides — and then applies — what an image shows when content supplies no sprite.
    /// </summary>
    /// <remarks>
    /// <see cref="Resolve"/> returns a decision rather than a sprite, which is what makes the rule
    /// testable without constructing a <see cref="Graphic"/>, and leaves each view with exactly one
    /// switch to apply.
    /// </remarks>
    public static class GraphicFallback
    {
        /// <summary>
        /// Source sprite wins; then a configured fallback sprite; then a flat colour; then nothing.
        /// Every step is optional, so "no art at all" is a supported configuration.
        /// </summary>
        public static GraphicFallbackMode Resolve(
            Sprite source,
            Sprite fallbackSprite,
            bool useFallbackColour)
        {
            if (source != null)
            {
                return GraphicFallbackMode.UseSource;
            }

            if (fallbackSprite != null)
            {
                return GraphicFallbackMode.UseFallbackSprite;
            }

            return useFallbackColour
                ? GraphicFallbackMode.UseFallbackColour
                : GraphicFallbackMode.HideGraphic;
        }

        public static GraphicFallbackMode Resolve(Sprite source, GraphicFallbackSettings settings)
        {
            if (settings == null)
            {
                return source != null ? GraphicFallbackMode.UseSource : GraphicFallbackMode.HideGraphic;
            }

            return Resolve(source, settings.FallbackSprite, settings.UseFallbackColour);
        }

        /// <summary>
        /// Applies the resolved decision to an image. A null image is ignored rather than throwing —
        /// an unwired optional slot must never take the game down.
        /// </summary>
        public static GraphicFallbackMode Apply(
            Image image,
            Sprite source,
            GraphicFallbackSettings settings)
        {
            GraphicFallbackMode mode = Resolve(source, settings);

            if (image == null)
            {
                return mode;
            }

            switch (mode)
            {
                case GraphicFallbackMode.UseSource:
                    image.sprite = source;
                    image.color = Color.white;
                    image.enabled = true;
                    break;

                case GraphicFallbackMode.UseFallbackSprite:
                    image.sprite = settings.FallbackSprite;
                    image.color = Color.white;
                    image.enabled = true;
                    break;

                case GraphicFallbackMode.UseFallbackColour:
                    // Sprite cleared so the flat colour is what shows, not a tinted leftover.
                    image.sprite = null;
                    image.color = settings.FallbackColour;
                    image.enabled = true;
                    break;

                default:
                    image.sprite = null;
                    image.enabled = false;
                    break;
            }

            return mode;
        }
    }
}
