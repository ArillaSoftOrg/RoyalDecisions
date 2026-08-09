namespace RoyalDecisions.Application
{
    /// <summary>
    /// How a load attempt ended, in the application's own vocabulary.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from Infrastructure's <c>LoadStatus</c> so the application layer never
    /// references a concrete persistence type. A composition-root adapter maps between them.
    /// </remarks>
    public enum RunLoadStatus
    {
        Success = 0,

        /// <summary>Loaded, but sanitization had to repair something.</summary>
        SuccessAfterRepair = 1,

        /// <summary>The main file was unusable and a backup answered instead.</summary>
        RecoveredFromBackup = 2,

        /// <summary>Nothing has been saved. Not an error.</summary>
        NoSave = 3,

        /// <summary>The file exists but cannot be understood. It is never deleted.</summary>
        Corrupt = 4,

        /// <summary>Written by a newer build. The file is left untouched.</summary>
        UnsupportedVersion = 5,

        /// <summary>The file system refused the read.</summary>
        ReadFailed = 6
    }
}
