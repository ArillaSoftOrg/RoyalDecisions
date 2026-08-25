using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Computes a "cover" (fill-and-crop) size for a portrait sitting inside a masked region,
    /// so art whose aspect ratio does not exactly match the frame's opening is scaled uniformly
    /// and cropped by the existing <c>Mask</c> rather than stretched non-uniformly to fit.
    /// </summary>
    public static class PortraitCoverFitMath
    {
        /// <summary>
        /// The size an image with <paramref name="spriteAspect"/> (width / height) must be to
        /// fully cover a <paramref name="containerSize"/> region without distortion, centred and
        /// overflowing evenly on whichever axis is cropped by the container's mask.
        /// </summary>
        public static Vector2 ComputeCoverSize(Vector2 containerSize, float spriteAspect)
        {
            if (containerSize.x <= 0f || containerSize.y <= 0f || spriteAspect <= 0f)
            {
                return containerSize;
            }

            float containerAspect = containerSize.x / containerSize.y;

            // Wider than the container: match height, let width overflow (crop left/right).
            // Narrower/taller than the container: match width, let height overflow (crop top/bottom).
            return spriteAspect > containerAspect
                ? new Vector2(containerSize.y * spriteAspect, containerSize.y)
                : new Vector2(containerSize.x, containerSize.x / spriteAspect);
        }
    }
}
