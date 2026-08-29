using System;
using System.Collections;
using RoyalDecisions.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Plays a data-driven, tap-advanced cinematic sequence of full-screen illustrations and
    /// subtitles: a crossfade between two alternating image layers, a subtitle that fades in after
    /// it, and very gentle continuous zoom/pan while each slide is held.
    /// </summary>
    /// <remarks>
    /// Knows nothing about scenes, Bootstrap, MainMenu, or gameplay — a caller supplies
    /// <see cref="PrologueSequenceData"/> and a completion callback via <see cref="Play"/>, exactly
    /// how <see cref="IntroSequenceController"/> and <c>StartupLoadingController</c> are driven by
    /// their own callers. Mirrors those controllers: unscaled time, no third-party tweening, a
    /// reduced-motion mode that shortens/removes decorative motion, and a completion guard so the
    /// callback can never fire more than once. No story text or file name is hard-coded here —
    /// everything slide-specific comes from <see cref="PrologueSequenceData"/>, so replacing the
    /// story later needs no code change (only re-editing that asset).
    /// </remarks>
    public sealed class PrologueSequenceController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Optional. Missing data (or an empty slide list) fails open — Play completes "
            + "immediately instead of locking the player on a blank screen.")]
        [SerializeField] private PrologueSequenceData sequenceData;

        [Header("Slide Layers")]
        [Tooltip("Two alternating layers make a clean crossfade possible: the incoming slide fades "
            + "in on top of whichever layer is already showing, instead of both sides fading at once.")]
        [SerializeField] private Image slideLayerAImage;
        [SerializeField] private AspectRatioFitter slideLayerAFitter;
        [SerializeField] private Image slideLayerBImage;
        [SerializeField] private AspectRatioFitter slideLayerBFitter;

        [Header("Story Text")]
        [SerializeField] private CanvasGroup storyTextGroup;
        [SerializeField] private TMP_Text storyText;

        [Header("Continue Indicator")]
        [SerializeField] private TMP_Text continueIndicatorText;
        [SerializeField] private string continueLabel = "DEVAM ETMEK İÇİN DOKUN";

        [Header("Skip")]
        [SerializeField] private TMP_Text skipButtonLabel;
        [SerializeField] private string skipLabel = "ATLA";

        [Header("Fade Overlay")]
        [Tooltip("Optional. Fades to opaque right before the completion callback fires, so leaving "
            + "the prologue never hard-cuts.")]
        [SerializeField] private CanvasGroup fadeOverlayGroup;

        [Header("Audio")]
        [Tooltip("Optional. Absent audio is a supported configuration, same as every other "
            + "IAudioService caller in this project.")]
        [SerializeField] private AudioService audioService;

        [Tooltip("Plays once, looping, as soon as the sequence actually starts showing slides. "
            + "Treated as music (respects master/mute and the music volume setting), not SFX.")]
        [SerializeField] private string ambientCueId = "prologue_ambient";

        [Header("Timing (unscaled seconds)")]
        [SerializeField] private float imageCrossfadeSeconds = 0.45f;
        [SerializeField] private float subtitleFadeInDelaySeconds = 0.20f;
        [SerializeField] private float subtitleFadeInSeconds = 0.45f;
        [SerializeField] private float subtitleFadeOutSeconds = 0.15f;
        // Brief, so the final tap reads as "fade then Game", never as a black screen that lingers
        // long enough to feel stuck.
        [SerializeField] private float completionFadeSeconds = 0.25f;

        [Header("Motion")]
        [Tooltip("How long the gentle zoom/pan takes to reach its target, then holds. Skipped "
            + "entirely in reduced motion, regardless of each slide's configured motion style.")]
        [SerializeField] private float motionDurationSeconds = 9f;
        [SerializeField] private float zoomTargetScale = 1.03f;
        [SerializeField] private float panDistance = 22f;

        private int currentSlideIndex = -1;
        private bool layerAActive = true;
        private bool hasStarted;
        private bool hasCompleted;
        private bool isTransitioning;
        private bool reducedMotionEnabled;
        private Action onComplete;

        /// <summary>Test-injection seam, separate from the Inspector-facing <see cref="audioService"/>
        /// field — mirrors how <c>MainMenuController.Configure</c> accepts interfaces for tests while
        /// scene authoring wires concrete types. Takes priority when set.</summary>
        private IAudioService audioServiceOverride;

        private IAudioService ActiveAudioService => audioServiceOverride ?? audioService;

        private Coroutine slideRoutine;
        private Coroutine motionRoutine;
        private Coroutine autoAdvanceRoutine;
        private Coroutine completionRoutine;

        private bool accessibilityDefaultsCaptured;
        private float defaultImageCrossfadeSeconds;
        private float defaultSubtitleFadeInDelaySeconds;
        private float defaultSubtitleFadeInSeconds;
        private float defaultSubtitleFadeOutSeconds;
        private float defaultCompletionFadeSeconds;

        public bool HasCompleted => hasCompleted;

        /// <summary>-1 before <see cref="Play"/> has run; otherwise the slide currently shown.</summary>
        public int CurrentSlideIndex => currentSlideIndex;

        public int SlideCount => sequenceData != null ? sequenceData.SlideCount : 0;

        private PrologueSlideData CurrentSlide =>
            sequenceData != null && currentSlideIndex >= 0 && currentSlideIndex < SlideCount
                ? sequenceData.Slides[currentSlideIndex]
                : null;

        private void Awake()
        {
            // Guards against a one-frame flash of whatever the Inspector happened to leave alpha at,
            // the same reasoning as IntroSequenceController.Awake.
            if (storyTextGroup != null)
            {
                storyTextGroup.alpha = 0f;
            }

            if (fadeOverlayGroup != null)
            {
                fadeOverlayGroup.alpha = 0f;
                fadeOverlayGroup.blocksRaycasts = false;
            }

            SetLayerAlpha(slideLayerAImage, 0f);
            SetLayerAlpha(slideLayerBImage, 0f);

            if (skipButtonLabel != null)
            {
                skipButtonLabel.text = skipLabel;
            }

            if (continueIndicatorText != null)
            {
                continueIndicatorText.text = continueLabel;
            }
        }

        /// <summary>
        /// Shortens or removes decorative fades and disables the continuous zoom/pan, matching
        /// <see cref="IntroSequenceController.SetReducedMotion"/> and
        /// <see cref="PanelFadeAnimator.SetReducedMotion"/>. Tap-to-advance interaction itself is
        /// unaffected. Call before <see cref="Play"/>.
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            reducedMotionEnabled = enabled;

            if (!accessibilityDefaultsCaptured)
            {
                defaultImageCrossfadeSeconds = imageCrossfadeSeconds;
                defaultSubtitleFadeInDelaySeconds = subtitleFadeInDelaySeconds;
                defaultSubtitleFadeInSeconds = subtitleFadeInSeconds;
                defaultSubtitleFadeOutSeconds = subtitleFadeOutSeconds;
                defaultCompletionFadeSeconds = completionFadeSeconds;
                accessibilityDefaultsCaptured = true;
            }

            imageCrossfadeSeconds = Shortened(enabled, defaultImageCrossfadeSeconds);
            subtitleFadeInDelaySeconds = Shortened(enabled, defaultSubtitleFadeInDelaySeconds);
            subtitleFadeInSeconds = Shortened(enabled, defaultSubtitleFadeInSeconds);
            subtitleFadeOutSeconds = Shortened(enabled, defaultSubtitleFadeOutSeconds);
            completionFadeSeconds = Shortened(enabled, defaultCompletionFadeSeconds);
        }

        private static float Shortened(bool reduced, float defaultValue)
        {
            return reduced ? Mathf.Min(defaultValue, 0.05f) : defaultValue;
        }

        /// <summary>Injection seam for tests, which must never touch real audio hardware/settings.
        /// Call before <see cref="Play"/>.</summary>
        public void SetAudioService(IAudioService service)
        {
            audioServiceOverride = service;
        }

        /// <summary>
        /// Applies the player's current audio volume/mute settings to this sequence's own
        /// AudioService instance — called by <c>PrologueSceneController</c> before <see cref="Play"/>,
        /// mirroring how <c>BootstrapController</c>/<c>GameSceneController</c> each apply
        /// <c>GameSettings</c> to their own scene's AudioService, since Prologue owns a fresh
        /// AudioService instance rather than sharing one across scenes. Entirely separate from
        /// <see cref="SetReducedMotion"/> — Reduced Motion is a visual setting and must never affect
        /// whether audio plays.
        /// </summary>
        public void ApplyAudioSettings(float masterVolume, float musicVolume, float sfxVolume, bool masterMuted)
        {
            IAudioService service = ActiveAudioService;
            if (service == null)
            {
                return;
            }

            service.SetMasterVolume(masterVolume);
            service.SetMusicVolume(musicVolume);
            service.SetSfxVolume(sfxVolume);
            service.SetMasterMuted(masterMuted);
        }

        /// <summary>
        /// Plays the sequence once, starting at the first slide. <paramref name="onSequenceComplete"/>
        /// fires exactly once, whether the sequence finishes naturally, is skipped, or cannot play at
        /// all (no data, or an empty slide list).
        /// </summary>
        public void Play(Action onSequenceComplete)
        {
            if (hasStarted)
            {
                // Already playing, skipped, or complete: this caller's own callback must still fire
                // so it is never silently dropped, even though no second sequence starts.
                onSequenceComplete?.Invoke();
                return;
            }

            hasStarted = true;
            onComplete = onSequenceComplete;

            if (SlideCount <= 0)
            {
                Complete();
                return;
            }

            // Started once real slides are actually about to show — not before the fail-open check
            // above, so a misconfigured (empty) sequence never even briefly starts the ambient just
            // to immediately stop it again.
            StartAmbient();

            (Image incoming, RectTransform incomingRect, PrologueSlideData slide) = ApplySlideState(0);

            if (!CanAnimate())
            {
                SetLayerAlpha(incoming, 1f);
                SetStoryTextAlpha(1f);
                return;
            }

            isTransitioning = true;
            slideRoutine = StartCoroutine(EnterSlideRoutine(incoming, incomingRect, slide));
        }

        /// <summary>
        /// Wired to a tap/click anywhere on the prologue. Advances to the next slide, or completes
        /// the sequence if already on the last one. Ignored while a transition is in progress (so
        /// spam-tapping cannot skip more than one slide at a time), before <see cref="Play"/>, or
        /// after completion.
        /// </summary>
        public void OnTapAdvance()
        {
            if (!hasStarted || hasCompleted || isTransitioning)
            {
                return;
            }

            int next = PrologueSequenceMath.NextSlideIndexOrCompletion(currentSlideIndex, SlideCount);
            if (next < 0)
            {
                // Already on the final slide: there is no next slide to transition to, so this must
                // complete right here in this same call — never start a transition coroutine (which
                // would need a further tap to resolve) and never show an index-out-of-range "phantom"
                // slide.
                Complete();
                return;
            }

            StopMotionRoutine();
            StopAutoAdvanceRoutine();

            if (!CanAnimate())
            {
                (Image incoming, _, _) = ApplySlideState(next);
                SetLayerAlpha(incoming, 1f);
                SetStoryTextAlpha(1f);
                return;
            }

            isTransitioning = true;
            slideRoutine = StartCoroutine(AdvanceToSlideRoutine(next));
        }

        /// <summary>Wired to the ATLA button. Completes the whole prologue immediately, from any
        /// slide. Safe to call more than once, and safe to call before <see cref="Play"/>.</summary>
        public void Skip()
        {
            if (hasCompleted)
            {
                return;
            }

            hasStarted = true;
            Complete();
        }

        private bool CanAnimate()
        {
            return Application.isPlaying && isActiveAndEnabled;
        }

        private (Image incoming, RectTransform incomingRect, PrologueSlideData slide) ApplySlideState(int index)
        {
            currentSlideIndex = PrologueSequenceMath.ClampSlideIndex(index, SlideCount);
            PrologueSlideData slide = CurrentSlide;

            // Exactly once per slide by construction: ApplySlideState is the single choke point both
            // Play() (first slide) and OnTapAdvance() (every later slide) call to make a slide
            // current, and OnTapAdvance's isTransitioning guard already prevents this from running
            // twice for the same slide — no extra de-duplication needed here for spam taps.
            PlaySlideAccentCue(slide);

            Image incoming = layerAActive ? slideLayerBImage : slideLayerAImage;
            AspectRatioFitter incomingFitter = layerAActive ? slideLayerBFitter : slideLayerAFitter;
            layerAActive = !layerAActive;

            RectTransform incomingRect = incoming != null ? incoming.rectTransform : null;

            ApplySlideSprite(incoming, incomingFitter, slide?.Illustration);
            SetLayerAlpha(incoming, 0f);
            ResetLayerTransform(incomingRect);

            if (incoming != null)
            {
                incoming.transform.SetAsLastSibling();
            }

            if (storyText != null)
            {
                storyText.text = slide?.Subtitle ?? string.Empty;
            }

            return (incoming, incomingRect, slide);
        }

        private IEnumerator EnterSlideRoutine(Image incoming, RectTransform incomingRect, PrologueSlideData slide)
        {
            float duration = Mathf.Max(
                imageCrossfadeSeconds, subtitleFadeInDelaySeconds + subtitleFadeInSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetLayerAlpha(incoming, PrologueSequenceMath.FadeInAlpha(elapsed, 0f, imageCrossfadeSeconds));
                SetStoryTextAlpha(PrologueSequenceMath.FadeInAlpha(
                    elapsed, subtitleFadeInDelaySeconds, subtitleFadeInSeconds));
                yield return null;
            }

            SetLayerAlpha(incoming, 1f);
            SetStoryTextAlpha(1f);
            isTransitioning = false;
            slideRoutine = null;

            motionRoutine = StartCoroutine(MotionRoutine(incomingRect, slide?.Motion ?? PrologueSlideMotion.None));

            if (slide != null && slide.HasAutoAdvance)
            {
                autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine(slide.AutoAdvanceSeconds));
            }
        }

        private IEnumerator AdvanceToSlideRoutine(int nextIndex)
        {
            yield return FadeStoryTextOutRoutine();
            (Image incoming, RectTransform incomingRect, PrologueSlideData slide) = ApplySlideState(nextIndex);
            yield return EnterSlideRoutine(incoming, incomingRect, slide);
        }

        private IEnumerator FadeStoryTextOutRoutine()
        {
            if (storyTextGroup == null || subtitleFadeOutSeconds <= 0f)
            {
                SetStoryTextAlpha(0f);
                yield break;
            }

            float start = storyTextGroup.alpha;
            float elapsed = 0f;

            while (elapsed < subtitleFadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / subtitleFadeOutSeconds);
                SetStoryTextAlpha(Mathf.Lerp(start, 0f, t));
                yield return null;
            }

            SetStoryTextAlpha(0f);
        }

        /// <summary>
        /// Very gentle, continuous zoom/pan for as long as this slide is shown: ramps to its target
        /// over <see cref="motionDurationSeconds"/>, then holds — never loops or reverses, so it
        /// always reads as a single calm drift rather than a repeating or jittery motion.
        /// </summary>
        private IEnumerator MotionRoutine(RectTransform rect, PrologueSlideMotion motion)
        {
            if (reducedMotionEnabled || motion == PrologueSlideMotion.None || rect == null)
            {
                yield break;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, motionDurationSeconds);

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - (2f * t)); // smoothstep: gentle ease, no library needed

                if (motion == PrologueSlideMotion.Zoom)
                {
                    float scale = Mathf.Lerp(1f, zoomTargetScale, eased);
                    rect.localScale = new Vector3(scale, scale, 1f);
                }
                else if (motion == PrologueSlideMotion.Pan)
                {
                    // Horizontal, not vertical: cover-fit (EnvelopeParent) on these near-9:16
                    // illustrations matches the container's height almost exactly at the 1080x1920
                    // reference, leaving ~zero vertical overflow to pan through — a vertical pan
                    // would open a visible gap at the top or bottom rather than reveal more image.
                    // The horizontal axis is what actually overflows (and overflows more on taller
                    // devices), so that is the axis with real room to drift into.
                    rect.anchoredPosition = new Vector2(Mathf.Lerp(0f, panDistance, eased), 0f);
                }

                yield return null;
            }
        }

        private IEnumerator AutoAdvanceRoutine(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            autoAdvanceRoutine = null;
            OnTapAdvance();
        }

        private void Complete()
        {
            if (hasCompleted)
            {
                return;
            }

            hasCompleted = true;
            StopAllRoutines();

            // Stopped immediately and synchronously — well before the visual fade even finishes, let
            // alone before Game loads — so neither the ambient nor a still-ringing accent cue can
            // ever bleed into the next scene. Covers both natural completion and Skip()/ATLA, since
            // both funnel through this one method, and never repeats on a second Complete() call
            // thanks to the hasCompleted guard above.
            StopPrologueAudio();

            if (fadeOverlayGroup != null && CanAnimate() && completionFadeSeconds > 0f)
            {
                // Self-driving: once started, this coroutine reaches InvokeCompletion() on its own a
                // fixed ~completionFadeSeconds later, with no further input required. Tracked in a
                // field (like every other routine here) purely so OnDisable's safety net below can
                // tell whether it is still the one that owes InvokeCompletion() a call.
                completionRoutine = StartCoroutine(CompletionFadeRoutine());
                return;
            }

            if (fadeOverlayGroup != null)
            {
                fadeOverlayGroup.alpha = 1f;
            }

            InvokeCompletion();
        }

        private IEnumerator CompletionFadeRoutine()
        {
            float start = fadeOverlayGroup.alpha;
            float elapsed = 0f;

            while (elapsed < completionFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlayGroup.alpha = Mathf.Lerp(start, 1f, Mathf.Clamp01(elapsed / completionFadeSeconds));
                yield return null;
            }

            fadeOverlayGroup.alpha = 1f;
            completionRoutine = null;
            InvokeCompletion();
        }

        private void InvokeCompletion()
        {
            // Cleared before invoking: a callback that somehow re-enters Play/Skip must not chain
            // into itself through a stale reference, same guard as IntroSequenceController.
            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        private static void ApplySlideSprite(Image image, AspectRatioFitter fitter, Sprite sprite)
        {
            if (image != null)
            {
                image.sprite = sprite;
                // Preserves whatever alpha the crossfade currently has; only RGB and sprite change
                // here. White lets an assigned sprite render at its own colours; black is the safe
                // fallback frame when a slide has no illustration yet.
                Color colour = sprite != null ? Color.white : Color.black;
                colour.a = image.color.a;
                image.color = colour;
            }

            if (fitter == null)
            {
                return;
            }

            if (sprite == null || sprite.rect.height <= 0f)
            {
                fitter.enabled = false;
                return;
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            fitter.enabled = true;
        }

        private static void ResetLayerTransform(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetLayerAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Color colour = image.color;
            colour.a = alpha;
            image.color = colour;
        }

        private void SetStoryTextAlpha(float alpha)
        {
            if (storyTextGroup != null)
            {
                storyTextGroup.alpha = alpha;
            }
        }

        /// <summary>No-op without an assigned service or cue ID — every "no sound" outcome here is
        /// normal, exactly like every other <see cref="IAudioService"/> caller in this codebase.
        /// Plays as music (looping, respecting the music/master volume and mute settings) rather than
        /// as an SFX one-shot, since this is a continuous bed, not a discrete event.</summary>
        private void StartAmbient()
        {
            if (string.IsNullOrEmpty(ambientCueId))
            {
                return;
            }

            ActiveAudioService?.PlayMusic(ambientCueId);
        }

        /// <summary>Plays a slide's optional one-shot accent. Silent when the slide has none —
        /// most slides are expected to.</summary>
        private void PlaySlideAccentCue(PrologueSlideData slide)
        {
            if (slide == null || !slide.HasAccentCue)
            {
                return;
            }

            ActiveAudioService?.Play(slide.AccentCueId);
        }

        /// <summary>Stops the ambient and cuts any accent cue still ringing out, so neither can bleed
        /// into whatever scene loads next. Reduced Motion never reaches this — audio only ever stops
        /// here, on genuine completion, not as a side effect of the visual accessibility setting.</summary>
        private void StopPrologueAudio()
        {
            ActiveAudioService?.StopMusic();
            ActiveAudioService?.StopSfx();
        }

        private void OnDisable()
        {
            StopAllRoutines();

            // Completion was already requested (hasCompleted) but the fade coroutine never reached
            // its own InvokeCompletion() call — e.g. this component was disabled mid-fade, which also
            // stops the coroutine at the engine level. The player must never be left on a black
            // screen with no way to reach Game, so resolve the callback here instead of losing it.
            if (hasCompleted && onComplete != null)
            {
                InvokeCompletion();
            }
        }

        private void StopAllRoutines()
        {
            StopCoroutineIfRunning(ref slideRoutine);
            StopMotionRoutine();
            StopAutoAdvanceRoutine();
            StopCoroutineIfRunning(ref completionRoutine);
        }

        private void StopMotionRoutine()
        {
            StopCoroutineIfRunning(ref motionRoutine);
        }

        private void StopAutoAdvanceRoutine()
        {
            StopCoroutineIfRunning(ref autoAdvanceRoutine);
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

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by scene setup and tests.</summary>
        public void SetAuthoringReferences(
            PrologueSequenceData data,
            Image layerAImage,
            AspectRatioFitter layerAFitter,
            Image layerBImage,
            AspectRatioFitter layerBFitter,
            CanvasGroup storyGroup,
            TMP_Text story,
            TMP_Text continueText,
            TMP_Text skipText,
            CanvasGroup fadeOverlay)
        {
            sequenceData = data;
            slideLayerAImage = layerAImage;
            slideLayerAFitter = layerAFitter;
            slideLayerBImage = layerBImage;
            slideLayerBFitter = layerBFitter;
            storyTextGroup = storyGroup;
            storyText = story;
            continueIndicatorText = continueText;
            skipButtonLabel = skipText;
            fadeOverlayGroup = fadeOverlay;
        }

        /// <summary>Editor-only wiring hook for the optional Prologue audio service. Separate from
        /// <see cref="SetAuthoringReferences"/>, mirroring
        /// <see cref="IntroSequenceController.SetAudioAuthoringReferences"/>.</summary>
        public void SetAudioAuthoringReferences(AudioService service)
        {
            audioService = service;
        }

        [ContextMenu("Debug/Play")]
        private void DebugPlay()
        {
            Play(() => Debug.Log("[PrologueSequenceController] Debug complete callback fired."));
        }

        [ContextMenu("Debug/Advance")]
        private void DebugAdvance()
        {
            OnTapAdvance();
        }

        [ContextMenu("Debug/Skip")]
        private void DebugSkip()
        {
            Skip();
        }
#endif
    }
}
