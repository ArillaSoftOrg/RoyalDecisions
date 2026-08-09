using System;
using System.Collections.Generic;

namespace RoyalDecisions.Editor
{
    [Serializable]
    public sealed class BalanceStrategyReport
    {
        public string strategy = string.Empty;
        public int completedRuns;
        public int censoredRuns;
        public int shortestTurns = int.MaxValue;
        public int longestTurns;
        public float meanTurns;
        public float medianTurns;
        public List<BalanceFrequency> cardSelections = new List<BalanceFrequency>();
        public List<BalanceFrequency> sideChoices = new List<BalanceFrequency>();
        public List<BalanceFrequency> endings = new List<BalanceFrequency>();
        public List<BalanceFrequency> precedingDeathChoices = new List<BalanceFrequency>();
        public List<string> neverObservedCards = new List<string>();
        public List<string> neverObservedEndings = new List<string>();
    }
}
