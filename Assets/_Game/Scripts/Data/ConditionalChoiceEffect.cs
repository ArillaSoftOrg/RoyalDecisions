using System;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// An effect that branches on a <see cref="NumericCondition"/> at resolution time, instead of
    /// applying a fixed <see cref="StatDeltas"/> unconditionally.
    /// </summary>
    /// <remarks>
    /// Covers two story-content shapes with one mechanism:
    /// <list type="bullet">
    /// <item>"Leader risk" choices, where the same choice is safe most of the time but fatal when
    /// leader health is already critical (<see cref="LeaderHealthBounds.CriticalThreshold"/>).</item>
    /// <item>"Destructive" choices, where driving a statistic to its floor does not end the run but
    /// instead ends the current leader's reign: the statistic is reset rather than left at the
    /// boundary, leader health resets for the successor, and the reign counter advances. See
    /// <c>ChoiceResolver</c> and <c>GameConstants.ReignSuccessionResetStatValue</c>.</item>
    /// </list>
    /// A choice that carries this effect leaves its own <see cref="ChoiceDefinition.Deltas"/> empty;
    /// the whole outcome comes from whichever branch below actually fires.
    /// </remarks>
    [Serializable]
    public sealed class ConditionalChoiceEffect
    {
        [SerializeField] private NumericCondition condition = NumericCondition.Always();
        [SerializeField] private StatDeltas deltasWhenTrue;
        [SerializeField] private StatDeltas deltasWhenFalse;
        [SerializeField] private int leaderHealthDeltaWhenTrue;
        [SerializeField] private int leaderHealthDeltaWhenFalse;
        [SerializeField] private bool triggersSuccessionWhenTrue;
        [SerializeField] private bool hasSuccessionResetStat;
        [SerializeField] private StatType successionResetStat;

        /// <param name="successionResetStat">
        /// Which statistic to reset on succession, or <see langword="null"/> when this effect's
        /// succession is triggered by leader health alone (see <see cref="LeaderHealthBounds"/>)
        /// and no core statistic needs resetting. Deliberately a nullable parameter rather than an
        /// extra <see cref="StatType"/> member: <see cref="StatType"/> is also used to index
        /// <c>StatValues</c> and iterated over in full by existing code, so adding an "unset"
        /// member to it would silently break every such iteration.
        /// </param>
        public ConditionalChoiceEffect(
            NumericCondition condition,
            StatDeltas deltasWhenTrue = default,
            StatDeltas deltasWhenFalse = default,
            int leaderHealthDeltaWhenTrue = 0,
            int leaderHealthDeltaWhenFalse = 0,
            bool triggersSuccessionWhenTrue = false,
            StatType? successionResetStat = null)
        {
            this.condition = condition ?? NumericCondition.Always();
            this.deltasWhenTrue = deltasWhenTrue;
            this.deltasWhenFalse = deltasWhenFalse;
            this.leaderHealthDeltaWhenTrue = leaderHealthDeltaWhenTrue;
            this.leaderHealthDeltaWhenFalse = leaderHealthDeltaWhenFalse;
            this.triggersSuccessionWhenTrue = triggersSuccessionWhenTrue;
            this.hasSuccessionResetStat = successionResetStat.HasValue;
            this.successionResetStat = successionResetStat ?? default;
        }

        public NumericCondition Condition => condition ?? NumericCondition.Always();

        public StatDeltas DeltasWhenTrue => deltasWhenTrue;

        public StatDeltas DeltasWhenFalse => deltasWhenFalse;

        public int LeaderHealthDeltaWhenTrue => leaderHealthDeltaWhenTrue;

        public int LeaderHealthDeltaWhenFalse => leaderHealthDeltaWhenFalse;

        public bool TriggersSuccessionWhenTrue => triggersSuccessionWhenTrue;

        public bool HasSuccessionResetStat => hasSuccessionResetStat;

        /// <summary>Meaningless unless <see cref="HasSuccessionResetStat"/> is true.</summary>
        public StatType SuccessionResetStat => successionResetStat;
    }
}
