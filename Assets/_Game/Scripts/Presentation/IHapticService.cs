namespace RoyalDecisions.Presentation
{
    public interface IHapticService
    {
        bool IsEnabled { get; }

        void SetEnabled(bool enabled);

        void Pulse(HapticFeedbackLevel level = HapticFeedbackLevel.Standard);
    }
}
