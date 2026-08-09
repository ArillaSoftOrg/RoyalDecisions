namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// The only route to the file system in the whole game.
    /// </summary>
    /// <remarks>
    /// Everything that touches a disk goes through this interface so the save system can be tested
    /// against an in-memory double — including the failure paths, which are otherwise impossible to
    /// reach reliably: a test cannot make a real disk fail mid-write on demand.
    /// </remarks>
    public interface IFileSystem
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        string ReadAllText(string path);

        void WriteAllText(string path, string contents);

        void Delete(string path);

        /// <summary>
        /// Moves <paramref name="sourcePath"/> onto <paramref name="destinationPath"/>, which must
        /// not already exist.
        /// </summary>
        void Move(string sourcePath, string destinationPath);

        /// <summary>
        /// Atomically replaces an existing <paramref name="destinationPath"/> with
        /// <paramref name="sourcePath"/>, preserving the previous contents at
        /// <paramref name="backupPath"/>.
        /// </summary>
        void Replace(string sourcePath, string destinationPath, string backupPath);
    }
}
