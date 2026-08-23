using System;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Passive Graphics tab of the settings menu. Deliberately thin: this is a 2D uGUI portrait
    /// game, so there is no quality tier, resolution scale, or shadow/texture setting to expose.
    /// </summary>
    public sealed class GraphicsSettingsPanelView : MonoBehaviour
    {
        /// <summary>A four-step slider: 0 = 30 FPS, 1 = 60 FPS, 2 = 90 FPS, 3 = 120 FPS.</summary>
        [SerializeField] private Slider frameRateSlider;

        [Tooltip("Optional. Shows the current step's name (e.g. \"60 FPS\") next to the slider.")]
        [SerializeField] private TMP_Text frameRateValueLabel;

        [SerializeField] private Toggle batterySaver;

        /// <summary>Raised once when the user changes a control on this tab; never for a Render().</summary>
        public event Action ToggleChanged;

        public FrameRateMode FrameRateMode => frameRateSlider != null
            ? StepToMode(Mathf.RoundToInt(frameRateSlider.value))
            : FrameRateMode.Sixty;

        public bool BatterySaverEnabled => batterySaver != null && batterySaver.isOn;

        private void OnEnable()
        {
            if (frameRateSlider != null) frameRateSlider.onValueChanged.AddListener(HandleFrameRateChanged);
            if (batterySaver != null) batterySaver.onValueChanged.AddListener(HandleToggleChanged);
        }

        private void OnDisable()
        {
            if (frameRateSlider != null) frameRateSlider.onValueChanged.RemoveListener(HandleFrameRateChanged);
            if (batterySaver != null) batterySaver.onValueChanged.RemoveListener(HandleToggleChanged);
        }

        private void HandleToggleChanged(bool value) => ToggleChanged?.Invoke();

        private void HandleFrameRateChanged(float value)
        {
            SetLabel(StepToMode(Mathf.RoundToInt(value)));
            ToggleChanged?.Invoke();
        }

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (frameRateSlider != null)
                frameRateSlider.SetValueWithoutNotify(ModeToStep(settings.FrameRateMode));
            SetLabel(settings.FrameRateMode);
            if (batterySaver != null)
                batterySaver.SetIsOnWithoutNotify(settings.BatterySaverEnabled);
        }

        private void SetLabel(FrameRateMode mode)
        {
            if (frameRateValueLabel != null)
            {
                frameRateValueLabel.text = DisplayName(mode);
            }
        }

        private static string DisplayName(FrameRateMode mode)
        {
            switch (mode)
            {
                case FrameRateMode.Thirty: return "30 FPS";
                case FrameRateMode.Ninety: return "90 FPS";
                case FrameRateMode.OneTwenty: return "120 FPS";
                default: return "60 FPS";
            }
        }

        private static int ModeToStep(FrameRateMode mode)
        {
            switch (mode)
            {
                case FrameRateMode.Thirty: return 0;
                case FrameRateMode.Ninety: return 2;
                case FrameRateMode.OneTwenty: return 3;
                default: return 1;
            }
        }

        private static FrameRateMode StepToMode(int step)
        {
            switch (step)
            {
                case 0: return FrameRateMode.Thirty;
                case 2: return FrameRateMode.Ninety;
                case 3: return FrameRateMode.OneTwenty;
                default: return FrameRateMode.Sixty;
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(Slider frameRate, TMP_Text frameRateLabel, Toggle battery)
        {
            frameRateSlider = frameRate;
            frameRateValueLabel = frameRateLabel;
            batterySaver = battery;
        }
#endif
    }
}
