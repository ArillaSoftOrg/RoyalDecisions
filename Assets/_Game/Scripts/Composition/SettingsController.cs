using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using RoyalDecisions.Infrastructure;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>Owns staged settings edits and applies them only on explicit confirmation.</summary>
    public sealed class SettingsController : MonoBehaviour
    {
        private const int HighFrameRate = 60;
        private const int LowFrameRate = 30;
        private const int AutoFrameRate = -1;

        [SerializeField] private SettingsPanelView view;
        [SerializeField] private AudioService audioService;
        [SerializeField] private FeedbackCueProfile cues;
        [SerializeField] private AccessibilityPresentationController accessibility;
        [SerializeField] private AboutPanelView aboutPanel;

        /// <summary>
        /// Deactivated for real while Settings (or About, reached from within it) is open, rather
        /// than merely covered by an opaque overlay — so the menu behind can neither be seen nor
        /// receive input regardless of layering.
        /// </summary>
        [SerializeField] private GameObject mainMenuRoot;

        private ISettingsStore store;
        private IHapticService haptics;
        private GameSettings current;

        public GameSettings Current => current;

        private void Awake()
        {
            if (store == null)
            {
                SavePaths paths = SavePaths.ForPersistentData();
                store = new SettingsServiceStore(
                    new SettingsSaveService(new SystemFileSystem(), paths));
            }
            haptics ??= new UnityHapticService();
            LoadAndApply();
        }

        private void OnEnable()
        {
            if (view == null)
            {
                return;
            }
            view.ApplyRequested += ApplyFromView;
            view.CancelRequested += HandleCancelButton;
            view.ResetRequested += ResetToDefaults;
            view.ResetTutorialRequested += HandleResetTutorialRequested;
            view.AboutRequested += HandleAboutRequested;
            view.MasterVolumeStepped += HandleMasterVolumeStepped;
            view.MusicVolumeStepped += HandleMusicVolumeStepped;
            view.SfxVolumeStepped += HandleSfxVolumeStepped;
            view.ToggleChanged += PlayUiClick;
            view.TabPressed += PlayUiClick;
            view.MasterMuteChanged += HandleMasterMuteChanged;
            if (aboutPanel != null) aboutPanel.CloseRequested += HandleAboutClosed;
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.ApplyRequested -= ApplyFromView;
                view.CancelRequested -= HandleCancelButton;
                view.ResetRequested -= ResetToDefaults;
                view.ResetTutorialRequested -= HandleResetTutorialRequested;
                view.AboutRequested -= HandleAboutRequested;
                view.MasterVolumeStepped -= HandleMasterVolumeStepped;
                view.MusicVolumeStepped -= HandleMusicVolumeStepped;
                view.SfxVolumeStepped -= HandleSfxVolumeStepped;
                view.ToggleChanged -= PlayUiClick;
                view.TabPressed -= PlayUiClick;
                view.MasterMuteChanged -= HandleMasterMuteChanged;
            }
            if (aboutPanel != null) aboutPanel.CloseRequested -= HandleAboutClosed;
        }

        public void Configure(ISettingsStore settingsStore, IHapticService hapticService = null)
        {
            store = settingsStore;
            haptics = hapticService ?? new NoOpHapticService();
            LoadAndApply();
        }

        public void Open()
        {
            PlayUiClick();
            EnsureLoaded();
            mainMenuRoot?.SetActive(false);
            view?.Show(current);
        }

        public bool CloseIfOpen()
        {
            if (view == null || !view.IsOpen)
            {
                return false;
            }
            Cancel();
            return true;
        }

        public void LoadAndApply()
        {
            current = store != null ? store.Load() : GameSettings.CreateDefault();
            ApplyRuntime(current);
            view?.Render(current);
        }

        public void ApplyFromView()
        {
            PlayUiClick();
            EnsureLoaded();
            current.SetMasterVolume(view != null ? view.MasterVolume : current.MasterVolume);
            current.SetMusicVolume(view != null ? view.MusicVolume : current.MusicVolume);
            current.SetSfxVolume(view != null ? view.SfxVolume : current.SfxVolume);
            current.SetMasterMuted(view != null && view.MasterMuted);
            current.SetHapticsEnabled(view == null || view.HapticsEnabled);
            current.SetReducedMotion(view != null && view.ReducedMotion);
            current.SetTextSizeMode(view != null ? view.TextSizeMode : current.TextSizeMode);
            current.SetHighContrast(view != null && view.HighContrast);
            current.SetFrameRateMode(view != null ? view.FrameRateMode : current.FrameRateMode);
            current.SetBatterySaverEnabled(view != null && view.BatterySaverEnabled);
            current.SetTapButtonsEnabled(view == null || view.TapButtonsEnabled);
            current.SetInvertSwipeRotation(view != null && view.InvertSwipeRotation);
            current.SetSwipeSensitivity(view != null ? view.SwipeSensitivity : current.SwipeSensitivity);
            current.SetDisableSwipe(view != null && view.DisableSwipe);
            SaveOutcome outcome = store != null ? store.Save(current) : SaveOutcome.Ok();
            if (!outcome.Succeeded)
            {
                Debug.LogWarning("[Settings] Could not save preferences: " + outcome.Message, this);
                return;
            }
            ApplyRuntime(current);
            view?.Hide();
            mainMenuRoot?.SetActive(true);
        }

        public void Cancel()
        {
            GameSettings saved = current ?? GameSettings.CreateDefault();
            view?.Render(saved);
            // Dragging the volume sliders previews live against audioService before Apply is
            // pressed (see HandleMusicVolumeStepped/HandleSfxVolumeStepped); Cancel must put that
            // back the way it was, or the live volume stays at the discarded preview.
            ApplyRuntime(saved);
            view?.Hide();
            mainMenuRoot?.SetActive(true);
        }

        /// <summary>Cancel as requested by the panel's Cancel button; adds the click cue that a
        /// programmatic close (e.g. the Android back button, via <see cref="CloseIfOpen"/>) must
        /// not play.</summary>
        private void HandleCancelButton()
        {
            PlayUiClick();
            Cancel();
        }

        public void ResetToDefaults()
        {
            PlayUiClick();
            current = GameSettings.CreateDefault();
            if (store != null)
            {
                SaveOutcome outcome = store.Save(current);
                if (!outcome.Succeeded)
                {
                    Debug.LogWarning("[Settings] Could not reset preferences: " + outcome.Message, this);
                }
            }
            ApplyRuntime(current);
            view?.Render(current);
        }

        private void ApplyRuntime(GameSettings settings)
        {
            if (audioService != null)
            {
                audioService.SetMasterVolume(settings.MasterVolume);
                audioService.SetMusicVolume(settings.MusicVolume);
                audioService.SetSfxVolume(settings.SfxVolume);
                audioService.SetMasterMuted(settings.MasterMuted);
            }
            haptics?.SetEnabled(settings.HapticsEnabled);
            accessibility?.Apply(settings);

            // Global engine setting: takes effect immediately and needs no scene-local reference.
            // The card's own swipe sensitivity/rotation live in the Game scene's CardSwipeController
            // and are re-applied there by GameSceneController when that scene loads its settings.
            UnityEngine.Application.targetFrameRate = ResolveTargetFrameRate(settings);
        }

        private static int ResolveTargetFrameRate(GameSettings settings)
        {
            if (settings.BatterySaverEnabled)
            {
                return LowFrameRate;
            }
            switch (settings.FrameRateMode)
            {
                case FrameRateMode.Thirty:
                    return LowFrameRate;
                case FrameRateMode.Auto:
                    // Unity's own "no target frame rate" value: the platform's default cadence is
                    // used as-is. Deliberately not a fabricated device-performance heuristic.
                    return AutoFrameRate;
                default:
                    return HighFrameRate;
            }
        }

        private void HandleResetTutorialRequested()
        {
            EnsureLoaded();
            current.SetTutorialCompleted(false);
            SaveOutcome outcome = store != null ? store.Save(current) : SaveOutcome.Ok();
            if (!outcome.Succeeded)
            {
                Debug.LogWarning("[Settings] Could not save preferences: " + outcome.Message, this);
            }
        }

        private void HandleAboutRequested()
        {
            view?.Hide();
            aboutPanel?.Show();
        }

        private void HandleAboutClosed() => view?.Reopen();

        /// <summary>
        /// The master mute toggle takes effect on the live AudioService immediately, unlike every
        /// other Settings control which stays staged until Apply — otherwise there is nothing to
        /// sequence the click cue against. Muting: click first, then mute, so the click itself is
        /// never swallowed by the mute it is about to cause. Unmuting: unmute first, then click, so
        /// the click is not swallowed by mute state that has not been lifted yet.
        /// </summary>
        private void HandleMasterMuteChanged(bool isMuted)
        {
            if (isMuted)
            {
                PlayUiClick();
                audioService?.SetMasterMuted(true);
            }
            else
            {
                audioService?.SetMasterMuted(false);
                PlayUiClick();
            }
        }

        /// <summary>As <see cref="HandleMusicVolumeStepped"/>, previewing the master multiplier that
        /// scales both music and SFX.</summary>
        private void HandleMasterVolumeStepped(float value)
        {
            audioService?.SetMasterVolume(value);
            PlaySliderTick();
        }

        /// <summary>
        /// Previews live against the actual AudioService while the slider is being dragged, so
        /// turning the music down is heard immediately rather than only after Apply. Not yet saved —
        /// <see cref="Cancel"/> puts the live volume back if the player backs out instead.
        /// </summary>
        private void HandleMusicVolumeStepped(float value)
        {
            audioService?.SetMusicVolume(value);
            PlaySliderTick();
        }

        /// <summary>As <see cref="HandleMusicVolumeStepped"/>, and previews the tick at the volume
        /// being dragged to, so the slider demonstrates the SFX level it is about to be set to.
        /// Scaled by the current master volume too — otherwise, with master turned down, this
        /// preview would play louder than the SFX will actually sound once applied.</summary>
        private void HandleSfxVolumeStepped(float value)
        {
            audioService?.SetSfxVolume(value);
            float master = audioService != null ? audioService.MasterVolume : GameSettings.MaxVolume;
            PlaySliderTick(value * master);
        }

        private void PlaySliderTick(float? previewVolume = null)
        {
            if (audioService == null || cues == null || string.IsNullOrEmpty(cues.SliderTick))
            {
                return;
            }

            if (previewVolume.HasValue)
            {
                audioService.Play(cues.SliderTick, previewVolume.Value);
            }
            else
            {
                audioService.Play(cues.SliderTick);
            }
        }

        private void EnsureLoaded()
        {
            if (current == null)
            {
                LoadAndApply();
            }
        }

        private void PlayUiClick()
        {
            if (audioService == null || cues == null || string.IsNullOrEmpty(cues.UiClick))
            {
                return;
            }

            audioService.Play(cues.UiClick);
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            SettingsPanelView settingsView,
            AudioService audio,
            AccessibilityPresentationController accessibilityController,
            AboutPanelView about = null,
            GameObject mainMenu = null)
        {
            view = settingsView;
            audioService = audio;
            accessibility = accessibilityController;
            aboutPanel = about;
            mainMenuRoot = mainMenu;
        }
#endif
    }
}
