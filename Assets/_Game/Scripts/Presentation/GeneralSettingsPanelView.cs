using System;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Passive General tab of the settings menu: accessibility toggles plus the local-only actions
    /// that replace an "Account" section CLAUDE.md forbids (no accounts/cloud saves/backend).
    /// </summary>
    /// <remarks>
    /// Resetting progress is destructive and irreversible, so it is armed by a first tap and only
    /// executed by a second — a two-tap confirmation kept inside this one small view instead of a
    /// reusable modal-dialog system the project does not otherwise need.
    /// </remarks>
    public sealed class GeneralSettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Toggle reducedMotion;

        /// <summary>A three-step slider: 0 = Small, 1 = Normal, 2 = Large.</summary>
        [SerializeField] private Slider textSizeSlider;

        [Tooltip("Optional. Shows the current step's name (e.g. \"Normal\") next to the slider.")]
        [SerializeField] private TMP_Text textSizeValueLabel;

        [SerializeField] private Toggle highContrast;

        [Tooltip("Optional. Read-only: no in-app localization system exists yet, so this shows "
            + "the current (only) supported language rather than offering a picker.")]
        [SerializeField] private TMP_Text languageValueLabel;

        [SerializeField] private Button resetProgressButton;
        [SerializeField] private GameObject resetProgressIdleLabel;
        [SerializeField] private GameObject resetProgressArmedLabel;

        [SerializeField] private Button resetTutorialButton;
        [SerializeField] private Button aboutButton;

        /// <summary>Raised only on the confirming second tap, never the arming first tap.</summary>
        public event Action ResetProgressConfirmed;
        public event Action ResetTutorialRequested;
        public event Action AboutRequested;

        /// <summary>Raised once when the user flips a toggle on this tab; never for a Render().</summary>
        public event Action ToggleChanged;

        public bool ReducedMotion => reducedMotion != null && reducedMotion.isOn;

        public TextSizeMode TextSizeMode => textSizeSlider != null
            ? StepToMode(Mathf.RoundToInt(textSizeSlider.value))
            : TextSizeMode.Normal;

        public bool HighContrast => highContrast != null && highContrast.isOn;
        public bool IsResetProgressArmed { get; private set; }

        private void OnEnable()
        {
            if (resetProgressButton != null) resetProgressButton.onClick.AddListener(HandleResetProgressClicked);
            if (resetTutorialButton != null) resetTutorialButton.onClick.AddListener(HandleResetTutorialClicked);
            if (aboutButton != null) aboutButton.onClick.AddListener(HandleAboutClicked);
            if (reducedMotion != null) reducedMotion.onValueChanged.AddListener(HandleToggleChanged);
            if (textSizeSlider != null) textSizeSlider.onValueChanged.AddListener(HandleTextSizeChanged);
            if (highContrast != null) highContrast.onValueChanged.AddListener(HandleToggleChanged);
        }

        private void OnDisable()
        {
            if (resetProgressButton != null) resetProgressButton.onClick.RemoveListener(HandleResetProgressClicked);
            if (resetTutorialButton != null) resetTutorialButton.onClick.RemoveListener(HandleResetTutorialClicked);
            if (aboutButton != null) aboutButton.onClick.RemoveListener(HandleAboutClicked);
            if (reducedMotion != null) reducedMotion.onValueChanged.RemoveListener(HandleToggleChanged);
            if (textSizeSlider != null) textSizeSlider.onValueChanged.RemoveListener(HandleTextSizeChanged);
            if (highContrast != null) highContrast.onValueChanged.RemoveListener(HandleToggleChanged);
            DisarmResetProgress();
        }

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (reducedMotion != null) reducedMotion.SetIsOnWithoutNotify(settings.ReducedMotion);

            TextSizeMode mode = settings.TextSizeMode;
            if (textSizeSlider != null) textSizeSlider.SetValueWithoutNotify(ModeToStep(mode));
            SetTextSizeLabel(mode);

            if (highContrast != null) highContrast.SetIsOnWithoutNotify(settings.HighContrast);

            if (languageValueLabel != null)
            {
                languageValueLabel.text = DisplayNameForLanguage(settings.Language);
            }

            DisarmResetProgress();
        }

        /// <summary>Called by the container whenever this tab stops being the visible one.</summary>
        public void DisarmResetProgress()
        {
            IsResetProgressArmed = false;
            UpdateResetProgressLabel();
        }

        private void HandleResetProgressClicked()
        {
            if (!IsResetProgressArmed)
            {
                IsResetProgressArmed = true;
                UpdateResetProgressLabel();
                return;
            }

            DisarmResetProgress();
            ResetProgressConfirmed?.Invoke();
        }

        private void UpdateResetProgressLabel()
        {
            if (resetProgressIdleLabel != null) resetProgressIdleLabel.SetActive(!IsResetProgressArmed);
            if (resetProgressArmedLabel != null) resetProgressArmedLabel.SetActive(IsResetProgressArmed);
        }

        private void HandleResetTutorialClicked() => ResetTutorialRequested?.Invoke();

        private void HandleAboutClicked() => AboutRequested?.Invoke();

        private void HandleToggleChanged(bool value) => ToggleChanged?.Invoke();

        private void HandleTextSizeChanged(float value)
        {
            SetTextSizeLabel(StepToMode(Mathf.RoundToInt(value)));
            ToggleChanged?.Invoke();
        }

        private void SetTextSizeLabel(TextSizeMode mode)
        {
            if (textSizeValueLabel != null)
            {
                textSizeValueLabel.text = DisplayNameForTextSize(mode);
            }
        }

        private static string DisplayNameForTextSize(TextSizeMode mode)
        {
            switch (mode)
            {
                case TextSizeMode.Small: return "Küçük";
                case TextSizeMode.Large: return "Büyük";
                default: return "Normal";
            }
        }

        private static int ModeToStep(TextSizeMode mode)
        {
            switch (mode)
            {
                case TextSizeMode.Small: return 0;
                case TextSizeMode.Large: return 2;
                default: return 1;
            }
        }

        private static TextSizeMode StepToMode(int step)
        {
            switch (step)
            {
                case 0: return TextSizeMode.Small;
                case 2: return TextSizeMode.Large;
                default: return TextSizeMode.Normal;
            }
        }

        /// <summary>No localization system exists yet (only Turkish content is authored), so this
        /// is a small fixed lookup rather than a real i18n table.</summary>
        private static string DisplayNameForLanguage(string languageCode)
        {
            return languageCode == "tr" ? "Türkçe" : languageCode;
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            Toggle motion,
            Slider textSize,
            TMP_Text textSizeLabel,
            Toggle contrast,
            Button resetProgress,
            GameObject resetProgressIdle,
            GameObject resetProgressArmed,
            Button resetTutorial,
            Button about,
            TMP_Text languageLabel = null)
        {
            reducedMotion = motion;
            textSizeSlider = textSize;
            textSizeValueLabel = textSizeLabel;
            highContrast = contrast;
            resetProgressButton = resetProgress;
            resetProgressIdleLabel = resetProgressIdle;
            resetProgressArmedLabel = resetProgressArmed;
            resetTutorialButton = resetTutorial;
            aboutButton = about;
            languageValueLabel = languageLabel;
        }
#endif
    }
}
