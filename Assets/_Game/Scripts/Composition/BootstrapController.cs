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

        private ISettingsStore settingsStore;
        private ISceneLoader sceneLoader;

        /// <summary>The settings that were applied. Exposed for diagnostics and tests.</summary>
        public GameSettings AppliedSettings { get; private set; }

        /// <summary>Injection seam for tests, which must never touch persistent data.</summary>
        public void Configure(ISettingsStore store, ISceneLoader loader)
        {
            settingsStore = store;
            sceneLoader = loader;
        }

        private void Start()
        {
            ApplySettings();
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
                audioService.SetVolume(AppliedSettings.SfxVolume);
                audioService.SetMuted(false);
            }

            return AppliedSettings;
        }
    }
}
