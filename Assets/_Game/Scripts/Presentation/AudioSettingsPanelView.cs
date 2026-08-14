using System;
using RoyalDecisions.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Passive Audio tab of the settings menu. Never touches the store or applies rules.</summary>
    public sealed class AudioSettingsPanelView : MonoBehaviour
    {
        /// <summary>Volume is divided into this many steps for tick feedback (10% per step).</summary>
        private const float StepCount = 10f;

        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider sfxVolume;
        [SerializeField] private Toggle masterMute;

        /// <summary>Raised at most once per ~10% of travel, never for a programmatic Render.</summary>
        public event Action<float> MusicVolumeStepped;

        /// <summary>As <see cref="MusicVolumeStepped"/>, carrying the value to preview it at.</summary>
        public event Action<float> SfxVolumeStepped;

        /// <summary>
        /// Raised once when the user flips the master mute toggle; never for a Render(). Carries
        /// the new value so a listener can sequence the live mute state against a click cue.
        /// </summary>
        public event Action<bool> MasterMuteChanged;

        private float lastMusicStep = float.NaN;
        private float lastSfxStep = float.NaN;

        public float MusicVolume => musicVolume != null ? musicVolume.value : GameSettings.DefaultVolume;
        public float SfxVolume => sfxVolume != null ? sfxVolume.value : GameSettings.DefaultVolume;
        public bool MasterMuted => masterMute != null && masterMute.isOn;

        private void OnEnable()
        {
            if (musicVolume != null) musicVolume.onValueChanged.AddListener(HandleMusicVolumeChanged);
            if (sfxVolume != null) sfxVolume.onValueChanged.AddListener(HandleSfxVolumeChanged);
            if (masterMute != null) masterMute.onValueChanged.AddListener(HandleMasterMuteChanged);
        }

        private void OnDisable()
        {
            if (musicVolume != null) musicVolume.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            if (sfxVolume != null) sfxVolume.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            if (masterMute != null) masterMute.onValueChanged.RemoveListener(HandleMasterMuteChanged);
        }

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (musicVolume != null) musicVolume.SetValueWithoutNotify(settings.MusicVolume);
            if (sfxVolume != null) sfxVolume.SetValueWithoutNotify(settings.SfxVolume);
            if (masterMute != null) masterMute.SetIsOnWithoutNotify(settings.MasterMuted);

            // Establishes the baseline step silently, so the first real drag only ticks once it
            // actually leaves the step the panel opened on.
            lastMusicStep = StepOf(settings.MusicVolume);
            lastSfxStep = StepOf(settings.SfxVolume);
        }

        private void HandleMusicVolumeChanged(float value)
        {
            float step = StepOf(value);
            if (Mathf.Approximately(step, lastMusicStep))
            {
                return;
            }
            lastMusicStep = step;
            MusicVolumeStepped?.Invoke(value);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            float step = StepOf(value);
            if (Mathf.Approximately(step, lastSfxStep))
            {
                return;
            }
            lastSfxStep = step;
            SfxVolumeStepped?.Invoke(value);
        }

        private static float StepOf(float value) => Mathf.Round(Mathf.Clamp01(value) * StepCount);

        private void HandleMasterMuteChanged(bool value) => MasterMuteChanged?.Invoke(value);

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
