using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    public sealed class FooterPresentationMathTests
    {
        [TestCase(-10, 1)]
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(9, 10)]
        public void ReignYearIsOneBasedAndClamped(int turn, int expected)
        {
            Assert.That(FooterPresentationMath.ToReignYear(turn), Is.EqualTo(expected));
        }

        [Test]
        public void EmptyFormatUsesSafeDefault()
        {
            Assert.That(FooterPresentationMath.FormatReign(2, string.Empty),
                Is.EqualTo("Reign Year 3"));
        }
    }
}
