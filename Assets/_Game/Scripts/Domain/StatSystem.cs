using System;
using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The only path through which statistics change, and the source of change notifications.
    /// </summary>
    /// <remarks>
    /// Wraps the run rather than holding its own copy of the stats. A second copy alongside
    /// <see cref="RunState"/> would be free to drift out of sync, and the save file would then
    /// disagree with what the player sees.
    /// </remarks>
    public sealed class StatSystem
    {
        /// <summary>Iterated explicitly to avoid the array Enum.GetValues allocates per call.</summary>
        private static readonly StatType[] AllStats =
        {
            StatType.Authority,
            StatType.People,
            StatType.Security,
            StatType.Wealth
        };

        private readonly RunState runState;

        public StatSystem(RunState runState)
        {
            this.runState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        /// <summary>Raised once for each statistic whose value actually moved.</summary>
        public event Action<StatChange> StatChanged;

        /// <summary>Raised once per change, for consumers that redraw everything at once.</summary>
        public event Action<StatValues> StatsChanged;

        public StatValues Current => runState.Stats;

        public int Get(StatType stat)
        {
            return runState.Stats[stat];
        }

        /// <summary>Applies a choice's deltas. Clamping is handled by <see cref="StatValues"/>.</summary>
        public void Apply(StatDeltas deltas)
        {
            Set(runState.Stats.WithDelta(deltas));
        }

        public void Set(StatValues values)
        {
            StatValues previous = runState.Stats;
            runState.SetStats(values);

            // Events are raised after the write so a handler reading Current sees the new value.
            for (int i = 0; i < AllStats.Length; i++)
            {
                StatType stat = AllStats[i];
                int before = previous[stat];
                int after = values[stat];

                // A delta that clamps to nothing must not fire, or the HUD would animate a
                // statistic that never moved.
                if (before != after)
                {
                    StatChanged?.Invoke(new StatChange(stat, before, after));
                }
            }

            StatsChanged?.Invoke(values);
        }

        /// <summary>True when the statistic sits on the boundary that ends a run.</summary>
        public bool IsAtBoundary(StatType stat, StatBoundary boundary)
        {
            return boundary == StatBoundary.Min
                ? runState.Stats.IsAtMin(stat)
                : runState.Stats.IsAtMax(stat);
        }
    }
}
