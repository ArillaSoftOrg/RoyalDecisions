using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The card a draw produced, plus why it was produced.
    /// </summary>
    public readonly struct CardSelectionResult
    {
        private CardSelectionResult(CardSelectionStatus status, CardDefinition card)
        {
            Status = status;
            Card = card;
        }

        public static CardSelectionResult Selected(CardDefinition card)
        {
            return new CardSelectionResult(CardSelectionStatus.Selected, card);
        }

        public static CardSelectionResult Forced(CardDefinition card)
        {
            return new CardSelectionResult(CardSelectionStatus.Forced, card);
        }

        /// <summary>
        /// A broken chain. <paramref name="fallbackCard"/> is whatever weighted selection produced
        /// instead, and may be null when nothing was eligible either.
        /// </summary>
        public static CardSelectionResult ForcedMissing(CardDefinition fallbackCard)
        {
            return new CardSelectionResult(CardSelectionStatus.ForcedCardMissing, fallbackCard);
        }

        public static CardSelectionResult NoEligibleCard()
        {
            return new CardSelectionResult(CardSelectionStatus.NoEligibleCard, null);
        }

        public static CardSelectionResult EmptyCatalogue()
        {
            return new CardSelectionResult(CardSelectionStatus.EmptyCatalogue, null);
        }

        public CardSelectionStatus Status { get; }

        public CardDefinition Card { get; }

        public bool HasCard => Card != null;
    }
}
