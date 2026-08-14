using System;
using RoyalDecisions.Domain;
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
        [SerializeField] private Toggle useHighFrameRateCap;
        [SerializeField] private Toggle batterySaver;

        /// <summary>Raised once when the user flips a toggle on this tab; never for a Render().</summary>
        public event Action ToggleChanged;

        public bool UseHighFrameRateCap => useHighFrameRateCap == null || useHighFrameRateCap.isOn;
        public bool BatterySaverEnabled => batterySaver != null && batterySaver.isOn;

        private void OnEnable()
        {
            if (useHighFrameRateCap != null)
                useHighFrameRateCap.onValueChanged.AddListener(HandleToggleChanged);
            if (batterySaver != null) batterySaver.onValueChanged.AddListener(HandleToggleChanged);
        }

        private void OnDisable()
        {
            if (useHighFrameRateCap != null)
                useHighFrameRateCap.onValueChanged.RemoveListener(HandleToggleChanged);
            if (batterySaver != null) batterySaver.onValueChanged.RemoveListener(HandleToggleChanged);
        }

        private void HandleToggleChanged(bool value) => ToggleChanged?.Invoke();

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (useHighFrameRateCap != null)
                useHighFrameRateCap.SetIsOnWithoutNotify(settings.UseHighFrameRateCap);
            if (batterySaver != null)
                batterySaver.SetIsOnWithoutNotify(settings.BatterySaverEnabled);
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(Toggle frameRateCap, Toggle battery)
        {
            useHighFrameRateCap = frameRateCap;
            batterySaver = battery;
        }
#endif
    }
}
