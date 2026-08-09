using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    public sealed class ResponsiveCardLayoutMathTests
    {
        [TestCase(1080f, 1608f)]
        [TestCase(886f, 1608f)]
        [TestCase(823f, 1608f)]
        public void TargetPortraitRatiosUseSeventyEightPercentWidthWhenHeightAllows(
            float width, float height)
        {
            Vector2 result = ResponsiveCardLayoutMath.Calculate(
                width, new Vector2(width, height), 0.78f, 0.68f, 0.94f, 920f);

            Assert.That(result.x / width, Is.EqualTo(0.78f).Within(0.001f));
            Assert.That(result.x / result.y, Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void HeightConstraintShrinksWidthAndPreservesAspect()
        {
            Vector2 result = ResponsiveCardLayoutMath.Calculate(
                new Vector2(1080f, 600f), 0.76f, 0.68f, 0.94f);

            Assert.That(result.y, Is.EqualTo(564f).Within(0.001f));
            Assert.That(result.x / result.y, Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void TabletWidthIsCappedWithoutChangingAspect()
        {
            Vector2 result = ResponsiveCardLayoutMath.Calculate(
                1440f, new Vector2(1400f, 1800f), 0.78f, 0.68f, 0.94f, 920f);

            Assert.That(result.x, Is.EqualTo(920f).Within(0.001f));
            Assert.That(result.x / result.y, Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void CardNeverExceedsTheAvailableAreaWidth()
        {
            Vector2 result = ResponsiveCardLayoutMath.Calculate(
                1080f, new Vector2(700f, 1600f), 0.78f, 0.68f, 0.94f, 920f);

            Assert.That(result.x, Is.EqualTo(700f).Within(0.001f));
        }

        [Test]
        public void InvalidDimensionsReturnNonNegativeSize()
        {
            Vector2 result = ResponsiveCardLayoutMath.Calculate(
                new Vector2(-10f, -20f), 0.76f, 0f, 0.94f);

            Assert.That(result, Is.EqualTo(Vector2.zero));
        }
    }
}
