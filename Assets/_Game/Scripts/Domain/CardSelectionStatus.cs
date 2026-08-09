namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Outcome of asking <see cref="CardDeckService"/> for the next card.
    /// </summary>
    public enum CardSelectionStatus
    {
        /// <summary>A card was drawn by weighted selection.</summary>
        Selected = 0,

        /// <summary>A forced chain card was returned without consulting the random source.</summary>
        Forced = 1,

        /// <summary>The catalogue holds cards, but none is currently eligible.</summary>
        NoEligibleCard = 2,

        /// <summary>The catalogue was null or empty.</summary>
        EmptyCatalogue = 3,

        /// <summary>
        /// A forced card ID matched nothing in the catalogue — a broken chain. Weighted selection
        /// ran instead, so a card may still be present; check the card rather than assuming none.
        /// </summary>
        ForcedCardMissing = 4
    }
}
