using System;
using UnityEngine;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Player preferences, stored separately from run progress.
    /// </summary>
    /// <remarks>
    /// Lives in Domain rather than Infrastructure so presentation code can read settings without
    /// depending on the file layer. Losing this file costs the player a volume slider, never a run —
    /// which is exactly why CLAUDE.md §8 keeps the two apart.
    /// </remarks>
    [Serializable]
    public sealed class GameSettings
    {
        public const float MinVolume = 0f;
        public const float MaxVolume = 1f;
        public const float DefaultVolume = 0.8f;

        [SerializeField] private float musicVolume = DefaultVolume;
        [SerializeField] private float sfxVolume = DefaultVolume;
        [SerializeField] private bool hapticsEnabled = true;
        [SerializeField] private bool masterMuted;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private bool largerText;
        [SerializeField] private bool highContrast;
        [SerializeField] private bool tutorialCompleted;

        // --- Graphics ---
        [SerializeField] private bool useHighFrameRateCap = true;
        [SerializeField] private bool batterySaverEnabled;

        // --- Controls ---
        [SerializeField] private bool tapButtonsEnabled = true;
        [SerializeField] private bool invertSwipeRotation;

        public static GameSettings CreateDefault()
        {
            return new GameSettings();
        }

        public float MusicVolume => musicVolume;

        public float SfxVolume => sfxVolume;

        public bool HapticsEnabled => hapticsEnabled;

        public bool MasterMuted => masterMuted;

        public bool ReducedMotion => reducedMotion;

        public bool LargerText => largerText;

        public bool HighContrast => highContrast;

        public bool TutorialCompleted => tutorialCompleted;

        public bool UseHighFrameRateCap => useHighFrameRateCap;

        public bool BatterySaverEnabled => batterySaverEnabled;

        public bool TapButtonsEnabled => tapButtonsEnabled;

        public bool InvertSwipeRotation => invertSwipeRotation;

        public void SetMusicVolume(float value)
        {
            musicVolume = ClampVolume(value);
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = ClampVolume(value);
        }

        public void SetHapticsEnabled(bool value)
        {
            hapticsEnabled = value;
        }

        public void SetMasterMuted(bool value) => masterMuted = value;

        public void SetReducedMotion(bool value) => reducedMotion = value;

        public void SetLargerText(bool value) => largerText = value;

        public void SetHighContrast(bool value) => highContrast = value;

        public void SetTutorialCompleted(bool value) => tutorialCompleted = value;

        public void SetUseHighFrameRateCap(bool value) => useHighFrameRateCap = value;

        public void SetBatterySaverEnabled(bool value) => batterySaverEnabled = value;

        public void SetTapButtonsEnabled(bool value) => tapButtonsEnabled = value;

        public void SetInvertSwipeRotation(bool value) => invertSwipeRotation = value;

        /// <summary>
        /// Repairs values written straight into the backing fields by deserialization.
        /// Returns whether anything had to change.
        /// </summary>
        public bool SanitizeAfterLoad()
        {
            bool repaired = false;

            float music = ClampVolume(musicVolume);
            if (!Mathf.Approximately(music, musicVolume) || float.IsNaN(musicVolume))
            {
                musicVolume = music;
                repaired = true;
            }

            float sfx = ClampVolume(sfxVolume);
            if (!Mathf.Approximately(sfx, sfxVolume) || float.IsNaN(sfxVolume))
            {
                sfxVolume = sfx;
                repaired = true;
            }

            return repaired;
        }

        /// <summary>
        /// NaN is handled explicitly: every comparison against NaN is false, so it would slide
        /// straight through <see cref="Mathf.Clamp"/> and reach the audio mixer intact.
        /// </summary>
        private static float ClampVolume(float value)
        {
            if (float.IsNaN(value))
            {
                return DefaultVolume;
            }

            return Mathf.Clamp(value, MinVolume, MaxVolume);
        }
    }
}
