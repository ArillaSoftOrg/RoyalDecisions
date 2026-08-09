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

            return AreConditionsMet(card.Conditions, runState);
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

            return true;
        }
    }
}
