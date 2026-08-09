namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Why a cue did or did not play.
    /// </summary>
    /// <remarks>
    /// Audio is optional throughout the MVP, so "nothing happened" is a normal outcome rather than
    /// an error. Returning which kind of nothing lets a caller — and a test — tell an unwired
    /// AudioSource apart from a cue that simply has no sound yet.
    /// </remarks>
    public enum AudioPlayResult
    {
        Played = 0,

        /// <summary>The choice carries no audio event ID. Expected for most cards.</summary>
        NoCueId = 1,

        /// <summary>No cue library is assigned.</summary>
        NoLibrary = 2,

        /// <summary>The library has no entry for this ID.</summary>
        UnknownCue = 3,

        /// <summary>The entry exists but its clip slot is empty.</summary>
        NullClip = 4,

        /// <summary>No AudioSource is assigned to play through.</summary>
        NoAudioSource = 5,

        /// <summary>Everything resolved, but audio is muted.</summary>
        Muted = 6
    }
}
