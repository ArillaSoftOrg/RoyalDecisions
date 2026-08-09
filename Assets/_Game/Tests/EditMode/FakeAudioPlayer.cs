using System.Collections.Generic;
using RoyalDecisions.Application;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>Records every cue the session asked for.</summary>
    public sealed class FakeAudioPlayer : IAudioPlayer
    {
        public List<string> Played { get; } = new List<string>();

        public void Play(string audioEventId)
        {
            Played.Add(audioEventId);
        }
    }
}
