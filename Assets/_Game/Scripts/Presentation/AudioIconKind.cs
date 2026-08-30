namespace RoyalDecisions.Presentation
{
    /// <summary>Which silhouette <see cref="ProceduralAudioIconGraphic"/> draws.</summary>
    public enum AudioIconKind
    {
        /// <summary>Speaker cone with radiating waves — master volume.</summary>
        Speaker = 0,

        /// <summary>Single musical note — music volume.</summary>
        Note = 1,

        /// <summary>Equaliser bars — sound effects volume.</summary>
        Effect = 2
    }
}
