using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Plays the coded startup intro: a logo fading and scaling in over black, holding briefly,
    /// then fading back to black.
    /// </summary>
    /// <remarks>
    /// Purely visual and self-contained: it knows nothing about scenes. <see cref="Play"/> takes a
    /// completion callback, and the caller (<c>RoyalDecisions.Composition.BootstrapController</c>)
    /// decides what happens next through the existing scene-loading abstraction, so this component
    /// never duplicates <c>SceneManager</c> calls itself. Mirrors <see cref="PanelFadeAnimator"/>
    /// and <see cref="CardSwipeController"/>: unscaled time, an <see cref="AnimationCurve"/> ease,
    /// no <c>Update</c> — every frame of motion runs inside a coroutine that stops itself once the
    /// sequence ends, is skipped, or cannot play at all.
    /// </remarks>
    public sealed class IntroSequenceController : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [Tooltip("Missing any of these safely skips straight to MainMenu.")]
        [SerializeField] private CanvasGroup logoCanvasGroup;
        [SerializeField] private RectTransform logoRectTransform;
        [SerializeField] private Image logoImage;

        [Header("Timing (unscaled seconds)")]
        [SerializeField] private float preBlackHoldSeconds = 0.35f;
        [SerializeField] private float fadeInDurationSeconds = 0.90f;
        [SerializeField] private float holdDurationSeconds = 1.05f;
        [SerializeField] private float fadeOutDurationSeconds = 0.80f;
        [SerializeField] private float postBlackHoldSeconds = 0.20f;

        [Header("Motion")]
        [SerializeField] private float fadeInStartScale = 0.92f;
        [SerializeField] private float restScale = 1f;
        [SerializeField] private float fadeOutEndScale = 1.02f;
        [SerializeField] private AnimationCurve fadeInEase = BuildRevealEase();
        [SerializeField] private AnimationCurve fadeOutEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Reveal glow")]
        [Tooltip("How far the logo dims below full brightness at the start of the reveal, and the "
            + "size of the hold's breathing pulse. Kept small so it reads as a glow, not a flash.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float glowAmplitude = 0.06f;

        [Tooltip("Extra uniform scale at the peak of the hold's breathing pulse.")]
        [Range(0f, 0.05f)]
        [SerializeField] private float holdPulseScaleAmplitude = 0.015f;

        [Header("Skip")]
        [SerializeField] private bool allowSkip = true;

        private Action onComplete;
        private Coroutine runningSequence;
        private bool hasStarted;
        private bool hasCompleted;

        private bool accessibilityDefaultsCaptured;
        private float defaultPreBlackHoldSeconds;
        private float defaultFadeInDurationSeconds;
        private float defaultHoldDurationSeconds;
        private float defaultFadeOutDurationSeconds;
        private float defaultPostBlackHoldSeconds;
        private float defaultFadeInStartScale;
        private float defaultFadeOutEndScale;
        private float defaultGlowAmplitude;
        private float defaultHoldPulseScaleAmplitude;

        public bool HasCompleted => hasCompleted;

        /// <summary>
        /// Reduces the intro to a brief plain fade: no scale or glow motion, short durations, no
        /// black holds either side. Call before <see cref="Play"/>. Mirrors
        /// <see cref="CardSwipeController.SetReducedMotion"/> and
        /// <see cref="PanelFadeAnimator.SetReducedMotion"/> — shortens rather than removes the
        /// transition, and the authored Inspector values are captured once as the "off" baseline.
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            if (!accessibilityDefaultsCaptured)
            {
                defaultPreBlackHoldSeconds = preBlackHoldSeconds;
                defaultFadeInDurationSeconds = fadeInDurationSeconds;
                defaultHoldDurationSeconds = holdDurationSeconds;
                defaultFadeOutDurationSeconds = fadeOutDurationSeconds;
                defaultPostBlackHoldSeconds = postBlackHoldSeconds;
                defaultFadeInStartScale = fadeInStartScale;
                defaultFadeOutEndScale = fadeOutEndScale;
                defaultGlowAmplitude = glowAmplitude;
                defaultHoldPulseScaleAmplitude = holdPulseScaleAmplitude;
                accessibilityDefaultsCaptured = true;
            }

            if (enabled)
            {
                preBlackHoldSeconds = 0f;
                fadeInDurationSeconds = Mathf.Min(defaultFadeInDurationSeconds, 0.25f);
                holdDurationSeconds = Mathf.Min(defaultHoldDurationSeconds, 0.35f);
                fadeOutDurationSeconds = Mathf.Min(defaultFadeOutDurationSeconds, 0.25f);
                postBlackHoldSeconds = 0f;
                fadeInStartScale = restScale;
                fadeOutEndScale = restScale;
                glowAmplitude = 0f;
                holdPulseScaleAmplitude = 0f;
            }
            else
            {
                preBlackHoldSeconds = defaultPreBlackHoldSeconds;
                fadeInDurationSeconds = defaultFadeInDurationSeconds;
                holdDurationSeconds = defaultHoldDurationSeconds;
                fadeOutDurationSeconds = defaultFadeOutDurationSeconds;
                postBlackHoldSeconds = defaultPostBlackHoldSeconds;
                fadeInStartScale = defaultFadeInStartScale;
                fadeOutEndScale = defaultFadeOutEndScale;
                glowAmplitude = defaultGlowAmplitude;
                holdPulseScaleAmplitude = defaultHoldPulseScaleAmplitude;
            }
        }

        private void Awake()
        {
            // Guards against a one-frame flash of the logo at its serialized default alpha
            // (CanvasGroup defaults to 1) if this was ever wired by hand instead of through
            // IntroSceneSetup, which already saves alpha 0 into the scene.
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Plays the sequence once. <paramref name="onSequenceComplete"/> fires exactly once,
        /// whether the sequence finishes naturally, is skipped, or cannot play at all (missing
        /// references, no sprite, or called outside Play Mode).
        /// </summary>
        public void Play(Action onSequenceComplete)
        {
            if (hasStarted)
            {
                // Already playing, skipped, or complete: this caller's own callback must still
                // fire so it is never silently dropped, even though no second sequence starts.
                onSequenceComplete?.Invoke();
                return;
            }

            hasStarted = true;
            onComplete = onSequenceComplete;

            if (!CanAnimate())
            {
                Complete();
                return;
            }

            ApplyLogoState(0f, fadeInStartScale, 1f - glowAmplitude);
            runningSequence = StartCoroutine(SequenceRoutine());
        }

        /// <summary>Wired to a tap anywhere on the intro. Safe to call more than once, and safe
        /// to call before <see cref="Play"/> — a later <see cref="Play"/> call still resolves its
        /// own callback immediately in that case instead of starting a suppressed animation.
        /// </summary>
        public void Skip()
        {
            if (!allowSkip || hasCompleted)
            {
                return;
            }

            hasStarted = true;
            StopRunningSequence();
            ApplyLogoState(0f, restScale, 1f);
            Complete();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Skip();
        }

        private bool CanAnimate()
        {
            return Application.isPlaying
                && isActiveAndEnabled
                && logoCanvasGroup != null
                && logoRectTransform != null
                && logoImage != null
                && logoImage.sprite != null;
        }

        private IEnumerator SequenceRoutine()
        {
            yield return WaitUnscaled(preBlackHoldSeconds);

            yield return TweenLogo(
                0f, 1f,
                fadeInStartScale, restScale,
                1f - glowAmplitude, 1f,
                fadeInDurationSeconds, fadeInEase);

            yield return HoldWithPulse(holdDurationSeconds);

            yield return TweenLogo(
                1f, 0f,
                restScale, fadeOutEndScale,
                1f, 1f,
                fadeOutDurationSeconds, fadeOutEase);

            yield return WaitUnscaled(postBlackHoldSeconds);

            runningSequence = null;
            Complete();
        }

        private IEnumerator TweenLogo(
            float fromAlpha, float toAlpha,
            float fromScale, float toScale,
            float fromBrightness, float toBrightness,
            float duration, AnimationCurve ease)
        {
            if (duration <= 0f)
            {
                ApplyLogoState(toAlpha, toScale, toBrightness);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Unscaled: the intro must keep moving even if something has paused game time.
                elapsed += Time.unscaledDeltaTime;
                float t = Evaluate(ease, elapsed / duration);
                ApplyLogoState(
                    Mathf.Lerp(fromAlpha, toAlpha, t),
                    Mathf.Lerp(fromScale, toScale, t),
                    Mathf.Lerp(fromBrightness, toBrightness, t));
                yield return null;
            }

            ApplyLogoState(toAlpha, toScale, toBrightness);
        }

        private IEnumerator HoldWithPulse(float duration)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Zero at both ends, peaking at the midpoint: one smooth breath in and back out,
                // so it starts and ends exactly at rest with no pop against the fades on either
                // side, and never doubles back on itself the way a full sine cycle would.
                float envelope = Mathf.Sin(t * Mathf.PI);
                float scale = restScale * (1f + (holdPulseScaleAmplitude * envelope));
                float brightness = 1f - (glowAmplitude * 0.3f * envelope);
                ApplyLogoState(1f, scale, brightness);
                yield return null;
            }

            ApplyLogoState(1f, restScale, 1f);
        }

        /// <summary>
        /// A quick, energetic start settling gently into rest: <c>1 - (1-t)^2</c>. Reads as the
        /// logo materializing rather than a plain linear-ish fade, using only a plain
        /// <see cref="AnimationCurve"/> — no tween library involved. Provably monotonic and
        /// overshoot-free across [0, 1] for these tangents (Hermite basis collapses to this
        /// closed form), so it stays a safe default without needing runtime clamping.
        /// </summary>
        private static AnimationCurve BuildRevealEase()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2f),
                new Keyframe(1f, 1f, 0f, 0f));
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(seconds);
        }

        private void ApplyLogoState(float alpha, float scale, float brightness)
        {
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = alpha;
            }

            if (logoRectTransform != null)
            {
                logoRectTransform.localScale = Vector3.one * scale;
            }

            if (logoImage != null)
            {
                logoImage.color = new Color(brightness, brightness, brightness, 1f);
            }
        }

        private void Complete()
        {
            if (hasCompleted)
            {
                return;
            }

            hasCompleted = true;
            StopRunningSequence();

            // Cleared before invoking: a callback that somehow re-enters Play/Skip must not chain
            // into itself through a stale reference.
            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        private void StopRunningSequence()
        {
            if (runningSequence == null)
            {
                return;
            }

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StopCoroutine(runningSequence);
            }

            runningSequence = null;
        }

        private void OnDisable()
        {
            StopRunningSequence();
        }

        private static float Evaluate(AnimationCurve curve, float rawProgress)
        {
            float t = Mathf.Clamp01(rawProgress);
            return curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by scene setup and tests.</summary>
        public void SetAuthoringReferences(
            CanvasGroup canvasGroup, RectTransform rectTransform, Image image)
        {
            logoCanvasGroup = canvasGroup;
            logoRectTransform = rectTransform;
            logoImage = image;
        }
#endif
    }
}
