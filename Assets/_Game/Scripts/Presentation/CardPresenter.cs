using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Turns a card definition into the values a view renders.
    /// </summary>
    /// <remarks>
    /// Pure and free of Unity lifecycle, so the decisions a view makes can be tested without
    /// constructing a view. It reads content and nothing else — no run state, no services.
    /// </remarks>
    public static class CardPresenter
    {
        /// <summary>Base rendering, ignoring any card variant or choice-availability override.</summary>
        public static CardPresentation Create(CardDefinition card)
        {
            if (card == null)
            {
                return CardPresentation.Empty;
            }

            // Content getters are already null-safe, but a choice reference itself can be absent on
            // a malformed asset — Phase 3 validation reports that as an error rather than a crash,
            // and the view must survive it either way.
            string left = card.LeftChoice != null ? card.LeftChoice.PreviewText : string.Empty;
            string right = card.RightChoice != null ? card.RightChoice.PreviewText : string.Empty;

            return new CardPresentation(
                card.Speaker,
                card.BodyText,
                left,
                right,
                card.Portrait);
        }

        /// <summary>
        /// Renders <paramref name="resolved"/> — the effective speaker/body/choices after variant
        /// and availability resolution — using <paramref name="card"/> only for its portrait, which
        /// a variant never overrides. An unavailable side renders with no preview text, so the
        /// player gets no directional cue toward a choice that cannot be confirmed.
        /// </summary>
        public static CardPresentation Create(CardDefinition card, ResolvedCard resolved)
        {
            if (card == null || !resolved.HasCard)
            {
                return CardPresentation.Empty;
            }

            string left = resolved.LeftAvailable && resolved.LeftChoice != null
                ? resolved.LeftChoice.PreviewText
                : string.Empty;
            string right = resolved.RightAvailable && resolved.RightChoice != null
                ? resolved.RightChoice.PreviewText
                : string.Empty;

            return new CardPresentation(
                resolved.Speaker,
                resolved.BodyText,
                left,
                right,
                card.Portrait);
        }
    }
}
