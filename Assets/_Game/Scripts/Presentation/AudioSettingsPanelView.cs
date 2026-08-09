using RoyalDecisions.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Passive Audio tab of the settings menu. Never touches the store or applies rules.</summary>
    public sealed class AudioSettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider sfxVolume;
        [SerializeField] private Toggle masterMute;

        public float MusicVolume => musicVolume != null ? musicVolume.value : GameSettings.DefaultVolume;
        public float SfxVolume => sfxVolume != null ? sfxVolume.value : GameSettings.DefaultVolume;
        public bool MasterMuted => masterMute != null && masterMute.isOn;

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (musicVolume != null) musicVolume.SetValueWithoutNotify(settings.MusicVolume);
            if (sfxVolume != null) sfxVolume.SetValueWithoutNotify(settings.SfxVolume);
            if (masterMute != null) masterMute.SetIsOnWithoutNotify(settings.MasterMuted);
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(Slider music, Slider sfx, Toggle mute)
        {
            musicVolume = music;
            sfxVolume = sfx;
            masterMute = mute;
        }
#endif
    }
}
