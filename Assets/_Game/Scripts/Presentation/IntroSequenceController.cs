using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Plays the coded startup intro: an AS mark fading and scaling in over black, then the
    /// "ARILLA GAMES" wordmark revealing left-to-right, holding briefly, then fading back to black.
    /// </summary>
    /// <remarks>
    /// Purely visual and self-contained: it knows nothing about scenes. <see cref="Play"/> takes a
    /// completion callback, and the caller (<c>RoyalDecisions.Composition.BootstrapController</c>)
    /// decides what happens next through the existing scene-loading abstraction, so this component
    /// never duplicates <c>SceneManager</c> calls itself. Mirrors <see cref="PanelFadeAnimator"/>
    /// and <see cref="CardSwipeController"/>: unscaled time, an <see cref="AnimationCurve"/> ease,
    /// no <c>Update</c> — every frame of motion runs inside a coroutine that stops itself once the
    /// sequence ends, is skipped, or cannot play at all.
    ///
    /// The mark and wordmark are separate sprites (<see cref="IntroSceneSetup"/> derives them once,
    /// as pixel-exact crops, from the master <c>ArillaGamesLogo.png</c>) so each can be sized
    /// independently. The wordmark reveal itself is a real clip: <see cref="wordmarkRevealMaskRect"/>
    /// carries a <c>RectMask2D</c> whose width animates from 0 to the wordmark's own full width,
    /// while <see cref="wordmarkImage"/> underneath it never moves or resizes — so what is rendered
    /// at every instant is always geometrically exact, never an approximation of a cover/feather
    /// blend over a shared, combined image.
    /// </remarks>
    public sealed class IntroSequenceController : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [Tooltip("Missing any of these safely skips straight to MainMenu.")]
        [SerializeField] private CanvasGroup logoCanvasGroup;
        [SerializeField] private RectTransform logoRectTransform;
        [SerializeField] private Image markImage;

        [Header("Timing (unscaled seconds)")]
        [SerializeField] private float preBlackHoldSeconds = 0.55f;
        [SerializeField] private float fadeInDurationSeconds = 0.90f;
        [SerializeField] private float holdDurationSeconds = 1.20f;
        [SerializeField] private float fadeOutDurationSeconds = 0.60f;
        [SerializeField] private float postBlackHoldSeconds = 0.15f;

        [Header("Motion")]
        [SerializeField] private float fadeInStartScale = 0.94f;
        [SerializeField] private float restScale = 1f;
        [SerializeField] private float fadeOutEndScale = 1.015f;
        [SerializeField] private AnimationCurve fadeInEase = BuildRevealEase();
        [SerializeField] private AnimationCurve fadeOutEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Reveal glow")]
        [Tooltip("How far the logo dims below full brightness at the start of the reveal. Kept "
            + "small so it reads as a glow, not a flash.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float glowAmplitude = 0.06f;

        [Tooltip("Extra uniform scale, and matching brightness dip, at the peak of the hold's "
            + "breathing pulse. The reference studio intro holds the completed logo perfectly "
            + "still, so this defaults to 0 — set above 0 only for a deliberate breathing effect.")]
        [Range(0f, 0.05f)]
        [SerializeField] private float holdPulseScaleAmplitude = 0f;

        [Header("Wordmark reveal")]
        [Tooltip("The 'ARILLA GAMES' wordmark, at its full final size. Never moves or resizes at "
            + "runtime — only wordmarkRevealMaskRect's width changes, which is what actually "
            + "reveals it left-to-right. Missing this safely skips straight to MainMenu, exactly "
            + "like a missing mark sprite.")]
        [SerializeField] private Image wordmarkImage;

        [Tooltip("The RectMask2D clip rect that reveals wordmarkImage. Left-pivoted; its width is "
            + "animated from 0 to the wordmark's own width over the reveal. Missing this safely "
            + "skips straight to MainMenu — without it the wordmark would otherwise be stuck "
            + "permanently invisible rather than gracefully degrading.")]
        [SerializeField] private RectTransform wordmarkRevealMaskRect;

        [Tooltip("Optional. A narrow soft-edged highlight that travels with the reveal edge and "
            + "disappears the instant the reveal completes — never a permanent fixture. Missing "
            + "this simply omits the highlight; the reveal itself is unaffected.")]
        [SerializeField] private Graphic wordmarkGlintImage;

        [Tooltip("Seconds after the AS-mark fade-in starts before the wordmark starts revealing. "
            + "A small positive gap after the fade-in finishes reads as a clean two-stage reveal "
            + "(mark settles, then the wordmark begins) rather than the wordmark cutting in early.")]
        [SerializeField] private float wordmarkRevealDelaySeconds = 1.00f;

        [SerializeField] private float wordmarkRevealDurationSeconds = 1.40f;
        [SerializeField] private AnimationCurve wordmarkRevealEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Peak opacity of the travelling glint highlight. Kept low so it reads as a sheen, "
            + "not a loading bar.")]
        [Range(0f, 1f)]
        [SerializeField] private float glintPeakAlpha = 0.35f;

        [Header("Audio")]
        [Tooltip("Optional. Missing audio service or a missing cue simply plays no sound; the "
            + "visual sequence is entirely unaffected either way.")]
        [SerializeField] private AudioService audioService;

        [Tooltip("Plays as the AS mark begins fading in. Skipped entirely in reduced motion.")]
        [SerializeField] private string logoRiseAudioEventId = "intro_logo_rise";

        [Tooltip("Plays as the reveal mask begins expanding. Never plays in reduced motion, since "
            + "there is no expanding reveal to synchronise it to there.")]
        [SerializeField] private string wordmarkSweepAudioEventId = "intro_wordmark_sweep";

        [Tooltip("Plays the instant the reveal mask reaches full width, whether by the timed "
            + "expansion or the instant reveal used in reduced motion.")]
        [SerializeField] private string resolveAudioEventId = "intro_resolve";

        [Header("Skip")]
        [SerializeField] private bool allowSkip = true;

        private Action onComplete;
        private Coroutine runningSequence;
        private Coroutine wordmarkRevealSequence;
        private bool hasStarted;
        private bool hasCompleted;
        private bool reducedMotionEnabled;

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
        private float defaultWordmarkRevealDelaySeconds;
        private float defaultWordmarkRevealDurationSeconds;

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
            reducedMotionEnabled = enabled;

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
                defaultWordmarkRevealDelaySeconds = wordmarkRevealDelaySeconds;
                defaultWordmarkRevealDurationSeconds = wordmarkRevealDurationSeconds;
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
                // No wipe: the wordmark simply appears together with the AS mark as part of the
                // same plain fade, exactly like every other element in reduced motion.
                wordmarkRevealDelaySeconds = 0f;
                wordmarkRevealDurationSeconds = 0f;
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
                wordmarkRevealDelaySeconds = defaultWordmarkRevealDelaySeconds;
                wordmarkRevealDurationSeconds = defaultWordmarkRevealDurationSeconds;
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

            // Same guard for the wordmark reveal: start fully masked (zero width) with the glint
            // hidden, regardless of how these were wired.
            SetRevealMaskWidth(0f);
            SetGlintAlpha(0f);
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
            ResetWordmarkReveal();
            StopIntroAudio();
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
                && markImage != null
                && markImage.sprite != null
                && wordmarkImage != null
                && wordmarkImage.sprite != null
                && wordmarkRevealMaskRect != null;
        }

        private IEnumerator SequenceRoutine()
        {
            yield return WaitUnscaled(preBlackHoldSeconds);

            // Started, not awaited: the wordmark reveal runs on its own unscaled timer, independent
            // of the fade-in's progress. Its delay is authored slightly longer than the fade-in
            // duration, so it begins just after the AS mark settles rather than overlapping it —
            // a clean two-stage cadence rather than the wordmark cutting in early.
            wordmarkRevealSequence = StartCoroutine(WordmarkRevealRoutine());

            // Reduced motion collapses the fade to a fraction of a second; the ~1.15s rise cue
            // would still be ringing out well after the visual is done, so it is skipped rather
            // than compressed.
            if (!reducedMotionEnabled)
            {
                PlayIntroCue(logoRiseAudioEventId);
            }

            yield return TweenLogo(
                0f, 1f,
                fadeInStartScale, restScale,
                1f - glowAmplitude, 1f,
                fadeInDurationSeconds, fadeInEase);

            // The wordmark reveal can still be running after the AS mark settles; this only ever
            // covers that tail (it is clamped to zero once the reveal finishes at or before the
            // fade-in itself, e.g. in reduced motion).
            float wordmarkTail = wordmarkRevealDelaySeconds + wordmarkRevealDurationSeconds - fadeInDurationSeconds;
            yield return WaitUnscaled(Mathf.Max(0f, wordmarkTail));

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
                // Both driven by the same amplitude: zero it and the hold is perfectly still, no
                // separate flag needed to fully flatten scale and brightness together.
                float brightness = 1f - (holdPulseScaleAmplitude * 1.8f * envelope);
                ApplyLogoState(1f, scale, brightness);
                yield return null;
            }

            ApplyLogoState(1f, restScale, 1f);
        }

        /// <summary>
        /// Grows <see cref="wordmarkRevealMaskRect"/> from zero to the wordmark's own full width on
        /// its own unscaled timer, independent of <see cref="TweenLogo"/>'s progress —
        /// <see cref="wordmarkRevealDelaySeconds"/> controls whether the two overlap or run
        /// back-to-back. <see cref="wordmarkImage"/> itself never moves or resizes here: only the
        /// mask's width changes, so at every instant what is rendered is an exact, un-stretched
        /// prefix of the full wordmark — never an approximation. No-op if the mask was never
        /// wired — <see cref="CanAnimate"/> already refuses to start the whole sequence in that
        /// case, so this only guards the coroutine itself.
        /// </summary>
        private IEnumerator WordmarkRevealRoutine()
        {
            if (wordmarkRevealMaskRect == null || wordmarkImage == null)
            {
                wordmarkRevealSequence = null;
                yield break;
            }

            SetRevealMaskWidth(0f);
            SetGlintAlpha(0f);

            yield return WaitUnscaled(wordmarkRevealDelaySeconds);

            float maxWidth = wordmarkImage.rectTransform.rect.width;
            float duration = wordmarkRevealDurationSeconds;
            if (duration <= 0f)
            {
                // Reduced motion: the wordmark simply appears at full width, so the sweep (a
                // motion-synced cue) is skipped, but the resolve accent still marks the same "now
                // fully revealed" moment it does in the full sequence.
                SetRevealMaskWidth(maxWidth);
                SetGlintAlpha(0f);
                PlayIntroCue(resolveAudioEventId);
                wordmarkRevealSequence = null;
                yield break;
            }

            PlayIntroCue(wordmarkSweepAudioEventId);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Evaluate(wordmarkRevealEase, elapsed / duration);
                SetRevealMaskWidth(maxWidth * t);
                UpdateGlint(t, maxWidth);
                yield return null;
            }

            SetRevealMaskWidth(maxWidth);
            SetGlintAlpha(0f);
            PlayIntroCue(resolveAudioEventId);
            wordmarkRevealSequence = null;
        }

        private void SetRevealMaskWidth(float width)
        {
            if (wordmarkRevealMaskRect == null)
            {
                return;
            }

            Vector2 size = wordmarkRevealMaskRect.sizeDelta;
            size.x = width;
            wordmarkRevealMaskRect.sizeDelta = size;
        }

        /// <summary>Moves the glint to the current reveal edge and fades it in/out around the
        /// midpoint so it never pops in or leaves a stray highlight once the reveal completes —
        /// a travelling accent, never a permanent fixture.</summary>
        private void UpdateGlint(float t, float maxWidth)
        {
            if (wordmarkGlintImage == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(t);
            float envelope = Mathf.Sin(clamped * Mathf.PI);
            Color color = wordmarkGlintImage.color;
            color.a = glintPeakAlpha * envelope;
            wordmarkGlintImage.color = color;

            // wordmarkGlintImage is a sibling of wordmarkRevealMaskRect's parent, positioned in
            // the same coordinate space; -maxWidth/2 is that parent's own left edge.
            RectTransform glintRect = wordmarkGlintImage.rectTransform;
            Vector2 position = glintRect.anchoredPosition;
            position.x = (-maxWidth * 0.5f) + (maxWidth * clamped);
            glintRect.anchoredPosition = position;
        }

        private void SetGlintAlpha(float alpha)
        {
            if (wordmarkGlintImage == null)
            {
                return;
            }

            Color color = wordmarkGlintImage.color;
            color.a = alpha;
            wordmarkGlintImage.color = color;
        }

        /// <summary>Forces the wordmark to its fully-revealed, glint-hidden end state. Safe to call
        /// even when the reveal never ran (e.g. skipped before <see cref="Play"/>): the whole logo
        /// (including this) is already hidden behind <see cref="logoCanvasGroup"/>'s alpha, so this
        /// only guards against a stale mid-reveal state if it were ever inspected or reused.</summary>
        private void ResetWordmarkReveal()
        {
            float maxWidth = wordmarkImage != null ? wordmarkImage.rectTransform.rect.width : 0f;
            SetRevealMaskWidth(maxWidth);
            SetGlintAlpha(0f);
        }

        /// <summary>No-op without an assigned service or cue ID — every "no sound" outcome here is
        /// normal, exactly like every other <see cref="IAudioService"/> caller in this codebase.</summary>
        private void PlayIntroCue(string audioEventId)
        {
            if (audioService == null || string.IsNullOrEmpty(audioEventId))
            {
                return;
            }

            audioService.Play(audioEventId);
        }

        /// <summary>Cuts any intro cue still playing. Only <see cref="Skip"/> calls this — natural
        /// completion lets the last cue's short tail decay into the following black hold instead.</summary>
        private void StopIntroAudio()
        {
            audioService?.StopSfx();
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

            Color tint = new Color(brightness, brightness, brightness, 1f);
            if (markImage != null)
            {
                markImage.color = tint;
            }

            if (wordmarkImage != null)
            {
                wordmarkImage.color = tint;
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
            StopCoroutineIfRunning(ref runningSequence);
            StopCoroutineIfRunning(ref wordmarkRevealSequence);
        }

        private void StopCoroutineIfRunning(ref Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StopCoroutine(coroutine);
            }

            coroutine = null;
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
            CanvasGroup canvasGroup, RectTransform rectTransform, Image mark)
        {
            logoCanvasGroup = canvasGroup;
            logoRectTransform = rectTransform;
            markImage = mark;
        }

        /// <summary>Editor-only wiring hook for the optional wordmark reveal elements.</summary>
        public void SetWordmarkAuthoringReferences(
            Image wordmark, RectTransform revealMaskRect, Graphic glintGraphic)
        {
            wordmarkImage = wordmark;
            wordmarkRevealMaskRect = revealMaskRect;
            wordmarkGlintImage = glintGraphic;
        }

        /// <summary>Editor-only wiring hook for the optional intro audio service.</summary>
        public void SetAudioAuthoringReferences(AudioService service)
        {
            audioService = service;
        }
#endif
    }
}
