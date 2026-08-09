namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// Outcome of reading a save.
    /// </summary>
    /// <remarks>
    /// Loading never throws — CLAUDE.md §14 requires that a corrupt save cannot block startup — so
    /// every way a file can be wrong is represented here instead.
    /// </remarks>
    public enum LoadStatus
    {
        /// <summary>Loaded exactly as written.</summary>
        Success = 0,

        /// <summary>Loaded, but sanitization had to repair something.</summary>
        SuccessAfterRepair = 1,

        /// <summary>The main file was unusable and the backup was good.</summary>
        RecoveredFromBackup = 2,

        /// <summary>Nothing has been saved yet. A normal first launch, not a failure.</summary>
        NoSaveFile = 3,

        /// <summary>The file exists but holds nothing but whitespace.</summary>
        Empty = 4,

        /// <summary>The file could not be parsed, or claims a version below the first.</summary>
        Corrupt = 5,

        /// <summary>Written by a newer build. The file is left untouched.</summary>
        UnsupportedVersion = 6,

        /// <summary>The file system threw while reading.</summary>
        ReadFailed = 7
    }
}
