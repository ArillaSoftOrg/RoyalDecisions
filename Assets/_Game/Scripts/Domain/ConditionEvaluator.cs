using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Decides whether a card may be drawn for the current run.
    /// </summary>
    /// <remarks>
    /// Stateless, so a single instance is safe to share. Forced cards deliberately bypass this
    /// check — see <see cref="CardDeckService"/>.
    /// </remarks>
    public sealed class ConditionEvaluator
    {
        public bool IsEligible(CardDefinition card, RunState runState)
        {
            if (card == null || runState == null || string.IsNullOrEmpty(card.Id))
            {
                return false;
            }

            if (card.OncePerRun && runState.HasShownCard(card.Id))
            {
                return false;
            }

            if (runState.IsOnCooldown(card.Id))
            {
                return false;
            }

            // Forced-next chains bypass this method entirely (see CardDeckService), so a
            // forced-chain-only card is still reachable exactly the way its author intended; this
            // only removes it from the normal weighted-draw pool.
            if (card.ForcedChainOnly)
            {
                return false;
            }

            return AreConditionsMet(card.Conditions, runState);
        }

        /// <summary>True when a choice's own availability conditions hold (or it has none).</summary>
        public bool IsChoiceAvailable(ChoiceDefinition choice, RunState runState)
        {
            if (choice == null)
            {
                return false;
            }

            return !choice.HasAvailabilityCondition || AreConditionsMet(choice.Availability, runState);
        }

        public bool AreConditionsMet(CardConditions conditions, RunState runState)
        {
            if (runState == null)
            {
                return false;
            }

            // No authored conditions means the card places no demands on the run.
            if (conditions == null)
            {
                return true;
            }

            IReadOnlyList<string> required = conditions.RequiredFlags;
            for (int i = 0; i < required.Count; i++)
            {
                if (!runState.HasFlag(required[i]))
                {
                    return false;
                }
            }

            IReadOnlyList<string> forbidden = conditions.ForbiddenFlags;
            for (int i = 0; i < forbidden.Count; i++)
            {
                if (runState.HasFlag(forbidden[i]))
                {
                    return false;
                }
            }

            IReadOnlyList<StatRange> ranges = conditions.StatRanges;
            StatValues stats = runState.Stats;
            for (int i = 0; i < ranges.Count; i++)
            {
                StatRange range = ranges[i];

                // An empty row left behind in the Inspector must not silently block the card.
                if (range == null)
                {
                    continue;
                }

                if (!range.Contains(stats[range.Stat]))
                {
                    return false;
                }
            }

            IReadOnlyList<NumericCondition> numericConditions = conditions.NumericConditions;
            for (int i = 0; i < numericConditions.Count; i++)
            {
                NumericCondition condition = numericConditions[i];
                if (condition == null)
                {
                    continue;
                }

                if (!EvaluateNumeric(condition, runState))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads whichever value <paramref name="condition"/> names and compares it to its
        /// threshold. Shared by card eligibility above and by <see cref="ChoiceResolver"/>'s
        /// conditional choice effects, so both use exactly one reading of "what does this number
        /// mean right now".
        /// </summary>
        public bool EvaluateNumeric(NumericCondition condition, RunState runState)
        {
            if (condition == null || runState == null)
            {
                return false;
            }

            if (condition.Comparison == NumericComparison.Always)
            {
                return true;
            }

            int actual = condition.Source switch
            {
                NumericSource.Stat => runState.Stats[condition.Stat],
                NumericSource.Counter => runState.GetCounter(condition.CounterId),
                NumericSource.LeaderHealth => runState.LeaderHealth,
                _ => 0
            };

            return condition.Comparison switch
            {
                NumericComparison.LessThan => actual < condition.Threshold,
                NumericComparison.LessOrEqual => actual <= condition.Threshold,
                NumericComparison.GreaterThan => actual > condition.Threshold,
                NumericComparison.GreaterOrEqual => actual >= condition.Threshold,
                _ => false
            };
        }
    }
}
