using System;
using System.Collections.Generic;

namespace RoyalDecisions.Editor
{
    /// <summary>Serializable deterministic balance output; safe for JSON/CSV export.</summary>
    [Serializable]
    public sealed class BalanceSimulationReport
    {
        public string contentFingerprint = string.Empty;
        public int runCount;
        public int baseSeed;
        public int maximumTurns;
        public List<BalanceStrategyReport> strategies = new List<BalanceStrategyReport>();
        public string reproducibilityHash = string.Empty;
    }
}
