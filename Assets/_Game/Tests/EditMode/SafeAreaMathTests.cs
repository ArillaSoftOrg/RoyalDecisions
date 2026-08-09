using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class SafeAreaMathTests
    {
        private const int Width = 1080;
        private const int Height = 2400;

        private static bool Calculate(Rect safeArea, out Vector2 min, out Vector2 max)
        {
            return SafeAreaMath.TryCalculateAnchors(safeArea, Width, Height, out min, out max);
        }

        [Test]
        public void AFullScreenSafeAreaFillsTheScreen()
        {
            bool ok = Calculate(new Rect(0f, 0f, Width, Height), out Vector2 min, out Vector2 max);

            Assert.That(ok, Is.True);
            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void ATopNotchLowersTheUpperAnchor()
        {
            // 120px cut from the top: the safe area starts at y=0 and stops short of the top.
            bool ok = Calculate(new Rect(0f, 0f, Width, Height - 120f), out Vector2 min, out Vector2 max);

            Assert.That(ok, Is.True);
            Assert.That(min.y, Is.EqualTo(0f));
            Assert.That(max.y, Is.EqualTo((Height - 120f) / Height).Within(0.0001f));
            Assert.That(max.y, Is.LessThan(1f));
        }

        [Test]
        public void ABottomHomeIndicatorRaisesTheLowerAnchor()
        {
            bool ok = Calculate(new Rect(0f, 60f, Width, Height - 60f), out Vector2 min, out Vector2 max);

            Assert.That(ok, Is.True);
            Assert.That(min.y, Is.EqualTo(60f / Height).Within(0.0001f));
            Assert.That(max.y, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ANotchAndAnIndicatorInsetBothEnds()
        {
            bool ok = Calculate(new Rect(0f, 60f, Width, Height - 180f), out Vector2 min, out Vector2 max);

            Assert.That(ok, Is.True);
            Assert.That(min.y, Is.GreaterThan(0f));
            Assert.That(max.y, Is.LessThan(1f));
            Assert.That(max.y, Is.GreaterThan(min.y));
        }

        [Test]
        public void ALandscapeStyleSideCutoutInsetsHorizontally()
        {
            bool ok = Calculate(new Rect(40f, 0f, Width - 80f, Height), out Vector2 min, out Vector2 max);

            Assert.That(ok, Is.True);
            Assert.That(min.x, Is.EqualTo(40f / Width).Within(0.0001f));
            Assert.That(max.x, Is.EqualTo((Width - 40f) / Width).Within(0.0001f));
        }

        [TestCase(0, 2400)]
        [TestCase(1080, 0)]
        [TestCase(-1080, 2400)]
        [TestCase(0, 0)]
        public void InvalidScreenDimensionsAreRefused(int width, int height)
        {
            bool ok = SafeAreaMath.TryCalculateAnchors(
                new Rect(0f, 0f, 100f, 100f), width, height, out Vector2 min, out Vector2 max);

            Assert.That(ok, Is.False, "no divide-by-zero, no inverted anchors");
            Assert.That(min, Is.EqualTo(Vector2.zero), "anchors stay at full screen");
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void AnEmptySafeAreaIsRefused()
        {
            Assert.That(Calculate(new Rect(0f, 0f, 0f, 0f), out _, out _), Is.False);
            Assert.That(Calculate(new Rect(100f, 100f, 0f, 50f), out _, out _), Is.False);
        }

        [Test]
        public void ASafeAreaLargerThanTheScreenIsClamped()
        {
            bool ok = Calculate(new Rect(-50f, -50f, Width + 500f, Height + 500f),
                out Vector2 min, out Vector2 max);

            Assert.That(ok, Is.True);
            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void ASafeAreaEntirelyOffScreenIsRefused()
        {
            // Clamping collapses this to a zero-width band, which must not become valid anchors.
            Assert.That(
                Calculate(new Rect(-500f, 0f, 200f, Height), out _, out _),
                Is.False);
        }
    }
}
