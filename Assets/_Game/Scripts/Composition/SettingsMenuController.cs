using System;
using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Drives a settings menu built from plain uGUI widgets: sliders, toggles and the
    /// Cancel/Apply pair, persisted through <see cref="PlayerPrefsSettingsStore"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A self-contained alternative to <see cref="SettingsController"/>, which drives the authored
    /// <c>SettingsPanelView</c> hierarchy and saves to versioned JSON instead. Both write the same
    /// <see cref="GameSettings"/>, so <b>only one of the two may be live in a scene</b> — wiring
    /// both splits the player's preferences across two stores.
    /// </para>
    /// <para>
    /// Edits are staged, never live-saved. Following <see cref="SettingsController"/>'s approach,
    /// there is no draft copy of <see cref="GameSettings"/>: the widgets themselves <i>are</i> the
    /// draft, and <see cref="current"/> holds only what was last saved, so
    /// <see cref="Cancel"/> always has an untouched state to return to.
    /// </para>
    /// <para>
    /// Boundary (CLAUDE.md §7): this is a controller, not a view. It calculates no rules — it maps
    /// widget state onto <see cref="GameSettings"/> and hands that to the injected save, audio and
    /// haptic seams.
    /// </para>
    /// </remarks>
    public sealed class SettingsMenuController : MonoBehaviour
    {
        /// <summary>Battery Saver caps the frame rate without overwriting the stored preference.</summary>
        private const int BatterySaverFrameRate = 30;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Tooltip("Optional. Shows the live percentage beside each volume slider.")]
        [SerializeField] private TMP_Text masterVolumeLabel;
        [SerializeField] private TMP_Text musicVolumeLabel;
        [SerializeField] private TMP_Text sfxVolumeLabel;

        [SerializeField] private Toggle masterMuteToggle;

        [Header("Haptics")]
        [SerializeField] private Toggle hapticsToggle;

        [Header("Graphics")]
        [Tooltip("Four steps: 0 = 30 FPS, 1 = 60 FPS, 2 = 90 FPS, 3 = 120 FPS.")]
        [SerializeField] private Slider frameRateSlider;
        [SerializeField] private TMP_Text frameRateLabel;
        [SerializeField] private Toggle batterySaverToggle;

        [Header("Controls")]
        [SerializeField] private Slider swipeSensitivitySlider;
        [SerializeField] private TMP_Text swipeSensitivityLabel;
        [SerializeField] private Toggle tapButtonsToggle;
        [SerializeField] private Toggle invertSwipeRotationToggle;
        [SerializeField] private Toggle disableSwipeToggle;

        [Header("General")]
        [Tooltip("Three steps: 0 = Small, 1 = Normal, 2 = Large.")]
        [SerializeField] private Slider textSizeSlider;
        [SerializeField] private TMP_Text textSizeLabel;
        [SerializeField] private Toggle reducedMotionToggle;
        [SerializeField] private Toggle highContrastToggle;

        [Header("Actions")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;

        [Tooltip("Optional. Restores factory defaults into the widgets; still needs Apply.")]
        [SerializeField] private Button resetButton;

        [Tooltip("Optional. Surfaces a failed save to the player instead of only the console.")]
        [SerializeField] private TMP_Text statusLabel;

        [Header("Runtime targets")]
        [Tooltip("Optional. Receives volume and mute changes live, before Apply.")]
        [SerializeField] private AudioService audioService;

        [Tooltip("Optional. Receives text size, reduced motion and high contrast live.")]
        [SerializeField] private AccessibilityPresentationController accessibility;

        [Tooltip("Optional. Hidden by Apply and Cancel when assigned.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [Tooltip("Format for the volume percentages. {0} is the whole-number percentage.")]
        [SerializeField] private string percentFormat = "{0}%";

        [Tooltip("Frame-rate step names, in slider order (30/60/90/120 FPS).")]
        [SerializeField] private string[] frameRateNames = { "30 FPS", "60 FPS", "90 FPS", "120 FPS" };

        [Tooltip("Text-size step names, in slider order (small to large).")]
        [SerializeField] private string[] textSizeNames = { "Küçük", "Normal", "Büyük" };

        [SerializeField] private string saveFailedMessage = "Ayarlar kaydedilemedi.";

        /// <summary>Raised after a successful Apply, carrying what was saved.</summary>
        public event Action<GameSettings> Applied;

        /// <summary>Raised after Cancel has reverted the widgets and the live preview.</summary>
        public event Action Cancelled;

        private ISettingsStore store;
        private IHapticService haptics;
        private GameSettings current;

        // Last percentage actually written per label. onValueChanged fires every frame of a drag,
        // so without this each frame would allocate a new string for text that has not changed.
        private int lastMasterPercent = int.MinValue;
        private int lastMusicPercent = int.MinValue;
        private int lastSfxPercent = int.MinValue;
        private int lastSensitivityPercent = int.MinValue;

        /// <summary>The settings as last saved — not the in-progress widget state.</summary>
        public GameSettings Current => current ??= GameSettings.CreateDefault();

        /// <summary>
        /// Injects the persistence and haptic seams. Call before the first <c>Awake</c> to override
        /// the PlayerPrefs default; tests use this to substitute an in-memory store.
        /// </summary>
        public void Configure(ISettingsStore settingsStore, IHapticService hapticService = null)
        {
            store = settingsStore;
            if (hapticService != null)
            {
                haptics = hapticService;
            }
        }

        private void Awake()
        {
            store ??= new PlayerPrefsSettingsStore();
            haptics ??= new UnityHapticService();
            LoadAndApply();
        }

        private void OnEnable()
        {
            AddSlider(masterVolumeSlider, HandleMasterVolumeChanged);
            AddSlider(musicVolumeSlider, HandleMusicVolumeChanged);
            AddSlider(sfxVolumeSlider, HandleSfxVolumeChanged);
            AddSlider(frameRateSlider, HandleFrameRateChanged);
            AddSlider(swipeSensitivitySlider, HandleSwipeSensitivityChanged);
            AddSlider(textSizeSlider, HandleTextSizeChanged);

            AddToggle(masterMuteToggle, HandleMasterMuteChanged);
            AddToggle(hapticsToggle, HandleHapticsChanged);
            AddToggle(batterySaverToggle, HandleAccessibilityToggleChanged);
            AddToggle(tapButtonsToggle, HandleAccessibilityToggleChanged);
            AddToggle(invertSwipeRotationToggle, HandleAccessibilityToggleChanged);
            AddToggle(disableSwipeToggle, HandleAccessibilityToggleChanged);
            AddToggle(reducedMotionToggle, HandleAccessibilityToggleChanged);
            AddToggle(highContrastToggle, HandleAccessibilityToggleChanged);

            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
        }

        private void OnDisable()
        {
            RemoveSlider(masterVolumeSlider, HandleMasterVolumeChanged);
            RemoveSlider(musicVolumeSlider, HandleMusicVolumeChanged);
            RemoveSlider(sfxVolumeSlider, HandleSfxVolumeChanged);
            RemoveSlider(frameRateSlider, HandleFrameRateChanged);
            RemoveSlider(swipeSensitivitySlider, HandleSwipeSensitivityChanged);
            RemoveSlider(textSizeSlider, HandleTextSizeChanged);

            RemoveToggle(masterMuteToggle, HandleMasterMuteChanged);
            RemoveToggle(hapticsToggle, HandleHapticsChanged);
            RemoveToggle(batterySaverToggle, HandleAccessibilityToggleChanged);
            RemoveToggle(tapButtonsToggle, HandleAccessibilityToggleChanged);
            RemoveToggle(invertSwipeRotationToggle, HandleAccessibilityToggleChanged);
            RemoveToggle(disableSwipeToggle, HandleAccessibilityToggleChanged);
            RemoveToggle(reducedMotionToggle, HandleAccessibilityToggleChanged);
            RemoveToggle(highContrastToggle, HandleAccessibilityToggleChanged);

            if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Cancel);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetToDefaults);
        }

        /// <summary>Reads the saved settings, shows them and applies them to the running game.</summary>
        public void LoadAndApply()
        {
            current = store != null ? store.Load() : GameSettings.CreateDefault();
            Render(current);
            ApplyRuntime(current);
        }

        /// <summary>Shows the panel with the saved settings, discarding any stale widget state.</summary>
        public void Open()
        {
            Render(Current);
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// Commits the widget state: writes it into <see cref="current"/>, persists it, then
        /// applies it. A failed write is reported, never swallowed, and the widgets keep the
        /// player's edits so they can try again.
        /// </summary>
        public void Apply()
        {
            GameSettings settings = Current;
            ReadWidgetsInto(settings);

            SaveOutcome outcome = store != null ? store.Save(settings) : SaveOutcome.Ok();
            if (!outcome.Succeeded)
            {
                ShowStatus(saveFailedMessage);
                Debug.LogWarning("Settings save failed: " + outcome.Message);
                return;
            }

            ShowStatus(string.Empty);
            ApplyRuntime(settings);
            Applied?.Invoke(settings);
            ClosePanel();
        }

        /// <summary>
        /// Discards the edits: re-renders the last saved settings and re-applies them, which is
        /// what undoes the live volume/accessibility preview.
        /// </summary>
        public void Cancel()
        {
            GameSettings settings = Current;
            Render(settings);
            ApplyRuntime(settings);
            ShowStatus(string.Empty);
            Cancelled?.Invoke();
            ClosePanel();
        }

        /// <summary>
        /// Puts factory defaults into the widgets and previews them. Deliberately does not save —
        /// the player still confirms with Apply, or backs out with Cancel.
        /// </summary>
        public void ResetToDefaults()
        {
            GameSettings defaults = GameSettings.CreateDefault();
            Render(defaults);
            PreviewAudio();
            PreviewAccessibility();
        }

        /// <summary>Pushes <paramref name="settings"/> into the widgets without raising handlers.</summary>
        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();

            SetSlider(masterVolumeSlider, settings.MasterVolume);
            SetSlider(musicVolumeSlider, settings.MusicVolume);
            SetSlider(sfxVolumeSlider, settings.SfxVolume);
            SetSlider(frameRateSlider, FrameRateModeToStep(settings.FrameRateMode));
            SetSlider(swipeSensitivitySlider, settings.SwipeSensitivity);
            SetSlider(textSizeSlider, TextSizeModeToStep(settings.TextSizeMode));

            SetToggle(masterMuteToggle, settings.MasterMuted);
            SetToggle(hapticsToggle, settings.HapticsEnabled);
            SetToggle(batterySaverToggle, settings.BatterySaverEnabled);
            SetToggle(tapButtonsToggle, settings.TapButtonsEnabled);
            SetToggle(invertSwipeRotationToggle, settings.InvertSwipeRotation);
            SetToggle(disableSwipeToggle, settings.DisableSwipe);
            SetToggle(reducedMotionToggle, settings.ReducedMotion);
            SetToggle(highContrastToggle, settings.HighContrast);

            // Rendering bypasses onValueChanged, so the labels are refreshed explicitly. The cached
            // percentages are reset first so a programmatic render always repaints its label.
            lastMasterPercent = int.MinValue;
            lastMusicPercent = int.MinValue;
            lastSfxPercent = int.MinValue;
            lastSensitivityPercent = int.MinValue;

            lastMasterPercent = WritePercent(masterVolumeLabel, settings.MasterVolume, lastMasterPercent);
            lastMusicPercent = WritePercent(musicVolumeLabel, settings.MusicVolume, lastMusicPercent);
            lastSfxPercent = WritePercent(sfxVolumeLabel, settings.SfxVolume, lastSfxPercent);
            lastSensitivityPercent = WritePercent(
                swipeSensitivityLabel, settings.SwipeSensitivity, lastSensitivityPercent);

            WriteStepName(frameRateLabel, frameRateNames, FrameRateModeToStep(settings.FrameRateMode));
            WriteStepName(textSizeLabel, textSizeNames, TextSizeModeToStep(settings.TextSizeMode));
        }

        // Widget handlers ---------------------------------------------------------------

        private void HandleMasterVolumeChanged(float value)
        {
            lastMasterPercent = WritePercent(masterVolumeLabel, value, lastMasterPercent);
            PreviewAudio();
        }

        private void HandleMusicVolumeChanged(float value)
        {
            lastMusicPercent = WritePercent(musicVolumeLabel, value, lastMusicPercent);
            PreviewAudio();
        }

        private void HandleSfxVolumeChanged(float value)
        {
            lastSfxPercent = WritePercent(sfxVolumeLabel, value, lastSfxPercent);
            PreviewAudio();
        }

        private void HandleSwipeSensitivityChanged(float value)
        {
            lastSensitivityPercent = WritePercent(
                swipeSensitivityLabel, value, lastSensitivityPercent);
        }

        private void HandleFrameRateChanged(float value)
        {
            WriteStepName(frameRateLabel, frameRateNames, Mathf.RoundToInt(value));
        }

        private void HandleTextSizeChanged(float value)
        {
            WriteStepName(textSizeLabel, textSizeNames, Mathf.RoundToInt(value));
            PreviewAccessibility();
        }

        private void HandleMasterMuteChanged(bool value)
        {
            PreviewAudio();
        }

        private void HandleHapticsChanged(bool value)
        {
            haptics?.SetEnabled(value);
            if (value)
            {
                // One pulse so switching it on is felt, not just read.
                haptics?.Pulse();
            }
        }

        private void HandleAccessibilityToggleChanged(bool value)
        {
            PreviewAccessibility();
        }

        // Live preview ------------------------------------------------------------------

        /// <summary>
        /// Sends the current volume widget state straight to the audio service so turning the music
        /// down is heard while dragging. Not saved — <see cref="Cancel"/> undoes it by re-applying
        /// <see cref="current"/>.
        /// </summary>
        private void PreviewAudio()
        {
            if (audioService == null)
            {
                return;
            }

            audioService.SetMasterVolume(ReadSlider(masterVolumeSlider, GameSettings.MaxVolume));
            audioService.SetMusicVolume(ReadSlider(musicVolumeSlider, GameSettings.DefaultVolume));
            audioService.SetSfxVolume(ReadSlider(sfxVolumeSlider, GameSettings.DefaultVolume));
            audioService.SetMasterMuted(ReadToggle(masterMuteToggle, false));
        }

        /// <summary>
        /// Previews text size, reduced motion and high contrast against the live views. Built from
        /// defaults plus the three widgets it needs, so it never mutates <see cref="current"/>.
        /// </summary>
        private void PreviewAccessibility()
        {
            if (accessibility == null)
            {
                return;
            }

            GameSettings preview = GameSettings.CreateDefault();
            preview.SetTextSizeMode(ReadTextSizeMode());
            preview.SetReducedMotion(ReadToggle(reducedMotionToggle, false));
            preview.SetHighContrast(ReadToggle(highContrastToggle, false));
            accessibility.Apply(preview);
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

            // Frame pacing is a runtime concern only. Outside play mode these are global editor
            // state that nothing would reset, so an EditMode test or an authoring pass must not
            // leave the editor itself capped at 30 FPS.
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }

            // vSync would otherwise override targetFrameRate on platforms that honour it.
            QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = settings.BatterySaverEnabled
                ? BatterySaverFrameRate
                : (int)settings.FrameRateMode;
        }

        // Widget <-> model --------------------------------------------------------------

        private void ReadWidgetsInto(GameSettings settings)
        {
            settings.SetMasterVolume(ReadSlider(masterVolumeSlider, settings.MasterVolume));
            settings.SetMusicVolume(ReadSlider(musicVolumeSlider, settings.MusicVolume));
            settings.SetSfxVolume(ReadSlider(sfxVolumeSlider, settings.SfxVolume));
            settings.SetMasterMuted(ReadToggle(masterMuteToggle, settings.MasterMuted));
            settings.SetHapticsEnabled(ReadToggle(hapticsToggle, settings.HapticsEnabled));

            settings.SetFrameRateMode(ReadFrameRateMode());
            settings.SetBatterySaverEnabled(
                ReadToggle(batterySaverToggle, settings.BatterySaverEnabled));

            settings.SetSwipeSensitivity(
                ReadSlider(swipeSensitivitySlider, settings.SwipeSensitivity));
            settings.SetTapButtonsEnabled(ReadToggle(tapButtonsToggle, settings.TapButtonsEnabled));
            settings.SetInvertSwipeRotation(
                ReadToggle(invertSwipeRotationToggle, settings.InvertSwipeRotation));
            settings.SetDisableSwipe(ReadToggle(disableSwipeToggle, settings.DisableSwipe));

            settings.SetTextSizeMode(ReadTextSizeMode());
            settings.SetReducedMotion(ReadToggle(reducedMotionToggle, settings.ReducedMotion));
            settings.SetHighContrast(ReadToggle(highContrastToggle, settings.HighContrast));
        }

        private FrameRateMode ReadFrameRateMode()
        {
            return frameRateSlider != null
                ? StepToFrameRateMode(Mathf.RoundToInt(frameRateSlider.value))
                : Current.FrameRateMode;
        }

        private TextSizeMode ReadTextSizeMode()
        {
            return textSizeSlider != null
                ? StepToTextSizeMode(Mathf.RoundToInt(textSizeSlider.value))
                : Current.TextSizeMode;
        }

        /// <summary>
        /// Writes the percentage only when the whole number actually changes, returning the value
        /// now shown. A drag raises onValueChanged every frame; repainting identical text would
        /// allocate a string per frame for no visible difference (CLAUDE.md §10).
        /// </summary>
        private int WritePercent(TMP_Text label, float normalized, int lastPercent)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f);
            if (label == null || percent == lastPercent)
            {
                return percent;
            }

            label.text = string.Format(percentFormat, percent);
            return percent;
        }

        private static void WriteStepName(TMP_Text label, string[] names, int step)
        {
            if (label == null || names == null || names.Length == 0)
            {
                return;
            }

            label.text = names[Mathf.Clamp(step, 0, names.Length - 1)];
        }

        private void ShowStatus(string message)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message ?? string.Empty;
            statusLabel.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private static void AddSlider(Slider slider, UnityAction<float> handler)
        {
            if (slider != null) slider.onValueChanged.AddListener(handler);
        }

        private static void RemoveSlider(Slider slider, UnityAction<float> handler)
        {
            if (slider != null) slider.onValueChanged.RemoveListener(handler);
        }

        private static void AddToggle(Toggle toggle, UnityAction<bool> handler)
        {
            if (toggle != null) toggle.onValueChanged.AddListener(handler);
        }

        private static void RemoveToggle(Toggle toggle, UnityAction<bool> handler)
        {
            if (toggle != null) toggle.onValueChanged.RemoveListener(handler);
        }

        private static void SetSlider(Slider slider, float value)
        {
            // Without notify: rendering must never look like a player edit.
            if (slider != null) slider.SetValueWithoutNotify(value);
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null) toggle.SetIsOnWithoutNotify(value);
        }

        private static float ReadSlider(Slider slider, float fallback) =>
            slider != null ? slider.value : fallback;

        private static bool ReadToggle(Toggle toggle, bool fallback) =>
            toggle != null ? toggle.isOn : fallback;

        private static int FrameRateModeToStep(FrameRateMode mode)
        {
            switch (mode)
            {
                case FrameRateMode.Thirty: return 0;
                case FrameRateMode.Ninety: return 2;
                case FrameRateMode.OneTwenty: return 3;
                default: return 1;
            }
        }

        private static FrameRateMode StepToFrameRateMode(int step)
        {
            switch (step)
            {
                case 0: return FrameRateMode.Thirty;
                case 2: return FrameRateMode.Ninety;
                case 3: return FrameRateMode.OneTwenty;
                default: return FrameRateMode.Sixty;
            }
        }

        // The enum's numeric order is Normal=0, Small=1, Large=2, but the slider runs smallest to
        // largest, so the two need an explicit mapping rather than a cast.
        private static int TextSizeModeToStep(TextSizeMode mode)
        {
            switch (mode)
            {
                case TextSizeMode.Small: return 0;
                case TextSizeMode.Large: return 2;
                default: return 1;
            }
        }

        private static TextSizeMode StepToTextSizeMode(int step)
        {
            switch (step)
            {
                case 0: return TextSizeMode.Small;
                case 2: return TextSizeMode.Large;
                default: return TextSizeMode.Normal;
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by prefab setup and tests.</summary>
        public void SetAudioAuthoringReferences(
            Slider master, Slider music, Slider sfx, Toggle mute,
            TMP_Text masterLabel = null, TMP_Text musicLabel = null, TMP_Text sfxLabel = null)
        {
            masterVolumeSlider = master;
            musicVolumeSlider = music;
            sfxVolumeSlider = sfx;
            masterMuteToggle = mute;
            masterVolumeLabel = masterLabel;
            musicVolumeLabel = musicLabel;
            sfxVolumeLabel = sfxLabel;
        }

        /// <summary>Editor-only wiring hook for everything outside the audio group.</summary>
        public void SetAuthoringReferences(
            Toggle hapticsSwitch = null,
            Slider frameRate = null, TMP_Text frameRateName = null, Toggle batterySaver = null,
            Slider sensitivity = null, TMP_Text sensitivityLabel = null,
            Toggle tapButtons = null, Toggle invertRotation = null, Toggle noSwipe = null,
            Slider textSize = null, TMP_Text textSizeName = null,
            Toggle reducedMotion = null, Toggle highContrast = null,
            Button apply = null, Button cancel = null, Button reset = null,
            GameObject root = null, TMP_Text status = null)
        {
            hapticsToggle = hapticsSwitch;
            frameRateSlider = frameRate;
            frameRateLabel = frameRateName;
            batterySaverToggle = batterySaver;
            swipeSensitivitySlider = sensitivity;
            swipeSensitivityLabel = sensitivityLabel;
            tapButtonsToggle = tapButtons;
            invertSwipeRotationToggle = invertRotation;
            disableSwipeToggle = noSwipe;
            textSizeSlider = textSize;
            textSizeLabel = textSizeName;
            reducedMotionToggle = reducedMotion;
            highContrastToggle = highContrast;
            applyButton = apply;
            cancelButton = cancel;
            resetButton = reset;
            panelRoot = root;
            statusLabel = status;
        }
#endif
    }
}
