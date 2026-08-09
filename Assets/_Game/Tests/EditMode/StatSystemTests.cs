using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class StatSystemTests
    {
        private const int TestSeed = 99;

        private RunState runState;
        private StatSystem statSystem;

        [SetUp]
        public void SetUp()
        {
            runState = RunState.CreateNew(TestSeed);
            statSystem = new StatSystem(runState);
        }

        [Test]
        public void Apply_IncreasesStats()
        {
            statSystem.Apply(new StatDeltas(10, 5, 0, 0));

            Assert.That(statSystem.Get(StatType.Authority), Is.EqualTo(StatBounds.Initial + 10));
            Assert.That(statSystem.Get(StatType.People), Is.EqualTo(StatBounds.Initial + 5));
        }

        [Test]
        public void Apply_DecreasesStats()
        {
            statSystem.Apply(new StatDeltas(-10, 0, -5, 0));

            Assert.That(statSystem.Get(StatType.Authority), Is.EqualTo(StatBounds.Initial - 10));
            Assert.That(statSystem.Get(StatType.Security), Is.EqualTo(StatBounds.Initial - 5));
        }

        [Test]
        public void Apply_ClampsAtBothBounds()
        {
            statSystem.Apply(new StatDeltas(999, -999, 0, 0));

            Assert.That(statSystem.Get(StatType.Authority), Is.EqualTo(StatBounds.Max));
            Assert.That(statSystem.Get(StatType.People), Is.EqualTo(StatBounds.Min));
        }

        [Test]
        public void Apply_WritesThroughToTheRunState()
        {
            statSystem.Apply(new StatDeltas(0, 0, 7, 0));

            // The system must not keep its own copy, or the save would disagree with the HUD.
            Assert.That(runState.Stats.Security, Is.EqualTo(StatBounds.Initial + 7));
            Assert.That(statSystem.Current.Security, Is.EqualTo(runState.Stats.Security));
        }

        [Test]
        public void StatChanged_FiresOncePerMovedStatWithTheRealisedDelta()
        {
            List<StatChange> changes = new List<StatChange>();
            statSystem.StatChanged += changes.Add;

            statSystem.Apply(new StatDeltas(10, -4, 0, 0));

            Assert.That(changes.Count, Is.EqualTo(2), "only the two stats that moved should fire");

            StatChange authority = changes.Find(c => c.Stat == StatType.Authority);
            Assert.That(authority.Previous, Is.EqualTo(StatBounds.Initial));
            Assert.That(authority.Current, Is.EqualTo(StatBounds.Initial + 10));
            Assert.That(authority.Delta, Is.EqualTo(10));

            StatChange people = changes.Find(c => c.Stat == StatType.People);
            Assert.That(people.Delta, Is.EqualTo(-4));
        }

        [Test]
        public void StatChanged_ReportsTheClampedDeltaNotTheRequestedOne()
        {
            List<StatChange> changes = new List<StatChange>();
            statSystem.StatChanged += changes.Add;

            statSystem.Apply(new StatDeltas(999, 0, 0, 0));

            Assert.That(changes[0].Delta, Is.EqualTo(StatBounds.Max - StatBounds.Initial));
        }

        [Test]
        public void StatChanged_DoesNotFireWhenClampingMakesTheDeltaANoOp()
        {
            statSystem.Apply(new StatDeltas(999, 0, 0, 0));
            Assert.That(statSystem.Get(StatType.Authority), Is.EqualTo(StatBounds.Max));

            List<StatChange> changes = new List<StatChange>();
            statSystem.StatChanged += changes.Add;

            statSystem.Apply(new StatDeltas(50, 0, 0, 0));

            Assert.That(changes, Is.Empty, "a stat already pinned at max did not move");
        }

        [Test]
        public void StatChanged_DoesNotFireForAnEmptyDelta()
        {
            List<StatChange> changes = new List<StatChange>();
            statSystem.StatChanged += changes.Add;

            statSystem.Apply(default);

            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void StatsChanged_FiresExactlyOncePerApply()
        {
            int calls = 0;
            statSystem.StatsChanged += _ => calls++;

            statSystem.Apply(new StatDeltas(1, 1, 1, 1));

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void StatsChanged_FiresEvenWhenNothingMoved()
        {
            // A whole-HUD refresh should still be able to run unconditionally.
            int calls = 0;
            statSystem.StatsChanged += _ => calls++;

            statSystem.Apply(default);

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Handlers_ObserveTheAlreadyUpdatedValue()
        {
            int observed = -1;
            statSystem.StatChanged += _ => observed = statSystem.Get(StatType.Authority);

            statSystem.Apply(new StatDeltas(10, 0, 0, 0));

            Assert.That(observed, Is.EqualTo(StatBounds.Initial + 10));
        }

        [Test]
        public void Set_ReplacesEveryStatAndClamps()
        {
            statSystem.Set(new StatValues(1, 2, 3, 999));

            Assert.That(statSystem.Get(StatType.Authority), Is.EqualTo(1));
            Assert.That(statSystem.Get(StatType.Wealth), Is.EqualTo(StatBounds.Max));
        }

        [Test]
        public void IsAtBoundary_TracksBothEnds()
        {
            Assert.That(statSystem.IsAtBoundary(StatType.People, StatBoundary.Min), Is.False);

            statSystem.Apply(new StatDeltas(0, -999, 0, 0));
            Assert.That(statSystem.IsAtBoundary(StatType.People, StatBoundary.Min), Is.True);
            Assert.That(statSystem.IsAtBoundary(StatType.People, StatBoundary.Max), Is.False);

            statSystem.Apply(new StatDeltas(0, 999, 0, 0));
            Assert.That(statSystem.IsAtBoundary(StatType.People, StatBoundary.Max), Is.True);
        }

        [Test]
        public void Constructor_RejectsANullRun()
        {
            Assert.That(() => new StatSystem(null), Throws.ArgumentNullException);
        }
    }
}
