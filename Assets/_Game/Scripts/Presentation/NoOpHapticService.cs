namespace RoyalDecisions.Presentation
{
    public sealed class NoOpHapticService : IHapticService
    {
        public bool IsEnabled { get; private set; }

        public void SetEnabled(bool enabled) => IsEnabled = enabled;

        public void Pulse() { }
    }
}
