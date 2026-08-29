using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// First scene: loads settings, applies them, and hands over to the menu.
    /// </summary>
    /// <remarks>
    /// Deliberately tiny. It touches no content and creates no run — its only job is that settings
    /// are applied before anything can make a sound.
    /// </remarks>
    public sealed class BootstrapController : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Audio")]
        [Tooltip("Optional. Absent audio is a supported configuration.")]
        [SerializeField] private AudioService audioService;

        [Header("Intro")]
        [Tooltip("Optional. Plays once, first, before loading (or the menu, if loading is absent). "
            + "Absent intro skips straight to loading (or the menu).")]
        [SerializeField] private IntroSequenceController introSequence;

        [Header("Loading")]
        [Tooltip("Optional. Runs after the intro (or first, if the intro is absent): shows real "
            + "startup progress and holds for its own configured minimum display duration before "
            + "fading out. Absent loading skips straight to the menu.")]
        [SerializeField] private StartupLoadingController loadingSequence;

        // Both already true by the time BeginStartupSequence reports them: ApplySettings loads
        // settings and applies them to audio in one atomic call below, so there is no separate real
        // moment to hook a distinct "audio applied" report without duplicating that work just to
        // manufacture one. Reported as two steps anyway so the bar still has real, ordered progress
        // to visibly interpolate through during the loading screen's minimum display duration.
        private const float SettingsAppliedProgress = 0.55f;
        private const float StartupReadyProgress = 0.9f;

        private ISettingsStore settingsStore;
        private ISceneLoader sceneLoader;
        private bool hasBegunStartup;
        private bool hasBegunLoading;
        private bool hasLoadedMainMenu;

        /// <summary>The settings that were applied. Exposed for diagnostics and tests.</summary>
        public GameSettings AppliedSettings { get; private set; }

        /// <summary>Injection seam for tests, which must never touch persistent data.</summary>
        public void Configure(
            ISettingsStore store,
            ISceneLoader loader,
            IntroSequenceController intro = null,
            StartupLoadingController loading = null)
        {
            settingsStore = store;
            sceneLoader = loader;
            introSequence = intro;
            loadingSequence = loading;
        }

        private void Start()
        {
            ApplySettings();

            // Already loaded as part of ApplySettings; reusing it here needs no new dependency.
            loadingSequence?.SetReducedMotion(AppliedSettings.ReducedMotion);
            introSequence?.SetReducedMotion(AppliedSettings.ReducedMotion);

            BeginStartupSequence();
        }

        /// <summary>
        /// Drives the full startup handoff, in order: the studio intro (if assigned) plays first; the
        /// instant its own final fade-out begins — not when it fully completes — the loading screen
        /// (if assigned) reveals itself and starts showing real startup milestones underneath, so the
        /// two overlap in a brief crossfade instead of appearing back-to-back. Loading then holds for
        /// its own configured minimum display duration and fades out; only once that finishes does
        /// <see cref="LoadMainMenuOnce"/> run. Either or both stages missing skips straight to
        /// whichever comes next, unchanged from before this existed. Public (rather than folded into
        /// <see cref="Start"/>) so a test can drive it directly after <see cref="Configure"/> without
        /// waiting on Unity's own lifecycle. Safe to call more than once — only the first call drives
        /// anything.
        /// </summary>
        public void BeginStartupSequence()
        {
            if (hasBegunStartup)
            {
                return;
            }

            hasBegunStartup = true;
            PlayIntro();
        }

        /// <summary>
        /// Plays the intro if one is assigned; skips straight to <see cref="BeginLoadingSequence"/>
        /// otherwise. <see cref="IntroSequenceController.FadeOutStarted"/> — not completion — is
        /// what actually starts Loading: the intro guarantees that event fires the instant its own
        /// final fade begins (whether reached naturally or via a post-lock skip), so Loading is
        /// already revealing underneath while the intro is still visibly fading out on top —
        /// one continuous handoff instead of two back-to-back screens. The intro's completion
        /// callback is wired too, purely as a defence-in-depth second caller of
        /// <see cref="BeginLoadingSequence"/>'s own guard — by construction it should always find
        /// Loading already begun by then.
        /// </summary>
        private void PlayIntro()
        {
            if (introSequence != null)
            {
                introSequence.FadeOutStarted += HandleIntroFadeOutStarted;
                introSequence.Play(HandleIntroCompleted);
            }
            else
            {
                BeginLoadingSequence();
            }
        }

        /// <summary>
        /// Unsubscribes itself immediately — this must drive <see cref="BeginLoadingSequence"/> at
        /// most once no matter how this is ever reached.
        /// </summary>
        private void HandleIntroFadeOutStarted()
        {
            if (introSequence != null)
            {
                introSequence.FadeOutStarted -= HandleIntroFadeOutStarted;
            }

            BeginLoadingSequence();
        }

        /// <summary>
        /// Normally a no-op by the time this runs — <see cref="HandleIntroFadeOutStarted"/> already
        /// started Loading, since <see cref="IntroSequenceController"/> guarantees
        /// <see cref="IntroSequenceController.FadeOutStarted"/> fires before its own completion.
        /// Kept as an explicit fallback so Loading still begins even if that guarantee were ever
        /// broken — <see cref="BeginLoadingSequence"/>'s own guard makes calling it twice harmless.
        /// </summary>
        private void HandleIntroCompleted()
        {
            BeginLoadingSequence();
        }

        /// <summary>
        /// Reveals the loading screen and shows real startup milestones, holding for its own
        /// configured minimum display duration before fading out into <see cref="LoadMainMenuOnce"/>.
        /// Skips straight to <see cref="LoadMainMenuOnce"/> if no loading screen is assigned. Guarded
        /// independently of <see cref="hasBegunStartup"/> so this can be triggered from either the
        /// intro's fade-start signal or its completion callback without ever starting Loading twice.
        /// </summary>
        private void BeginLoadingSequence()
        {
            if (hasBegunLoading)
            {
                return;
            }

            hasBegunLoading = true;

            if (loadingSequence == null)
            {
                LoadMainMenuOnce();
                return;
            }

            loadingSequence.BeginLoading();
            loadingSequence.ReportProgress(SettingsAppliedProgress);
            loadingSequence.ReportProgress(StartupReadyProgress);
            loadingSequence.CompleteLoading(LoadMainMenuOnce);
        }

        /// <summary>Symmetric with the subscription in <see cref="PlayIntro"/>: guards against a
        /// dangling handler if this object is destroyed before the intro ever signals.</summary>
        private void OnDestroy()
        {
            if (introSequence != null)
            {
                introSequence.FadeOutStarted -= HandleIntroFadeOutStarted;
            }
        }

        /// <summary>
        /// The single choke point that actually loads MainMenu. Guarded independently of
        /// <see cref="hasBegunStartup"/> so MainMenu loads exactly once even if
        /// <see cref="BeginLoadingSequence"/> itself is ever reached more than once (e.g. a loading
        /// screen and an already-completed intro both resolving to it in some future call order).
        /// </summary>
        private void LoadMainMenuOnce()
        {
            if (hasLoadedMainMenu)
            {
                return;
            }

            hasLoadedMainMenu = true;
            sceneLoader?.LoadScene(mainMenuSceneName);
        }

        /// <summary>Loads settings and applies them through the audio service's public API only.</summary>
        public GameSettings ApplySettings()
        {
            if (settingsStore == null)
            {
                SavePaths paths = SavePaths.ForPersistentData();
                settingsStore = new SettingsServiceStore(
                    new SettingsSaveService(new SystemFileSystem(), paths));
            }

            sceneLoader ??= new UnitySceneLoader();

            // Never fails: unreadable preferences resolve to defaults rather than blocking launch.
            AppliedSettings = settingsStore.Load();

            if (audioService != null)
            {
                // Same public-API-only pattern as GameSceneController.ApplySettings. The intro
                // plays its cues as ordinary SFX through this service, so master volume/mute must
                // be respected before it can play anything — hard-coding unmuted here would ignore
                // a player who has muted audio in Settings.
                audioService.SetMasterVolume(AppliedSettings.MasterVolume);
                audioService.SetSfxVolume(AppliedSettings.SfxVolume);
                audioService.SetMasterMuted(AppliedSettings.MasterMuted);
            }

            return AppliedSettings;
        }
    }
}
