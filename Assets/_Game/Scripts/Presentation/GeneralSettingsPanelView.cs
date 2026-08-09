using System;
using RoyalDecisions.Domain;
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
        [SerializeField] private Toggle largerText;
        [SerializeField] private Toggle highContrast;

        [SerializeField] private Button resetProgressButton;
        [SerializeField] private GameObject resetProgressIdleLabel;
        [SerializeField] private GameObject resetProgressArmedLabel;

        [SerializeField] private Button resetTutorialButton;
        [SerializeField] private Button aboutButton;

        /// <summary>Raised only on the confirming second tap, never the arming first tap.</summary>
        public event Action ResetProgressConfirmed;
        public event Action ResetTutorialRequested;
        public event Action AboutRequested;

        public bool ReducedMotion => reducedMotion != null && reducedMotion.isOn;
        public bool LargerText => largerText != null && largerText.isOn;
        public bool HighContrast => highContrast != null && highContrast.isOn;
        public bool IsResetProgressArmed { get; private set; }

        private void OnEnable()
        {
            if (resetProgressButton != null) resetProgressButton.onClick.AddListener(HandleResetProgressClicked);
            if (resetTutorialButton != null) resetTutorialButton.onClick.AddListener(HandleResetTutorialClicked);
            if (aboutButton != null) aboutButton.onClick.AddListener(HandleAboutClicked);
        }

        private void OnDisable()
        {
            if (resetProgressButton != null) resetProgressButton.onClick.RemoveListener(HandleResetProgressClicked);
            if (resetTutorialButton != null) resetTutorialButton.onClick.RemoveListener(HandleResetTutorialClicked);
            if (aboutButton != null) aboutButton.onClick.RemoveListener(HandleAboutClicked);
            DisarmResetProgress();
        }

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (reducedMotion != null) reducedMotion.SetIsOnWithoutNotify(settings.ReducedMotion);
            if (largerText != null) largerText.SetIsOnWithoutNotify(settings.LargerText);
            if (highContrast != null) highContrast.SetIsOnWithoutNotify(settings.HighContrast);
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

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            Toggle motion,
            Toggle text,
            Toggle contrast,
            Button resetProgress,
            GameObject resetProgressIdle,
            GameObject resetProgressArmed,
            Button resetTutorial,
            Button about)
        {
            reducedMotion = motion;
            largerText = text;
            highContrast = contrast;
            resetProgressButton = resetProgress;
            resetProgressIdleLabel = resetProgressIdle;
            resetProgressArmedLabel = resetProgressArmed;
            resetTutorialButton = resetTutorial;
            aboutButton = about;
        }
#endif
    }
}
