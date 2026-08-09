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

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public float MusicVolume => audioPanel != null ? audioPanel.MusicVolume : GameSettings.DefaultVolume;
        public float SfxVolume => audioPanel != null ? audioPanel.SfxVolume : GameSettings.DefaultVolume;
        public bool MasterMuted => audioPanel != null && audioPanel.MasterMuted;

        public bool UseHighFrameRateCap => graphicsPanel == null || graphicsPanel.UseHighFrameRateCap;
        public bool BatterySaverEnabled => graphicsPanel != null && graphicsPanel.BatterySaverEnabled;

        public bool TapButtonsEnabled => controlsPanel == null || controlsPanel.TapButtonsEnabled;
        public bool InvertSwipeRotation => controlsPanel != null && controlsPanel.InvertSwipeRotation;
        public bool HapticsEnabled => controlsPanel == null || controlsPanel.HapticsEnabled;

        public bool ReducedMotion => generalPanel != null && generalPanel.ReducedMotion;
        public bool LargerText => generalPanel != null && generalPanel.LargerText;
        public bool HighContrast => generalPanel != null && generalPanel.HighContrast;

        private void OnEnable()
        {
            if (applyButton != null) applyButton.onClick.AddListener(HandleApply);
            if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancel);
            if (resetButton != null) resetButton.onClick.AddListener(HandleReset);

            if (audioTabButton != null) audioTabButton.onClick.AddListener(ShowAudioTab);
            if (graphicsTabButton != null) graphicsTabButton.onClick.AddListener(ShowGraphicsTab);
            if (controlsTabButton != null) controlsTabButton.onClick.AddListener(ShowControlsTab);
            if (generalTabButton != null) generalTabButton.onClick.AddListener(ShowGeneralTab);

            if (generalPanel != null)
            {
                generalPanel.ResetProgressConfirmed += HandleResetProgressConfirmed;
                generalPanel.ResetTutorialRequested += HandleResetTutorialRequested;
                generalPanel.AboutRequested += HandleAboutRequested;
            }
        }

        private void OnDisable()
        {
            if (applyButton != null) applyButton.onClick.RemoveListener(HandleApply);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancel);
            if (resetButton != null) resetButton.onClick.RemoveListener(HandleReset);

            if (audioTabButton != null) audioTabButton.onClick.RemoveListener(ShowAudioTab);
            if (graphicsTabButton != null) graphicsTabButton.onClick.RemoveListener(ShowGraphicsTab);
            if (controlsTabButton != null) controlsTabButton.onClick.RemoveListener(ShowControlsTab);
            if (generalTabButton != null) generalTabButton.onClick.RemoveListener(ShowGeneralTab);

            if (generalPanel != null)
            {
                generalPanel.ResetProgressConfirmed -= HandleResetProgressConfirmed;
                generalPanel.ResetTutorialRequested -= HandleResetTutorialRequested;
                generalPanel.AboutRequested -= HandleAboutRequested;
            }
        }

        public void Show(GameSettings settings)
        {
            Render(settings);
            ShowAudioTab();
            OpenPanel();
        }

        public void Hide() => panelRoot?.SetActive(false);

        /// <summary>
        /// Reactivates the panel without re-rendering or resetting the active tab — used when
        /// returning from a full-screen sub-page (About) so in-progress, unapplied edits and the
        /// currently selected tab aren't lost.
        /// </summary>
        public void Reopen() => OpenPanel();

        private void OpenPanel()
        {
            panelRoot?.SetActive(true);
            // Reasserted every time in case another overlay (About) has since taken the last
            // sibling slot — guarantees Settings renders above the menu regardless of history.
            if (panelRoot != null) panelRoot.transform.SetAsLastSibling();
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

        private void SetActiveTab(
            bool audio = false, bool graphics = false, bool controls = false, bool general = false)
        {
            if (audioPanel != null) audioPanel.gameObject.SetActive(audio);
            if (graphicsPanel != null) graphicsPanel.gameObject.SetActive(graphics);
            if (controlsPanel != null) controlsPanel.gameObject.SetActive(controls);
            if (generalPanel != null) generalPanel.gameObject.SetActive(general);

            TintTab(audioTabButton, audio);
            TintTab(graphicsTabButton, graphics);
            TintTab(controlsTabButton, controls);
            TintTab(generalTabButton, general);
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
            Button reset)
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
        }
#endif
    }
}
