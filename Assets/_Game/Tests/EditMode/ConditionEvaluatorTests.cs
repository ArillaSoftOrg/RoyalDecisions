using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class ConditionEvaluatorTests
    {
        private const int TestSeed = 7;

        private ConditionEvaluator evaluator;
        private RunState runState;

        [SetUp]
        public void SetUp()
        {
            evaluator = new ConditionEvaluator();
            runState = RunState.CreateNew(TestSeed);
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        [Test]
        public void CardWithNoConditions_IsEligible()
        {
            Assert.That(evaluator.IsEligible(CardTestFactory.Card(), runState), Is.True);
        }

        [Test]
        public void NullCard_IsIneligibleWithoutThrowing()
        {
            Assert.That(evaluator.IsEligible(null, runState), Is.False);
        }

        [Test]
        public void CardWithoutAnId_IsIneligible()
        {
            Assert.That(evaluator.IsEligible(CardTestFactory.Card(id: string.Empty), runState),
                Is.False);
        }

        // --- Required flags -------------------------------------------------

        [Test]
        public void RequiredFlag_BlocksUntilTheRunCarriesIt()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(requiredFlags: new[] { "war_declared" }));

            Assert.That(evaluator.IsEligible(card, runState), Is.False);

            runState.AddFlag("war_declared");
            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        [Test]
        public void EveryRequiredFlag_MustBePresent()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(
                    requiredFlags: new[] { "war_declared", "treasury_audited" }));

            runState.AddFlag("war_declared");
            Assert.That(evaluator.IsEligible(card, runState), Is.False, "one of two is not enough");

            runState.AddFlag("treasury_audited");
            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        // --- Forbidden flags ------------------------------------------------

        [Test]
        public void ForbiddenFlag_BlocksOnceTheRunCarriesIt()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(forbiddenFlags: new[] { "treaty_signed" }));

            Assert.That(evaluator.IsEligible(card, runState), Is.True);

            runState.AddFlag("treaty_signed");
            Assert.That(evaluator.IsEligible(card, runState), Is.False);
        }

        [Test]
        public void AnySingleForbiddenFlag_IsEnoughToBlock()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(
                    forbiddenFlags: new[] { "treaty_signed", "exiled" }));

            runState.AddFlag("exiled");

            Assert.That(evaluator.IsEligible(card, runState), Is.False);
        }

        // --- Stat ranges ----------------------------------------------------

        [Test]
        public void StatRange_BlocksWhenTheStatSitsOutsideIt()
        {
            // "people <= 25", exactly as the placeholder content will express it.
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(
                    statRanges: new[] { new StatRange(StatType.People, StatBounds.Min, 25) }));

            Assert.That(evaluator.IsEligible(card, runState), Is.False, "people starts at 50");

            runState.SetStats(runState.Stats.With(StatType.People, 25));
            Assert.That(evaluator.IsEligible(card, runState), Is.True, "inclusive upper bound");

            runState.SetStats(runState.Stats.With(StatType.People, 26));
            Assert.That(evaluator.IsEligible(card, runState), Is.False);
        }

        [Test]
        public void StatRange_IsInclusiveAtTheLowerBoundToo()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(
                    statRanges: new[] { new StatRange(StatType.Wealth, 10, 25) }));

            runState.SetStats(runState.Stats.With(StatType.Wealth, 9));
            Assert.That(evaluator.IsEligible(card, runState), Is.False);

            runState.SetStats(runState.Stats.With(StatType.Wealth, 10));
            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        [Test]
        public void WealthAtOrBelowTwentyFive_IsExpressible()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(
                    statRanges: new[] { new StatRange(StatType.Wealth, StatBounds.Min, 25) }));

            runState.SetStats(runState.Stats.With(StatType.Wealth, 25));

            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        [Test]
        public void EveryStatRange_MustHold()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(statRanges: new[]
                {
                    new StatRange(StatType.People, StatBounds.Min, 25),
                    new StatRange(StatType.Wealth, StatBounds.Min, 25)
                }));

            runState.SetStats(runState.Stats.With(StatType.People, 20));
            Assert.That(evaluator.IsEligible(card, runState), Is.False, "wealth is still 50");

            runState.SetStats(runState.Stats.With(StatType.Wealth, 20));
            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        [Test]
        public void FlagsAndStatRanges_MustBothHold()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(
                    requiredFlags: new[] { "war_declared" },
                    statRanges: new[] { new StatRange(StatType.People, StatBounds.Min, 25) }));

            runState.AddFlag("war_declared");
            Assert.That(evaluator.IsEligible(card, runState), Is.False, "stat condition still fails");

            runState.SetStats(runState.Stats.With(StatType.People, 10));
            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        // --- Once per run and cooldown --------------------------------------

        [Test]
        public void OncePerRunCard_IsIneligibleAfterItHasBeenShown()
        {
            CardDefinition card = CardTestFactory.Card(id: "card_once", oncePerRun: true);

            Assert.That(evaluator.IsEligible(card, runState), Is.True);

            runState.MarkCardShown("card_once");
            Assert.That(evaluator.IsEligible(card, runState), Is.False);
        }

        [Test]
        public void RepeatableCard_StaysEligibleAfterBeingShown()
        {
            CardDefinition card = CardTestFactory.Card(id: "card_repeat", oncePerRun: false);
            runState.MarkCardShown("card_repeat");

            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        [Test]
        public void CooldownCard_IsIneligibleUntilItsReleaseTurn()
        {
            CardDefinition card = CardTestFactory.Card(id: "card_cool", cooldownTurns: 2);
            runState.SetCooldown("card_cool", 3);

            Assert.That(evaluator.IsEligible(card, runState), Is.False, "turn 0 < 3");

            runState.AdvanceTurn();
            runState.AdvanceTurn();
            Assert.That(evaluator.IsEligible(card, runState), Is.False, "turn 2 < 3");

            runState.AdvanceTurn();
            Assert.That(evaluator.IsEligible(card, runState), Is.True, "turn 3 releases it");
        }

        // --- Direct condition evaluation ------------------------------------

        [Test]
        public void AreConditionsMet_TreatsNullConditionsAsUnrestricted()
        {
            Assert.That(evaluator.AreConditionsMet(null, runState), Is.True);
        }

        [Test]
        public void AreConditionsMet_IgnoresAnEmptyStatRangeRow()
        {
            // A row left blank in the Inspector must not silently remove the card from the deck.
            CardConditions conditions = CardTestFactory.Conditions(
                statRanges: new StatRange[] { null });

            Assert.That(evaluator.AreConditionsMet(conditions, runState), Is.True);
        }

        [Test]
        public void AreConditionsMet_RejectsANullRun()
        {
            Assert.That(evaluator.AreConditionsMet(CardTestFactory.Conditions(), null), Is.False);
        }

        // --- Numeric conditions ----------------------------------------------

        [Test]
        public void EvaluateNumeric_AlwaysIsTrueRegardlessOfState()
        {
            Assert.That(evaluator.EvaluateNumeric(NumericCondition.Always(), runState), Is.True);
        }

        [Test]
        public void EvaluateNumeric_ReadsAStatByTheConfiguredComparison()
        {
            NumericCondition condition = new NumericCondition(
                NumericSource.Stat, NumericComparison.GreaterOrEqual, 60, stat: StatType.Wealth);

            Assert.That(evaluator.EvaluateNumeric(condition, runState), Is.False, "wealth starts at 50");

            runState.SetStats(runState.Stats.With(StatType.Wealth, 60));
            Assert.That(evaluator.EvaluateNumeric(condition, runState), Is.True);
        }

        [Test]
        public void EvaluateNumeric_ReadsACounterAsZeroUntilTouched()
        {
            NumericCondition condition = new NumericCondition(
                NumericSource.Counter, NumericComparison.GreaterOrEqual, 2, counterId: "pharma_arastirma");

            Assert.That(evaluator.EvaluateNumeric(condition, runState), Is.False);

            runState.AddToCounter("pharma_arastirma", 2);
            Assert.That(evaluator.EvaluateNumeric(condition, runState), Is.True);
        }

        [Test]
        public void EvaluateNumeric_ReadsLeaderHealth()
        {
            NumericCondition condition = new NumericCondition(
                NumericSource.LeaderHealth, NumericComparison.LessThan, LeaderHealthBounds.CriticalThreshold);

            Assert.That(evaluator.EvaluateNumeric(condition, runState), Is.False, "leader health starts full");

            runState.SetLeaderHealth(LeaderHealthBounds.CriticalThreshold - 1);
            Assert.That(evaluator.EvaluateNumeric(condition, runState), Is.True);
        }

        [Test]
        public void EvaluateNumeric_RejectsANullConditionOrRun()
        {
            Assert.That(evaluator.EvaluateNumeric(null, runState), Is.False);
            Assert.That(evaluator.EvaluateNumeric(NumericCondition.Always(), null), Is.False);
        }

        // --- Forced-chain-only cards ------------------------------------------

        [Test]
        public void ForcedChainOnlyCard_IsIneligibleForNormalSelection()
        {
            CardDefinition card = CardTestFactory.Card(forcedChainOnly: true);

            Assert.That(evaluator.IsEligible(card, runState), Is.False);
        }

        [Test]
        public void RegularCard_WithoutForcedChainOnly_StaysEligible()
        {
            CardDefinition card = CardTestFactory.Card(forcedChainOnly: false);

            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }

        // --- Choice availability ----------------------------------------------

        [Test]
        public void IsChoiceAvailable_WithNoAvailabilityCondition_IsTrue()
        {
            ChoiceDefinition choice = CardTestFactory.Choice("Always");

            Assert.That(evaluator.IsChoiceAvailable(choice, runState), Is.True);
        }

        [Test]
        public void IsChoiceAvailable_RequiresItsFlagLikeACardCondition()
        {
            ChoiceDefinition choice = CardTestFactory.Choice("Gated",
                availability: CardTestFactory.Conditions(requiredFlags: new[] { "has_key" }));

            Assert.That(evaluator.IsChoiceAvailable(choice, runState), Is.False);

            runState.AddFlag("has_key");
            Assert.That(evaluator.IsChoiceAvailable(choice, runState), Is.True);
        }

        [Test]
        public void IsChoiceAvailable_RejectsANullChoice()
        {
            Assert.That(evaluator.IsChoiceAvailable(null, runState), Is.False);
        }

        [Test]
        public void CardConditions_NumericConditionCanBlockEligibility()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: new CardConditions(null, null, null, new[]
                {
                    new NumericCondition(
                        NumericSource.Counter, NumericComparison.GreaterOrEqual, 3,
                        counterId: "pharma_arastirma")
                }));

            Assert.That(evaluator.IsEligible(card, runState), Is.False);

            runState.AddToCounter("pharma_arastirma", 3);
            Assert.That(evaluator.IsEligible(card, runState), Is.True);
        }
    }
}
