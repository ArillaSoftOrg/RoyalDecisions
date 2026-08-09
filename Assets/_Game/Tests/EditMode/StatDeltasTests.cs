using System;
using NUnit.Framework;
using RoyalDecisions.Data;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class StatDeltasTests
    {
        [Test]
        public void Default_LeavesEveryStatUntouched()
        {
            StatDeltas deltas = default;

            Assert.That(deltas.IsEmpty, Is.True);
            Assert.That(deltas.Authority, Is.Zero);
            Assert.That(deltas.People, Is.Zero);
            Assert.That(deltas.Security, Is.Zero);
            Assert.That(deltas.Wealth, Is.Zero);
        }

        [Test]
        public void Constructor_StoresEachStatSeparately()
        {
            StatDeltas deltas = new StatDeltas(1, -2, 3, -4);

            Assert.That(deltas.Authority, Is.EqualTo(1));
            Assert.That(deltas.People, Is.EqualTo(-2));
            Assert.That(deltas.Security, Is.EqualTo(3));
            Assert.That(deltas.Wealth, Is.EqualTo(-4));
        }

        [TestCase(StatType.Authority, 1)]
        [TestCase(StatType.People, -2)]
        [TestCase(StatType.Security, 3)]
        [TestCase(StatType.Wealth, -4)]
        public void Indexer_ReturnsTheMatchingStat(StatType stat, int expected)
        {
            StatDeltas deltas = new StatDeltas(1, -2, 3, -4);

            Assert.That(deltas[stat], Is.EqualTo(expected));
        }

        [Test]
        public void Indexer_RejectsUnknownStat()
        {
            StatDeltas deltas = new StatDeltas(1, 2, 3, 4);

            Assert.That(() => deltas[(StatType)99], Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void IsEmpty_IsFalseWhenAnySingleStatChanges()
        {
            Assert.That(new StatDeltas(1, 0, 0, 0).IsEmpty, Is.False);
            Assert.That(new StatDeltas(0, 1, 0, 0).IsEmpty, Is.False);
            Assert.That(new StatDeltas(0, 0, 1, 0).IsEmpty, Is.False);
            Assert.That(new StatDeltas(0, 0, 0, 1).IsEmpty, Is.False);
        }
    }
}
