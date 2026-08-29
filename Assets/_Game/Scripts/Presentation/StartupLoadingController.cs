using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Owns the startup loading screen: replaceable background, dark overlay, status/percentage
    /// text, progress bar, and the fade that hands off to whatever plays next.
    /// </summary>
    /// <remarks>
    /// Knows nothing about scenes, Bootstrap, or the intro — a caller reports real progress and
    /// registers a completion callback, exactly how <see cref="IntroSequenceController"/> is driven
    /// by <c>RoyalDecisions.Composition.BootstrapController</c>. Mirrors that controller and
    /// <see cref="PanelFadeAnimator"/>: unscaled time, an <see cref="AnimationCurve"/> ease, a
    /// reduced-motion mode that shortens rather than removes decorative motion, and a completion
    /// guard so the callback can never fire more than once. The background sprite itself is never
    /// referenced by name here — whatever is assigned to <see cref="backgroundImage"/> in the Editor
    /// is what shows, so replacing the art later needs no code change.
    /// </remarks>
    public sealed class StartupLoadingController : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Background")]
        [Tooltip("Replaceable artwork. Assign any Sprite here (or leave empty) — this component "
            + "never loads art by filename. Missing sprite still shows the loading UI over black.")]
        [SerializeField] private Image backgroundImage;
        [Tooltip("Drives cover-fit (fill viewport, preserve aspect, crop overflow) for the background.")]
        [SerializeField] private AspectRatioFitter backgroundFitter;

        [Header("Loading UI")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text percentageText;
        [Tooltip("Optional. Fades in from transparent when loading begins. Leave empty to show the "
            + "loading content at full opacity immediately.")]
        [SerializeField] private CanvasGroup contentGroup;

        [Header("Blood Tube")]
        [Tooltip("Optional. Left-anchored RectTransform whose width is set to "
            + "fullTubeInnerWidth * displayedProgress every update — an actual RectMask2D reveal, "
            + "never a horizontal scale/stretch of the blood artwork itself.")]
        [SerializeField] private RectTransform bloodMask;
        [Tooltip("Optional. Stays at the tube's full inner width at all times; only bloodMask's "
            + "width changes. Also the 100% completion pulse's target.")]
        [SerializeField] private Graphic bloodFill;
        [Tooltip("Optional. Tracks the current fill boundary and adds a small restrained wobble, "
            + "skipped under reduced motion and settled once displayed progress reaches 100%. Its "
            + "colour alpha also fades in over the first sliver of progress so it never shows as a "
            + "stray dot at 0%.")]
        [SerializeField] private Graphic bloodLeadingEdge;
        [Tooltip("Optional. Provides the tube's full inner width — read from its own RectTransform "
            + "width each update (minus bloodMask's left inset, mirrored on the right), so this works "
            + "at any screen size without a hard-coded pixel value.")]
        [SerializeField] private RectTransform tubeInterior;

        [Header("Percentage")]
        [SerializeField] private bool showPercentage = true;

        [Header("Text")]
        [SerializeField] private string statusLabel = "YÜKLENİYOR...";
        [SerializeField] private string percentageFormat = "{0}%";

        [Header("Timing (unscaled seconds)")]
        [Tooltip("Never finishes faster than this, even if real startup work completes instantly — "
            + "avoids a one-frame 0->100 flash.")]
        [SerializeField] private float minimumDisplaySeconds = 1.75f;
        [Tooltip("How fast the visible bar catches up to the real target, in fraction-of-bar-per-second.")]
        [SerializeField] private float progressCatchUpSpeed = 1.6f;
        [SerializeField] private float completeHoldSeconds = 0.2f;
        [SerializeField] private float fadeOutSeconds = 0.4f;
        [SerializeField] private AnimationCurve fadeOutEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("How long LoadingContent takes to fade in from transparent when loading begins. "
            + "Skipped entirely (shown immediately) under reduced motion.")]
        [SerializeField] private float contentFadeInSeconds = 0.35f;

        // Purely decorative, restrained "alive" pulse on the status label — not part of the timing
        // model above, so unlike those fields it is never exposed for reduced-motion shortening;
        // reduced motion disables it outright instead (see ApplyStatusBreathing).
        private const float StatusBreathingPeriodSeconds = 2.6f;
        private const float StatusBreathingAmplitude = 0.12f;

        // Same reasoning for the blood tube's leading-edge wobble: restrained by design, disabled
        // outright under reduced motion rather than shortened (see ComputeLeadingEdgeWobble).
        private const float LeadingEdgeWobbleAmplitude = 3f;
        private const float LeadingEdgeWobbleSpeed = 2.4f;
        // Below this displayed progress the leading-edge cap fades in rather than snapping to full
        // opacity, so it never reads as a stray dot sitting at the very start of an empty tube.
        private const float LeadingEdgeFadeInThreshold = 0.035f;
        // How far the 100% completion pulse lerps bloodFill's colour toward white at its peak.
        // Image.color is a Color32 under the hood, which clamps to [0,255] on assignment — pushing
        // it above 1.0 to fake "over-bright" would just silently clamp back to opaque, so brightening
        // is done by lerping toward white instead, same reasoning as the retired BloodFillGraphic's
        // own SetBrightness.
        private const float CompletionPulseBrightenFraction = 0.4f;

        private float targetProgress;
        private float displayedProgress;
        private float elapsedUnscaledSeconds;
        private bool hasBegun;
        private bool completionRequested;
        private bool hasCompleted;
        private bool reducedMotionEnabled;
        private Action onComplete;
        private Coroutine runningRoutine;
        private Coroutine contentFadeInRoutine;

        private bool accessibilityDefaultsCaptured;
        private float defaultCompleteHoldSeconds;
        private float defaultFadeOutSeconds;
        private float statusBaseAlpha = 1f;
        private float leadingEdgeBaseAlpha = 1f;
        private Color bloodFillBaseColor = Color.white;

        /// <summary>Current smoothed 0..1 progress actually shown on screen. Exposed for tests/diagnostics.</summary>
        public float DisplayedProgress => displayedProgress;

        /// <summary>Current whole-number percentage shown on screen. Exposed for tests/diagnostics.</summary>
        public int DisplayedPercentage => StartupLoadingProgressMath.PercentageFor(displayedProgress);

        public bool HasCompleted => hasCompleted;

        private void Awake()
        {
            // Starts hidden and non-blocking — the same reasoning as IntroSequenceController.Awake
            // guarding against a one-frame flash, but here it also matters for ordering: whatever
            // plays before this (e.g. the studio intro) must never have this screen flash on top of
            // it or swallow its input before BeginLoading() is actually called. BeginLoading() is
            // solely responsible for revealing this (alpha 1, blocksRaycasts true).
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            statusBaseAlpha = statusText != null ? statusText.color.a : 1f;
            leadingEdgeBaseAlpha = bloodLeadingEdge != null ? bloodLeadingEdge.color.a : 1f;
            bloodFillBaseColor = bloodFill != null ? bloodFill.color : Color.white;

            ApplyPercentageVisibility();
            ApplyBackgroundFallback();
            ApplyStatusText();
            ApplyDisplayedProgress();
        }

        /// <summary>
        /// Shortens the hold and fade instead of removing them, matching
        /// <see cref="IntroSequenceController.SetReducedMotion"/> and
        /// <see cref="PanelFadeAnimator.SetReducedMotion"/>. Progress smoothing itself is left alone —
        /// it communicates real work finishing, not decoration. Call before <see cref="BeginLoading"/>.
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            reducedMotionEnabled = enabled;

            if (!accessibilityDefaultsCaptured)
            {
                defaultCompleteHoldSeconds = completeHoldSeconds;
                defaultFadeOutSeconds = fadeOutSeconds;
                accessibilityDefaultsCaptured = true;
            }

            completeHoldSeconds = enabled ? Mathf.Min(defaultCompleteHoldSeconds, 0.05f) : defaultCompleteHoldSeconds;
            fadeOutSeconds = enabled ? Mathf.Min(defaultFadeOutSeconds, 0.05f) : defaultFadeOutSeconds;
        }

        /// <summary>
        /// Shows the loading screen and starts the display-progress/minimum-duration timer. Safe to
        /// call more than once — only the first call has any effect.
        /// </summary>
        public void BeginLoading()
        {
            if (hasBegun)
            {
                return;
            }

            hasBegun = true;
            elapsedUnscaledSeconds = 0f;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            BeginContentFadeIn();
            EnsureRoutineRunning();
        }

        /// <summary>
        /// Fades <see cref="contentGroup"/> in from transparent, or shows it immediately under
        /// reduced motion / outside Play Mode. Purely decorative — never blocks or delays anything
        /// else in the sequence, mirroring how <see cref="fadeOutSeconds"/> is skipped rather than
        /// awaited when it cannot animate.
        /// </summary>
        private void BeginContentFadeIn()
        {
            if (contentGroup == null)
            {
                return;
            }

            if (reducedMotionEnabled || contentFadeInSeconds <= 0f || !CanRunCoroutines())
            {
                contentGroup.alpha = 1f;
                return;
            }

            contentGroup.alpha = 0f;
            contentFadeInRoutine = StartCoroutine(ContentFadeInRoutine());
        }

        private IEnumerator ContentFadeInRoutine()
        {
            float elapsed = 0f;

            while (elapsed < contentFadeInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                contentGroup.alpha = Mathf.Clamp01(elapsed / contentFadeInSeconds);
                yield return null;
            }

            contentGroup.alpha = 1f;
            contentFadeInRoutine = null;
        }

        /// <summary>
        /// Reports real startup progress in <c>0..1</c>. The visible bar catches up smoothly rather
        /// than jumping; out-of-range values are clamped. Safe to call before <see cref="BeginLoading"/>.
        /// </summary>
        public void ReportProgress(float progress01)
        {
            targetProgress = StartupLoadingProgressMath.ClampProgress(progress01);

            if (!CanRunCoroutines())
            {
                displayedProgress = targetProgress;
                ApplyDisplayedProgress();
            }
        }

        /// <summary>
        /// Marks real startup work as finished. <paramref name="onLoadingComplete"/> fires exactly
        /// once, once the bar has visibly reached 100%, the minimum display duration has elapsed, the
        /// hold has passed, and the screen has faded out — or immediately if this cannot animate
        /// (outside Play Mode, or this component disabled). Safe to call more than once: every
        /// caller's callback still fires exactly once, but only the first call drives the sequence.
        /// </summary>
        public void CompleteLoading(Action onLoadingComplete)
        {
            if (hasCompleted)
            {
                onLoadingComplete?.Invoke();
                return;
            }

            if (onLoadingComplete != null)
            {
                onComplete += onLoadingComplete;
            }

            if (completionRequested)
            {
                return;
            }

            completionRequested = true;
            targetProgress = 1f;

            if (!CanRunCoroutines())
            {
                displayedProgress = 1f;
                ApplyDisplayedProgress();
                Complete();
                return;
            }

            // Guards against a caller invoking CompleteLoading before BeginLoading in Play Mode —
            // without this, the drive routine (which only BeginLoading otherwise starts) would never
            // run and the screen would hang forever instead of completing.
            EnsureRoutineRunning();
        }

        private void EnsureRoutineRunning()
        {
            if (runningRoutine == null && CanRunCoroutines())
            {
                runningRoutine = StartCoroutine(DriveRoutine());
            }
        }

        private IEnumerator DriveRoutine()
        {
            while (!hasCompleted)
            {
                float delta = Time.unscaledDeltaTime;
                elapsedUnscaledSeconds += delta;
                displayedProgress = StartupLoadingProgressMath.AdvanceDisplayed(
                    displayedProgress, targetProgress, progressCatchUpSpeed, delta);
                ApplyDisplayedProgress();
                ApplyStatusBreathing();

                if (StartupLoadingProgressMath.ShouldBeginFadeOut(
                        completionRequested, displayedProgress, elapsedUnscaledSeconds, minimumDisplaySeconds))
                {
                    break;
                }

                yield return null;
            }

            if (hasCompleted)
            {
                yield break;
            }

            runningRoutine = null;
            yield return FinishRoutine();
        }

        private IEnumerator FinishRoutine()
        {
            displayedProgress = 1f;
            ApplyDisplayedProgress();

            yield return PlayCompletionHoldRoutine();
            yield return FadeOutRoutine();

            Complete();
        }

        /// <summary>
        /// The existing completion hold (unchanged duration), now with one restrained blood/glass
        /// brightness pulse layered on top when this can animate — zero at both ends, peaking at the
        /// midpoint, so it never pops in or leaves a stray bright frame. Skipped under reduced motion,
        /// which already shortens <see cref="completeHoldSeconds"/> to a very short static hold via
        /// <see cref="SetReducedMotion"/>. Never touches <see cref="hasCompleted"/> or
        /// <see cref="completionRequested"/> — purely decorative, so it cannot double-complete loading.
        /// </summary>
        private IEnumerator PlayCompletionHoldRoutine()
        {
            float duration = completeHoldSeconds;

            if (reducedMotionEnabled || bloodFill == null || duration <= 0f)
            {
                yield return WaitUnscaled(duration);
                if (bloodFill != null)
                {
                    bloodFill.color = bloodFillBaseColor;
                }

                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(t * Mathf.PI);
                bloodFill.color = Color.Lerp(bloodFillBaseColor, Color.white, envelope * CompletionPulseBrightenFraction);
                yield return null;
            }

            bloodFill.color = bloodFillBaseColor;
        }

        private IEnumerator FadeOutRoutine()
        {
            if (canvasGroup == null || fadeOutSeconds <= 0f)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }

                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Evaluate(fadeOutEase, elapsed / fadeOutSeconds);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        private void Complete()
        {
            if (hasCompleted)
            {
                return;
            }

            hasCompleted = true;
            StopRunningRoutine();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            // Cleared before invoking: a callback that somehow re-enters CompleteLoading must not
            // chain into itself through a stale reference, same guard as IntroSequenceController.
            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        private void ApplyBackgroundFallback()
        {
            if (backgroundImage == null)
            {
                return;
            }

            Sprite sprite = backgroundImage.sprite;
            backgroundImage.color = sprite != null ? Color.white : Color.black;
            // Always enabled: sprite art, or a flat black fallback so the frame is never punched
            // through to whatever renders behind the canvas.
            backgroundImage.enabled = true;

            if (backgroundFitter == null)
            {
                return;
            }

            if (sprite == null || sprite.rect.height <= 0f)
            {
                backgroundFitter.enabled = false;
                return;
            }

            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            backgroundFitter.enabled = true;
        }

        private void ApplyStatusText()
        {
            if (statusText != null)
            {
                statusText.text = statusLabel;
            }
        }

        /// <summary>
        /// Tiny restrained opacity pulse on the status label — "alive but restrained", never removed
        /// under reduced motion but pinned back to its authored alpha instead, the same
        /// shorten-not-strip philosophy <see cref="SetReducedMotion"/> already applies elsewhere.
        /// </summary>
        private void ApplyStatusBreathing()
        {
            if (statusText == null)
            {
                return;
            }

            float breathing = reducedMotionEnabled
                ? 1f
                : 1f - StatusBreathingAmplitude
                    * (1f - Mathf.Cos(2f * Mathf.PI * elapsedUnscaledSeconds / StatusBreathingPeriodSeconds))
                    * 0.5f;

            Color color = statusText.color;
            color.a = statusBaseAlpha * breathing;
            statusText.color = color;
        }

        /// <summary>
        /// Applied both at <see cref="Awake"/> and immediately after <c>SetAuthoringReferences</c>
        /// (Editor/tests): <c>AddComponent</c> already runs <see cref="Awake"/> before that wiring
        /// call can supply the real <see cref="showPercentage"/> value, so relying on <see cref="Awake"/>
        /// alone would apply whatever the compiled default happened to be instead.
        /// </summary>
        private void ApplyPercentageVisibility()
        {
            if (percentageText != null)
            {
                percentageText.gameObject.SetActive(showPercentage);
            }
        }

        private void ApplyDisplayedProgress()
        {
            if (percentageText != null)
            {
                percentageText.text = string.Format(percentageFormat, DisplayedPercentage);
            }

            ApplyBloodTube();
        }

        /// <summary>
        /// Drives the blood-tube visual from <see cref="displayedProgress"/> only — never an
        /// independent animation. <see cref="bloodMask"/>'s width is the actual reveal (an honest
        /// RectMask2D clip, not a horizontal scale of <see cref="bloodFill"/>, which always stays at
        /// <see cref="ComputeTubeInnerWidth"/>'s full value).
        /// </summary>
        private void ApplyBloodTube()
        {
            float innerWidth = ComputeTubeInnerWidth();

            if (bloodMask != null)
            {
                Vector2 size = bloodMask.sizeDelta;
                size.x = innerWidth * displayedProgress;
                bloodMask.sizeDelta = size;
            }

            if (bloodFill != null)
            {
                RectTransform fillRect = bloodFill.rectTransform;
                Vector2 size = fillRect.sizeDelta;
                size.x = innerWidth;
                fillRect.sizeDelta = size;
            }

            if (bloodLeadingEdge != null && bloodMask != null)
            {
                RectTransform edgeRect = bloodLeadingEdge.rectTransform;
                Vector2 pos = edgeRect.anchoredPosition;
                // bloodMask's own left inset is the same coordinate origin its width grows from, so
                // the edge must add it too — both share tubeInterior as their parent, and the mask's
                // actual right edge in that shared local space is leftInset + maskWidth, not maskWidth
                // alone.
                pos.x = bloodMask.anchoredPosition.x + (innerWidth * displayedProgress);
                pos.y = ComputeLeadingEdgeWobble();
                edgeRect.anchoredPosition = pos;

                Color edgeColor = bloodLeadingEdge.color;
                edgeColor.a = leadingEdgeBaseAlpha
                    * Mathf.Clamp01(displayedProgress / LeadingEdgeFadeInThreshold);
                bloodLeadingEdge.color = edgeColor;
            }
        }

        /// <summary>
        /// The tube's inner fillable width, read live from <see cref="tubeInterior"/>'s own
        /// RectTransform rather than a hard-coded constant, so it is correct at any screen size.
        /// Assumes <see cref="bloodMask"/> is inset from <see cref="tubeInterior"/>'s left edge by the
        /// same amount mirrored on the right (how <c>StartupLoadingSetup</c> authors it).
        /// </summary>
        private float ComputeTubeInnerWidth()
        {
            if (tubeInterior == null || bloodMask == null)
            {
                return 0f;
            }

            float leftInset = bloodMask.anchoredPosition.x;
            return Mathf.Max(0f, tubeInterior.rect.width - (2f * leftInset));
        }

        /// <summary>Tiny restrained vertical wobble on the leading edge — disabled under reduced
        /// motion, and settles to zero once displayed progress visibly reaches 100%.</summary>
        private float ComputeLeadingEdgeWobble()
        {
            if (reducedMotionEnabled || displayedProgress >= 1f)
            {
                return 0f;
            }

            return LeadingEdgeWobbleAmplitude * Mathf.Sin(elapsedUnscaledSeconds * LeadingEdgeWobbleSpeed);
        }

        private bool CanRunCoroutines()
        {
            return Application.isPlaying && isActiveAndEnabled;
        }

        private void OnDisable()
        {
            StopRunningRoutine();

            if (contentFadeInRoutine != null)
            {
                if (Application.isPlaying && isActiveAndEnabled)
                {
                    StopCoroutine(contentFadeInRoutine);
                }

                contentFadeInRoutine = null;
            }
        }

        private void StopRunningRoutine()
        {
            if (runningRoutine == null)
            {
                return;
            }

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StopCoroutine(runningRoutine);
            }

            runningRoutine = null;
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(seconds);
        }

        private static float Evaluate(AnimationCurve curve, float rawProgress)
        {
            float t = Mathf.Clamp01(rawProgress);
            return curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by scene setup and tests.</summary>
        public void SetAuthoringReferences(
            CanvasGroup group,
            Image background,
            AspectRatioFitter backgroundCoverFitter,
            TMP_Text status,
            TMP_Text percentage,
            CanvasGroup content = null,
            bool showPercentageValue = true)
        {
            canvasGroup = group;
            backgroundImage = background;
            backgroundFitter = backgroundCoverFitter;
            statusText = status;
            percentageText = percentage;
            contentGroup = content;
            showPercentage = showPercentageValue;
            ApplyPercentageVisibility();
        }

        /// <summary>Editor-only wiring hook for the blood-tube progress visual. Every parameter is
        /// optional — leaving any (or all) of them unassigned degrades to that piece of the tube
        /// simply not updating, never a blocked startup.</summary>
        public void SetBloodTubeAuthoringReferences(
            RectTransform mask,
            Graphic fill,
            Graphic leadingEdge,
            RectTransform interior)
        {
            bloodMask = mask;
            bloodFill = fill;
            bloodLeadingEdge = leadingEdge;
            tubeInterior = interior;
            bloodFillBaseColor = bloodFill != null ? bloodFill.color : Color.white;
        }

        [ContextMenu("Debug/Begin Loading")]
        private void DebugBeginLoading()
        {
            BeginLoading();
        }

        [ContextMenu("Debug/Report 65% Progress")]
        private void DebugReportProgress()
        {
            ReportProgress(0.65f);
        }

        [ContextMenu("Debug/Complete Loading")]
        private void DebugCompleteLoading()
        {
            CompleteLoading(() => Debug.Log("[StartupLoadingController] Debug complete callback fired."));
        }
#endif
    }
}
