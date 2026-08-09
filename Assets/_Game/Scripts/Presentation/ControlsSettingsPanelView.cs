using RoyalDecisions.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Passive Controls tab of the settings menu. The game has a single swipe gesture plus an
    /// optional tap-button alternative, so this stays limited to that toggle, visual-rotation
    /// inversion, and touch haptics — no key or button remapping exists to expose.
    /// </summary>
    public sealed class ControlsSettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Toggle tapButtonsEnabled;
        [SerializeField] private Toggle invertSwipeRotation;
        [SerializeField] private Toggle haptics;

        public bool TapButtonsEnabled => tapButtonsEnabled == null || tapButtonsEnabled.isOn;

        public bool InvertSwipeRotation => invertSwipeRotation != null && invertSwipeRotation.isOn;

        public bool HapticsEnabled => haptics == null || haptics.isOn;

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (tapButtonsEnabled != null)
                tapButtonsEnabled.SetIsOnWithoutNotify(settings.TapButtonsEnabled);
            if (invertSwipeRotation != null)
                invertSwipeRotation.SetIsOnWithoutNotify(settings.InvertSwipeRotation);
            if (haptics != null) haptics.SetIsOnWithoutNotify(settings.HapticsEnabled);
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(Toggle tapButtons, Toggle invertRotation, Toggle hapticToggle)
        {
            tapButtonsEnabled = tapButtons;
            invertSwipeRotation = invertRotation;
            haptics = hapticToggle;
        }
#endif
    }
}
