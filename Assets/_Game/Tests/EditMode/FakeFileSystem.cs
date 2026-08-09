using System;
using System.Collections.Generic;
using System.IO;
using RoyalDecisions.Infrastructure;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// An in-memory <see cref="IFileSystem"/> that can be told to fail.
    /// </summary>
    /// <remarks>
    /// The failure injection is the point: a test cannot make a real disk fail mid-write on demand,
    /// and the atomic-write guarantee is only meaningful if the failure path is exercised.
    /// </remarks>
    public sealed class FakeFileSystem : IFileSystem
    {
        private readonly Dictionary<string, string> files =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly HashSet<string> directories =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>When set, every write throws.</summary>
        public bool FailAllWrites { get; set; }

        /// <summary>When set, writes to paths containing this fragment throw.</summary>
        public string FailWritesContaining { get; set; }

        /// <summary>
        /// When set, writes store their content and <em>then</em> throw — what a disk filling up
        /// part-way through looks like, and the only way to leave a temp file behind to clean up.
        /// </summary>
        public bool FailWritesAfterStoring { get; set; }

        /// <summary>When set, every read throws.</summary>
        public bool FailAllReads { get; set; }

        /// <summary>When set, deletes throw — used to exercise cleanup failure reporting.</summary>
        public bool FailAllDeletes { get; set; }

        public int WriteCount { get; private set; }

        public int ReadCount { get; private set; }

        public IReadOnlyCollection<string> Paths => files.Keys;

        public int FileCount => files.Count;

        /// <summary>Writes a file directly, bypassing failure injection. For test arrangement.</summary>
        public void Seed(string path, string contents)
        {
            files[Normalize(path)] = contents;
            RememberDirectoryOf(Normalize(path));
        }

        public string Peek(string path)
        {
            return files.TryGetValue(Normalize(path), out string contents) ? contents : null;
        }

        public bool FileExists(string path)
        {
            return files.ContainsKey(Normalize(path));
        }

        public bool DirectoryExists(string path)
        {
            return directories.Contains(Normalize(path));
        }

        public void CreateDirectory(string path)
        {
            directories.Add(Normalize(path));
        }

        public string ReadAllText(string path)
        {
            ReadCount++;

            if (FailAllReads)
            {
                throw new IOException("FakeFileSystem was told to fail every read.");
            }

            string key = Normalize(path);
            if (!files.TryGetValue(key, out string contents))
            {
                throw new FileNotFoundException("No such file in FakeFileSystem.", path);
            }

            return contents;
        }

        public void WriteAllText(string path, string contents)
        {
            WriteCount++;

            string key = Normalize(path);

            if (FailAllWrites || FailWritesAfterStoring || ShouldFailWrite(key))
            {
                if (FailWritesAfterStoring)
                {
                    files[key] = contents;
                    RememberDirectoryOf(key);
                }

                throw new IOException("FakeFileSystem was told to fail writes to " + path + ".");
            }

            files[key] = contents;
            RememberDirectoryOf(key);
        }

        public void Delete(string path)
        {
            if (FailAllDeletes)
            {
                throw new IOException("FakeFileSystem was told to fail every delete.");
            }

            files.Remove(Normalize(path));
        }

        public void Move(string sourcePath, string destinationPath)
        {
            string source = Normalize(sourcePath);
            string destination = Normalize(destinationPath);

            if (!files.TryGetValue(source, out string contents))
            {
                throw new FileNotFoundException("No such file in FakeFileSystem.", sourcePath);
            }

            if (files.ContainsKey(destination))
            {
                throw new IOException("Destination already exists: " + destinationPath);
            }

            files[destination] = contents;
            files.Remove(source);
            RememberDirectoryOf(destination);
        }

        public void Replace(string sourcePath, string destinationPath, string backupPath)
        {
            string source = Normalize(sourcePath);
            string destination = Normalize(destinationPath);

            if (!files.TryGetValue(source, out string incoming))
            {
                throw new FileNotFoundException("No such file in FakeFileSystem.", sourcePath);
            }

            if (!files.TryGetValue(destination, out string outgoing))
            {
                throw new FileNotFoundException("No file to replace.", destinationPath);
            }

            if (!string.IsNullOrEmpty(backupPath))
            {
                files[Normalize(backupPath)] = outgoing;
            }

            files[destination] = incoming;
            files.Remove(source);
        }

        private bool ShouldFailWrite(string normalizedPath)
        {
            return !string.IsNullOrEmpty(FailWritesContaining)
                && normalizedPath.IndexOf(FailWritesContaining, StringComparison.Ordinal) >= 0;
        }

        private void RememberDirectoryOf(string normalizedPath)
        {
            int lastSlash = normalizedPath.LastIndexOf('/');
            if (lastSlash > 0)
            {
                directories.Add(normalizedPath.Substring(0, lastSlash));
            }
        }

        private static string Normalize(string path)
        {
            return path == null ? string.Empty : path.Replace('\\', '/');
        }
    }
}
