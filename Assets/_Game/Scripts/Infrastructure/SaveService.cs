using System;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// Persists and restores the player's run.
    /// </summary>
    /// <remarks>
    /// Constructed with its dependencies rather than reached through a singleton, so a test points
    /// one at a temporary directory and the game points one at persistent data.
    ///
    /// No method throws. CLAUDE.md §14 requires a corrupt save not to block startup, so every
    /// failure is a <see cref="LoadStatus"/> the caller can act on.
    /// </remarks>
    public sealed class SaveService
    {
        private readonly IFileSystem fileSystem;
        private readonly AtomicJsonFileStore store;
        private readonly SavePaths paths;

        public SaveService(IFileSystem fileSystem, SavePaths paths)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
            store = new AtomicJsonFileStore(fileSystem);
        }

        public bool HasSave()
        {
            return fileSystem.FileExists(paths.RunSavePath);
        }

        public SaveResult Save(RunState runState)
        {
            if (runState == null)
            {
                return SaveResult.Failure(
                    SaveStatus.InvalidData,
                    "Refusing to save a null run.");
            }

            string json;
            try
            {
                RunSaveFile file = new RunSaveFile(GameConstants.CurrentSaveVersion, runState);
                json = JsonUtility.ToJson(file, true);
            }
            catch (Exception error)
            {
                return SaveResult.Failure(SaveStatus.SerializationFailed, error.Message);
            }

            return store.Write(paths.RunSavePath, paths.RunTempPath, paths.RunBackupPath, json);
        }

        /// <summary>
        /// Reads the run, falling back to the backup when the main file is unusable.
        /// </summary>
        /// <remarks>
        /// A file that cannot be read is never deleted: recovering it by hand stays possible, and
        /// an unsupported-version file must survive intact so downgrading and upgrading again does
        /// not destroy a newer save.
        /// </remarks>
        public RunLoadResult Load()
        {
            RunLoadResult primary = LoadFrom(paths.RunSavePath);

            if (primary.Succeeded)
            {
                return primary;
            }

            // A newer save is not damage, so do not reach for an older backup and quietly downgrade
            // the player's progress.
            if (primary.Status == LoadStatus.UnsupportedVersion)
            {
                return primary;
            }

            RunLoadResult backup = LoadFrom(paths.RunBackupPath);
            if (backup.Succeeded)
            {
                return RunLoadResult.Loaded(LoadStatus.RecoveredFromBackup, backup.RunState);
            }

            // Report what was wrong with the save the player actually has, not with its backup.
            return primary;
        }

        public SaveResult Delete()
        {
            try
            {
                store.DeleteIfPresent(paths.RunSavePath);
                store.DeleteIfPresent(paths.RunTempPath);
                store.DeleteIfPresent(paths.RunBackupPath);
                return SaveResult.Success();
            }
            catch (Exception error)
            {
                return SaveResult.Failure(SaveStatus.WriteFailed, error.Message);
            }
        }

        private RunLoadResult LoadFrom(string path)
        {
            TextReadResult read = store.Read(path);
            if (!read.HasText)
            {
                return RunLoadResult.Failure(read.Status, read.Message);
            }

            if (!TryReadVersion(read.Text, out int version, out string versionError))
            {
                return RunLoadResult.Failure(LoadStatus.Corrupt, versionError);
            }

            if (version > GameConstants.CurrentSaveVersion)
            {
                return RunLoadResult.Failure(
                    LoadStatus.UnsupportedVersion,
                    string.Format(
                        "Save version {0} was written by a newer build; this one understands {1}.",
                        version,
                        GameConstants.CurrentSaveVersion));
            }

            if (version < FirstSupportedVersion)
            {
                return RunLoadResult.Failure(
                    LoadStatus.Corrupt,
                    string.Format("Save version {0} is not a valid version.", version));
            }

            RunSaveFile file;
            try
            {
                file = JsonUtility.FromJson<RunSaveFile>(read.Text);
            }
            catch (Exception error)
            {
                return RunLoadResult.Failure(LoadStatus.Corrupt, error.Message);
            }

            if (file == null)
            {
                return RunLoadResult.Failure(
                    LoadStatus.Corrupt,
                    "The save file could not be read as a save.");
            }

            // The markers are the only thing separating a real save from a truncated one.
            // JsonUtility materialises the nested payload even when the JSON omits it, and every
            // field of a run has a legitimate default, so inspecting the run itself can never
            // reveal that it was never actually written.
            if (!file.HasValidMarker)
            {
                return RunLoadResult.Failure(
                    LoadStatus.Corrupt,
                    string.Format(
                        "Expected format marker '{0}' but found '{1}'.",
                        RunSaveFile.FormatMarker,
                        file.Format ?? "<absent>"));
            }

            RunSavePayload payload = file.Payload;

            if (payload == null || !payload.HasValidMarker)
            {
                return RunLoadResult.Failure(
                    LoadStatus.Corrupt,
                    string.Format(
                        "Expected payload marker '{0}' but found '{1}'.",
                        RunSavePayload.KindMarker,
                        payload == null ? "<absent>" : payload.Kind ?? "<absent>"));
            }

            if (payload.Run == null)
            {
                return RunLoadResult.Failure(
                    LoadStatus.Corrupt,
                    "The save file carries valid markers but no run.");
            }

            RunState runState = payload.Run;

            // Sanitization happens here so no caller can observe an unrepaired run.
            bool repaired = runState.SanitizeAfterLoad();

            return RunLoadResult.Loaded(
                repaired ? LoadStatus.SuccessAfterRepair : LoadStatus.Success,
                runState);
        }

        private static bool TryReadVersion(string json, out int version, out string error)
        {
            version = 0;

            try
            {
                SaveFileHeader header = JsonUtility.FromJson<SaveFileHeader>(json);
                if (header == null)
                {
                    error = "The save file could not be read as JSON.";
                    return false;
                }

                version = header.SaveVersion;
                error = string.Empty;
                return true;
            }
            catch (Exception parseError)
            {
                error = parseError.Message;
                return false;
            }
        }

        /// <summary>The first version ever written. Anything below it is nonsense, not history.</summary>
        private const int FirstSupportedVersion = 1;
    }
}
