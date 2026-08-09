using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class GameOverEvaluatorTests
    {
        private const int TestSeed = 3;

        private GameOverEvaluator evaluator;
        private RunState runState;
        private List<EndingDefinition> endings;

        [SetUp]
        public void SetUp()
        {
            evaluator = new GameOverEvaluator();
            runState = RunState.CreateNew(TestSeed);
            endings = CardTestFactory.AllBoundaryEndings();
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        private void PutStatAt(StatType stat, int value)
        {
            runState.SetStats(runState.Stats.With(stat, value));
        }

        // --- All eight boundaries -------------------------------------------

        [TestCase(StatType.Authority, StatBoundary.Min)]
        [TestCase(StatType.Authority, StatBoundary.Max)]
        [TestCase(StatType.People, StatBoundary.Min)]
        [TestCase(StatType.People, StatBoundary.Max)]
        [TestCase(StatType.Security, StatBoundary.Min)]
        [TestCase(StatType.Security, StatBoundary.Max)]
        [TestCase(StatType.Wealth, StatBoundary.Min)]
        [TestCase(StatType.Wealth, StatBoundary.Max)]
        public void EveryBoundary_EndsTheRunWithItsOwnEnding(StatType stat, StatBoundary boundary)
        {
            PutStatAt(stat, boundary == StatBoundary.Min ? StatBounds.Min : StatBounds.Max);

            GameOverResult result = evaluator.Evaluate(runState, endings);

            Assert.That(result.IsGameOver, Is.True);
            Assert.That(result.TriggerStat, Is.EqualTo(stat));
            Assert.That(result.Boundary, Is.EqualTo(boundary));
            Assert.That(result.HasEnding, Is.True);
            Assert.That(result.Ending.Id, Is.EqualTo(CardTestFactory.EndingId(stat, boundary)));
        }

        [Test]
        public void MidRangeStats_DoNotEndTheRun()
        {
            GameOverResult result = evaluator.Evaluate(runState, endings);

            Assert.That(result.IsGameOver, Is.False);
            Assert.That(result.HasEnding, Is.False);
        }

        [Test]
        public void AStatOneStepFromABoundary_DoesNotEndTheRun()
        {
            PutStatAt(StatType.People, StatBounds.Min + 1);
            Assert.That(evaluator.Evaluate(runState, endings).IsGameOver, Is.False);

            PutStatAt(StatType.People, StatBounds.Max - 1);
            Assert.That(evaluator.Evaluate(runState, endings).IsGameOver, Is.False);
        }

        // --- Missing content -------------------------------------------------

        [Test]
        public void BoundaryWithNoAuthoredEnding_StillEndsTheRun()
        {
            PutStatAt(StatType.Wealth, StatBounds.Min);

            GameOverResult result = evaluator.Evaluate(
                runState,
                new List<EndingDefinition>());

            Assert.That(result.IsGameOver, Is.True, "a dead stat must not let the run continue");
            Assert.That(result.HasEnding, Is.False);
            Assert.That(result.TriggerStat, Is.EqualTo(StatType.Wealth));
        }

        [Test]
        public void NullEndingList_DoesNotThrow()
        {
            PutStatAt(StatType.Wealth, StatBounds.Min);

            GameOverResult result = evaluator.Evaluate(runState, null);

            Assert.That(result.IsGameOver, Is.True);
            Assert.That(result.HasEnding, Is.False);
        }

        [Test]
        public void ACoveredBoundary_BeatsAnUncoveredOne()
        {
            // Authority would win on iteration order, but only People has an ending.
            PutStatAt(StatType.Authority, StatBounds.Min);
            PutStatAt(StatType.People, StatBounds.Min);

            List<EndingDefinition> onlyPeople = new List<EndingDefinition>
            {
                CardTestFactory.Ending(
                    id: "ending_people_min",
                    triggerStat: StatType.People,
                    boundary: StatBoundary.Min)
            };

            GameOverResult result = evaluator.Evaluate(runState, onlyPeople);

            Assert.That(result.TriggerStat, Is.EqualTo(StatType.People));
            Assert.That(result.HasEnding, Is.True);
        }

        // --- Simultaneous boundaries ------------------------------------------

        [Test]
        public void SimultaneousBoundaries_HighestPriorityWins()
        {
            PutStatAt(StatType.Authority, StatBounds.Min);
            PutStatAt(StatType.Wealth, StatBounds.Min);

            List<EndingDefinition> weighted = new List<EndingDefinition>
            {
                CardTestFactory.Ending(
                    id: "ending_authority_min",
                    triggerStat: StatType.Authority,
                    boundary: StatBoundary.Min,
                    priority: 1),
                CardTestFactory.Ending(
                    id: "ending_wealth_min",
                    triggerStat: StatType.Wealth,
                    boundary: StatBoundary.Min,
                    priority: 5)
            };

            GameOverResult result = evaluator.Evaluate(runState, weighted);

            Assert.That(result.TriggerStat, Is.EqualTo(StatType.Wealth));
        }

        [Test]
        public void SimultaneousBoundariesWithEqualPriority_FallBackToStatOrder()
        {
            PutStatAt(StatType.People, StatBounds.Min);
            PutStatAt(StatType.Wealth, StatBounds.Min);

            GameOverResult result = evaluator.Evaluate(runState, endings);

            Assert.That(result.TriggerStat, Is.EqualTo(StatType.People),
                "People precedes Wealth in evaluation order");
        }

        [Test]
        public void SimultaneousBoundaries_ResolveIdenticallyEveryTime()
        {
            PutStatAt(StatType.Authority, StatBounds.Max);
            PutStatAt(StatType.Security, StatBounds.Min);

            GameOverResult first = evaluator.Evaluate(runState, endings);
            GameOverResult second = evaluator.Evaluate(runState, endings);

            Assert.That(second.TriggerStat, Is.EqualTo(first.TriggerStat));
            Assert.That(second.Boundary, Is.EqualTo(first.Boundary));
            Assert.That(second.Ending, Is.SameAs(first.Ending));
        }

        [Test]
        public void OneStatAtBothEndsIsImpossible_ButMinAndMaxAcrossStatsResolves()
        {
            PutStatAt(StatType.People, StatBounds.Min);
            PutStatAt(StatType.Security, StatBounds.Max);

            GameOverResult result = evaluator.Evaluate(runState, endings);

            Assert.That(result.IsGameOver, Is.True);
            Assert.That(result.TriggerStat, Is.EqualTo(StatType.People));
            Assert.That(result.Boundary, Is.EqualTo(StatBoundary.Min));
        }

        // --- Duplicate endings ------------------------------------------------

        [Test]
        public void DuplicateEndingsForOneBoundary_ResolveByPriorityThenOrdinalId()
        {
            PutStatAt(StatType.People, StatBounds.Min);

            List<EndingDefinition> duplicates = new List<EndingDefinition>
            {
                CardTestFactory.Ending(id: "ending_z", triggerStat: StatType.People,
                    boundary: StatBoundary.Min, priority: 1),
                CardTestFactory.Ending(id: "ending_a", triggerStat: StatType.People,
                    boundary: StatBoundary.Min, priority: 1)
            };

            GameOverResult forward = evaluator.Evaluate(runState, duplicates);

            duplicates.Reverse();
            GameOverResult reversed = evaluator.Evaluate(runState, duplicates);

            Assert.That(forward.Ending.Id, Is.EqualTo("ending_a"));
            Assert.That(reversed.Ending.Id, Is.EqualTo("ending_a"),
                "list order must not decide which ending shows");
        }

        [Test]
        public void NullRun_IsNotGameOver()
        {
            Assert.That(evaluator.Evaluate(null, endings).IsGameOver, Is.False);
        }

        [Test]
        public void NullEntriesInTheEndingList_AreSkipped()
        {
            PutStatAt(StatType.People, StatBounds.Min);

            List<EndingDefinition> withHole = new List<EndingDefinition>
            {
                null,
                CardTestFactory.Ending(
                    id: "ending_people_min",
                    triggerStat: StatType.People,
                    boundary: StatBoundary.Min)
            };

            GameOverResult result = evaluator.Evaluate(runState, withHole);

            Assert.That(result.HasEnding, Is.True);
            Assert.That(result.Ending.Id, Is.EqualTo("ending_people_min"));
        }
    }
}
