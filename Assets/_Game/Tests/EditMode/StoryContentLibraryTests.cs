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
    /// Exercises the full 250-card story content set in memory — no AssetDatabase involvement.
    /// </summary>
    [TestFixture]
    public class StoryContentLibraryTests
    {
        private const int ExpectedCardCount = 250;
        private const int ExpectedEndingCount = 8;

        private List<CardDefinition> cards;
        private List<EndingDefinition> endings;

        [SetUp]
        public void SetUp()
        {
            cards = StoryContentLibrary.CreateCards();
            endings = StoryContentLibrary.CreateEndings();
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
        public void ProducesTwoHundredFiftyCards()
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
        public void EveryCardIsMarkedOncePerRunAndForcedChainOnly()
        {
            // A linear, hand-authored story never repeats a scene and is never drawn at random.
            for (int i = 0; i < cards.Count; i++)
            {
                Assert.That(cards[i].OncePerRun, Is.True, cards[i].Id);
                Assert.That(cards[i].ForcedChainOnly, Is.True, cards[i].Id);
            }
        }

        // --- Validation ------------------------------------------------------------

        [Test]
        public void ContentPassesValidationWithNoErrors()
        {
            ContentValidationReport report = new ContentValidator()
                .Validate(cards, endings, StoryContentLibrary.OpeningCardId);

            Assert.That(report.HasErrors, Is.False, DescribeIssues(report));
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

        [Test]
        public void OpeningCardExistsInTheSet()
        {
            Assert.That(CardById(StoryContentLibrary.OpeningCardId), Is.Not.Null);
        }

        [Test]
        public void EveryForcedNextTargetExistsInTheSet()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                AssertTargetExists(card.LeftChoice.ForcedNextCardId, card.Id, "left");
                AssertTargetExists(card.RightChoice.ForcedNextCardId, card.Id, "right");

                for (int v = 0; v < card.Variants.Count; v++)
                {
                    CardVariant variant = card.Variants[v];
                    AssertTargetExists(variant.LeftChoice?.ForcedNextCardId, card.Id, "variant left");
                    AssertTargetExists(variant.RightChoice?.ForcedNextCardId, card.Id, "variant right");
                }
            }
        }

        private void AssertTargetExists(string targetId, string cardId, string side)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return;
            }

            Assert.That(CardById(targetId), Is.Not.Null,
                cardId + "'s " + side + " choice forces a card that does not exist: " + targetId);
        }

        [Test]
        public void OnlyTheFinalCardHasNoForcedNextOnEitherSide()
        {
            List<string> withoutForcedNext = new List<string>();

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (!card.LeftChoice.HasForcedNextCard && !card.RightChoice.HasForcedNextCard)
                {
                    withoutForcedNext.Add(card.Id);
                }
            }

            Assert.That(withoutForcedNext, Is.EqualTo(new[] { "story_k250" }));
        }

        [Test]
        public void EndingsCoverEveryStatAndBothBoundaries()
        {
            foreach (StatType stat in new[]
                     { StatType.Authority, StatType.People, StatType.Security, StatType.Wealth })
            {
                foreach (StatBoundary boundary in new[] { StatBoundary.Min, StatBoundary.Max })
                {
                    Assert.That(
                        endings.Exists(e => e.TriggerStat == stat && e.Boundary == boundary),
                        Is.True,
                        "no ending for " + stat + "/" + boundary);
                }
            }
        }

        // --- Required mechanic coverage ------------------------------------------------

        [Test]
        public void CoversAnUnconditionalReignEndingChoice()
        {
            // K8-B: giving away the last of the food ends the reign outright.
            CardDefinition card = CardById("story_k008");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.RightChoice.HasConditionalEffect, Is.True);
            Assert.That(card.RightChoice.ConditionalEffect.TriggersSuccessionWhenTrue, Is.True);
            Assert.That(card.RightChoice.ConditionalEffect.Condition.Comparison,
                Is.EqualTo(NumericComparison.Always));
        }

        [Test]
        public void CoversALeaderRiskChoiceGatedOnLeaderHealth()
        {
            // K14-A: fatal only when leader health is already critical.
            CardDefinition card = CardById("story_k014");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.LeftChoice.HasConditionalEffect, Is.True);
            Assert.That(card.LeftChoice.ConditionalEffect.Condition.Source,
                Is.EqualTo(NumericSource.LeaderHealth));
            Assert.That(card.LeftChoice.ConditionalEffect.TriggersSuccessionWhenTrue, Is.True);
        }

        [Test]
        public void NoChoiceUsesRandomOutcome()
        {
            // v12's determinism principle (Hıkaye.md §3): every outcome is driven by an existing
            // flag, a resource threshold, or a prior player decision — never by chance. K18-B used
            // to be the one RandomStatOutcome example in this catalogue; it is now a plain
            // deterministic choice, and no other card should reintroduce randomness in its place.
            foreach (CardDefinition card in cards)
            {
                AssertChoiceHasNoRandomOutcome(card.Id, "left", card.LeftChoice);
                AssertChoiceHasNoRandomOutcome(card.Id, "right", card.RightChoice);

                foreach (CardVariant variant in card.Variants)
                {
                    if (variant.LeftChoice != null)
                    {
                        AssertChoiceHasNoRandomOutcome(card.Id, "variant left", variant.LeftChoice);
                    }

                    if (variant.RightChoice != null)
                    {
                        AssertChoiceHasNoRandomOutcome(card.Id, "variant right", variant.RightChoice);
                    }
                }
            }
        }

        private static void AssertChoiceHasNoRandomOutcome(
            string cardId, string side, ChoiceDefinition choice)
        {
            Assert.That(choice.HasRandomOutcome, Is.False,
                cardId + " (" + side + ") still uses RandomStatOutcome.");
        }

        [Test]
        public void CoversAStoryCounterChoice()
        {
            // K19-A: questioning the stranger raises the Vertak clue counter.
            CardDefinition card = CardById("story_k019");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.LeftChoice.CounterDeltas.Count, Is.EqualTo(1));
            Assert.That(card.LeftChoice.CounterDeltas[0].CounterId,
                Is.EqualTo(StoryContentLibrary.CounterVertakIpucu));
        }

        [Test]
        public void CoversAFlagAddedByOneChoice()
        {
            CardDefinition k3 = CardById("story_k003");
            Assert.That(k3.LeftChoice.FlagsToAdd, Has.Some.EqualTo("k3_yolu"));
        }

        [Test]
        public void CoversACardVariantSelectedByAnEarlierFlag()
        {
            // K28: text and both choices differ depending on K27's cit_yaklastik_evet flag.
            CardDefinition card = CardById("story_k028");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.Variants.Count, Is.GreaterThanOrEqualTo(1));

            RunState withoutFlag = RunState.CreateNew(1);
            RunState withFlag = RunState.CreateNew(1);
            withFlag.AddFlag("cit_yaklastik_evet");

            CardVariantResolver resolver = new CardVariantResolver(new ConditionEvaluator());
            ResolvedCard baseResolved = resolver.Resolve(card, withoutFlag);
            ResolvedCard variantResolved = resolver.Resolve(card, withFlag);

            Assert.That(variantResolved.BodyText, Is.Not.EqualTo(baseResolved.BodyText));
        }

        [Test]
        public void CoversACounterGatedVariant()
        {
            // K135: text differs once pharma_arastirma reaches 3.
            CardDefinition card = CardById("story_k135");
            RunState low = RunState.CreateNew(1);
            RunState high = RunState.CreateNew(1);
            high.AddToCounter(StoryContentLibrary.CounterPharmaArastirma, 3);

            CardVariantResolver resolver = new CardVariantResolver(new ConditionEvaluator());
            Assert.That(resolver.Resolve(card, low).BodyText,
                Is.Not.EqualTo(resolver.Resolve(card, high).BodyText));
        }

        // --- End-to-end against the engine ---------------------------------------------

        [Test]
        public void ContentDrivesARealRunThroughTheRuleEngineToTheStorysEnd()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                PlayRun(seed, maxTurns: 260);
            }
        }

        private void PlayRun(int seed, int maxTurns)
        {
            CardDeckService deck = new CardDeckService(new ConditionEvaluator());
            GameOverEvaluator gameOver = new GameOverEvaluator();
            CardVariantResolver variantResolver = new CardVariantResolver(new ConditionEvaluator());

            RunState state = RunState.CreateNew(seed);
            StatSystem stats = new StatSystem(state);
            ChoiceResolver resolver = new ChoiceResolver(stats);

            state.SetForcedNextCardId(StoryContentLibrary.OpeningCardId);

            bool reachedEndOfAuthoredContent = false;

            for (int turn = 0; turn < maxTurns; turn++)
            {
                CardSelectionResult selection = deck.SelectCard(
                    state, cards, SeededRandomSource.ForTurn(state.Seed, state.Turn));

                Assert.That(selection.Status, Is.Not.EqualTo(CardSelectionStatus.ForcedCardMissing),
                    "seed " + seed + ": a forced chain pointed at a card that does not exist");

                if (!selection.HasCard)
                {
                    // Running out of eligible cards after the specification's final card (K250) is
                    // the expected, designed stop — see StoryChapter6Cards's remarks.
                    reachedEndOfAuthoredContent = true;
                    break;
                }

                if (turn == 0)
                {
                    Assert.That(selection.Card.Id, Is.EqualTo(StoryContentLibrary.OpeningCardId),
                        "the run must open on the designated opening card");
                }

                state.SetCurrentCardId(selection.Card.Id);
                state.ClearForcedNextCardId();

                ResolvedCard resolved = variantResolver.Resolve(selection.Card, state);
                ChoiceSide side = (turn % 2 == 0) ? ChoiceSide.Left : ChoiceSide.Right;

                // Alternate sides, but never one the resolved card marks unavailable.
                if (!resolved.IsAvailable(side))
                {
                    side = side == ChoiceSide.Left ? ChoiceSide.Right : ChoiceSide.Left;
                }

                ChoiceResolution resolution = resolver.Resolve(
                    state, selection.Card, resolved.Choice(side), side);

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

            Assert.That(reachedEndOfAuthoredContent, Is.True,
                "seed " + seed + ": the story did not terminate within " + maxTurns + " turns");
        }
    }
}
