using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The result of resolving one choice, including the stats either side of the change so a view
    /// can animate the transition without re-reading the run.
    /// </summary>
    public readonly struct ChoiceResolution
    {
        private ChoiceResolution(
            ChoiceResolutionStatus status,
            ChoiceSide side,
            StatValues statsBefore,
            StatValues statsAfter,
            string forcedNextCardId)
        {
            Status = status;
            Side = side;
            StatsBefore = statsBefore;
            StatsAfter = statsAfter;
            ForcedNextCardId = forcedNextCardId ?? string.Empty;
        }

        public static ChoiceResolution Applied(
            ChoiceSide side,
            StatValues statsBefore,
            StatValues statsAfter,
            string forcedNextCardId)
        {
            return new ChoiceResolution(
                ChoiceResolutionStatus.Applied,
                side,
                statsBefore,
                statsAfter,
                forcedNextCardId);
        }

        /// <summary>
        /// A rejection carries no stat values: nothing was written, so there is no "after".
        /// </summary>
        public static ChoiceResolution Rejected(ChoiceResolutionStatus status)
        {
            return new ChoiceResolution(status, default, default, default, string.Empty);
        }

        public ChoiceResolutionStatus Status { get; }

        public bool Succeeded => Status == ChoiceResolutionStatus.Applied;

        public ChoiceSide Side { get; }

        public StatValues StatsBefore { get; }

        public StatValues StatsAfter { get; }

        public string ForcedNextCardId { get; }

        public bool HasForcedNextCard => !string.IsNullOrEmpty(ForcedNextCardId);
    }
}
