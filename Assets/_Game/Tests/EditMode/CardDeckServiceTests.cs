using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class CardDeckServiceTests
    {
        private const int TestSeed = 20260729;
        private const int TurnsToReplay = 12;

        private CardDeckService deck;
        private RunState runState;

        [SetUp]
        public void SetUp()
        {
            deck = new CardDeckService(new ConditionEvaluator());
            runState = RunState.CreateNew(TestSeed);
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        /// <summary>Draws a card per turn, exactly as the game flow will, and records the IDs.</summary>
        private static List<string> ReplaySequence(
            CardDeckService deck,
            IReadOnlyList<CardDefinition> catalogue,
            int seed,
            int turns)
        {
            RunState state = RunState.CreateNew(seed);
            List<string> drawn = new List<string>(turns);

            for (int turn = 0; turn < turns; turn++)
            {
                CardSelectionResult result = deck.SelectCard(
                    state,
                    catalogue,
                    SeededRandomSource.ForTurn(state.Seed, state.Turn));

                Assert.That(result.HasCard, Is.True);
                drawn.Add(result.Card.Id);
                state.AdvanceTurn();
            }

            return drawn;
        }

        // --- Determinism -----------------------------------------------------

        [Test]
        public void SameSeed_ReplaysTheSameCardSequence()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(
                ("card_a", 1), ("card_b", 2), ("card_c", 3), ("card_d", 4));

            List<string> first = ReplaySequence(deck, catalogue, TestSeed, TurnsToReplay);
            List<string> second = ReplaySequence(deck, catalogue, TestSeed, TurnsToReplay);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void DifferentSeeds_DivergeAtSomePoint()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(
                ("card_a", 1), ("card_b", 1), ("card_c", 1), ("card_d", 1));

            List<string> first = ReplaySequence(deck, catalogue, TestSeed, TurnsToReplay);
            List<string> second = ReplaySequence(deck, catalogue, TestSeed + 1, TurnsToReplay);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void CatalogueOrder_DoesNotChangeTheSequence()
        {
            List<CardDefinition> original = CardTestFactory.WeightedCards(
                ("card_a", 1), ("card_b", 2), ("card_c", 3), ("card_d", 4));

            List<CardDefinition> reversed = new List<CardDefinition>(original);
            reversed.Reverse();

            // A fixed shuffle, not a random one: the test must fail the same way every run.
            List<CardDefinition> shuffled = new List<CardDefinition>
            {
                original[2], original[0], original[3], original[1]
            };

            List<string> fromOriginal = ReplaySequence(deck, original, TestSeed, TurnsToReplay);
            List<string> fromReversed = ReplaySequence(deck, reversed, TestSeed, TurnsToReplay);
            List<string> fromShuffled = ReplaySequence(deck, shuffled, TestSeed, TurnsToReplay);

            Assert.That(fromReversed, Is.EqualTo(fromOriginal));
            Assert.That(fromShuffled, Is.EqualTo(fromOriginal));
        }

        [Test]
        public void OrdinalIdOrder_DefinesTheWeightBands()
        {
            // Authored deliberately out of order. Sorted by ordinal ID the bands are
            // card_a [0], card_b [1..2], card_c [3..5] over a total weight of 6.
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(
                ("card_c", 3), ("card_a", 1), ("card_b", 2));

            AssertRollSelects(catalogue, roll: 0, expectedId: "card_a");
            AssertRollSelects(catalogue, roll: 1, expectedId: "card_b");
            AssertRollSelects(catalogue, roll: 2, expectedId: "card_b");
            AssertRollSelects(catalogue, roll: 3, expectedId: "card_c");
            AssertRollSelects(catalogue, roll: 5, expectedId: "card_c");
        }

        [Test]
        public void Ordering_IsOrdinalNotCultureSensitive()
        {
            // Ordinal puts 'C' (0x43) before 'c' (0x63), so Card_B sorts first. A culture-aware
            // comparison would order card_a first, and so would leaving the catalogue untouched —
            // this roll therefore fails unless the sort is genuinely ordinal.
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(
                ("card_a", 1), ("Card_B", 1));

            Assert.That(StringComparer.Ordinal.Compare("Card_B", "card_a"), Is.LessThan(0),
                "precondition: ordinal ordering puts Card_B first");

            AssertRollSelects(catalogue, roll: 0, expectedId: "Card_B");
            AssertRollSelects(catalogue, roll: 1, expectedId: "card_a");
        }

        private void AssertRollSelects(
            IReadOnlyList<CardDefinition> catalogue,
            int roll,
            string expectedId)
        {
            RunState state = RunState.CreateNew(TestSeed);
            FakeRandomSource random = new FakeRandomSource(roll);

            CardSelectionResult result = deck.SelectCard(state, catalogue, random);

            Assert.That(result.Card.Id, Is.EqualTo(expectedId), "roll {0}", roll);
        }

        [Test]
        public void SelectCard_DoesNotReorderTheCallersCatalogue()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(
                ("card_c", 1), ("card_a", 1), ("card_b", 1));
            List<CardDefinition> snapshot = new List<CardDefinition>(catalogue);

            deck.SelectCard(runState, catalogue, new FakeRandomSource(0));

            Assert.That(catalogue, Is.EqualTo(snapshot), "the caller's list must survive intact");
            Assert.That(catalogue[0].Id, Is.EqualTo("card_c"));
        }

        [Test]
        public void SelectCard_DoesNotMutateTheRun()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(("card_a", 1));
            runState.SetCurrentCardId("card_previous");

            deck.SelectCard(runState, catalogue, new FakeRandomSource(0));

            Assert.That(runState.Turn, Is.EqualTo(GameConstants.FirstTurn));
            Assert.That(runState.Flags, Is.Empty);
            Assert.That(runState.ShownCardIds, Is.Empty);
            Assert.That(runState.CurrentCardId, Is.EqualTo("card_previous"),
                "presenting the card is the game flow's job, not the deck's");
            Assert.That(runState.HasForcedNextCard, Is.False);
        }

        // --- Weighting --------------------------------------------------------

        [Test]
        public void NextInt_IsAskedForExactlyTheTotalWeight()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(
                ("card_a", 2), ("card_b", 3), ("card_c", 5));
            FakeRandomSource random = new FakeRandomSource(0);

            deck.SelectCard(runState, catalogue, random);

            Assert.That(random.LastExclusiveMax, Is.EqualTo(10));
            Assert.That(random.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void ZeroWeightCard_IsStillDrawable()
        {
            // SelectionWeight floors at 1, so an unweighted card never silently leaves the deck.
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(("card_a", 0));
            FakeRandomSource random = new FakeRandomSource(0);

            CardSelectionResult result = deck.SelectCard(runState, catalogue, random);

            Assert.That(result.Card.Id, Is.EqualTo("card_a"));
            Assert.That(random.LastExclusiveMax, Is.EqualTo(1));
        }

        [Test]
        public void IneligibleCards_AreExcludedFromThePoolAndTheWeightTotal()
        {
            CardDefinition blocked = CardTestFactory.Card(
                id: "card_blocked",
                selectionWeight: 9,
                conditions: CardTestFactory.Conditions(requiredFlags: new[] { "never_set" }));
            CardDefinition open = CardTestFactory.Card(id: "card_open", selectionWeight: 2);

            List<CardDefinition> catalogue = new List<CardDefinition> { blocked, open };
            FakeRandomSource random = new FakeRandomSource(0);

            CardSelectionResult result = deck.SelectCard(runState, catalogue, random);

            Assert.That(result.Card.Id, Is.EqualTo("card_open"));
            Assert.That(random.LastExclusiveMax, Is.EqualTo(2), "the blocked weight must not count");
        }

        [Test]
        public void OncePerRunAndCooldownCards_AreFilteredOut()
        {
            CardDefinition once = CardTestFactory.Card(id: "card_once", oncePerRun: true);
            CardDefinition cooling = CardTestFactory.Card(id: "card_cool");
            CardDefinition open = CardTestFactory.Card(id: "card_open");

            runState.MarkCardShown("card_once");
            runState.SetCooldown("card_cool", 5);

            List<CardDefinition> catalogue = new List<CardDefinition> { once, cooling, open };
            FakeRandomSource random = new FakeRandomSource(0);

            CardSelectionResult result = deck.SelectCard(runState, catalogue, random);

            Assert.That(result.Card.Id, Is.EqualTo("card_open"));
            Assert.That(random.LastExclusiveMax, Is.EqualTo(1));
        }

        // --- Empty and exhausted decks ----------------------------------------

        [Test]
        public void NoEligibleCards_ReportsNoEligibleCard()
        {
            List<CardDefinition> catalogue = new List<CardDefinition>
            {
                CardTestFactory.Card(
                    id: "card_blocked",
                    conditions: CardTestFactory.Conditions(requiredFlags: new[] { "never_set" }))
            };

            CardSelectionResult result = deck.SelectCard(
                runState, catalogue, new FakeRandomSource());

            Assert.That(result.Status, Is.EqualTo(CardSelectionStatus.NoEligibleCard));
            Assert.That(result.HasCard, Is.False);
        }

        [Test]
        public void EmptyCatalogue_ReportsEmptyCatalogue()
        {
            CardSelectionResult result = deck.SelectCard(
                runState, new List<CardDefinition>(), new FakeRandomSource());

            Assert.That(result.Status, Is.EqualTo(CardSelectionStatus.EmptyCatalogue));
            Assert.That(result.HasCard, Is.False);
        }

        [Test]
        public void NullCatalogue_ReportsEmptyCatalogueWithoutThrowing()
        {
            CardSelectionResult result = deck.SelectCard(runState, null, new FakeRandomSource());

            Assert.That(result.Status, Is.EqualTo(CardSelectionStatus.EmptyCatalogue));
        }

        // --- Forced cards ------------------------------------------------------

        [Test]
        public void ForcedCard_IsReturnedWithoutConsumingRandomness()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(
                ("card_a", 1), ("card_forced", 1));
            runState.SetForcedNextCardId("card_forced");

            FakeRandomSource random = new FakeRandomSource();
            CardSelectionResult result = deck.SelectCard(runState, catalogue, random);

            Assert.That(result.Status, Is.EqualTo(CardSelectionStatus.Forced));
            Assert.That(result.Card.Id, Is.EqualTo("card_forced"));
            Assert.That(random.CallCount, Is.Zero,
                "a forced turn must leave the stream untouched");
        }

        [Test]
        public void ForcedCard_IgnoresItsOwnConditions()
        {
            CardDefinition forced = CardTestFactory.Card(
                id: "card_forced",
                conditions: CardTestFactory.Conditions(requiredFlags: new[] { "never_set" }));
            CardDefinition other = CardTestFactory.Card(id: "card_a");

            runState.SetForcedNextCardId("card_forced");

            CardSelectionResult result = deck.SelectCard(
                runState,
                new List<CardDefinition> { other, forced },
                new FakeRandomSource());

            Assert.That(result.Card.Id, Is.EqualTo("card_forced"),
                "an authored chain must not break when stats drift");
        }

        [Test]
        public void ForcedCard_IsFoundWhereverItSitsInTheCatalogue()
        {
            CardDefinition forced = CardTestFactory.Card(id: "card_forced");
            CardDefinition first = CardTestFactory.Card(id: "card_a");
            runState.SetForcedNextCardId("card_forced");

            CardSelectionResult atEnd = deck.SelectCard(
                runState, new List<CardDefinition> { first, forced }, new FakeRandomSource());
            CardSelectionResult atStart = deck.SelectCard(
                runState, new List<CardDefinition> { forced, first }, new FakeRandomSource());

            Assert.That(atEnd.Card.Id, Is.EqualTo("card_forced"));
            Assert.That(atStart.Card.Id, Is.EqualTo("card_forced"));
        }

        [Test]
        public void ForcedCard_ThatDoesNotExist_FallsBackToWeightedSelection()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(("card_a", 1));
            runState.SetForcedNextCardId("card_missing");

            FakeRandomSource random = new FakeRandomSource(0);
            CardSelectionResult result = deck.SelectCard(runState, catalogue, random);

            Assert.That(result.Status, Is.EqualTo(CardSelectionStatus.ForcedCardMissing));
            Assert.That(result.HasCard, Is.True, "a broken chain must not dead-end the run");
            Assert.That(result.Card.Id, Is.EqualTo("card_a"));
            Assert.That(random.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void ForcedCardMissing_WithNothingEligible_ReportsTheBrokenChain()
        {
            List<CardDefinition> catalogue = new List<CardDefinition>
            {
                CardTestFactory.Card(
                    id: "card_blocked",
                    conditions: CardTestFactory.Conditions(requiredFlags: new[] { "never_set" }))
            };
            runState.SetForcedNextCardId("card_missing");

            CardSelectionResult result = deck.SelectCard(
                runState, catalogue, new FakeRandomSource());

            Assert.That(result.Status, Is.EqualTo(CardSelectionStatus.ForcedCardMissing));
            Assert.That(result.HasCard, Is.False);
        }

        // --- Guards -------------------------------------------------------------

        [Test]
        public void Constructor_RejectsANullEvaluator()
        {
            Assert.That(() => new CardDeckService(null), Throws.ArgumentNullException);
        }

        [Test]
        public void SelectCard_RejectsANullRunOrRandomSource()
        {
            List<CardDefinition> catalogue = CardTestFactory.WeightedCards(("card_a", 1));

            Assert.That(() => deck.SelectCard(null, catalogue, new FakeRandomSource(0)),
                Throws.ArgumentNullException);
            Assert.That(() => deck.SelectCard(runState, catalogue, null),
                Throws.ArgumentNullException);
        }
    }
}
