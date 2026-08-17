using System.Collections.Generic;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>Records every Pulse() call instead of touching real hardware.</summary>
    public sealed class FakeHapticService : IHapticService
    {
        public List<HapticFeedbackLevel> Pulses { get; } = new List<HapticFeedbackLevel>();

        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled) => IsEnabled = enabled;

        public void Pulse(HapticFeedbackLevel level = HapticFeedbackLevel.Standard)
        {
            Pulses.Add(level);
        }
    }
}
