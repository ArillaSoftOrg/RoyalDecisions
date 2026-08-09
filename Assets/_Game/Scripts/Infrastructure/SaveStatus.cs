namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// Outcome of writing a save.
    /// </summary>
    public enum SaveStatus
    {
        Success = 0,

        /// <summary>Nothing was written because the supplied data was unusable.</summary>
        InvalidData = 1,

        /// <summary>The data could not be turned into JSON.</summary>
        SerializationFailed = 2,

        /// <summary>The file system refused the write. Any previous save is left intact.</summary>
        WriteFailed = 3
    }
}
