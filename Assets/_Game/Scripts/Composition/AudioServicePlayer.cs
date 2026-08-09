using RoyalDecisions.Application;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Adapts the presentation audio service to the application's audio port.
    /// </summary>
    /// <remarks>
    /// A null service is a supported configuration: the project ships with no clips authored, and a
    /// missing cue must never interrupt a decision.
    /// </remarks>
    public sealed class AudioServicePlayer : IAudioPlayer
    {
        private readonly IAudioService audioService;

        public AudioServicePlayer(IAudioService audioService)
        {
            this.audioService = audioService;
        }

        public void Play(string audioEventId)
        {
            if (audioService == null)
            {
                return;
            }

            // The result is deliberately discarded: every "no sound" outcome is normal here.
            audioService.Play(audioEventId);
        }
    }
}
