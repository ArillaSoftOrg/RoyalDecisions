using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Every calculation behind a swipe, as pure functions.
    /// </summary>
    /// <remarks>
    /// Separated from the controller so threshold, rotation, preview and exit behaviour can be
    /// covered exhaustively without a Canvas, a pointer, or a frame. Nothing here touches a Unity
    /// object.
    /// </remarks>
    public static class SwipeMath
    {
        /// <summary>The smallest threshold that can ever be returned, so it is never zero.</summary>
        public const float AbsoluteMinimumThreshold = 1f;

        /// <summary>
        /// How far the card must travel to confirm, as a fraction of the parent's width.
        /// </summary>
        /// <remarks>
        /// Resolution-independent by construction: a screen-pixel constant would make the gesture
        /// twice as hard on a denser display. The floor guards a parent that has not been laid out
        /// yet — a zero threshold would otherwise make every release a confirmation.
        /// </remarks>
        public static float ThresholdDistance(
            float parentWidth,
            float thresholdRatio,
            float minimumThresholdDistance)
        {
            float fromWidth = parentWidth * thresholdRatio;
            float floor = Mathf.Max(minimumThresholdDistance, AbsoluteMinimumThreshold);
            return Mathf.Max(fromWidth, floor);
        }

        /// <summary>Unsigned progress towards the threshold, clamped to <c>0..1</c>.</summary>
        public static float Progress(float displacement, float thresholdDistance)
        {
            if (thresholdDistance <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.Abs(displacement) / thresholdDistance);
        }

        /// <summary>Signed progress in <c>-1..+1</c>. Negative is a left drag.</summary>
        public static float SignedProgress(float displacement, float thresholdDistance)
        {
            if (thresholdDistance <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(displacement / thresholdDistance, -1f, 1f);
        }

        /// <summary>
        /// Preview strengths for both sides. Only the side being dragged towards ever rises; the
        /// other stays at zero, so the player never sees two competing outcomes.
        /// </summary>
        public static void PreviewStrengths(
            float displacement,
            float thresholdDistance,
            out float leftStrength,
            out float rightStrength)
        {
            float progress = Progress(displacement, thresholdDistance);

            leftStrength = displacement < 0f ? progress : 0f;
            rightStrength = displacement > 0f ? progress : 0f;
        }

        /// <summary>
        /// Tilt in degrees, derived from signed displacement and clamped to the configured maximum.
        /// </summary>
        public static float Rotation(
            float displacement,
            float thresholdDistance,
            float maxRotationDegrees,
            bool clockwiseOnRightDrag)
        {
            float signed = SignedProgress(displacement, thresholdDistance);
            float magnitude = Mathf.Abs(maxRotationDegrees);

            // Negative Z is clockwise in Unity's UI space.
            float angle = -signed * magnitude;
            return clockwiseOnRightDrag ? angle : -angle;
        }

        /// <summary>
        /// Whether a release at this displacement confirms. Inclusive: landing exactly on the
        /// threshold counts as a decision.
        /// </summary>
        public static bool IsConfirmed(float displacement, float thresholdDistance)
        {
            if (thresholdDistance <= 0f)
            {
                return false;
            }

            return Mathf.Abs(displacement) >= thresholdDistance;
        }

        /// <summary>Which side a displacement points at. Zero resolves to the right.</summary>
        public static ChoiceSide SideFor(float displacement)
        {
            return displacement >= 0f ? ChoiceSide.Right : ChoiceSide.Left;
        }

        /// <summary>
        /// Where the card must end up to be completely off screen, derived from geometry rather
        /// than a pixel literal.
        /// </summary>
        /// <remarks>
        /// Half the parent clears its edge, half the card clears its own centre, and the margin
        /// puts the trailing edge beyond that.
        /// </remarks>
        public static float ExitTargetX(
            float initialX,
            ChoiceSide side,
            float parentWidth,
            float cardWidth,
            float exitMarginMultiplier)
        {
            float direction = side == ChoiceSide.Right ? 1f : -1f;
            float margin = Mathf.Max(0f, exitMarginMultiplier);
            float distance = (parentWidth * 0.5f) + (cardWidth * (0.5f + margin));

            return initialX + (direction * distance);
        }
    }
}
