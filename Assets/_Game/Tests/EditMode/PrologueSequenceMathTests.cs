using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class PrologueSequenceMathTests
    {
        // --- HasSlides / ClampSlideIndex ----------------------------------------------

        [TestCase(0, false)]
        [TestCase(-1, false)]
        [TestCase(1, true)]
        [TestCase(5, true)]
        public void HasSlides_ReflectsSlideCount(int slideCount, bool expected)
        {
            Assert.That(PrologueSequenceMath.HasSlides(slideCount), Is.EqualTo(expected));
        }

        [Test]
        public void ClampSlideIndex_EmptySequence_ReturnsZero()
        {
            Assert.That(PrologueSequenceMath.ClampSlideIndex(3, 0), Is.EqualTo(0));
        }

        [TestCase(-5, 5, 0)]
        [TestCase(0, 5, 0)]
        [TestCase(2, 5, 2)]
        [TestCase(4, 5, 4)]
        [TestCase(9, 5, 4)]
        public void ClampSlideIndex_ClampsIntoValidRange(int index, int slideCount, int expected)
        {
            Assert.That(PrologueSequenceMath.ClampSlideIndex(index, slideCount), Is.EqualTo(expected));
        }

        // --- IsLastSlide ----------------------------------------------------------------

        [Test]
        public void IsLastSlide_EmptySequence_IsFalse()
        {
            Assert.That(PrologueSequenceMath.IsLastSlide(0, 0), Is.False);
        }

        [TestCase(3, 5, false)]
        [TestCase(4, 5, true)]
        [TestCase(0, 1, true)]
        public void IsLastSlide_MatchesFinalIndex(int index, int slideCount, bool expected)
        {
            Assert.That(PrologueSequenceMath.IsLastSlide(index, slideCount), Is.EqualTo(expected));
        }

        // --- NextSlideIndexOrCompletion ---------------------------------------------------

        [Test]
        public void NextSlideIndexOrCompletion_EmptySequence_SignalsCompletion()
        {
            Assert.That(PrologueSequenceMath.NextSlideIndexOrCompletion(0, 0), Is.EqualTo(-1));
        }

        [Test]
        public void NextSlideIndexOrCompletion_AdvancesThroughEveryFiveSampleSlides()
        {
            const int slideCount = 5;
            int index = 0;

            for (int expectedNext = 1; expectedNext < slideCount; expectedNext++)
            {
                int next = PrologueSequenceMath.NextSlideIndexOrCompletion(index, slideCount);
                Assert.That(next, Is.EqualTo(expectedNext));
                index = next;
            }

            Assert.That(PrologueSequenceMath.NextSlideIndexOrCompletion(index, slideCount), Is.EqualTo(-1),
                "Tapping past the final slide must signal completion, not wrap or overrun.");
        }

        // --- FadeInAlpha ---------------------------------------------------------------

        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f, 1f)]
        [TestCase(2f, 1f)]
        public void FadeInAlpha_NoDelay_RampsLinearlyThenClamps(float elapsed, float expected)
        {
            Assert.That(PrologueSequenceMath.FadeInAlpha(elapsed, 0f, 1f), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void FadeInAlpha_BeforeDelayElapses_IsZero()
        {
            Assert.That(PrologueSequenceMath.FadeInAlpha(0.1f, 0.2f, 0.4f), Is.EqualTo(0f));
        }

        [Test]
        public void FadeInAlpha_AfterDelayAndDuration_IsOne()
        {
            Assert.That(PrologueSequenceMath.FadeInAlpha(1f, 0.2f, 0.4f), Is.EqualTo(1f));
        }

        [Test]
        public void FadeInAlpha_ZeroDuration_IsAnImmediateStepAtTheDelay()
        {
            Assert.That(PrologueSequenceMath.FadeInAlpha(0.19f, 0.2f, 0f), Is.EqualTo(0f));
            Assert.That(PrologueSequenceMath.FadeInAlpha(0.2f, 0.2f, 0f), Is.EqualTo(1f));
        }
    }
}
