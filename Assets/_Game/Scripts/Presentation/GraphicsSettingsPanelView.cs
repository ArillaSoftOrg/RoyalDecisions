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

        public bool UseHighFrameRateCap => useHighFrameRateCap == null || useHighFrameRateCap.isOn;
        public bool BatterySaverEnabled => batterySaver != null && batterySaver.isOn;

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
