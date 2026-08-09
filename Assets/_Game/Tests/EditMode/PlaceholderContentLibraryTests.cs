using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Editor;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Exercises the placeholder content set in memory — no AssetDatabase involvement.
    /// </summary>
    [TestFixture]
    public class PlaceholderContentLibraryTests
    {
        private const int ExpectedCardCount = 20;
        private const int ExpectedEndingCount = 8;
        private const int LowStatThreshold = 25;

        private List<CardDefinition> cards;
        private List<EndingDefinition> endings;

        [SetUp]
        public void SetUp()
        {
            cards = PlaceholderContentLibrary.CreateCards();
            endings = PlaceholderContentLibrary.CreateEndings();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAll(cards);
            DestroyAll(endings);
            CardTestFactory.DestroyAll();
        }

        private static void DestroyAll<T>(List<T> assets) where T : ScriptableObject
        {
            if (assets == null)
            {
                return;
            }

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(assets[i]);
                }
            }

            assets.Clear();
        }

        private CardDefinition CardById(string id)
        {
            return cards.Find(card => string.Equals(card.Id, id, StringComparison.Ordinal));
        }

        // --- Shape --------------------------------------------------------------

        [Test]
        public void ProducesExactlyTwentyCards()
        {
            Assert.That(cards.Count, Is.EqualTo(ExpectedCardCount));
        }

        [Test]
        public void ProducesExactlyEightEndings()
        {
            Assert.That(endings.Count, Is.EqualTo(ExpectedEndingCount));
        }

        [Test]
        public void EveryCardIdIsUniqueAndNonEmpty()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                string id = cards[i].Id;
                Assert.That(id, Is.Not.Empty);
                Assert.That(seen.Add(id), Is.True, "duplicate card ID: " + id);
            }
        }

        [Test]
        public void EveryEndingIdIsUniqueAndNonEmpty()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < endings.Count; i++)
            {
                string id = endings[i].Id;
                Assert.That(id, Is.Not.Empty);
                Assert.That(seen.Add(id), Is.True, "duplicate ending ID: " + id);
            }
        }

        [Test]
        public void CardsAreReturnedInOrdinalIdOrder()
        {
            for (int i = 1; i < cards.Count; i++)
            {
                Assert.That(
                    StringComparer.Ordinal.Compare(cards[i - 1].Id, cards[i].Id),
                    Is.LessThan(0),
                    "cards must be ascending by ordinal ID");
            }
        }

        [Test]
        public void EveryCardIsMarkedAsPlaceholder()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                Assert.That(cards[i].Speaker,
                    Does.StartWith(PlaceholderContentLibrary.PlaceholderTag),
                    cards[i].Id);
            }
        }

        // --- Validation ------------------------------------------------------------

        [Test]
        public void ContentPassesValidationWithNoErrorsOrWarnings()
        {
            ContentValidationReport report = new ContentValidator()
                .Validate(cards, endings, PlaceholderContentLibrary.OpeningCardId);

            Assert.That(report.HasErrors, Is.False, DescribeIssues(report));
            Assert.That(report.HasWarnings, Is.False, DescribeIssues(report));
        }

        private static string DescribeIssues(ContentValidationReport report)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(report.ToString());
            for (int i = 0; i < report.Issues.Count; i++)
            {
                builder.AppendLine().Append(report.Issues[i]);
            }

            return builder.ToString();
        }

        // --- Required capability coverage ---------------------------------------------

        [Test]
        public void OpeningCardExistsInTheSet()
        {
            Assert.That(CardById(PlaceholderContentLibrary.OpeningCardId), Is.Not.Null);
        }

        [Test]
        public void CoversOrdinaryStatChangesOnBothSides()
        {
            int withDeltasOnBothSides = cards.FindAll(card =>
                !card.LeftChoice.Deltas.IsEmpty && !card.RightChoice.Deltas.IsEmpty).Count;

            Assert.That(withDeltasOnBothSides, Is.GreaterThanOrEqualTo(ExpectedCardCount));
        }

        [Test]
        public void CoversAtLeastThreeOncePerRunCards()
        {
            Assert.That(cards.FindAll(card => card.OncePerRun).Count,
                Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void CoversAtLeastTwoCooldownCards()
        {
            Assert.That(cards.FindAll(card => card.HasCooldown).Count,
                Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void CoversWeightedSelectionWithMoreThanOneDistinctWeight()
        {
            HashSet<int> weights = new HashSet<int>();
            for (int i = 0; i < cards.Count; i++)
            {
                weights.Add(cards[i].SelectionWeight);
            }

            Assert.That(weights.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(weights, Has.Some.GreaterThan(1));
        }

        [Test]
        public void CoversRequiredFlags()
        {
            Assert.That(
                cards.Exists(card => card.Conditions.RequiredFlags.Count > 0),
                Is.True);
        }

        [Test]
        public void CoversForbiddenFlags()
        {
            Assert.That(
                cards.Exists(card => card.Conditions.ForbiddenFlags.Count > 0),
                Is.True);
        }

        [Test]
        public void CoversFlagAdditionAndRemoval()
        {
            bool adds = cards.Exists(card =>
                card.LeftChoice.FlagsToAdd.Count > 0 || card.RightChoice.FlagsToAdd.Count > 0);
            bool removes = cards.Exists(card =>
                card.LeftChoice.FlagsToRemove.Count > 0 || card.RightChoice.FlagsToRemove.Count > 0);

            Assert.That(adds, Is.True, "no choice adds a flag");
            Assert.That(removes, Is.True, "no choice removes a flag");
        }

        [Test]
        public void CoversAPeopleAtOrBelowTwentyFiveCondition()
        {
            Assert.That(HasLowThresholdRange(StatType.People), Is.True);
        }

        [Test]
        public void CoversAWealthAtOrBelowTwentyFiveCondition()
        {
            Assert.That(HasLowThresholdRange(StatType.Wealth), Is.True);
        }

        private bool HasLowThresholdRange(StatType stat)
        {
            return cards.Exists(card =>
            {
                IReadOnlyList<StatRange> ranges = card.Conditions.StatRanges;
                for (int i = 0; i < ranges.Count; i++)
                {
                    StatRange range = ranges[i];
                    if (range != null
                        && range.Stat == stat
                        && range.Min == StatBounds.Min
                        && range.Max == LowStatThreshold)
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        [Test]
        public void CoversAForcedTwoCardChain()
        {
            CardDefinition start = cards.Find(card => card.HasForcedNextCard);

            Assert.That(start, Is.Not.Null, "no card starts a forced chain");
            Assert.That(CardById(start.ForcedNextCardId), Is.Not.Null,
                "the chain target must exist");
        }

        [Test]
        public void CoversChoiceLevelForcedPrecedenceOverCardLevel()
        {
            // A card whose card-level chain is overridden on one side only, so the two sides lead
            // to different cards.
            CardDefinition card = cards.Find(c =>
                c.HasForcedNextCard
                && (c.LeftChoice.HasForcedNextCard || c.RightChoice.HasForcedNextCard));

            Assert.That(card, Is.Not.Null, "no card exercises choice-level precedence");

            string leftTarget = card.LeftChoice.HasForcedNextCard
                ? card.LeftChoice.ForcedNextCardId
                : card.ForcedNextCardId;
            string rightTarget = card.RightChoice.HasForcedNextCard
                ? card.RightChoice.ForcedNextCardId
                : card.ForcedNextCardId;

            Assert.That(leftTarget, Is.Not.EqualTo(rightTarget),
                "the override should send the two sides to different cards");
            Assert.That(CardById(leftTarget), Is.Not.Null);
            Assert.That(CardById(rightTarget), Is.Not.Null);
        }

        [Test]
        public void EndingsCoverEveryStatAndBothBoundaries()
        {
            foreach (StatType stat in (StatType[])Enum.GetValues(typeof(StatType)))
            {
                foreach (StatBoundary boundary in (StatBoundary[])Enum.GetValues(typeof(StatBoundary)))
                {
                    Assert.That(
                        endings.Exists(e => e.TriggerStat == stat && e.Boundary == boundary),
                        Is.True,
                        "no ending for " + stat + "/" + boundary);
                }
            }
        }

        // --- End-to-end against the Phase 2 engine -------------------------------------------

        [Test]
        public void ContentDrivesARealRunThroughTheRuleEngine()
        {
            // The strongest check available without a scene: the actual placeholder content, the
            // actual services, many turns, several seeds.
            for (int seed = 1; seed <= 5; seed++)
            {
                PlayRun(seed, maxTurns: 200);
            }
        }

        private void PlayRun(int seed, int maxTurns)
        {
            CardDeckService deck = new CardDeckService(new ConditionEvaluator());
            GameOverEvaluator gameOver = new GameOverEvaluator();

            RunState state = RunState.CreateNew(seed);
            StatSystem stats = new StatSystem(state);
            ChoiceResolver resolver = new ChoiceResolver(stats);

            state.SetForcedNextCardId(PlaceholderContentLibrary.OpeningCardId);

            for (int turn = 0; turn < maxTurns; turn++)
            {
                CardSelectionResult selection = deck.SelectCard(
                    state, cards, SeededRandomSource.ForTurn(state.Seed, state.Turn));

                Assert.That(selection.Status, Is.Not.EqualTo(CardSelectionStatus.ForcedCardMissing),
                    "seed " + seed + ": a forced chain pointed at a card that does not exist");

                if (!selection.HasCard)
                {
                    // Running out of eligible cards is a legitimate outcome, not a failure.
                    return;
                }

                if (turn == 0)
                {
                    Assert.That(selection.Card.Id,
                        Is.EqualTo(PlaceholderContentLibrary.OpeningCardId),
                        "the run must open on the designated opening card");
                }

                state.SetCurrentCardId(selection.Card.Id);
                state.ClearForcedNextCardId();

                ChoiceSide side = (turn % 2 == 0) ? ChoiceSide.Left : ChoiceSide.Right;
                ChoiceResolution resolution = resolver.Resolve(state, selection.Card, side);

                Assert.That(resolution.Succeeded, Is.True,
                    "seed " + seed + " turn " + turn + ": " + resolution.Status);

                GameOverResult over = gameOver.Evaluate(state, endings);
                if (over.IsGameOver)
                {
                    Assert.That(over.HasEnding, Is.True,
                        "every reachable boundary must have an ending: "
                        + over.TriggerStat + "/" + over.Boundary);
                    return;
                }
            }
        }

        [Test]
        public void EveryEndingIsReachableFromTheOpeningPosition()
        {
            // Drives each statistic to each boundary directly and checks an ending answers for it.
            GameOverEvaluator gameOver = new GameOverEvaluator();

            foreach (StatType stat in (StatType[])Enum.GetValues(typeof(StatType)))
            {
                AssertBoundaryHasEnding(gameOver, stat, StatBoundary.Min, StatBounds.Min);
                AssertBoundaryHasEnding(gameOver, stat, StatBoundary.Max, StatBounds.Max);
            }
        }

        private void AssertBoundaryHasEnding(
            GameOverEvaluator gameOver,
            StatType stat,
            StatBoundary boundary,
            int value)
        {
            RunState state = RunState.CreateNew(1);
            state.SetStats(state.Stats.With(stat, value));

            GameOverResult result = gameOver.Evaluate(state, endings);

            Assert.That(result.IsGameOver, Is.True, stat + "/" + boundary);
            Assert.That(result.HasEnding, Is.True, stat + "/" + boundary);
        }
    }
}
