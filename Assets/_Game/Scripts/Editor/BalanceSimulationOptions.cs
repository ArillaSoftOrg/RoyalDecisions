namespace RoyalDecisions.Editor
{
    /// <summary>Deterministic, editor-only balance run parameters.</summary>
    public sealed class BalanceSimulationOptions
    {
        public int RunCount { get; set; } = 10000;

        public int BaseSeed { get; set; } = 1000;

        public int MaximumTurns { get; set; } = 500;

        public BalanceSimulationStrategy[] Strategies { get; set; } =
        {
            BalanceSimulationStrategy.Random,
            BalanceSimulationStrategy.AlwaysLeft,
            BalanceSimulationStrategy.AlwaysRight,
            BalanceSimulationStrategy.SafestImmediateChoice,
            BalanceSimulationStrategy.StatBalancing
        };
    }
}
