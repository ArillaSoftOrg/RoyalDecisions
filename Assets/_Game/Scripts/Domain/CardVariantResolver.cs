using System;
using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Resolves a card and the current run into the presentation and effective choices that should
    /// actually be shown and applied.
    /// </summary>
    /// <remarks>
    /// Pure: it reads <see cref="RunState"/> and the card's authored data and writes nothing.
    /// Precedence is simple and deterministic — <see cref="CardDefinition.Variants"/> is an ordered
    /// list, and the first variant whose conditions hold wins outright; later variants are not
    /// merged with it. A card with no matching variant resolves to its own base fields, so content
    /// that never uses variants behaves exactly as it did before this type existed.
    /// </remarks>
    public sealed class CardVariantResolver
    {
        private readonly ConditionEvaluator conditionEvaluator;

        public CardVariantResolver(ConditionEvaluator conditionEvaluator)
        {
            this.conditionEvaluator = conditionEvaluator
                ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        }

        public ResolvedCard Resolve(CardDefinition card, RunState runState)
        {
            if (card == null)
            {
                return ResolvedCard.Empty;
            }

            CardVariant variant = SelectVariant(card, runState);

            string speaker = variant != null && variant.HasSpeakerOverride ? variant.Speaker : card.Speaker;
            string bodyText = variant != null && variant.HasBodyTextOverride ? variant.BodyText : card.BodyText;
            ChoiceDefinition left = variant?.LeftChoice ?? card.LeftChoice;
            ChoiceDefinition right = variant?.RightChoice ?? card.RightChoice;

            bool leftAvailable = conditionEvaluator.IsChoiceAvailable(left, runState);
            bool rightAvailable = conditionEvaluator.IsChoiceAvailable(right, runState);

            return new ResolvedCard(card, speaker, bodyText, left, leftAvailable, right, rightAvailable);
        }

        /// <summary>The first variant in authoring order whose conditions the run satisfies, or null.</summary>
        private CardVariant SelectVariant(CardDefinition card, RunState runState)
        {
            IReadOnlyList<CardVariant> variants = card.Variants;

            for (int i = 0; i < variants.Count; i++)
            {
                CardVariant variant = variants[i];
                if (variant != null && conditionEvaluator.AreConditionsMet(variant.Conditions, runState))
                {
                    return variant;
                }
            }

            return null;
        }
    }
}
