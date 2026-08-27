using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class StartupLoadingProgressMathTests
    {
        // --- ClampProgress ----------------------------------------------------

        [TestCase(-5f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f, 1f)]
        [TestCase(5f, 1f)]
        public void ClampProgress_ClampsToUnitRange(float input, float expected)
        {
            Assert.That(StartupLoadingProgressMath.ClampProgress(input), Is.EqualTo(expected));
        }

        // --- AdvanceDisplayed ---------------------------------------------------

        [Test]
        public void AdvanceDisplayed_MovesTowardsTargetWithoutOvershooting()
        {
            float result = StartupLoadingProgressMath.AdvanceDisplayed(
                displayedProgress: 0.25f, targetProgress: 0.70f, maxDeltaPerSecond: 1f, deltaSeconds: 0.1f);

            Assert.That(result, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(result, Is.LessThan(0.70f));
        }

        [Test]
        public void AdvanceDisplayed_NeverOvershootsTheTarget()
        {
            float result = StartupLoadingProgressMath.AdvanceDisplayed(
                displayedProgress: 0.25f, targetProgress: 0.30f, maxDeltaPerSecond: 5f, deltaSeconds: 1f);

            Assert.That(result, Is.EqualTo(0.30f).Within(0.0001f));
        }

        [Test]
        public void AdvanceDisplayed_ClampsOutOfRangeInputsFirst()
        {
            float result = StartupLoadingProgressMath.AdvanceDisplayed(
                displayedProgress: -1f, targetProgress: 2f, maxDeltaPerSecond: 10f, deltaSeconds: 10f);

            Assert.That(result, Is.EqualTo(1f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void AdvanceDisplayed_ZeroOrNegativeDelta_LeavesDisplayedUnchanged(float deltaSeconds)
        {
            float result = StartupLoadingProgressMath.AdvanceDisplayed(
                displayedProgress: 0.4f, targetProgress: 1f, maxDeltaPerSecond: 1f, deltaSeconds: deltaSeconds);

            Assert.That(result, Is.EqualTo(0.4f));
        }

        [Test]
        public void AdvanceDisplayed_ZeroSpeed_LeavesDisplayedUnchanged()
        {
            float result = StartupLoadingProgressMath.AdvanceDisplayed(
                displayedProgress: 0.4f, targetProgress: 1f, maxDeltaPerSecond: 0f, deltaSeconds: 1f);

            Assert.That(result, Is.EqualTo(0.4f));
        }

        // --- PercentageFor ------------------------------------------------------

        [TestCase(0f, 0)]
        [TestCase(0.004f, 0)]
        [TestCase(0.5f, 50)]
        [TestCase(0.999f, 100)]
        [TestCase(1f, 100)]
        [TestCase(-1f, 0)]
        [TestCase(2f, 100)]
        public void PercentageFor_RoundsAndClampsToWholePercent(float progress, int expected)
        {
            Assert.That(StartupLoadingProgressMath.PercentageFor(progress), Is.EqualTo(expected));
        }

        // --- ShouldBeginFadeOut ---------------------------------------------------

        [Test]
        public void ShouldBeginFadeOut_FalseWithoutCompletionRequested()
        {
            bool result = StartupLoadingProgressMath.ShouldBeginFadeOut(
                completionRequested: false, displayedProgress: 1f, elapsedSeconds: 100f, minimumDisplaySeconds: 0f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldBeginFadeOut_FalseWhileDisplayedProgressHasNotCaughtUp()
        {
            bool result = StartupLoadingProgressMath.ShouldBeginFadeOut(
                completionRequested: true, displayedProgress: 0.98f, elapsedSeconds: 100f, minimumDisplaySeconds: 0f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldBeginFadeOut_FalseBeforeMinimumDisplayDurationElapses()
        {
            bool result = StartupLoadingProgressMath.ShouldBeginFadeOut(
                completionRequested: true, displayedProgress: 1f, elapsedSeconds: 0.5f, minimumDisplaySeconds: 1.75f);

            Assert.That(result, Is.False,
                "A near-instant startup must not flash 0->100 in one frame.");
        }

        [Test]
        public void ShouldBeginFadeOut_TrueOnceEveryConditionIsMet()
        {
            bool result = StartupLoadingProgressMath.ShouldBeginFadeOut(
                completionRequested: true, displayedProgress: 1f, elapsedSeconds: 2f, minimumDisplaySeconds: 1.75f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldBeginFadeOut_NegativeMinimumDisplayIsTreatedAsZero()
        {
            bool result = StartupLoadingProgressMath.ShouldBeginFadeOut(
                completionRequested: true, displayedProgress: 1f, elapsedSeconds: 0f, minimumDisplaySeconds: -5f);

            Assert.That(result, Is.True);
        }
    }
}
