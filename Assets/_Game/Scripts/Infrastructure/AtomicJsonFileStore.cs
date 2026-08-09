using System;
using System.IO;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// Reads and writes text files in a way that cannot leave a half-written save behind.
    /// </summary>
    /// <remarks>
    /// The write is temp-then-replace: the target file is only ever swapped for a file that is
    /// already complete on disk. A process killed part-way through leaves the previous save intact
    /// plus an orphaned temp file, never a truncated target.
    /// </remarks>
    public sealed class AtomicJsonFileStore
    {
        private readonly IFileSystem fileSystem;

        public AtomicJsonFileStore(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public SaveResult Write(string targetPath, string tempPath, string backupPath, string contents)
        {
            try
            {
                EnsureDirectory(targetPath);

                // An orphan from a previous failed write would otherwise block this one.
                if (fileSystem.FileExists(tempPath))
                {
                    fileSystem.Delete(tempPath);
                }

                fileSystem.WriteAllText(tempPath, contents);

                if (fileSystem.FileExists(targetPath))
                {
                    // Atomic swap that also captures the previous contents as the backup.
                    fileSystem.Replace(tempPath, targetPath, backupPath);
                }
                else
                {
                    fileSystem.Move(tempPath, targetPath);
                }

                return SaveResult.Success();
            }
            catch (Exception error)
            {
                // The previous save is still whole; only the temp file needs clearing up.
                string cleanupNote = TryRemoveTemp(tempPath);
                return SaveResult.Failure(SaveStatus.WriteFailed, error.Message + cleanupNote);
            }
        }

        public TextReadResult Read(string path)
        {
            try
            {
                if (!fileSystem.FileExists(path))
                {
                    return TextReadResult.Failure(LoadStatus.NoSaveFile, "No file at " + path);
                }

                string text = fileSystem.ReadAllText(path);

                if (string.IsNullOrWhiteSpace(text))
                {
                    return TextReadResult.Failure(LoadStatus.Empty, "File is empty: " + path);
                }

                return TextReadResult.Success(text);
            }
            catch (Exception error)
            {
                return TextReadResult.Failure(LoadStatus.ReadFailed, error.Message);
            }
        }

        public bool Exists(string path)
        {
            return fileSystem.FileExists(path);
        }

        /// <summary>Deletes a file if it is there. Returns whether anything was removed.</summary>
        public bool DeleteIfPresent(string path)
        {
            if (!fileSystem.FileExists(path))
            {
                return false;
            }

            fileSystem.Delete(path);
            return true;
        }

        private void EnsureDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);

            if (string.IsNullOrEmpty(directory) || fileSystem.DirectoryExists(directory))
            {
                return;
            }

            fileSystem.CreateDirectory(directory);
        }

        /// <summary>
        /// Clears the temp file after a failed write. A failure here is reported as extra detail on
        /// the original error rather than swallowed — an undeletable temp file is worth knowing
        /// about, but it must not mask the write failure that caused it.
        /// </summary>
        private string TryRemoveTemp(string tempPath)
        {
            try
            {
                if (fileSystem.FileExists(tempPath))
                {
                    fileSystem.Delete(tempPath);
                }

                return string.Empty;
            }
            catch (Exception cleanupError)
            {
                return " (the temporary file could also not be removed: " + cleanupError.Message + ")";
            }
        }
    }
}
