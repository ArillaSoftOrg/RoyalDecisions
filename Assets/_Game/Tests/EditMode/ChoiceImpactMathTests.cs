using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    public sealed class ChoiceImpactMathTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(-5, 1)]
        [TestCase(6, 2)]
        [TestCase(-12, 2)]
        [TestCase(13, 3)]
        [TestCase(-15, 3)]
        [TestCase(int.MinValue, 3)]
        public void MagnitudeUsesAuthoredDeltaBoundaries(int delta, int expected)
        {
            Assert.That(ChoiceImpactMath.MagnitudeLevel(delta), Is.EqualTo(expected));
        }

        [TestCase(5, "▲")]
        [TestCase(10, "▲▲")]
        [TestCase(15, "▲▲▲")]
        [TestCase(-5, "▼")]
        [TestCase(-10, "▼▼")]
        [TestCase(-15, "▼▼▼")]
        [TestCase(0, "")]
        public void FormatShowsDirectionWithoutExactValue(int delta, string expected)
        {
            Assert.That(ChoiceImpactMath.Format(delta, "▲", "▼"), Is.EqualTo(expected));
        }
    }
}
