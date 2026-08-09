using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Converts a device safe area into normalised RectTransform anchors.
    /// </summary>
    /// <remarks>
    /// Takes the safe area as a parameter rather than reading <see cref="Screen"/>, so the whole
    /// calculation is testable without a device. <see cref="SafeAreaFitter"/> supplies the real
    /// values.
    /// </remarks>
    public static class SafeAreaMath
    {
        /// <summary>
        /// Returns false — leaving the anchors at full screen — for any input that cannot describe a
        /// usable area, rather than dividing by zero or producing inverted anchors.
        /// </summary>
        public static bool TryCalculateAnchors(
            Rect safeArea,
            int screenWidth,
            int screenHeight,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return false;
            }

            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return false;
            }

            Vector2 min = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenWidth),
                Mathf.Clamp01(safeArea.yMin / screenHeight));

            Vector2 max = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenWidth),
                Mathf.Clamp01(safeArea.yMax / screenHeight));

            // Clamping can collapse a safe area that lay entirely off-screen.
            if (max.x <= min.x || max.y <= min.y)
            {
                return false;
            }

            anchorMin = min;
            anchorMax = max;
            return true;
        }
    }
}
