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
        public const float DefaultSwipeSensitivity = 0.5f;
        public const string DefaultLanguage = "tr";

        [SerializeField] private float musicVolume = DefaultVolume;
        [SerializeField] private float sfxVolume = DefaultVolume;

        /// <summary>
        /// Multiplies both <see cref="musicVolume"/> and <see cref="sfxVolume"/> before either
        /// reaches the audio system. Defaults to full so preferences saved before this field
        /// existed keep sounding exactly as loud as they did (additive JSON: an old file missing
        /// this key loads this field initializer, not zero).
        /// </summary>
        [SerializeField] private float masterVolume = MaxVolume;

        [SerializeField] private bool hapticsEnabled = true;
        [SerializeField] private bool masterMuted;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private TextSizeMode textSizeMode = TextSizeMode.Normal;
        [SerializeField] private bool highContrast;
        [SerializeField] private bool tutorialCompleted;
        [SerializeField] private string language = DefaultLanguage;

        // --- Graphics ---
        [SerializeField] private FrameRateMode frameRateMode = FrameRateMode.Sixty;
        [SerializeField] private bool batterySaverEnabled;

        // --- Controls ---
        [SerializeField] private bool tapButtonsEnabled = true;
        [SerializeField] private bool invertSwipeRotation;
        [SerializeField] private float swipeSensitivity = DefaultSwipeSensitivity;
        [SerializeField] private bool disableSwipe;

        public static GameSettings CreateDefault()
        {
            return new GameSettings();
        }

        public float MusicVolume => musicVolume;

        public float SfxVolume => sfxVolume;

        public float MasterVolume => masterVolume;

        public bool HapticsEnabled => hapticsEnabled;

        public bool MasterMuted => masterMuted;

        public bool ReducedMotion => reducedMotion;

        public TextSizeMode TextSizeMode => textSizeMode;

        public bool HighContrast => highContrast;

        public bool TutorialCompleted => tutorialCompleted;

        public string Language => language;

        public FrameRateMode FrameRateMode => frameRateMode;

        public bool BatterySaverEnabled => batterySaverEnabled;

        public bool TapButtonsEnabled => tapButtonsEnabled;

        public bool InvertSwipeRotation => invertSwipeRotation;

        public float SwipeSensitivity => swipeSensitivity;

        public bool DisableSwipe => disableSwipe;

        public void SetMusicVolume(float value)
        {
            musicVolume = ClampUnitRange(value, DefaultVolume);
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = ClampUnitRange(value, DefaultVolume);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = ClampUnitRange(value, MaxVolume);
        }

        public void SetHapticsEnabled(bool value)
        {
            hapticsEnabled = value;
        }

        public void SetMasterMuted(bool value) => masterMuted = value;

        public void SetReducedMotion(bool value) => reducedMotion = value;

        public void SetTextSizeMode(TextSizeMode value) => textSizeMode = value;

        public void SetHighContrast(bool value) => highContrast = value;

        public void SetTutorialCompleted(bool value) => tutorialCompleted = value;

        public void SetLanguage(string value) =>
            language = string.IsNullOrWhiteSpace(value) ? DefaultLanguage : value;

        public void SetFrameRateMode(FrameRateMode value) => frameRateMode = value;

        public void SetBatterySaverEnabled(bool value) => batterySaverEnabled = value;

        public void SetTapButtonsEnabled(bool value) => tapButtonsEnabled = value;

        public void SetInvertSwipeRotation(bool value) => invertSwipeRotation = value;

        public void SetSwipeSensitivity(float value) =>
            swipeSensitivity = ClampUnitRange(value, DefaultSwipeSensitivity);

        public void SetDisableSwipe(bool value) => disableSwipe = value;

        /// <summary>
        /// Repairs values written straight into the backing fields by deserialization.
        /// Returns whether anything had to change.
        /// </summary>
        public bool SanitizeAfterLoad()
        {
            bool repaired = false;

            float music = ClampUnitRange(musicVolume, DefaultVolume);
            if (!Mathf.Approximately(music, musicVolume) || float.IsNaN(musicVolume))
            {
                musicVolume = music;
                repaired = true;
            }

            float sfx = ClampUnitRange(sfxVolume, DefaultVolume);
            if (!Mathf.Approximately(sfx, sfxVolume) || float.IsNaN(sfxVolume))
            {
                sfxVolume = sfx;
                repaired = true;
            }

            float master = ClampUnitRange(masterVolume, MaxVolume);
            if (!Mathf.Approximately(master, masterVolume) || float.IsNaN(masterVolume))
            {
                masterVolume = master;
                repaired = true;
            }

            float sensitivity = ClampUnitRange(swipeSensitivity, DefaultSwipeSensitivity);
            if (!Mathf.Approximately(sensitivity, swipeSensitivity) || float.IsNaN(swipeSensitivity))
            {
                swipeSensitivity = sensitivity;
                repaired = true;
            }

            if (!Enum.IsDefined(typeof(FrameRateMode), frameRateMode))
            {
                frameRateMode = FrameRateMode.Sixty;
                repaired = true;
            }

            if (!Enum.IsDefined(typeof(TextSizeMode), textSizeMode))
            {
                textSizeMode = TextSizeMode.Normal;
                repaired = true;
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                language = DefaultLanguage;
                repaired = true;
            }

            return repaired;
        }

        /// <summary>
        /// NaN is handled explicitly: every comparison against NaN is false, so it would slide
        /// straight through <see cref="Mathf.Clamp(float, float, float)"/> and reach the audio
        /// mixer (or the swipe threshold) intact.
        /// </summary>
        private static float ClampUnitRange(float value, float fallbackForNaN)
        {
            if (float.IsNaN(value))
            {
                return fallbackForNaN;
            }

            return Mathf.Clamp(value, MinVolume, MaxVolume);
        }
    }
}
