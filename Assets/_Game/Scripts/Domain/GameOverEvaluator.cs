using System;
using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Detects whether any statistic has hit a boundary and picks the ending that covers it.
    /// </summary>
    /// <remarks>
    /// Game-over is decided by statistics alone; resolving the ending asset is a separate lookup
    /// that may legitimately find nothing. Keeping the two apart means missing content cannot make
    /// a doomed run continue.
    /// </remarks>
    public sealed class GameOverEvaluator
    {
        /// <summary>Iteration order, and therefore the tie-break, when several stats hit at once.</summary>
        private static readonly StatType[] EvaluationOrder =
        {
            StatType.Authority,
            StatType.People,
            StatType.Security,
            StatType.Wealth
        };

        private static readonly StatBoundary[] BoundaryOrder =
        {
            StatBoundary.Min,
            StatBoundary.Max
        };

        public GameOverResult Evaluate(RunState runState, IReadOnlyList<EndingDefinition> endings)
        {
            if (runState == null)
            {
                return GameOverResult.NotOver();
            }

            StatValues stats = runState.Stats;

            bool found = false;
            StatType bestStat = default;
            StatBoundary bestBoundary = default;
            EndingDefinition bestEnding = null;
            int bestPriority = 0;

            for (int i = 0; i < EvaluationOrder.Length; i++)
            {
                StatType stat = EvaluationOrder[i];

                for (int b = 0; b < BoundaryOrder.Length; b++)
                {
                    StatBoundary boundary = BoundaryOrder[b];

                    bool hit = boundary == StatBoundary.Min
                        ? stats.IsAtMin(stat)
                        : stats.IsAtMax(stat);

                    if (!hit)
                    {
                        continue;
                    }

                    EndingDefinition ending = FindEnding(endings, stat, boundary);

                    // A boundary with no authored ending ranks below every covered one, so a
                    // playable ending always wins over a content gap.
                    int priority = ending != null ? ending.Priority : int.MinValue;

                    // Strictly greater: the first hit in iteration order keeps the tie, which is
                    // what makes simultaneous boundary hits resolve deterministically.
                    if (!found || priority > bestPriority)
                    {
                        found = true;
                        bestPriority = priority;
                        bestStat = stat;
                        bestBoundary = boundary;
                        bestEnding = ending;
                    }
                }
            }

            return found
                ? GameOverResult.Over(bestStat, bestBoundary, bestEnding)
                : GameOverResult.NotOver();
        }

        /// <summary>
        /// Picks the ending for a boundary. Duplicates are a content error, but the highest
        /// priority then the ordinal-lowest ID is used so the choice never depends on list order.
        /// </summary>
        private static EndingDefinition FindEnding(
            IReadOnlyList<EndingDefinition> endings,
            StatType stat,
            StatBoundary boundary)
        {
            if (endings == null)
            {
                return null;
            }

            EndingDefinition best = null;

            for (int i = 0; i < endings.Count; i++)
            {
                EndingDefinition candidate = endings[i];

                if (candidate == null ||
                    candidate.TriggerStat != stat ||
                    candidate.Boundary != boundary)
                {
                    continue;
                }

                if (best == null || IsBetterEnding(candidate, best))
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static bool IsBetterEnding(EndingDefinition candidate, EndingDefinition current)
        {
            if (candidate.Priority != current.Priority)
            {
                return candidate.Priority > current.Priority;
            }

            return StringComparer.Ordinal.Compare(candidate.Id, current.Id) < 0;
        }
    }
}
