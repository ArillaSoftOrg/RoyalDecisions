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
        [Tooltip("Optional. Plays once before the menu loads. Absent intro loads MainMenu "
            + "immediately, exactly as before this existed.")]
        [SerializeField] private IntroSequenceController introSequence;

        private ISettingsStore settingsStore;
        private ISceneLoader sceneLoader;

        /// <summary>The settings that were applied. Exposed for diagnostics and tests.</summary>
        public GameSettings AppliedSettings { get; private set; }

        /// <summary>Injection seam for tests, which must never touch persistent data.</summary>
        public void Configure(ISettingsStore store, ISceneLoader loader, IntroSequenceController intro = null)
        {
            settingsStore = store;
            sceneLoader = loader;
            introSequence = intro;
        }

        private void Start()
        {
            ApplySettings();

            // Already loaded as part of ApplySettings; reusing it here needs no new dependency.
            introSequence?.SetReducedMotion(AppliedSettings.ReducedMotion);

            ProceedToMainMenu();
        }

        /// <summary>
        /// Plays the intro if one is assigned, then loads MainMenu; loads MainMenu immediately
        /// otherwise. Public (rather than folded into <see cref="Start"/>) so a test can drive it
        /// directly after <see cref="Configure"/> without waiting on Unity's own lifecycle.
        /// </summary>
        public void ProceedToMainMenu()
        {
            if (introSequence != null)
            {
                introSequence.Play(() => sceneLoader?.LoadScene(mainMenuSceneName));
            }
            else
            {
                sceneLoader?.LoadScene(mainMenuSceneName);
            }
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
                audioService.SetVolume(AppliedSettings.SfxVolume);
                audioService.SetMuted(false);
            }

            return AppliedSettings;
        }
    }
}
