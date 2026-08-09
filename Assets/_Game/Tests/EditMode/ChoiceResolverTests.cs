using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class ChoiceResolverTests
    {
        private const int TestSeed = 11;

        private RunState runState;
        private ChoiceResolver resolver;

        [SetUp]
        public void SetUp()
        {
            runState = RunState.CreateNew(TestSeed);
            resolver = new ChoiceResolver(new StatSystem(runState));
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        /// <summary>Presents a card the way the game flow will: by arming the resolution token.</summary>
        private CardDefinition Present(CardDefinition card)
        {
            runState.SetCurrentCardId(card.Id);
            return card;
        }

        // --- Deltas ---------------------------------------------------------

        [Test]
        public void Resolve_AppliesOnlyTheChosenSidesDeltas()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice("Left", authority: 10),
                right: CardTestFactory.Choice("Right", people: 20)));

            ChoiceResolution result = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial + 10));
            Assert.That(runState.Stats.People, Is.EqualTo(StatBounds.Initial), "right side not applied");
        }

        [Test]
        public void Resolve_AppliesTheRightSideWhenChosen()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice("Left", authority: 10),
                right: CardTestFactory.Choice("Right", people: 20)));

            resolver.Resolve(runState, card, ChoiceSide.Right);

            Assert.That(runState.Stats.People, Is.EqualTo(StatBounds.Initial + 20));
            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial));
        }

        [Test]
        public void Resolve_ClampsDeltasAtBothBounds()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice("Left", authority: 999, people: -999)));

            ChoiceResolution result = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(runState.Stats.People, Is.EqualTo(StatBounds.Min));
            Assert.That(result.StatsBefore.Authority, Is.EqualTo(StatBounds.Initial));
            Assert.That(result.StatsAfter.Authority, Is.EqualTo(StatBounds.Max));
        }

        // --- Flags ----------------------------------------------------------

        [Test]
        public void Resolve_AddsAndRemovesFlags()
        {
            runState.AddFlag("peace");

            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice(
                    "Left",
                    flagsToAdd: new[] { "war_declared" },
                    flagsToRemove: new[] { "peace" })));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.HasFlag("war_declared"), Is.True);
            Assert.That(runState.HasFlag("peace"), Is.False);
        }

        [Test]
        public void Resolve_AppliesAdditionsBeforeRemovals()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice(
                    "Left",
                    flagsToAdd: new[] { "contested" },
                    flagsToRemove: new[] { "contested" })));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.HasFlag("contested"), Is.False,
                "a flag in both lists ends up removed");
        }

        // --- History, cooldown, chains, turn ---------------------------------

        [Test]
        public void Resolve_RecordsTheCardAsShown()
        {
            CardDefinition card = Present(CardTestFactory.Card(id: "card_a"));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.HasShownCard("card_a"), Is.True);
        }

        [Test]
        public void Resolve_SetsCooldownOneTurnBeyondTheCooldownLength()
        {
            CardDefinition card = Present(CardTestFactory.Card(id: "card_cool", cooldownTurns: 3));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            // Shown on turn 0 with cooldown 3 -> drawable again on turn 4.
            Assert.That(runState.TryGetCooldownTurn("card_cool", out int availableOn), Is.True);
            Assert.That(availableOn, Is.EqualTo(4));
            Assert.That(runState.IsOnCooldown("card_cool"), Is.True, "now on turn 1");
        }

        [Test]
        public void Resolve_WithCooldownOfOne_BlocksTheFollowingTurn()
        {
            // Without the +1 offset this cooldown would expire immediately and mean nothing.
            CardDefinition card = Present(CardTestFactory.Card(id: "card_cool", cooldownTurns: 1));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.Turn, Is.EqualTo(1));
            Assert.That(runState.IsOnCooldown("card_cool"), Is.True);

            runState.AdvanceTurn();
            Assert.That(runState.IsOnCooldown("card_cool"), Is.False);
        }

        [Test]
        public void Resolve_WritesNoCooldownEntryWhenTheCardHasNone()
        {
            CardDefinition card = Present(CardTestFactory.Card(id: "card_a", cooldownTurns: 0));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.Cooldowns, Is.Empty);
        }

        [Test]
        public void Resolve_PrefersTheChoiceLevelForcedCardOverTheCardLevelOne()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice("Left", forcedNextCardId: "card_from_choice"),
                forcedNextCardId: "card_from_card"));

            ChoiceResolution result = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.ForcedNextCardId, Is.EqualTo("card_from_choice"));
            Assert.That(result.ForcedNextCardId, Is.EqualTo("card_from_choice"));
            Assert.That(result.HasForcedNextCard, Is.True);
        }

        [Test]
        public void Resolve_FallsBackToTheCardLevelForcedCard()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice("Left"),
                forcedNextCardId: "card_from_card"));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.ForcedNextCardId, Is.EqualTo("card_from_card"));
        }

        [Test]
        public void Resolve_LeavesNoForcedCardWhenNeitherLevelSetsOne()
        {
            CardDefinition card = Present(CardTestFactory.Card());

            ChoiceResolution result = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.HasForcedNextCard, Is.False);
            Assert.That(result.HasForcedNextCard, Is.False);
        }

        [Test]
        public void Resolve_AdvancesTheTurnExactlyOnce()
        {
            CardDefinition card = Present(CardTestFactory.Card());

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.Turn, Is.EqualTo(GameConstants.FirstTurn + 1));
        }

        // --- Duplicate resolution -------------------------------------------

        [Test]
        public void Resolve_Twice_AppliesTheDecisionOnlyOnce()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                id: "card_a",
                left: CardTestFactory.Choice("Left", authority: 10, flagsToAdd: new[] { "flagged" })));

            ChoiceResolution first = resolver.Resolve(runState, card, ChoiceSide.Left);
            ChoiceResolution second = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.Status, Is.EqualTo(ChoiceResolutionStatus.NoActiveCard));

            // The whole run must be untouched by the second call, not just the stats.
            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial + 10));
            Assert.That(runState.Flags.Count, Is.EqualTo(1));
            Assert.That(runState.Turn, Is.EqualTo(GameConstants.FirstTurn + 1));
            Assert.That(runState.ShownCardIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_ClearsTheCurrentCardToken()
        {
            CardDefinition card = Present(CardTestFactory.Card(id: "card_a"));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.CurrentCardId, Is.Empty);
        }

        [Test]
        public void Resolve_WithNoPresentedCard_IsRejected()
        {
            CardDefinition card = CardTestFactory.Card(id: "card_a");

            ChoiceResolution result = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(result.Status, Is.EqualTo(ChoiceResolutionStatus.NoActiveCard));
            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial));
            Assert.That(runState.Turn, Is.EqualTo(GameConstants.FirstTurn));
        }

        [Test]
        public void Resolve_WithADifferentCardThanPresented_IsRejected()
        {
            Present(CardTestFactory.Card(id: "card_presented"));
            CardDefinition other = CardTestFactory.Card(
                id: "card_other",
                left: CardTestFactory.Choice("Left", authority: 10));

            ChoiceResolution result = resolver.Resolve(runState, other, ChoiceSide.Left);

            Assert.That(result.Status, Is.EqualTo(ChoiceResolutionStatus.CardMismatch));
            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial));
            Assert.That(runState.CurrentCardId, Is.EqualTo("card_presented"));
        }

        [Test]
        public void Resolve_OnAnEndedRun_IsRejected()
        {
            CardDefinition card = Present(CardTestFactory.Card(
                left: CardTestFactory.Choice("Left", authority: 10)));
            runState.EndRun();

            ChoiceResolution result = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(result.Status, Is.EqualTo(ChoiceResolutionStatus.RunNotActive));
            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial));
        }

        [Test]
        public void Resolve_WithANullCard_IsRejectedWithoutThrowing()
        {
            ChoiceResolution result = resolver.Resolve(runState, null, ChoiceSide.Left);

            Assert.That(result.Status, Is.EqualTo(ChoiceResolutionStatus.InvalidCard));
        }

        [Test]
        public void Resolve_WithAnIdlessCard_IsRejected()
        {
            CardDefinition card = CardTestFactory.Card(id: string.Empty);

            ChoiceResolution result = resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(result.Status, Is.EqualTo(ChoiceResolutionStatus.InvalidCard));
        }

        [Test]
        public void Constructor_RejectsANullStatSystem()
        {
            Assert.That(() => new ChoiceResolver(null), Throws.ArgumentNullException);
        }
    }
}
