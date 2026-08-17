namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// The duration/amplitude a <see cref="HapticFeedbackLevel"/> maps to on hardware that supports
    /// amplitude control. Pulled out as plain data (rather than inlined in <see cref="UnityHapticService"/>)
    /// so the level-to-feel mapping is unit-testable without any platform code.
    /// </summary>
    public readonly struct HapticProfile
    {
        public HapticProfile(long durationMilliseconds, int amplitude)
        {
            DurationMilliseconds = durationMilliseconds;
            Amplitude = amplitude;
        }

        public long DurationMilliseconds { get; }

        /// <summary>Android VibrationEffect amplitude, 1-255.</summary>
        public int Amplitude { get; }

        public static HapticProfile For(HapticFeedbackLevel level)
        {
            switch (level)
            {
                case HapticFeedbackLevel.Light:
                    return new HapticProfile(12L, 40);
                case HapticFeedbackLevel.Critical:
                    return new HapticProfile(35L, 220);
                case HapticFeedbackLevel.Standard:
                default:
                    return new HapticProfile(20L, 110);
            }
        }
    }
}
