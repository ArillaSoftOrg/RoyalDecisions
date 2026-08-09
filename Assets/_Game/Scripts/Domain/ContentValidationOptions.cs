namespace RoyalDecisions.Domain
{
    /// <summary>Non-gameplay thresholds used by content-authoring diagnostics.</summary>
    public sealed class ContentValidationOptions
    {
        public int MaximumSingleStatDelta { get; set; } = 25;

        public int MaximumTotalAbsoluteDelta { get; set; } = 40;

        public int MaximumSpeakerLength { get; set; } = 40;

        public int MaximumBodyLength { get; set; } = 180;

        public int MaximumPreviewLength { get; set; } = 32;

        public bool IncludeInformation { get; set; } = true;
    }
}
