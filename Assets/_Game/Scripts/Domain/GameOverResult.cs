using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Whether the run has ended and, if so, which boundary ended it.
    /// </summary>
    /// <remarks>
    /// <see cref="IsGameOver"/> is true with a null <see cref="Ending"/> when a statistic hit a
    /// boundary but no ending asset covers it. The run still ends — continuing with a dead
    /// statistic would be worse than showing nothing — and Phase 3 content validation is
    /// responsible for making that state unreachable in shipped content.
    /// </remarks>
    public readonly struct GameOverResult
    {
        private GameOverResult(
            bool isGameOver,
            StatType triggerStat,
            StatBoundary boundary,
            EndingDefinition ending)
        {
            IsGameOver = isGameOver;
            TriggerStat = triggerStat;
            Boundary = boundary;
            Ending = ending;
        }

        public static GameOverResult NotOver()
        {
            return new GameOverResult(false, default, default, null);
        }

        public static GameOverResult Over(
            StatType triggerStat,
            StatBoundary boundary,
            EndingDefinition ending)
        {
            return new GameOverResult(true, triggerStat, boundary, ending);
        }

        public bool IsGameOver { get; }

        public StatType TriggerStat { get; }

        public StatBoundary Boundary { get; }

        public EndingDefinition Ending { get; }

        public bool HasEnding => Ending != null;
    }
}
