using System.IO;
using System.Text;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// The real file system, backed by <see cref="System.IO"/>.
    /// </summary>
    public sealed class SystemFileSystem : IFileSystem
    {
        /// <summary>
        /// UTF-8 with no byte order mark. A BOM would sit in front of the opening brace and make
        /// the file harder to inspect or diff, for no benefit.
        /// </summary>
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path, Utf8NoBom);
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents, Utf8NoBom);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public void Move(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        public void Replace(string sourcePath, string destinationPath, string backupPath)
        {
            // File.Replace is the atomic swap: the destination is never observed half-written, and
            // the previous contents land at the backup path in the same operation.
            File.Replace(sourcePath, destinationPath, backupPath);
        }
    }
}
