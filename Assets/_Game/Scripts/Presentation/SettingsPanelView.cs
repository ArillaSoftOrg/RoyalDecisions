using System;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Settings menu shell: owns the panel root, the Apply/Cancel/Reset actions, and switching
    /// between the four category tabs. Each tab is a small passive view of its own — this class
    /// only aggregates them, it never mutates <see cref="GameSettings"/> or writes a save itself.
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;

        [Header("Transitions")]
        [Tooltip("Optional. Animates the whole panel opening/closing; falls back to an instant "
            + "SetActive when absent.")]
        [SerializeField] private PanelFadeAnimator panelAnimator;

        [Tooltip("Optional. Crossfades the active tab's content when switching tabs; falls back to "
            + "an instant swap when absent.")]
        [SerializeField] private PanelFadeAnimator tabCrossfadeAnimator;

        [Tooltip("Optional. Bursts the CRT overlay's flicker in sync with a real tab switch (not a "
            + "no-op reselect of the already-active tab).")]
        [SerializeField] private CrtFlickerAnimator crtFlicker;

        [Header("Tabs")]
        [SerializeField] private AudioSettingsPanelView audioPanel;
        [SerializeField] private GraphicsSettingsPanelView graphicsPanel;
        [SerializeField] private ControlsSettingsPanelView controlsPanel;
        [SerializeField] private GeneralSettingsPanelView generalPanel;

        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private Button generalTabButton;

        [Header("Actions")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetButton;

        public event Action ApplyRequested;
        public event Action CancelRequested;
        public event Action ResetRequested;

        /// <summary>Forwarded from the General tab — a run reset, not a settings change.</summary>
        public event Action ResetProgressConfirmed;
        public event Action ResetTutorialRequested;
        public event Action AboutRequested;

        /// <summary>Forwarded from the Audio tab; at most once per ~10% of slider travel.</summary>
        public event Action<float> MasterVolumeStepped;

        /// <summary>As <see cref="MasterVolumeStepped"/>.</summary>
        public event Action<float> MusicVolumeStepped;

        /// <summary>As <see cref="MasterVolumeStepped"/>, carrying the value to preview it at.</summary>
        public event Action<float> SfxVolumeStepped;

        /// <summary>Forwarded from the Controls tab; at most once per ~10% of slider travel.</summary>
        public event Action<float> SwipeSensitivityStepped;

        /// <summary>Forwarded from any tab; once per user-flipped toggle, never a programmatic Render.</summary>
        public event Action ToggleChanged;

        /// <summary>Forwarded from the Audio tab's master mute toggle, carrying its new value.</summary>
        public event Action<bool> MasterMuteChanged;

        /// <summary>Raised once per actual tab-button press; never for the default tab Show() selects.</summary>
        public event Action TabPressed;

        private enum SettingsTabId { Audio, Graphics, Controls, General }

        /// <summary>
        /// Matches the panel's authored resting state (Audio active, the rest inactive; see
        /// <c>ConfigureSettingsPanel</c>), so the first <see cref="ShowAudioTab"/> call after
        /// authoring correctly no-ops instead of animating a swap nothing actually changed.
        /// </summary>
        private SettingsTabId activeTabId = SettingsTabId.Audio;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public float MasterVolume => audioPanel != null ? audioPanel.MasterVolume : GameSettings.MaxVolume;
        public float MusicVolume => audioPanel != null ? audioPanel.MusicVolume : GameSettings.DefaultVolume;
        public float SfxVolume => audioPanel != null ? audioPanel.SfxVolume : GameSettings.DefaultVolume;
        public bool MasterMuted => audioPanel != null && audioPanel.MasterMuted;

        public FrameRateMode FrameRateMode =>
            graphicsPanel != null ? graphicsPanel.FrameRateMode : FrameRateMode.Sixty;
        public bool BatterySaverEnabled => graphicsPanel != null && graphicsPanel.BatterySaverEnabled;

        public bool TapButtonsEnabled => controlsPanel == null || controlsPanel.TapButtonsEnabled;
        public bool InvertSwipeRotation => controlsPanel != null && controlsPanel.InvertSwipeRotation;
        public bool DisableSwipe => controlsPanel != null && controlsPanel.DisableSwipe;
        public float SwipeSensitivity => controlsPanel != null
            ? controlsPanel.SwipeSensitivity
            : GameSettings.DefaultSwipeSensitivity;
        public bool HapticsEnabled => controlsPanel == null || controlsPanel.HapticsEnabled;

        public bool ReducedMotion => generalPanel != null && generalPanel.ReducedMotion;
        public TextSizeMode TextSizeMode =>
            generalPanel != null ? generalPanel.TextSizeMode : TextSizeMode.Normal;
        public bool HighContrast => generalPanel != null && generalPanel.HighContrast;

        private void OnEnable()
        {
            if (applyButton != null) applyButton.onClick.AddListener(HandleApply);
            if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancel);
            if (resetButton != null) resetButton.onClick.AddListener(HandleReset);

            if (audioTabButton != null) audioTabButton.onClick.AddListener(HandleAudioTabPressed);
            if (graphicsTabButton != null) graphicsTabButton.onClick.AddListener(HandleGraphicsTabPressed);
            if (controlsTabButton != null) controlsTabButton.onClick.AddListener(HandleControlsTabPressed);
            if (generalTabButton != null) generalTabButton.onClick.AddListener(HandleGeneralTabPressed);

            if (generalPanel != null)
            {
                generalPanel.ResetProgressConfirmed += HandleResetProgressConfirmed;
                generalPanel.ResetTutorialRequested += HandleResetTutorialRequested;
                generalPanel.AboutRequested += HandleAboutRequested;
                generalPanel.ToggleChanged += HandleToggleChanged;
            }

            if (audioPanel != null)
            {
                audioPanel.MasterVolumeStepped += HandleMasterVolumeStepped;
                audioPanel.MusicVolumeStepped += HandleMusicVolumeStepped;
                audioPanel.SfxVolumeStepped += HandleSfxVolumeStepped;
                audioPanel.MasterMuteChanged += HandleMasterMuteChanged;
            }

            if (graphicsPanel != null) graphicsPanel.ToggleChanged += HandleToggleChanged;
            if (controlsPanel != null)
            {
                controlsPanel.ToggleChanged += HandleToggleChanged;
                controlsPanel.SwipeSensitivityStepped += HandleSwipeSensitivityStepped;
            }
        }

        private void OnDisable()
        {
            if (applyButton != null) applyButton.onClick.RemoveListener(HandleApply);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancel);
            if (resetButton != null) resetButton.onClick.RemoveListener(HandleReset);

            if (audioTabButton != null) audioTabButton.onClick.RemoveListener(HandleAudioTabPressed);
            if (graphicsTabButton != null) graphicsTabButton.onClick.RemoveListener(HandleGraphicsTabPressed);
            if (controlsTabButton != null) controlsTabButton.onClick.RemoveListener(HandleControlsTabPressed);
            if (generalTabButton != null) generalTabButton.onClick.RemoveListener(HandleGeneralTabPressed);

            if (generalPanel != null)
            {
                generalPanel.ResetProgressConfirmed -= HandleResetProgressConfirmed;
                generalPanel.ResetTutorialRequested -= HandleResetTutorialRequested;
                generalPanel.AboutRequested -= HandleAboutRequested;
                generalPanel.ToggleChanged -= HandleToggleChanged;
            }

            if (audioPanel != null)
            {
                audioPanel.MasterVolumeStepped -= HandleMasterVolumeStepped;
                audioPanel.MusicVolumeStepped -= HandleMusicVolumeStepped;
                audioPanel.SfxVolumeStepped -= HandleSfxVolumeStepped;
                audioPanel.MasterMuteChanged -= HandleMasterMuteChanged;
            }

            if (graphicsPanel != null) graphicsPanel.ToggleChanged -= HandleToggleChanged;
            if (controlsPanel != null)
            {
                controlsPanel.ToggleChanged -= HandleToggleChanged;
                controlsPanel.SwipeSensitivityStepped -= HandleSwipeSensitivityStepped;
            }
        }

        public void Show(GameSettings settings)
        {
            Render(settings);
            ShowAudioTab();
            OpenPanel();
        }

        public void Hide()
        {
            if (panelAnimator != null)
            {
                panelAnimator.Hide();
            }
            else
            {
                panelRoot?.SetActive(false);
            }
        }

        /// <summary>
        /// Reactivates the panel without re-rendering or resetting the active tab — used when
        /// returning from a full-screen sub-page (About) so in-progress, unapplied edits and the
        /// currently selected tab aren't lost.
        /// </summary>
        public void Reopen() => OpenPanel();

        private void OpenPanel()
        {
            // Reasserted every time in case another overlay (About) has since taken the last
            // sibling slot — guarantees Settings renders above the menu regardless of history.
            if (panelRoot != null) panelRoot.transform.SetAsLastSibling();

            if (panelAnimator != null)
            {
                panelAnimator.Show();
            }
            else
            {
                panelRoot?.SetActive(true);
            }
        }

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            audioPanel?.Render(settings);
            graphicsPanel?.Render(settings);
            controlsPanel?.Render(settings);
            generalPanel?.Render(settings);
        }

        public void ShowAudioTab() => SetActiveTab(audio: true);
        public void ShowGraphicsTab() => SetActiveTab(graphics: true);
        public void ShowControlsTab() => SetActiveTab(controls: true);
        public void ShowGeneralTab() => SetActiveTab(general: true);

        /// <summary>
        /// Wraps each <c>ShowXTab</c> for the tab button's own <c>onClick</c> only, so pressing a
        /// tab raises <see cref="TabPressed"/> — <see cref="Show"/> calls <see cref="ShowAudioTab"/>
        /// directly to select the default tab on open, which must stay silent.
        /// </summary>
        private void HandleAudioTabPressed() { TabPressed?.Invoke(); ShowAudioTab(); }
        private void HandleGraphicsTabPressed() { TabPressed?.Invoke(); ShowGraphicsTab(); }
        private void HandleControlsTabPressed() { TabPressed?.Invoke(); ShowControlsTab(); }
        private void HandleGeneralTabPressed() { TabPressed?.Invoke(); ShowGeneralTab(); }

        private void SetActiveTab(
            bool audio = false, bool graphics = false, bool controls = false, bool general = false)
        {
            // Button tint always re-applies — cheap and idempotent — even when the content swap
            // below is skipped as a no-op reselect.
            TintTab(audioTabButton, audio);
            TintTab(graphicsTabButton, graphics);
            TintTab(controlsTabButton, controls);
            TintTab(generalTabButton, general);

            SettingsTabId target = audio ? SettingsTabId.Audio
                : graphics ? SettingsTabId.Graphics
                : controls ? SettingsTabId.Controls
                : SettingsTabId.General;

            // Spam-guard: reselecting the already-active tab (or Show() re-selecting the tab it was
            // already on) applies the (idempotent) content state directly instead of animating a
            // crossfade over content that isn't actually changing.
            if (target == activeTabId)
            {
                ApplyActiveTabContent(audio, graphics, controls, general);
                return;
            }
            activeTabId = target;
            crtFlicker?.TriggerBurst();

            if (tabCrossfadeAnimator != null)
            {
                tabCrossfadeAnimator.Swap(() => ApplyActiveTabContent(audio, graphics, controls, general));
            }
            else
            {
                ApplyActiveTabContent(audio, graphics, controls, general);
            }
        }

        private void ApplyActiveTabContent(bool audio, bool graphics, bool controls, bool general)
        {
            if (audioPanel != null) audioPanel.gameObject.SetActive(audio);
            if (graphicsPanel != null) graphicsPanel.gameObject.SetActive(graphics);
            if (controlsPanel != null) controlsPanel.gameObject.SetActive(controls);
            if (generalPanel != null) generalPanel.gameObject.SetActive(general);
        }

        /// <summary>Gives the selected category a distinct fill so the active tab is unambiguous.</summary>
        private static void TintTab(Button tabButton, bool active)
        {
            if (tabButton == null || tabButton.targetGraphic == null)
            {
                return;
            }
            tabButton.targetGraphic.color = active
                ? SettingsPanelTheme.ActiveTabColour
                : SettingsPanelTheme.InactiveTabColour;

            // White-on-gold reads as low-contrast, so the active tab's text flips dark too.
            TextMeshProUGUI label = tabButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = active
                    ? SettingsPanelTheme.ActiveTabTextColour
                    : SettingsPanelTheme.InactiveTabTextColour;
            }
        }

        private void HandleApply() => ApplyRequested?.Invoke();
        private void HandleCancel() => CancelRequested?.Invoke();
        private void HandleReset() => ResetRequested?.Invoke();
        private void HandleResetProgressConfirmed() => ResetProgressConfirmed?.Invoke();
        private void HandleResetTutorialRequested() => ResetTutorialRequested?.Invoke();
        private void HandleAboutRequested() => AboutRequested?.Invoke();
        private void HandleMasterVolumeStepped(float value) => MasterVolumeStepped?.Invoke(value);
        private void HandleMusicVolumeStepped(float value) => MusicVolumeStepped?.Invoke(value);
        private void HandleSfxVolumeStepped(float value) => SfxVolumeStepped?.Invoke(value);
        private void HandleSwipeSensitivityStepped(float value) => SwipeSensitivityStepped?.Invoke(value);
        private void HandleToggleChanged() => ToggleChanged?.Invoke();
        private void HandleMasterMuteChanged(bool isMuted) => MasterMuteChanged?.Invoke(isMuted);

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            GameObject root,
            AudioSettingsPanelView audio,
            GraphicsSettingsPanelView graphics,
            ControlsSettingsPanelView controls,
            GeneralSettingsPanelView general,
            Button audioTab,
            Button graphicsTab,
            Button controlsTab,
            Button generalTab,
            Button apply,
            Button cancel,
            Button reset,
            PanelFadeAnimator panelTransition = null,
            PanelFadeAnimator tabTransition = null,
            CrtFlickerAnimator flicker = null)
        {
            panelRoot = root;
            audioPanel = audio;
            graphicsPanel = graphics;
            controlsPanel = controls;
            generalPanel = general;
            audioTabButton = audioTab;
            graphicsTabButton = graphicsTab;
            controlsTabButton = controlsTab;
            generalTabButton = generalTab;
            applyButton = apply;
            cancelButton = cancel;
            resetButton = reset;
            panelAnimator = panelTransition;
            tabCrossfadeAnimator = tabTransition;
            crtFlicker = flicker;
        }
#endif
    }
}
