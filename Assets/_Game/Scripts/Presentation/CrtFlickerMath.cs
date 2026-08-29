using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Pure calculations behind <see cref="CrtFlickerAnimator"/>, kept separate so the timing curve
    /// and reduced-motion scaling can be tested without a live MonoBehaviour.
    /// </summary>
    public static class CrtFlickerMath
    {
        /// <summary>
        /// A triangle curve for one flicker burst: 1 (no dip) at <paramref name="burstProgress01"/>
        /// 0 or 1, <paramref name="dipMultiplier"/> at the midpoint.
        /// </summary>
        public static float BurstAlphaMultiplier(float burstProgress01, float dipMultiplier)
        {
            float t = Mathf.Clamp01(burstProgress01);
            float distanceFromCentre = Mathf.Abs(t - 0.5f) * 2f;
            return Mathf.Lerp(dipMultiplier, 1f, distanceFromCentre);
        }

        /// <summary>Multiplies <paramref name="baseValue"/> by <paramref name="scale"/> only when
        /// <paramref name="reducedMotion"/> is set, otherwise returns it unchanged.</summary>
        public static float ScaleForReducedMotion(float baseValue, bool reducedMotion, float scale)
        {
            return reducedMotion ? baseValue * scale : baseValue;
        }
    }
}
