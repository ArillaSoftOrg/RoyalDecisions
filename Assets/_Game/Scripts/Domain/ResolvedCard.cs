using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// A card's presentation and effective choices after resolving any matching
    /// <see cref="CardVariant"/> and choice availability against the current run.
    /// </summary>
    /// <remarks>
    /// The output of <see cref="CardVariantResolver"/>. Presentation renders from this rather than
    /// from <see cref="CardDefinition"/> directly, and <see cref="ChoiceResolver"/> applies whichever
    /// <see cref="ChoiceDefinition"/> this struct names — not necessarily the base card's — so a
    /// variant's overridden deltas/flags/forced-next take effect exactly like the base card's would.
    /// </remarks>
    public readonly struct ResolvedCard
    {
        public ResolvedCard(
            CardDefinition sourceCard,
            string speaker,
            string bodyText,
            ChoiceDefinition leftChoice,
            bool leftAvailable,
            ChoiceDefinition rightChoice,
            bool rightAvailable)
        {
            SourceCard = sourceCard;
            Speaker = speaker ?? string.Empty;
            BodyText = bodyText ?? string.Empty;
            LeftChoice = leftChoice;
            LeftAvailable = leftAvailable;
            RightChoice = rightChoice;
            RightAvailable = rightAvailable;
        }

        public static ResolvedCard Empty => default;

        public CardDefinition SourceCard { get; }

        public string Speaker { get; }

        public string BodyText { get; }

        public ChoiceDefinition LeftChoice { get; }

        public bool LeftAvailable { get; }

        public ChoiceDefinition RightChoice { get; }

        public bool RightAvailable { get; }

        public bool HasCard => SourceCard != null;

        public ChoiceDefinition Choice(ChoiceSide side)
        {
            return side == ChoiceSide.Left ? LeftChoice : RightChoice;
        }

        public bool IsAvailable(ChoiceSide side)
        {
            return side == ChoiceSide.Left ? LeftAvailable : RightAvailable;
        }
    }
}
