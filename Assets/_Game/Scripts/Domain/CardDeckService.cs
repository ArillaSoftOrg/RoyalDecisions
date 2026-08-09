using System;
using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Filters the catalogue to the cards a run may currently draw, then picks one by weight.
    /// </summary>
    /// <remarks>
    /// Stateless and free of side effects: it writes to neither the run nor the caller's
    /// catalogue. Selection is a pure query, which is what makes replaying a seed a meaningful
    /// test rather than a coincidence. Consuming the forced card and recording the presented card
    /// belong to the game flow, not here.
    /// </remarks>
    public sealed class CardDeckService
    {
        /// <summary>
        /// Ordinal, never culture-sensitive. A culture-aware comparison would order IDs differently
        /// depending on device locale, so the same seed could draw a different card on a Turkish
        /// handset than on an English one.
        /// </summary>
        private static readonly Comparison<CardDefinition> ByOrdinalId =
            (left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id);

        private readonly ConditionEvaluator conditionEvaluator;

        public CardDeckService(ConditionEvaluator conditionEvaluator)
        {
            this.conditionEvaluator = conditionEvaluator
                ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        }

        public CardSelectionResult SelectCard(
            RunState runState,
            IReadOnlyList<CardDefinition> catalogue,
            IRandomSource random)
        {
            if (runState == null)
            {
                throw new ArgumentNullException(nameof(runState));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (catalogue == null || catalogue.Count == 0)
            {
                return CardSelectionResult.EmptyCatalogue();
            }

            bool forcedWasMissing = false;

            if (runState.HasForcedNextCard)
            {
                CardDefinition forced = FindById(catalogue, runState.ForcedNextCardId);
                if (forced != null)
                {
                    // Forced cards bypass eligibility so an authored chain cannot silently break
                    // when stats drift, and the random source is left untouched so whether a turn
                    // was forced has no effect on the stream.
                    return CardSelectionResult.Forced(forced);
                }

                forcedWasMissing = true;
            }

            List<CardDefinition> eligible = CollectEligible(runState, catalogue);
            if (eligible.Count == 0)
            {
                return forcedWasMissing
                    ? CardSelectionResult.ForcedMissing(null)
                    : CardSelectionResult.NoEligibleCard();
            }

            // Weight bands are keyed on ordinal ID order, never on catalogue order, so reordering
            // assets or regenerating content in a different sequence cannot change which card a
            // given roll lands on. This holds only for unique IDs: List.Sort is unstable, so
            // duplicates would sit in an undefined relative position. Duplicate and missing IDs
            // are a Phase 3 content-validation error.
            eligible.Sort(ByOrdinalId);

            CardDefinition picked = PickWeighted(eligible, random);

            return forcedWasMissing
                ? CardSelectionResult.ForcedMissing(picked)
                : CardSelectionResult.Selected(picked);
        }

        /// <summary>
        /// Copies the eligible cards into a local list. The copy is what guarantees the caller's
        /// catalogue is never reordered by the sort that follows.
        /// </summary>
        private List<CardDefinition> CollectEligible(
            RunState runState,
            IReadOnlyList<CardDefinition> catalogue)
        {
            List<CardDefinition> eligible = new List<CardDefinition>(catalogue.Count);

            for (int i = 0; i < catalogue.Count; i++)
            {
                CardDefinition card = catalogue[i];
                if (conditionEvaluator.IsEligible(card, runState))
                {
                    eligible.Add(card);
                }
            }

            return eligible;
        }

        private static CardDefinition PickWeighted(
            List<CardDefinition> eligible,
            IRandomSource random)
        {
            int total = 0;
            for (int i = 0; i < eligible.Count; i++)
            {
                total += eligible[i].SelectionWeight;
            }

            int roll = random.NextInt(total);

            int cumulative = 0;
            for (int i = 0; i < eligible.Count; i++)
            {
                cumulative += eligible[i].SelectionWeight;
                if (roll < cumulative)
                {
                    return eligible[i];
                }
            }

            // Unreachable while every weight is at least 1 and roll < total. Returning the last
            // card rather than null keeps a pathological source from handing the caller a null.
            return eligible[eligible.Count - 1];
        }

        private static CardDefinition FindById(IReadOnlyList<CardDefinition> catalogue, string id)
        {
            for (int i = 0; i < catalogue.Count; i++)
            {
                CardDefinition card = catalogue[i];
                if (card != null && string.Equals(card.Id, id, StringComparison.Ordinal))
                {
                    return card;
                }
            }

            return null;
        }
    }
}
