namespace RoyalDecisions.Application
{
    /// <summary>
    /// Plays a named cue, or does nothing.
    /// </summary>
    /// <remarks>
    /// Audio is optional throughout the MVP, so this returns nothing and never throws — a missing
    /// clip must never interrupt a decision. The application layer defines its own port rather than
    /// using Presentation's audio interface, because the dependency arrow points the other way.
    /// </remarks>
    public interface IAudioPlayer
    {
        void Play(string audioEventId);
    }
}
