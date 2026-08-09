using System;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// Persists player preferences, in their own file beside the run save.
    /// </summary>
    /// <remarks>
    /// <see cref="Load"/> always returns usable settings. Preferences are cheap to lose and must
    /// never stand between the player and the game, so every failure resolves to defaults rather
    /// than to a status the caller has to handle.
    /// </remarks>
    public sealed class SettingsSaveService
    {
        /// <summary>Kept alongside the run's version so the two files can diverge later.</summary>
        public const int CurrentSettingsVersion = 1;

        private const int FirstSupportedVersion = 1;

        private readonly IFileSystem fileSystem;
        private readonly AtomicJsonFileStore store;
        private readonly SavePaths paths;

        public SettingsSaveService(IFileSystem fileSystem, SavePaths paths)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
            store = new AtomicJsonFileStore(fileSystem);
        }

        public bool HasSettings()
        {
            return fileSystem.FileExists(paths.SettingsSavePath);
        }

        public SaveResult Save(GameSettings settings)
        {
            if (settings == null)
            {
                return SaveResult.Failure(
                    SaveStatus.InvalidData,
                    "Refusing to save null settings.");
            }

            string json;
            try
            {
                SettingsSaveFile file = new SettingsSaveFile(CurrentSettingsVersion, settings);
                json = JsonUtility.ToJson(file, true);
            }
            catch (Exception error)
            {
                return SaveResult.Failure(SaveStatus.SerializationFailed, error.Message);
            }

            return store.Write(
                paths.SettingsSavePath,
                paths.SettingsTempPath,
                paths.SettingsBackupPath,
                json);
        }

        /// <summary>Never fails: anything unreadable resolves to defaults.</summary>
        public GameSettings Load()
        {
            GameSettings fromMain = TryLoadFrom(paths.SettingsSavePath);
            if (fromMain != null)
            {
                return fromMain;
            }

            GameSettings fromBackup = TryLoadFrom(paths.SettingsBackupPath);
            if (fromBackup != null)
            {
                return fromBackup;
            }

            return GameSettings.CreateDefault();
        }

        private GameSettings TryLoadFrom(string path)
        {
            TextReadResult read = store.Read(path);
            if (!read.HasText)
            {
                return null;
            }

            try
            {
                SaveFileHeader header = JsonUtility.FromJson<SaveFileHeader>(read.Text);
                if (header == null
                    || header.SaveVersion < FirstSupportedVersion
                    || header.SaveVersion > CurrentSettingsVersion)
                {
                    return null;
                }

                SettingsSaveFile file = JsonUtility.FromJson<SettingsSaveFile>(read.Text);
                if (file == null || file.Settings == null)
                {
                    return null;
                }

                file.Settings.SanitizeAfterLoad();
                return file.Settings;
            }
            catch (Exception error)
            {
                // Reported, not swallowed — but as a warning, because the caller falls back to
                // defaults and a malformed preferences file must not interrupt launch.
                Debug.LogWarning(
                    "[Settings] Could not read '" + path + "', falling back to defaults: "
                    + error.Message);
                return null;
            }
        }
    }
}
