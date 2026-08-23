using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>Runs the production application/domain flow with in-memory adapters only.</summary>
    public sealed class BalanceSimulationRunner
    {
        public BalanceSimulationReport Run(
            ContentCatalogue catalogue,
            BalanceSimulationOptions options,
            Func<bool> cancellationRequested = null,
            Action<float, string> progress = null)
        {
            if (catalogue == null)
            {
                throw new ArgumentNullException(nameof(catalogue));
            }
            options ??= new BalanceSimulationOptions();
            if (options.RunCount < 1 || options.MaximumTurns < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            BalanceSimulationReport report = new BalanceSimulationReport
            {
                contentFingerprint = ContentFingerprint(catalogue),
                runCount = options.RunCount,
                baseSeed = options.BaseSeed,
                maximumTurns = options.MaximumTurns
            };
            BalanceSimulationStrategy[] strategies = options.Strategies
                ?? Array.Empty<BalanceSimulationStrategy>();
            int total = Math.Max(1, strategies.Length * options.RunCount);
            int completed = 0;
            for (int s = 0; s < strategies.Length; s++)
            {
                BalanceStrategyReport strategyReport = RunStrategy(
                    catalogue, options, strategies[s], cancellationRequested,
                    (message) => progress?.Invoke((float)completed / total, message),
                    ref completed);
                report.strategies.Add(strategyReport);
                if (cancellationRequested != null && cancellationRequested())
                {
                    break;
                }
            }
            report.reproducibilityHash = ComputeHash(JsonUtility.ToJson(report, false));
            return report;
        }

        private static BalanceStrategyReport RunStrategy(
            ContentCatalogue catalogue,
            BalanceSimulationOptions options,
            BalanceSimulationStrategy strategy,
            Func<bool> cancelled,
            Action<string> progress,
            ref int totalCompleted)
        {
            Dictionary<string, int> cards = NewCounter(catalogue.Cards);
            Dictionary<string, int> endings = NewCounter(catalogue.Endings);
            Dictionary<string, int> sides = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "Left", 0 }, { "Right", 0 }
            };
            Dictionary<string, int> deaths = new Dictionary<string, int>(StringComparer.Ordinal);
            List<int> lengths = new List<int>(options.RunCount);
            BalanceStrategyReport result = new BalanceStrategyReport
            {
                strategy = strategy.ToString()
            };

            for (int runIndex = 0; runIndex < options.RunCount; runIndex++)
            {
                if (cancelled != null && cancelled())
                {
                    break;
                }
                int seed = unchecked(options.BaseSeed + runIndex);
                SimulationPresenter presenter = new SimulationPresenter();
                GameSession session = new GameSession(new GameSessionDependencies(
                    catalogue,
                    presenter,
                    new MemorySaveStore(),
                    new FixedSeedProvider(seed)));
                SessionResult start = session.StartNewGame();
                if (!start.Accepted)
                {
                    throw new InvalidOperationException("Simulation could not start: " + start.Error.Message);
                }
                string lastDecision = string.Empty;
                while (session.State == GameSessionState.AwaitingDecision
                    && session.CurrentRun.Turn < options.MaximumTurns)
                {
                    CardDefinition card = session.CurrentCard;
                    Increment(cards, card.Id);
                    ChoiceSide side = SelectSide(strategy, seed, session.CurrentRun, card);
                    Increment(sides, side.ToString());
                    lastDecision = card.Id + "/" + side;
                    SessionResult decision = session.ConfirmDecision(side);
                    if (!decision.Accepted)
                    {
                        throw new InvalidOperationException(
                            "Simulation decision failed: " + decision.Error.Message);
                    }
                    SessionResult exit = session.NotifyCardExitCompleted();
                    if (!exit.Accepted)
                    {
                        throw new InvalidOperationException(
                            "Simulation exit failed: " + exit.Error.Message);
                    }
                }

                int turns = session.CurrentRun != null ? session.CurrentRun.Turn : 0;
                lengths.Add(turns);
                result.shortestTurns = Math.Min(result.shortestTurns, turns);
                result.longestTurns = Math.Max(result.longestTurns, turns);
                if (session.State == GameSessionState.ShowingGameOver)
                {
                    result.completedRuns++;
                    string endingId = presenter.LastGameOver.Ending != null
                        ? presenter.LastGameOver.Ending.Id
                        : presenter.LastGameOver.TriggerStat + "/" + presenter.LastGameOver.Boundary;
                    Increment(endings, endingId);
                    Increment(deaths, lastDecision);
                }
                else
                {
                    result.censoredRuns++;
                }
                session.Shutdown();
                totalCompleted++;
                progress?.Invoke(strategy + " " + (runIndex + 1) + "/" + options.RunCount);
            }

            lengths.Sort();
            long sum = 0;
            for (int i = 0; i < lengths.Count; i++)
            {
                sum += lengths[i];
            }
            result.meanTurns = lengths.Count > 0 ? (float)sum / lengths.Count : 0f;
            result.medianTurns = Median(lengths);
            if (result.shortestTurns == int.MaxValue)
            {
                result.shortestTurns = 0;
            }
            result.cardSelections = Frequencies(cards);
            result.sideChoices = Frequencies(sides);
            result.endings = Frequencies(endings);
            result.precedingDeathChoices = Frequencies(deaths);
            result.neverObservedCards = ZeroKeys(cards);
            result.neverObservedEndings = ZeroKeys(endings);
            return result;
        }

        private static ChoiceSide SelectSide(
            BalanceSimulationStrategy strategy,
            int seed,
            RunState run,
            CardDefinition card)
        {
            switch (strategy)
            {
                case BalanceSimulationStrategy.AlwaysLeft:
                    return ChoiceSide.Left;
                case BalanceSimulationStrategy.AlwaysRight:
                    return ChoiceSide.Right;
                case BalanceSimulationStrategy.Random:
                    return new System.Random(unchecked(seed * 397) ^ run.Turn).Next(0, 2) == 0
                        ? ChoiceSide.Left : ChoiceSide.Right;
                case BalanceSimulationStrategy.SafestImmediateChoice:
                    return ScoreSafety(run.Stats.WithDelta(card.LeftChoice.Deltas))
                        >= ScoreSafety(run.Stats.WithDelta(card.RightChoice.Deltas))
                        ? ChoiceSide.Left : ChoiceSide.Right;
                case BalanceSimulationStrategy.StatBalancing:
                    return ScoreBalance(run.Stats.WithDelta(card.LeftChoice.Deltas))
                        <= ScoreBalance(run.Stats.WithDelta(card.RightChoice.Deltas))
                        ? ChoiceSide.Left : ChoiceSide.Right;
                default:
                    return ChoiceSide.Left;
            }
        }

        private static int ScoreSafety(StatValues values)
        {
            int score = int.MaxValue;
            StatType[] stats = { StatType.Authority, StatType.People, StatType.Security, StatType.Wealth };
            for (int i = 0; i < stats.Length; i++)
            {
                int value = values[stats[i]];
                score = Math.Min(score, Math.Min(value, StatBounds.Max - value));
            }
            return score;
        }

        private static int ScoreBalance(StatValues values) =>
            Math.Abs(values.Authority - StatBounds.Initial)
            + Math.Abs(values.People - StatBounds.Initial)
            + Math.Abs(values.Security - StatBounds.Initial)
            + Math.Abs(values.Wealth - StatBounds.Initial);

        private static Dictionary<string, int> NewCounter<T>(IReadOnlyList<T> content)
            where T : UnityEngine.Object
        {
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < content.Count; i++)
            {
                string id = content[i] switch
                {
                    CardDefinition card => card.Id,
                    EndingDefinition ending => ending.Id,
                    _ => string.Empty
                };
                if (!string.IsNullOrEmpty(id) && !result.ContainsKey(id))
                {
                    result.Add(id, 0);
                }
            }
            return result;
        }

        private static void Increment(Dictionary<string, int> counts, string id)
        {
            id ??= string.Empty;
            counts.TryGetValue(id, out int count);
            counts[id] = count + 1;
        }

        private static List<BalanceFrequency> Frequencies(Dictionary<string, int> counts)
        {
            List<string> keys = new List<string>(counts.Keys);
            keys.Sort(StringComparer.Ordinal);
            List<BalanceFrequency> result = new List<BalanceFrequency>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                result.Add(new BalanceFrequency { id = keys[i], count = counts[keys[i]] });
            }
            return result;
        }

        private static List<string> ZeroKeys(Dictionary<string, int> counts)
        {
            List<string> result = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (pair.Value == 0)
                {
                    result.Add(pair.Key);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static float Median(List<int> sorted)
        {
            if (sorted.Count == 0)
            {
                return 0f;
            }
            int middle = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[middle - 1] + sorted[middle]) * 0.5f
                : sorted[middle];
        }

        private static string ContentFingerprint(ContentCatalogue catalogue)
        {
            StringBuilder builder = new StringBuilder(catalogue.OpeningCardId);
            for (int i = 0; i < catalogue.Cards.Count; i++)
            {
                CardDefinition card = catalogue.Cards[i];
                builder.Append('|').Append(card != null ? JsonUtility.ToJson(card, false) : "<null>");
            }
            for (int i = 0; i < catalogue.Endings.Count; i++)
            {
                EndingDefinition ending = catalogue.Endings[i];
                builder.Append('|').Append(ending != null ? JsonUtility.ToJson(ending, false) : "<null>");
            }
            return ComputeHash(builder.ToString());
        }

        private static string ComputeHash(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private sealed class FixedSeedProvider : ISeedProvider
        {
            private readonly int seed;
            public FixedSeedProvider(int seed) => this.seed = seed;
            public int NextSeed() => seed;
        }

        private sealed class MemorySaveStore : IRunSaveStore
        {
            private RunState state;
            public bool HasSave() => state != null;
            public RunLoadOutcome Load() => state != null
                ? RunLoadOutcome.Loaded(RunLoadStatus.Success, state)
                : RunLoadOutcome.Failure(RunLoadStatus.NoSave, string.Empty);
            public SaveOutcome Save(RunState runState)
            {
                state = runState;
                return SaveOutcome.Ok();
            }
            public SaveOutcome Delete()
            {
                state = null;
                return SaveOutcome.Ok();
            }
        }

        private sealed class SimulationPresenter : IGamePresenter
        {
            public GameOverResult LastGameOver { get; private set; }
            public void ShowCard(CardDefinition card, ResolvedCard resolved) { }
            public void ClearCard() { }
            public void PrepareForInput() { }
            public void CancelInput() { }
            public void BindStats(StatSystem statSystem) { }
            public void UnbindStats() { }
            public void RefreshStats(StatValues values) { }
            public void ShowTurn(int oneBasedTurn) { }
            public void ShowGameOver(GameOverResult result) => LastGameOver = result;
            public void HideGameOver() { }
        }
    }
}
