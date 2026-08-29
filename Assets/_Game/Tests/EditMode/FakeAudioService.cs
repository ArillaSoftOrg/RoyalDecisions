using System.Collections.Generic;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Records every audio request instead of touching real audio hardware. Unlike the real
    /// <see cref="AudioService"/>, this never suppresses a request just because <see cref="IsMuted"/>
    /// is true — muting/volume gating is <see cref="AudioService"/>'s own responsibility (see
    /// AudioServiceTests), so this fake exists purely to answer "what did the caller ask for", and
    /// separately "what settings did the caller apply".
    /// </summary>
    public sealed class FakeAudioService : IAudioService
    {
        public List<string> PlayedCues { get; } = new List<string>();

        public List<string> MusicRequests { get; } = new List<string>();

        public int StopMusicCount { get; private set; }

        public int StopSfxCount { get; private set; }

        public float Volume { get; private set; } = 1f;

        public bool IsMuted { get; private set; }

        public float MusicVolume { get; private set; } = 1f;

        public float MasterVolume { get; private set; } = 1f;

        public AudioPlayResult Play(string audioEventId)
        {
            if (string.IsNullOrEmpty(audioEventId))
            {
                return AudioPlayResult.NoCueId;
            }

            PlayedCues.Add(audioEventId);
            return AudioPlayResult.Played;
        }

        public AudioPlayResult PlayMusic(string audioEventId, bool loop = true)
        {
            if (string.IsNullOrEmpty(audioEventId))
            {
                return AudioPlayResult.NoCueId;
            }

            MusicRequests.Add(audioEventId);
            return AudioPlayResult.Played;
        }

        public void StopMusic()
        {
            StopMusicCount++;
        }

        public void StopSfx()
        {
            StopSfxCount++;
        }

        public void SetVolume(float volume)
        {
            Volume = volume;
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
        }

        public void SetSfxVolume(float volume)
        {
            Volume = volume;
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = volume;
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
        }

        public void SetMasterMuted(bool muted)
        {
            IsMuted = muted;
        }
    }
}
