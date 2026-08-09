using System;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class StatValuesTests
    {
        [Test]
        public void CreateInitial_PutsEveryStatAtTheMidpoint()
        {
            StatValues stats = StatValues.CreateInitial();

            Assert.That(stats.Authority, Is.EqualTo(StatBounds.Initial));
            Assert.That(stats.People, Is.EqualTo(StatBounds.Initial));
            Assert.That(stats.Security, Is.EqualTo(StatBounds.Initial));
            Assert.That(stats.Wealth, Is.EqualTo(StatBounds.Initial));
        }

        [Test]
        public void WithDelta_IncreasesStats()
        {
            StatValues stats = StatValues.CreateInitial().WithDelta(new StatDeltas(10, 5, 1, 20));

            Assert.That(stats.Authority, Is.EqualTo(60));
            Assert.That(stats.People, Is.EqualTo(55));
            Assert.That(stats.Security, Is.EqualTo(51));
            Assert.That(stats.Wealth, Is.EqualTo(70));
        }

        [Test]
        public void WithDelta_DecreasesStats()
        {
            StatValues stats = StatValues.CreateInitial().WithDelta(new StatDeltas(-10, -5, -1, -20));

            Assert.That(stats.Authority, Is.EqualTo(40));
            Assert.That(stats.People, Is.EqualTo(45));
            Assert.That(stats.Security, Is.EqualTo(49));
            Assert.That(stats.Wealth, Is.EqualTo(30));
        }

        [Test]
        public void WithDelta_ClampsAtMaximumInsteadOfOvershooting()
        {
            StatValues stats = StatValues.CreateInitial().WithDelta(new StatDeltas(500, 500, 500, 500));

            Assert.That(stats.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(stats.People, Is.EqualTo(StatBounds.Max));
            Assert.That(stats.Security, Is.EqualTo(StatBounds.Max));
            Assert.That(stats.Wealth, Is.EqualTo(StatBounds.Max));
        }

        [Test]
        public void WithDelta_ClampsAtMinimumInsteadOfGoingNegative()
        {
            StatValues stats = StatValues.CreateInitial().WithDelta(new StatDeltas(-500, -500, -500, -500));

            Assert.That(stats.Authority, Is.EqualTo(StatBounds.Min));
            Assert.That(stats.People, Is.EqualTo(StatBounds.Min));
            Assert.That(stats.Security, Is.EqualTo(StatBounds.Min));
            Assert.That(stats.Wealth, Is.EqualTo(StatBounds.Min));
        }

        [Test]
        public void WithDelta_LeavesTheOriginalUnchanged()
        {
            StatValues original = StatValues.CreateInitial();

            original.WithDelta(new StatDeltas(10, 10, 10, 10));

            Assert.That(original.Authority, Is.EqualTo(StatBounds.Initial));
        }

        [Test]
        public void Constructor_ClampsValuesOutsideTheLegalRange()
        {
            StatValues stats = new StatValues(-40, 140, StatBounds.Min, StatBounds.Max);

            Assert.That(stats.Authority, Is.EqualTo(StatBounds.Min));
            Assert.That(stats.People, Is.EqualTo(StatBounds.Max));
            Assert.That(stats.Security, Is.EqualTo(StatBounds.Min));
            Assert.That(stats.Wealth, Is.EqualTo(StatBounds.Max));
        }

        [TestCase(StatType.Authority, 10)]
        [TestCase(StatType.People, 20)]
        [TestCase(StatType.Security, 30)]
        [TestCase(StatType.Wealth, 40)]
        public void Indexer_ReturnsTheMatchingStat(StatType stat, int expected)
        {
            StatValues stats = new StatValues(10, 20, 30, 40);

            Assert.That(stats[stat], Is.EqualTo(expected));
        }

        [Test]
        public void Indexer_RejectsUnknownStat()
        {
            StatValues stats = StatValues.CreateInitial();

            Assert.That(() => stats[(StatType)99], Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(StatType.Authority)]
        [TestCase(StatType.People)]
        [TestCase(StatType.Security)]
        [TestCase(StatType.Wealth)]
        public void With_ReplacesOnlyTheNamedStat(StatType stat)
        {
            StatValues stats = StatValues.CreateInitial().With(stat, 7);

            Assert.That(stats[stat], Is.EqualTo(7));

            foreach (StatType other in (StatType[])Enum.GetValues(typeof(StatType)))
            {
                if (other != stat)
                {
                    Assert.That(stats[other], Is.EqualTo(StatBounds.Initial));
                }
            }
        }

        [Test]
        public void With_ClampsTheReplacementValue()
        {
            Assert.That(StatValues.CreateInitial().With(StatType.People, 999).People,
                Is.EqualTo(StatBounds.Max));
            Assert.That(StatValues.CreateInitial().With(StatType.People, -999).People,
                Is.EqualTo(StatBounds.Min));
        }

        [TestCase(StatType.Authority)]
        [TestCase(StatType.People)]
        [TestCase(StatType.Security)]
        [TestCase(StatType.Wealth)]
        public void IsAtMin_IsTrueOnlyWhenTheStatBottomsOut(StatType stat)
        {
            Assert.That(StatValues.CreateInitial().IsAtMin(stat), Is.False);
            Assert.That(StatValues.CreateInitial().With(stat, StatBounds.Min).IsAtMin(stat), Is.True);
        }

        [TestCase(StatType.Authority)]
        [TestCase(StatType.People)]
        [TestCase(StatType.Security)]
        [TestCase(StatType.Wealth)]
        public void IsAtMax_IsTrueOnlyWhenTheStatTopsOut(StatType stat)
        {
            Assert.That(StatValues.CreateInitial().IsAtMax(stat), Is.False);
            Assert.That(StatValues.CreateInitial().With(stat, StatBounds.Max).IsAtMax(stat), Is.True);
        }

        [Test]
        public void Sanitized_ClampsValuesInjectedByDeserialization()
        {
            // JsonUtility writes the backing fields directly, bypassing the clamping constructor,
            // which is exactly how a corrupt or hand-edited save reaches the game.
            StatValues loaded = JsonUtility.FromJson<StatValues>(
                "{\"authority\":9999,\"people\":-9999,\"security\":50,\"wealth\":101}");

            Assert.That(loaded.Authority, Is.EqualTo(9999), "precondition: deserialisation bypasses clamping");

            StatValues sanitized = loaded.Sanitized();

            Assert.That(sanitized.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(sanitized.People, Is.EqualTo(StatBounds.Min));
            Assert.That(sanitized.Security, Is.EqualTo(50));
            Assert.That(sanitized.Wealth, Is.EqualTo(StatBounds.Max));
        }
    }
}
