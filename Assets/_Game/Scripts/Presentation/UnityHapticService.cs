using UnityEngine;

namespace RoyalDecisions.Presentation
{
    public sealed class UnityHapticService : IHapticService
    {
        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled) => IsEnabled = enabled;

        public void Pulse()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsEnabled)
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}
