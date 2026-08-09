using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>WCAG relative-luminance and contrast helpers for authoring validation.</summary>
    public static class UIContrastMath
    {
        public static float ContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            float lighter = Mathf.Max(firstLuminance, secondLuminance);
            float darker = Mathf.Min(firstLuminance, secondLuminance);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        public static bool MeetsNormalText(Color foreground, Color background)
        {
            return ContrastRatio(foreground, background) >= 4.5f;
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * Linearize(color.r)
                + 0.7152f * Linearize(color.g)
                + 0.0722f * Linearize(color.b);
        }

        private static float Linearize(float channel)
        {
            return channel <= 0.04045f
                ? channel / 12.92f
                : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }
    }
}
