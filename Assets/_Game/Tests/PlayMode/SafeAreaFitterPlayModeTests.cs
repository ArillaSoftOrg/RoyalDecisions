using System.Collections;
using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Covers the parts of Safe Area handling that only exist while frames are running.
    /// </summary>
    [TestFixture]
    public class SafeAreaFitterPlayModeTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.Destroy(host);
                host = null;
            }
        }

        private SafeAreaFitter CreateFitter()
        {
            host = new GameObject("SafeArea");
            RectTransform rect = host.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            return host.AddComponent<SafeAreaFitter>();
        }

        [UnityTest]
        public IEnumerator AppliesTheScreenSafeAreaOnEnable()
        {
            SafeAreaFitter fitter = CreateFitter();
            yield return null;

            Assert.That(fitter.Target, Is.Not.Null, "it should default to its own RectTransform");
            Assert.That(fitter.Target.anchorMin.x, Is.InRange(0f, 1f));
            Assert.That(fitter.Target.anchorMax.x, Is.InRange(0f, 1f));
            Assert.That(fitter.Target.anchorMax.y, Is.GreaterThan(fitter.Target.anchorMin.y));
        }

        [UnityTest]
        public IEnumerator DoesNoWorkWhenNothingHasChanged()
        {
            SafeAreaFitter fitter = CreateFitter();
            yield return null;

            // The screen cannot change between frames in a batch-mode run, so a second pass must
            // report that it had nothing to do.
            Assert.That(fitter.ApplyIfChanged(), Is.False);

            yield return null;
            Assert.That(fitter.ApplyIfChanged(), Is.False);
        }

        [UnityTest]
        public IEnumerator AppliesAnExplicitSafeAreaAndThenDetectsAChange()
        {
            SafeAreaFitter fitter = CreateFitter();
            yield return null;

            Assert.That(fitter.ApplyTo(new Rect(0f, 100f, 1080f, 2200f), 1080, 2400), Is.True);
            Assert.That(fitter.Target.anchorMin.y, Is.EqualTo(100f / 2400f).Within(0.0001f));

            // A different safe area must be picked up rather than suppressed by the change check.
            Assert.That(fitter.ApplyTo(new Rect(0f, 0f, 1080f, 2400f), 1080, 2400), Is.True);
            Assert.That(fitter.Target.anchorMin.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator OffsetsAreZeroedSoTheRectFillsItsAnchors()
        {
            SafeAreaFitter fitter = CreateFitter();
            fitter.Target.offsetMin = new Vector2(25f, 25f);
            fitter.Target.offsetMax = new Vector2(-25f, -25f);

            yield return null;
            fitter.ApplyTo(new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);

            Assert.That(fitter.Target.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(fitter.Target.offsetMax, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator InvalidDimensionsLeaveTheRectAlone()
        {
            SafeAreaFitter fitter = CreateFitter();
            yield return null;

            fitter.ApplyTo(new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);
            Vector2 before = fitter.Target.anchorMin;

            Assert.That(fitter.ApplyTo(new Rect(0f, 0f, 1080f, 2400f), 0, 0), Is.False);
            Assert.That(fitter.Target.anchorMin, Is.EqualTo(before));
        }
    }
}
