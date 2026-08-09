using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>Pure responsive sizing for the active and queued card surfaces.</summary>
    public static class ResponsiveCardLayoutMath
    {
        public static Vector2 Calculate(
            Vector2 availableSize,
            float preferredWidthRatio,
            float widthToHeightRatio,
            float maximumHeightRatio)
        {
            return Calculate(
                availableSize.x,
                availableSize,
                preferredWidthRatio,
                widthToHeightRatio,
                maximumHeightRatio,
                float.PositiveInfinity);
        }

        public static Vector2 Calculate(
            float widthReference,
            Vector2 availableSize,
            float preferredWidthRatio,
            float widthToHeightRatio,
            float maximumHeightRatio,
            float maximumWidth)
        {
            float safeReference = Mathf.Max(0f, widthReference);
            float availableWidth = Mathf.Max(0f, availableSize.x);
            float widthLimit = float.IsNaN(maximumWidth)
                ? 0f
                : Mathf.Max(0f, maximumWidth);
            float width = safeReference * Mathf.Clamp(preferredWidthRatio, 0.1f, 1f);
            width = Mathf.Min(width, availableWidth);
            if (!float.IsPositiveInfinity(widthLimit))
            {
                width = Mathf.Min(width, widthLimit);
            }

            float aspect = Mathf.Max(0.01f, widthToHeightRatio);
            float maximumHeight = Mathf.Max(0f, availableSize.y) * Mathf.Clamp01(maximumHeightRatio);
            float height = width / aspect;

            if (height > maximumHeight)
            {
                height = maximumHeight;
                width = height * aspect;
            }

            return new Vector2(width, height);
        }
    }
}
