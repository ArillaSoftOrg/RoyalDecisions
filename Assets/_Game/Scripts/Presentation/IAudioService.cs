namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Plays a named audio cue, or explains why it did not.
    /// </summary>
    /// <remarks>
    /// Lives in Presentation rather than Domain: Domain must never depend on Presentation, and
    /// nothing in Domain calls audio. Phase 7 introduces the layer that triggers cues and can take
    /// this interface from here.
    ///
    /// No implementation throws. Missing IDs, missing sources and empty clips are all normal.
    /// </remarks>
    public interface IAudioService
    {
        AudioPlayResult Play(string audioEventId);

        AudioPlayResult PlayMusic(string audioEventId, bool loop = true);

        void StopMusic();

        void SetVolume(float volume);

        void SetMuted(bool muted);

        void SetSfxVolume(float volume);

        void SetMusicVolume(float volume);

        /// <summary>
        /// Multiplies both SFX and music before either reaches the audio system:
        /// <c>effectiveVolume = MasterVolume * (Volume | MusicVolume)</c>.
        /// </summary>
        void SetMasterVolume(float volume);

        void SetMasterMuted(bool muted);

        float Volume { get; }

        bool IsMuted { get; }

        float MusicVolume { get; }

        float MasterVolume { get; }
    }
}
