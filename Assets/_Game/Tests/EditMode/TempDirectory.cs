using System;
using System.IO;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// A throwaway directory under the OS temp path, removed on dispose.
    /// </summary>
    /// <remarks>
    /// Tests that need a genuine disk — to prove the real atomic write, not just the fake — use this
    /// rather than <c>Application.persistentDataPath</c>, which belongs to the Editor installation
    /// and must never be written to by a test run.
    /// </remarks>
    public sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "RoyalDecisionsTests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (!Directory.Exists(Path))
            {
                return;
            }

            Directory.Delete(Path, true);
        }
    }
}
