using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class StatDisplayMathTests
    {
        [TestCase(0, 0f)]
        [TestCase(50, 0.5f)]
        [TestCase(100, 1f)]
        [TestCase(25, 0.25f)]
        public void ValuesMapOntoTheirFraction(int value, float expected)
        {
            Assert.That(StatDisplayMath.ToFill(value), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(-50)]
        [TestCase(int.MinValue)]
        public void ValuesBelowRangeClampToEmpty(int value)
        {
            Assert.That(StatDisplayMath.ToFill(value), Is.EqualTo(0f));
        }

        [TestCase(500)]
        [TestCase(int.MaxValue)]
        public void ValuesAboveRangeClampToFull(int value)
        {
            Assert.That(StatDisplayMath.ToFill(value), Is.EqualTo(1f));
        }

        [TestCase(StatType.Authority)]
        [TestCase(StatType.People)]
        [TestCase(StatType.Security)]
        [TestCase(StatType.Wealth)]
        public void EveryStatIsReadable(StatType stat)
        {
            StatValues values = StatValues.CreateInitial().With(stat, 75);

            Assert.That(StatDisplayMath.ToFill(values, stat), Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void ReadingAStatDoesNotChangeIt()
        {
            StatValues values = StatValues.CreateInitial();

            StatDisplayMath.ToFill(values, StatType.Authority);
            StatDisplayMath.ToFill(values, StatType.Wealth);

            Assert.That(values.Authority, Is.EqualTo(StatBounds.Initial));
            Assert.That(values.Wealth, Is.EqualTo(StatBounds.Initial));
        }

        [TestCase(0, 1f)]
        [TestCase(100, 1f)]
        [TestCase(50, 0f)]
        [TestCase(75, 0.5f)]
        public void BoundaryProximityPeaksAtBothEnds(int value, float expected)
        {
            Assert.That(
                StatDisplayMath.BoundaryProximity(value),
                Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
