using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Every calculation behind prologue slide stepping and fades, as pure functions.
    /// </summary>
    /// <remarks>
    /// Separated from <see cref="PrologueSequenceController"/> so slide-index advancing and fade
    /// timing can be covered exhaustively without a Canvas, a coroutine, or a frame — mirrors
    /// <see cref="SwipeMath"/> and <see cref="StartupLoadingProgressMath"/>.
    /// </remarks>
    public static class PrologueSequenceMath
    {
        public static bool HasSlides(int slideCount)
        {
            return slideCount > 0;
        }

        /// <summary>Clamps an index into the valid slide range. Zero for an empty sequence.</summary>
        public static int ClampSlideIndex(int index, int slideCount)
        {
            if (!HasSlides(slideCount))
            {
                return 0;
            }

            return Mathf.Clamp(index, 0, slideCount - 1);
        }

        public static bool IsLastSlide(int index, int slideCount)
        {
            return HasSlides(slideCount) && index >= slideCount - 1;
        }

        /// <summary>
        /// The index a tap-to-advance should move to, or -1 when the sequence has no slides or is
        /// already on its last one — the caller's cue to complete instead of showing another slide.
        /// </summary>
        public static int NextSlideIndexOrCompletion(int currentIndex, int slideCount)
        {
            if (!HasSlides(slideCount) || IsLastSlide(currentIndex, slideCount))
            {
                return -1;
            }

            return ClampSlideIndex(currentIndex + 1, slideCount);
        }

        /// <summary>
        /// Fade-in alpha for elapsed unscaled time, given a delay before the fade starts and its
        /// duration. Used identically for an image crossfade (delay 0) and a subtitle fade-in (delay
        /// greater than 0) — both are just "wait, then ramp linearly to 1".
        /// </summary>
        public static float FadeInAlpha(float elapsedSeconds, float delaySeconds, float durationSeconds)
        {
            float delay = Mathf.Max(0f, delaySeconds);

            if (durationSeconds <= 0f)
            {
                return elapsedSeconds >= delay ? 1f : 0f;
            }

            float t = (elapsedSeconds - delay) / durationSeconds;
            return Mathf.Clamp01(t);
        }
    }
}
