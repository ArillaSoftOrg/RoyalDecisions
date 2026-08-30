using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Stores <see cref="GameSettings"/> in <see cref="PlayerPrefs"/>, one key per field.
    /// </summary>
    /// <remarks>
    /// Settings are small preferences, which is the one thing CLAUDE.md §8 does allow PlayerPrefs
    /// to hold — run progress still belongs to the versioned JSON save and never comes near this
    /// class. Kept behind <see cref="ISettingsStore"/> so the menu controller never mentions
    /// PlayerPrefs itself and can be pointed at the JSON store instead by changing one constructor
    /// call.
    /// <para>
    /// PlayerPrefs has no bool or enum support, so bools are written as 0/1 and enums as their
    /// underlying int. Nothing here trusts what it reads back: every load ends in
    /// <see cref="GameSettings.SanitizeAfterLoad"/>, which clamps ranges, repairs NaN floats and
    /// rejects enum values that are not defined.
    /// </para>
    /// </remarks>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        /// <summary>Bumped when the stored key layout changes in a way older data cannot satisfy.</summary>
        public const int CurrentVersion = 1;

        public const string DefaultKeyPrefix = "royaldecisions.settings.";

        private const string VersionKey = "version";
        private const string MasterVolumeKey = "masterVolume";
        private const string MusicVolumeKey = "musicVolume";
        private const string SfxVolumeKey = "sfxVolume";
        private const string MasterMutedKey = "masterMuted";
        private const string HapticsEnabledKey = "hapticsEnabled";
        private const string ReducedMotionKey = "reducedMotion";
        private const string TextSizeModeKey = "textSizeMode";
        private const string HighContrastKey = "highContrast";
        private const string TutorialCompletedKey = "tutorialCompleted";
        private const string LanguageKey = "language";
        private const string FrameRateModeKey = "frameRateMode";
        private const string BatterySaverKey = "batterySaver";
        private const string TapButtonsKey = "tapButtons";
        private const string InvertSwipeRotationKey = "invertSwipeRotation";
        private const string SwipeSensitivityKey = "swipeSensitivity";
        private const string DisableSwipeKey = "disableSwipe";

        private readonly string prefix;

        /// <param name="keyPrefix">
        /// Namespaces every key. Tests pass a unique prefix so they can delete exactly what they
        /// wrote instead of clearing the editor's real preferences.
        /// </param>
        public PlayerPrefsSettingsStore(string keyPrefix = DefaultKeyPrefix)
        {
            prefix = string.IsNullOrEmpty(keyPrefix) ? DefaultKeyPrefix : keyPrefix;
        }

        /// <summary>Every key this store owns, for cleanup and for tests.</summary>
        public string[] AllKeys()
        {
            return new[]
            {
                Key(VersionKey), Key(MasterVolumeKey), Key(MusicVolumeKey), Key(SfxVolumeKey),
                Key(MasterMutedKey), Key(HapticsEnabledKey), Key(ReducedMotionKey),
                Key(TextSizeModeKey), Key(HighContrastKey), Key(TutorialCompletedKey),
                Key(LanguageKey), Key(FrameRateModeKey), Key(BatterySaverKey), Key(TapButtonsKey),
                Key(InvertSwipeRotationKey), Key(SwipeSensitivityKey), Key(DisableSwipeKey)
            };
        }

        /// <summary>
        /// Never throws and never blocks startup: anything missing, unreadable or from an
        /// unsupported version falls back to defaults, exactly as <see cref="ISettingsStore"/>
        /// promises.
        /// </summary>
        public GameSettings Load()
        {
            GameSettings settings = GameSettings.CreateDefault();

            // No version key at all means nothing has ever been saved — first run, not corruption.
            if (!PlayerPrefs.HasKey(Key(VersionKey)))
            {
                return settings;
            }

            int version = PlayerPrefs.GetInt(Key(VersionKey), 0);
            if (version != CurrentVersion)
            {
                // A future or unrecognised layout: the player loses their preferences, never a run.
                Debug.LogWarning(
                    "Settings were saved with an unsupported version (" + version + ", expected "
                    + CurrentVersion + "); falling back to defaults.");
                return settings;
            }

            settings.SetMasterVolume(GetFloat(MasterVolumeKey, settings.MasterVolume));
            settings.SetMusicVolume(GetFloat(MusicVolumeKey, settings.MusicVolume));
            settings.SetSfxVolume(GetFloat(SfxVolumeKey, settings.SfxVolume));
            settings.SetMasterMuted(GetBool(MasterMutedKey, settings.MasterMuted));
            settings.SetHapticsEnabled(GetBool(HapticsEnabledKey, settings.HapticsEnabled));
            settings.SetReducedMotion(GetBool(ReducedMotionKey, settings.ReducedMotion));
            settings.SetTextSizeMode((TextSizeMode)GetInt(TextSizeModeKey, (int)settings.TextSizeMode));
            settings.SetHighContrast(GetBool(HighContrastKey, settings.HighContrast));
            settings.SetTutorialCompleted(GetBool(TutorialCompletedKey, settings.TutorialCompleted));
            settings.SetLanguage(PlayerPrefs.GetString(Key(LanguageKey), settings.Language));
            settings.SetFrameRateMode(
                (FrameRateMode)GetInt(FrameRateModeKey, (int)settings.FrameRateMode));
            settings.SetBatterySaverEnabled(GetBool(BatterySaverKey, settings.BatterySaverEnabled));
            settings.SetTapButtonsEnabled(GetBool(TapButtonsKey, settings.TapButtonsEnabled));
            settings.SetInvertSwipeRotation(
                GetBool(InvertSwipeRotationKey, settings.InvertSwipeRotation));
            settings.SetSwipeSensitivity(GetFloat(SwipeSensitivityKey, settings.SwipeSensitivity));
            settings.SetDisableSwipe(GetBool(DisableSwipeKey, settings.DisableSwipe));

            // Anything out of range, NaN, or an undefined enum is repaired here rather than being
            // trusted onward into the audio and accessibility systems.
            settings.SanitizeAfterLoad();
            return settings;
        }

        public SaveOutcome Save(GameSettings settings)
        {
            if (settings == null)
            {
                return SaveOutcome.Failure("Cannot save null settings.");
            }

            try
            {
                PlayerPrefs.SetInt(Key(VersionKey), CurrentVersion);
                PlayerPrefs.SetFloat(Key(MasterVolumeKey), settings.MasterVolume);
                PlayerPrefs.SetFloat(Key(MusicVolumeKey), settings.MusicVolume);
                PlayerPrefs.SetFloat(Key(SfxVolumeKey), settings.SfxVolume);
                SetBool(MasterMutedKey, settings.MasterMuted);
                SetBool(HapticsEnabledKey, settings.HapticsEnabled);
                SetBool(ReducedMotionKey, settings.ReducedMotion);
                PlayerPrefs.SetInt(Key(TextSizeModeKey), (int)settings.TextSizeMode);
                SetBool(HighContrastKey, settings.HighContrast);
                SetBool(TutorialCompletedKey, settings.TutorialCompleted);
                PlayerPrefs.SetString(Key(LanguageKey), settings.Language ?? GameSettings.DefaultLanguage);
                PlayerPrefs.SetInt(Key(FrameRateModeKey), (int)settings.FrameRateMode);
                SetBool(BatterySaverKey, settings.BatterySaverEnabled);
                SetBool(TapButtonsKey, settings.TapButtonsEnabled);
                SetBool(InvertSwipeRotationKey, settings.InvertSwipeRotation);
                PlayerPrefs.SetFloat(Key(SwipeSensitivityKey), settings.SwipeSensitivity);
                SetBool(DisableSwipeKey, settings.DisableSwipe);

                // Flush now rather than at the next natural quit: a settings screen is exactly the
                // place a player force-closes the app right after pressing Apply.
                PlayerPrefs.Save();
                return SaveOutcome.Ok();
            }
            catch (PlayerPrefsException exception)
            {
                // Reported, never swallowed — the caller surfaces this to the player.
                return SaveOutcome.Failure("Could not write settings: " + exception.Message);
            }
        }

        private string Key(string name) => prefix + name;

        private float GetFloat(string name, float fallback) =>
            PlayerPrefs.GetFloat(Key(name), fallback);

        private int GetInt(string name, int fallback) => PlayerPrefs.GetInt(Key(name), fallback);

        private bool GetBool(string name, bool fallback) =>
            PlayerPrefs.GetInt(Key(name), fallback ? 1 : 0) != 0;

        private void SetBool(string name, bool value) => PlayerPrefs.SetInt(Key(name), value ? 1 : 0);
    }
}
