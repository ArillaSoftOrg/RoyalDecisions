using System;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Passive Controls tab of the settings menu. The game has a single swipe gesture plus an
    /// optional tap-button alternative, so this stays limited to that toggle, swipe sensitivity and
    /// visual-rotation inversion, an opt-out of the swipe gesture entirely, and touch haptics — no
    /// key or button remapping exists to expose.
    /// </summary>
    public sealed class ControlsSettingsPanelView : MonoBehaviour
    {
        /// <summary>Sensitivity is divided into this many steps for tick feedback (10% per step).</summary>
        private const float StepCount = 10f;

        [SerializeField] private Slider swipeSensitivity;

        [Tooltip("Optional. Shows the live percentage next to the sensitivity slider.")]
        [SerializeField] private TMP_Text swipeSensitivityValueLabel;

        [SerializeField] private Toggle tapButtonsEnabled;
        [SerializeField] private Toggle invertSwipeRotation;
        [SerializeField] private Toggle disableSwipe;
        [SerializeField] private Toggle haptics;

        /// <summary>Raised once when the user flips a toggle on this tab; never for a Render().</summary>
        public event Action ToggleChanged;

        /// <summary>Raised at most once per ~10% of travel, never for a programmatic Render.</summary>
        public event Action<float> SwipeSensitivityStepped;

        private float lastSensitivityStep = float.NaN;

        public float SwipeSensitivity => swipeSensitivity != null
            ? swipeSensitivity.value
            : GameSettings.DefaultSwipeSensitivity;

        public bool TapButtonsEnabled => tapButtonsEnabled == null || tapButtonsEnabled.isOn;

        public bool InvertSwipeRotation => invertSwipeRotation != null && invertSwipeRotation.isOn;

        public bool DisableSwipe => disableSwipe != null && disableSwipe.isOn;

        public bool HapticsEnabled => haptics == null || haptics.isOn;

        private void OnEnable()
        {
            if (swipeSensitivity != null)
                swipeSensitivity.onValueChanged.AddListener(HandleSwipeSensitivityChanged);
            if (tapButtonsEnabled != null)
                tapButtonsEnabled.onValueChanged.AddListener(HandleToggleChanged);
            if (invertSwipeRotation != null)
                invertSwipeRotation.onValueChanged.AddListener(HandleToggleChanged);
            if (disableSwipe != null)
                disableSwipe.onValueChanged.AddListener(HandleToggleChanged);
            if (haptics != null) haptics.onValueChanged.AddListener(HandleToggleChanged);
        }

        private void OnDisable()
        {
            if (swipeSensitivity != null)
                swipeSensitivity.onValueChanged.RemoveListener(HandleSwipeSensitivityChanged);
            if (tapButtonsEnabled != null)
                tapButtonsEnabled.onValueChanged.RemoveListener(HandleToggleChanged);
            if (invertSwipeRotation != null)
                invertSwipeRotation.onValueChanged.RemoveListener(HandleToggleChanged);
            if (disableSwipe != null)
                disableSwipe.onValueChanged.RemoveListener(HandleToggleChanged);
            if (haptics != null) haptics.onValueChanged.RemoveListener(HandleToggleChanged);
        }

        private void HandleToggleChanged(bool value) => ToggleChanged?.Invoke();

        private void HandleSwipeSensitivityChanged(float value)
        {
            SetPercentLabel(swipeSensitivityValueLabel, value);
            float step = Mathf.Round(Mathf.Clamp01(value) * StepCount);
            if (Mathf.Approximately(step, lastSensitivityStep))
            {
                return;
            }
            lastSensitivityStep = step;
            SwipeSensitivityStepped?.Invoke(value);
        }

        private static void SetPercentLabel(TMP_Text label, float normalizedValue)
        {
            if (label == null)
            {
                return;
            }
            label.text = Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 100f) + "%";
        }

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (swipeSensitivity != null)
                swipeSensitivity.SetValueWithoutNotify(settings.SwipeSensitivity);
            SetPercentLabel(swipeSensitivityValueLabel, settings.SwipeSensitivity);
            lastSensitivityStep = Mathf.Round(Mathf.Clamp01(settings.SwipeSensitivity) * StepCount);

            if (tapButtonsEnabled != null)
                tapButtonsEnabled.SetIsOnWithoutNotify(settings.TapButtonsEnabled);
            if (invertSwipeRotation != null)
                invertSwipeRotation.SetIsOnWithoutNotify(settings.InvertSwipeRotation);
            if (disableSwipe != null)
                disableSwipe.SetIsOnWithoutNotify(settings.DisableSwipe);
            if (haptics != null) haptics.SetIsOnWithoutNotify(settings.HapticsEnabled);
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            Slider sensitivity,
            Toggle tapButtons,
            Toggle invertRotation,
            Toggle disableSwipeToggle,
            Toggle hapticToggle,
            TMP_Text sensitivityLabel = null)
        {
            swipeSensitivity = sensitivity;
            swipeSensitivityValueLabel = sensitivityLabel;
            tapButtonsEnabled = tapButtons;
            invertSwipeRotation = invertRotation;
            disableSwipe = disableSwipeToggle;
            haptics = hapticToggle;
        }
#endif
    }
}
