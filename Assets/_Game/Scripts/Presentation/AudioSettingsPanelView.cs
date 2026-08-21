using System;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Passive Audio tab of the settings menu. Never touches the store or applies rules.</summary>
    public sealed class AudioSettingsPanelView : MonoBehaviour
    {
        /// <summary>Volume is divided into this many steps for tick feedback (10% per step).</summary>
        private const float StepCount = 10f;

        [SerializeField] private Slider masterVolume;
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider sfxVolume;
        [SerializeField] private Toggle masterMute;

        [Tooltip("Optional. Shows the live percentage next to each slider.")]
        [SerializeField] private TMP_Text masterVolumeValueLabel;
        [SerializeField] private TMP_Text musicVolumeValueLabel;
        [SerializeField] private TMP_Text sfxVolumeValueLabel;

        /// <summary>Raised at most once per ~10% of travel, never for a programmatic Render.</summary>
        public event Action<float> MasterVolumeStepped;

        /// <summary>As <see cref="MasterVolumeStepped"/>.</summary>
        public event Action<float> MusicVolumeStepped;

        /// <summary>As <see cref="MasterVolumeStepped"/>, carrying the value to preview it at.</summary>
        public event Action<float> SfxVolumeStepped;

        /// <summary>
        /// Raised once when the user flips the master mute toggle; never for a Render(). Carries
        /// the new value so a listener can sequence the live mute state against a click cue.
        /// </summary>
        public event Action<bool> MasterMuteChanged;

        private float lastMasterStep = float.NaN;
        private float lastMusicStep = float.NaN;
        private float lastSfxStep = float.NaN;

        public float MasterVolume => masterVolume != null ? masterVolume.value : GameSettings.MaxVolume;
        public float MusicVolume => musicVolume != null ? musicVolume.value : GameSettings.DefaultVolume;
        public float SfxVolume => sfxVolume != null ? sfxVolume.value : GameSettings.DefaultVolume;
        public bool MasterMuted => masterMute != null && masterMute.isOn;

        private void OnEnable()
        {
            if (masterVolume != null) masterVolume.onValueChanged.AddListener(HandleMasterVolumeChanged);
            if (musicVolume != null) musicVolume.onValueChanged.AddListener(HandleMusicVolumeChanged);
            if (sfxVolume != null) sfxVolume.onValueChanged.AddListener(HandleSfxVolumeChanged);
            if (masterMute != null) masterMute.onValueChanged.AddListener(HandleMasterMuteChanged);
        }

        private void OnDisable()
        {
            if (masterVolume != null) masterVolume.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            if (musicVolume != null) musicVolume.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            if (sfxVolume != null) sfxVolume.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            if (masterMute != null) masterMute.onValueChanged.RemoveListener(HandleMasterMuteChanged);
        }

        public void Render(GameSettings settings)
        {
            settings ??= GameSettings.CreateDefault();
            if (masterVolume != null) masterVolume.SetValueWithoutNotify(settings.MasterVolume);
            if (musicVolume != null) musicVolume.SetValueWithoutNotify(settings.MusicVolume);
            if (sfxVolume != null) sfxVolume.SetValueWithoutNotify(settings.SfxVolume);
            if (masterMute != null) masterMute.SetIsOnWithoutNotify(settings.MasterMuted);

            SetPercentLabel(masterVolumeValueLabel, settings.MasterVolume);
            SetPercentLabel(musicVolumeValueLabel, settings.MusicVolume);
            SetPercentLabel(sfxVolumeValueLabel, settings.SfxVolume);

            // Establishes the baseline step silently, so the first real drag only ticks once it
            // actually leaves the step the panel opened on.
            lastMasterStep = StepOf(settings.MasterVolume);
            lastMusicStep = StepOf(settings.MusicVolume);
            lastSfxStep = StepOf(settings.SfxVolume);
        }

        private void HandleMasterVolumeChanged(float value)
        {
            SetPercentLabel(masterVolumeValueLabel, value);
            float step = StepOf(value);
            if (Mathf.Approximately(step, lastMasterStep))
            {
                return;
            }
            lastMasterStep = step;
            MasterVolumeStepped?.Invoke(value);
        }

        private void HandleMusicVolumeChanged(float value)
        {
            SetPercentLabel(musicVolumeValueLabel, value);
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
            SetPercentLabel(sfxVolumeValueLabel, value);
            float step = StepOf(value);
            if (Mathf.Approximately(step, lastSfxStep))
            {
                return;
            }
            lastSfxStep = step;
            SfxVolumeStepped?.Invoke(value);
        }

        private static float StepOf(float value) => Mathf.Round(Mathf.Clamp01(value) * StepCount);

        private static void SetPercentLabel(TMP_Text label, float normalizedValue)
        {
            if (label == null)
            {
                return;
            }
            label.text = Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 100f) + "%";
        }

        private void HandleMasterMuteChanged(bool value) => MasterMuteChanged?.Invoke(value);

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            Slider master,
            Slider music,
            Slider sfx,
            Toggle mute,
            TMP_Text masterLabel = null,
            TMP_Text musicLabel = null,
            TMP_Text sfxLabel = null)
        {
            masterVolume = master;
            musicVolume = music;
            sfxVolume = sfx;
            masterMute = mute;
            masterVolumeValueLabel = masterLabel;
            musicVolumeValueLabel = musicLabel;
            sfxVolumeValueLabel = sfxLabel;
        }
#endif
    }
}
