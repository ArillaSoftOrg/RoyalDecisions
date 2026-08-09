using System;
using UnityEngine;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// Where saves live, derived from a single root directory.
    /// </summary>
    /// <remarks>
    /// A plain class rather than an interface: the root string is the entire seam a test needs, so
    /// an <c>ISavePathProvider</c> would be abstraction for its own sake. Run data and settings are
    /// separate files, so deleting a run never disturbs preferences and a corrupt run save cannot
    /// cost the player their volume settings.
    /// </remarks>
    public sealed class SavePaths
    {
        public const string RunFileName = "run.json";
        public const string SettingsFileName = "settings.json";
        public const string TempExtension = ".tmp";
        public const string BackupExtension = ".bak";

        public SavePaths(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("A save root directory is required.", nameof(rootDirectory));
            }

            RootDirectory = rootDirectory.Replace('\\', '/').TrimEnd('/');

            RunSavePath = RootDirectory + "/" + RunFileName;
            RunTempPath = RunSavePath + TempExtension;
            RunBackupPath = RunSavePath + BackupExtension;

            SettingsSavePath = RootDirectory + "/" + SettingsFileName;
            SettingsTempPath = SettingsSavePath + TempExtension;
            SettingsBackupPath = SettingsSavePath + BackupExtension;
        }

        /// <summary>
        /// The production paths. This is the only place in the codebase that reads
        /// <see cref="Application.persistentDataPath"/>; tests always construct their own root.
        /// </summary>
        public static SavePaths ForPersistentData()
        {
            return new SavePaths(Application.persistentDataPath);
        }

        public string RootDirectory { get; }

        public string RunSavePath { get; }

        public string RunTempPath { get; }

        public string RunBackupPath { get; }

        public string SettingsSavePath { get; }

        public string SettingsTempPath { get; }

        public string SettingsBackupPath { get; }
    }
}
