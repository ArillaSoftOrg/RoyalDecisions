using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers the story-content mechanisms layered onto <see cref="ChoiceResolver"/>: leader-health
    /// deltas, reign succession, random outcomes and story counters. Plain unconditional deltas and
    /// the duplicate-resolution guard are covered by <see cref="ChoiceResolverTests"/>.
    /// </summary>
    [TestFixture]
    public class ChoiceResolverConditionalEffectTests
    {
        private const int TestSeed = 2026;

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

        private CardDefinition Present(CardDefinition card)
        {
            runState.SetCurrentCardId(card.Id);
            return card;
        }

        // --- Plain leader-health deltas (Always, no succession) ----------------

        [Test]
        public void ConditionalEffect_Always_AppliesLeaderHealthDeltaWithNoSuccession()
        {
            runState.SetLeaderHealth(6);

            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Join the watch",
                authority: 1,
                conditionalEffect: new ConditionalChoiceEffect(
                    NumericCondition.Always(), leaderHealthDeltaWhenTrue: -1))));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.LeaderHealth, Is.EqualTo(5));
            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial + 1),
                "the choice's own Deltas still apply alongside the conditional effect");
            Assert.That(runState.ReignNumber, Is.EqualTo(GameConstants.FirstReignNumber));
        }

        // --- Leader risk: safe when leader health is healthy --------------------

        [Test]
        public void LeaderRisk_WithHealthyLeader_AppliesTheSafeDeltaAndSurvives()
        {
            NumericCondition risk = new NumericCondition(
                NumericSource.LeaderHealth, NumericComparison.LessThan,
                LeaderHealthBounds.CriticalThreshold);

            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Go personally",
                security: 0,
                conditionalEffect: new ConditionalChoiceEffect(
                    risk,
                    leaderHealthDeltaWhenFalse: -3,
                    deltasWhenFalse: new StatDeltas(0, 0, 2, 0),
                    triggersSuccessionWhenTrue: true))));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.LeaderHealth, Is.EqualTo(LeaderHealthBounds.Initial - 3));
            Assert.That(runState.Stats.Security, Is.EqualTo(StatBounds.Initial + 2));
            Assert.That(runState.ReignNumber, Is.EqualTo(GameConstants.FirstReignNumber),
                "a healthy leader must not trigger succession");
        }

        // --- Leader risk: fatal when leader health is already critical ---------

        [Test]
        public void LeaderRisk_WithCriticalLeader_TriggersSuccessionInstead()
        {
            runState.SetLeaderHealth(LeaderHealthBounds.CriticalThreshold - 1);

            NumericCondition risk = new NumericCondition(
                NumericSource.LeaderHealth, NumericComparison.LessThan,
                LeaderHealthBounds.CriticalThreshold);

            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Go personally",
                conditionalEffect: new ConditionalChoiceEffect(
                    risk,
                    leaderHealthDeltaWhenFalse: -3,
                    triggersSuccessionWhenTrue: true))));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.LeaderHealth, Is.EqualTo(LeaderHealthBounds.Initial),
                "the successor starts at full health");
            Assert.That(runState.ReignNumber, Is.EqualTo(GameConstants.FirstReignNumber + 1));
        }

        // --- Destructive (Yikici): unconditional ---------------------------------

        [Test]
        public void UnconditionalSuccession_ResetsTheNamedStatInsteadOfLeavingItAtTheBoundary()
        {
            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Give away every last reserve",
                conditionalEffect: new ConditionalChoiceEffect(
                    NumericCondition.Always(),
                    triggersSuccessionWhenTrue: true,
                    successionResetStat: StatType.Wealth))));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.Stats.Wealth, Is.EqualTo(GameConstants.ReignSuccessionResetStatValue));
            Assert.That(runState.LeaderHealth, Is.EqualTo(LeaderHealthBounds.Initial));
            Assert.That(runState.ReignNumber, Is.EqualTo(GameConstants.FirstReignNumber + 1));
        }

        [Test]
        public void UnconditionalSuccession_NeverLeavesTheStatAtZeroForGameOverEvaluatorToSee()
        {
            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Give away every last reserve",
                conditionalEffect: new ConditionalChoiceEffect(
                    NumericCondition.Always(),
                    triggersSuccessionWhenTrue: true,
                    successionResetStat: StatType.Wealth))));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            GameOverResult result = new GameOverEvaluator().Evaluate(runState, CardTestFactory.AllBoundaryEndings());
            Assert.That(result.IsGameOver, Is.False,
                "a reign succession must not also read as a game-over ending");
        }

        // --- Destructive (Yikici): conditional on the current value -------------

        [Test]
        public void ConditionalSuccession_OnlyTriggersWhenTheStatIsAlreadyCritical()
        {
            NumericCondition alreadyLow = new NumericCondition(
                NumericSource.Stat, NumericComparison.LessOrEqual, 3, stat: StatType.Authority);

            ChoiceDefinition suppress = CardTestFactory.Choice(
                "Suppress the crowd",
                conditionalEffect: new ConditionalChoiceEffect(
                    alreadyLow,
                    deltasWhenFalse: new StatDeltas(-1, 0, 0, 0),
                    triggersSuccessionWhenTrue: true,
                    successionResetStat: StatType.Authority));

            // Healthy: the ordinary -1 applies and nothing succeeds the leader.
            CardDefinition healthyCard = Present(CardTestFactory.Card(id: "card_healthy", left: suppress));
            resolver.Resolve(runState, healthyCard, ChoiceSide.Left);

            Assert.That(runState.Stats.Authority, Is.EqualTo(StatBounds.Initial - 1));
            Assert.That(runState.ReignNumber, Is.EqualTo(GameConstants.FirstReignNumber));

            // Drive authority down to the critical threshold, then take the same choice again.
            runState.SetStats(runState.Stats.With(StatType.Authority, 3));
            CardDefinition criticalCard = Present(CardTestFactory.Card(id: "card_critical", left: suppress));
            resolver.Resolve(runState, criticalCard, ChoiceSide.Left);

            Assert.That(runState.Stats.Authority, Is.EqualTo(GameConstants.ReignSuccessionResetStatValue));
            Assert.That(runState.ReignNumber, Is.EqualTo(GameConstants.FirstReignNumber + 1));
        }

        // --- Random outcomes -----------------------------------------------------

        [Test]
        public void RandomOutcome_AppliesThePickedOption()
        {
            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Improvise treatment",
                randomOutcome: new RandomStatOutcome(
                    new StatDeltas(0, 1, 0, 0),
                    new StatDeltas(0, -1, 0, 0)))));

            resolver.Resolve(runState, card, ChoiceSide.Left, new FakeRandomSource(1));

            Assert.That(runState.Stats.People, Is.EqualTo(StatBounds.Initial - 1));
        }

        [Test]
        public void RandomOutcome_WithoutAnInjectedSource_StillResolvesDeterministically()
        {
            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Improvise treatment",
                randomOutcome: new RandomStatOutcome(
                    new StatDeltas(0, 1, 0, 0),
                    new StatDeltas(0, -1, 0, 0)))));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.Stats.People, Is.EqualTo(StatBounds.Initial + 1)
                .Or.EqualTo(StatBounds.Initial - 1));
        }

        // --- Story counters -----------------------------------------------------

        [Test]
        public void Resolve_AppliesCounterDeltas()
        {
            CardDefinition card = Present(CardTestFactory.Card(left: CardTestFactory.Choice(
                "Question the stranger",
                counterDeltas: new[] { new CounterDelta("vertak_ipucu", 1) })));

            resolver.Resolve(runState, card, ChoiceSide.Left);

            Assert.That(runState.GetCounter("vertak_ipucu"), Is.EqualTo(1));
        }
    }
}
