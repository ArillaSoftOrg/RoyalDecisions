using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Every calculation behind the startup loading bar, as pure functions.
    /// </summary>
    /// <remarks>
    /// Separated from <see cref="StartupLoadingController"/> so the progress smoothing, percentage
    /// formatting, and "when has it visibly finished" rule can be covered exhaustively without a
    /// Canvas, a coroutine, or a frame — mirrors <see cref="SwipeMath"/>.
    /// </remarks>
    public static class StartupLoadingProgressMath
    {
        /// <summary>Clamps any reported or displayed progress to the valid <c>0..1</c> range.</summary>
        public static float ClampProgress(float progress01)
        {
            return Mathf.Clamp01(progress01);
        }

        /// <summary>
        /// Moves displayed progress towards target at a fixed rate (fraction of the bar per second)
        /// rather than jumping, so a real target that leaps from e.g. 0.25 to 0.70 still reads as
        /// smooth motion on screen. Both inputs are clamped first so a caller can pass raw values.
        /// </summary>
        public static float AdvanceDisplayed(
            float displayedProgress, float targetProgress, float maxDeltaPerSecond, float deltaSeconds)
        {
            float displayed = ClampProgress(displayedProgress);
            float target = ClampProgress(targetProgress);

            if (deltaSeconds <= 0f || maxDeltaPerSecond <= 0f)
            {
                return displayed;
            }

            float maxDelta = maxDeltaPerSecond * deltaSeconds;
            return Mathf.MoveTowards(displayed, target, maxDelta);
        }

        /// <summary>Whole-number percentage for display, clamped to <c>0..100</c>.</summary>
        public static int PercentageFor(float progress01)
        {
            return Mathf.Clamp(Mathf.RoundToInt(ClampProgress(progress01) * 100f), 0, 100);
        }

        /// <summary>
        /// Whether the loading screen may begin its hold-then-fade-out sequence: real startup work
        /// must be finished, the bar must have visibly caught up to 100%, and the configured minimum
        /// display duration must have elapsed — so a near-instant startup never flashes 0-&gt;100 in a
        /// single frame.
        /// </summary>
        public static bool ShouldBeginFadeOut(
            bool completionRequested,
            float displayedProgress,
            float elapsedSeconds,
            float minimumDisplaySeconds)
        {
            if (!completionRequested)
            {
                return false;
            }

            return ClampProgress(displayedProgress) >= 1f
                && elapsedSeconds >= Mathf.Max(0f, minimumDisplaySeconds);
        }
    }
}
