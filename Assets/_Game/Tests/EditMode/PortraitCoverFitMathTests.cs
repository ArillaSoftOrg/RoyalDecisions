using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class PortraitCoverFitMathTests
    {
        // The real card: a 1024x1536 (2:3, aspect 0.667) portrait inside a slightly wider mask
        // region (aspect ~0.707), matching PortraitRegion's authored anchors in
        // SceneSetupAutomation.ConfigureCard.
        private static readonly Vector2 MaskSize = new Vector2(660f, 934f);
        private const float PortraitAspect = 1024f / 1536f;

        [Test]
        public void ANarrowerPortraitMatchesContainerWidthAndOverflowsHeight()
        {
            Vector2 size = PortraitCoverFitMath.ComputeCoverSize(MaskSize, PortraitAspect);

            Assert.That(size.x, Is.EqualTo(MaskSize.x).Within(0.001f));
            Assert.That(size.y, Is.GreaterThan(MaskSize.y),
                "a portrait narrower than its frame must overflow vertically, not shrink to fit");
        }

        [Test]
        public void AWiderImageMatchesContainerHeightAndOverflowsWidth()
        {
            const float wideAspect = 2f; // wider than any square/portrait container
            Vector2 size = PortraitCoverFitMath.ComputeCoverSize(MaskSize, wideAspect);

            Assert.That(size.y, Is.EqualTo(MaskSize.y).Within(0.001f));
            Assert.That(size.x, Is.GreaterThan(MaskSize.x));
        }

        [Test]
        public void TheComputedSizeAlwaysMatchesTheSourceAspectRatio()
        {
            Vector2 size = PortraitCoverFitMath.ComputeCoverSize(MaskSize, PortraitAspect);

            Assert.That(size.x / size.y, Is.EqualTo(PortraitAspect).Within(0.0001f),
                "cover-fit must never distort the source image's own proportions");
        }

        [Test]
        public void AnExactAspectMatchNeitherGrowsNorShrinksEitherAxis()
        {
            float containerAspect = MaskSize.x / MaskSize.y;
            Vector2 size = PortraitCoverFitMath.ComputeCoverSize(MaskSize, containerAspect);

            Assert.That(size.x, Is.EqualTo(MaskSize.x).Within(0.001f));
            Assert.That(size.y, Is.EqualTo(MaskSize.y).Within(0.001f));
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 0f)]
        [TestCase(-10f, 100f)]
        public void AnUnlaidOutOrInvalidContainerFallsBackToTheContainerSizeRatherThanNaN(
            float width, float height)
        {
            Vector2 container = new Vector2(width, height);
            Vector2 size = PortraitCoverFitMath.ComputeCoverSize(container, PortraitAspect);

            Assert.That(size, Is.EqualTo(container),
                "zero/negative input must degrade safely instead of producing NaN or infinity");
        }

        [Test]
        public void AZeroOrNegativeSpriteAspectFallsBackToTheContainerSize()
        {
            Vector2 size = PortraitCoverFitMath.ComputeCoverSize(MaskSize, 0f);

            Assert.That(size, Is.EqualTo(MaskSize));
        }
    }
}
