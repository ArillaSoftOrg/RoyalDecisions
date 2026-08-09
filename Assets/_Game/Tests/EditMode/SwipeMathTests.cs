using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class SwipeMathTests
    {
        private const float ParentWidth = 1000f;
        private const float Ratio = 0.25f;
        private const float Minimum = 40f;
        private const float Threshold = 250f; // ParentWidth * Ratio

        // --- Threshold -------------------------------------------------------

        [Test]
        public void ThresholdIsAFractionOfTheParentWidth()
        {
            Assert.That(
                SwipeMath.ThresholdDistance(ParentWidth, Ratio, Minimum),
                Is.EqualTo(Threshold).Within(0.001f));
        }

        [Test]
        public void ThresholdScalesWithTheParentRatherThanPixels()
        {
            float narrow = SwipeMath.ThresholdDistance(600f, Ratio, Minimum);
            float wide = SwipeMath.ThresholdDistance(1200f, Ratio, Minimum);

            Assert.That(wide, Is.EqualTo(narrow * 2f).Within(0.001f),
                "the gesture must feel the same on any screen density");
        }

        [TestCase(0f)]
        [TestCase(10f)]
        public void ANarrowOrUnlaidOutParentFallsBackToTheMinimum(float parentWidth)
        {
            Assert.That(
                SwipeMath.ThresholdDistance(parentWidth, Ratio, Minimum),
                Is.EqualTo(Minimum).Within(0.001f));
        }

        [Test]
        public void TheThresholdIsNeverZero()
        {
            // A zero threshold would make every release a confirmation.
            Assert.That(
                SwipeMath.ThresholdDistance(0f, Ratio, 0f),
                Is.GreaterThanOrEqualTo(SwipeMath.AbsoluteMinimumThreshold));
        }

        // --- Progress -----------------------------------------------------------

        [TestCase(0f, 0f)]
        [TestCase(125f, 0.5f)]
        [TestCase(-125f, 0.5f)]
        [TestCase(250f, 1f)]
        [TestCase(-250f, 1f)]
        [TestCase(9999f, 1f)]
        [TestCase(-9999f, 1f)]
        public void ProgressIsUnsignedAndClamped(float displacement, float expected)
        {
            Assert.That(
                SwipeMath.Progress(displacement, Threshold),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(125f, 0.5f)]
        [TestCase(-125f, -0.5f)]
        [TestCase(9999f, 1f)]
        [TestCase(-9999f, -1f)]
        public void SignedProgressKeepsDirectionAndClamps(float displacement, float expected)
        {
            Assert.That(
                SwipeMath.SignedProgress(displacement, Threshold),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void AZeroThresholdYieldsNoProgressRatherThanInfinity()
        {
            Assert.That(SwipeMath.Progress(100f, 0f), Is.EqualTo(0f));
            Assert.That(SwipeMath.SignedProgress(100f, 0f), Is.EqualTo(0f));
        }

        // --- Previews -------------------------------------------------------------

        [Test]
        public void DraggingLeftDrivesOnlyTheLeftPreview()
        {
            SwipeMath.PreviewStrengths(-125f, Threshold, out float left, out float right);

            Assert.That(left, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(right, Is.EqualTo(0f));
        }

        [Test]
        public void DraggingRightDrivesOnlyTheRightPreview()
        {
            SwipeMath.PreviewStrengths(125f, Threshold, out float left, out float right);

            Assert.That(left, Is.EqualTo(0f));
            Assert.That(right, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void AtRestNeitherPreviewShows()
        {
            SwipeMath.PreviewStrengths(0f, Threshold, out float left, out float right);

            Assert.That(left, Is.EqualTo(0f));
            Assert.That(right, Is.EqualTo(0f));
        }

        [TestCase(-99999f)]
        [TestCase(-250f)]
        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(250f)]
        [TestCase(99999f)]
        public void PreviewStrengthsAlwaysStayInRange(float displacement)
        {
            SwipeMath.PreviewStrengths(displacement, Threshold, out float left, out float right);

            Assert.That(left, Is.InRange(0f, 1f));
            Assert.That(right, Is.InRange(0f, 1f));
        }

        // --- Rotation ----------------------------------------------------------------

        [Test]
        public void RotationIsZeroAtRest()
        {
            Assert.That(SwipeMath.Rotation(0f, Threshold, 12f, true), Is.EqualTo(0f));
        }

        [Test]
        public void RotationTakesItsSignFromTheDragDirection()
        {
            float right = SwipeMath.Rotation(125f, Threshold, 12f, true);
            float left = SwipeMath.Rotation(-125f, Threshold, 12f, true);

            Assert.That(right, Is.LessThan(0f), "dragging right tilts clockwise");
            Assert.That(left, Is.GreaterThan(0f));
            Assert.That(right, Is.EqualTo(-left).Within(0.0001f));
        }

        [Test]
        public void RotationClampsAtTheConfiguredMaximum()
        {
            Assert.That(SwipeMath.Rotation(99999f, Threshold, 12f, true),
                Is.EqualTo(-12f).Within(0.0001f));
            Assert.That(SwipeMath.Rotation(-99999f, Threshold, 12f, true),
                Is.EqualTo(12f).Within(0.0001f));
        }

        [Test]
        public void RotationDirectionIsConfigurable()
        {
            float clockwise = SwipeMath.Rotation(125f, Threshold, 12f, true);
            float counter = SwipeMath.Rotation(125f, Threshold, 12f, false);

            Assert.That(counter, Is.EqualTo(-clockwise).Within(0.0001f));
        }

        [Test]
        public void AZeroMaximumProducesNoRotation()
        {
            Assert.That(SwipeMath.Rotation(9999f, Threshold, 0f, true), Is.EqualTo(0f));
        }

        // --- Confirmation ---------------------------------------------------------------

        [TestCase(0f, false)]
        [TestCase(249.9f, false)]
        [TestCase(-249.9f, false)]
        [TestCase(250f, true)]
        [TestCase(-250f, true)]
        [TestCase(400f, true)]
        [TestCase(-400f, true)]
        public void ConfirmationIsInclusiveAtTheThreshold(float displacement, bool expected)
        {
            Assert.That(SwipeMath.IsConfirmed(displacement, Threshold), Is.EqualTo(expected));
        }

        [Test]
        public void AZeroThresholdNeverConfirms()
        {
            Assert.That(SwipeMath.IsConfirmed(500f, 0f), Is.False);
        }

        [TestCase(1f, ChoiceSide.Right)]
        [TestCase(0f, ChoiceSide.Right)]
        [TestCase(-1f, ChoiceSide.Left)]
        public void SideFollowsTheSign(float displacement, ChoiceSide expected)
        {
            Assert.That(SwipeMath.SideFor(displacement), Is.EqualTo(expected));
        }

        // --- Exit target -------------------------------------------------------------------

        [Test]
        public void ExitTargetClearsTheParentOnBothSides()
        {
            const float cardWidth = 600f;
            const float parentHalf = ParentWidth * 0.5f;

            float right = SwipeMath.ExitTargetX(0f, ChoiceSide.Right, ParentWidth, cardWidth, 1f);
            float left = SwipeMath.ExitTargetX(0f, ChoiceSide.Left, ParentWidth, cardWidth, 1f);

            // The card's trailing edge must be past the parent edge, not merely its centre.
            Assert.That(right - (cardWidth * 0.5f), Is.GreaterThan(parentHalf));
            Assert.That(left + (cardWidth * 0.5f), Is.LessThan(-parentHalf));
        }

        [Test]
        public void ExitTargetIsSymmetricAroundTheStartingPosition()
        {
            float right = SwipeMath.ExitTargetX(120f, ChoiceSide.Right, ParentWidth, 600f, 1f);
            float left = SwipeMath.ExitTargetX(120f, ChoiceSide.Left, ParentWidth, 600f, 1f);

            Assert.That(right - 120f, Is.EqualTo(-(left - 120f)).Within(0.001f));
        }

        [Test]
        public void ExitTargetGrowsWithParentAndCardWidth()
        {
            float small = SwipeMath.ExitTargetX(0f, ChoiceSide.Right, 500f, 300f, 1f);
            float large = SwipeMath.ExitTargetX(0f, ChoiceSide.Right, 1500f, 900f, 1f);

            Assert.That(large, Is.GreaterThan(small),
                "the distance is geometry, not a pixel literal");
        }

        [Test]
        public void ANegativeExitMarginIsTreatedAsZero()
        {
            float clamped = SwipeMath.ExitTargetX(0f, ChoiceSide.Right, ParentWidth, 600f, -5f);
            float zero = SwipeMath.ExitTargetX(0f, ChoiceSide.Right, ParentWidth, 600f, 0f);

            Assert.That(clamped, Is.EqualTo(zero).Within(0.001f));
        }
    }
}
