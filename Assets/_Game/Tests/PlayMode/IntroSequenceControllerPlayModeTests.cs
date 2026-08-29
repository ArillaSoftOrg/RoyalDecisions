using System.Collections;
using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Covers the parts of the intro's skip handling that only exist while frames are running: the
    /// short window where skip is deliberately ignored, and that a tap past it still hands off and
    /// completes exactly once.
    /// </summary>
    /// <remarks>
    /// The exactly-once/FadeOutStarted guarantees themselves are proven in EditMode (they hold
    /// synchronously there too, since Play() resolves through Complete() immediately outside Play
    /// Mode). What is left here is genuinely temporal: real unscaled time has to elapse for the lock
    /// window to matter at all. The lock itself is shortened via
    /// <see cref="IntroSequenceController.SetSkipLockSecondsForTesting"/> so these stay fast; the
    /// final wait for completion is bounded by a frame budget so a hang fails with a message
    /// instead of stalling the suite. Every test asserts arrival, never exact frame counts.
    /// </remarks>
    [TestFixture]
    public class IntroSequenceControllerPlayModeTests
    {
        private const int FrameBudget = 600;
        private const float ShortLockSeconds = 0.15f;

        private GameObject root;
        private IntroSequenceController controller;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Canvas");
            root.AddComponent<Canvas>();

            RectTransform logoGroup = Child(root.transform, "LogoGroup");
            CanvasGroup canvasGroup = logoGroup.gameObject.AddComponent<CanvasGroup>();

            RectTransform markRect = Child(logoGroup, "Mark");
            Image mark = markRect.gameObject.AddComponent<Image>();
            mark.sprite = CreateSprite();

            RectTransform wordmarkRect = Child(logoGroup, "Wordmark");
            Image wordmark = wordmarkRect.gameObject.AddComponent<Image>();
            wordmark.sprite = CreateSprite();

            RectTransform maskRect = Child(logoGroup, "RevealMask");

            controller = Child(root.transform, "IntroController").gameObject
                .AddComponent<IntroSequenceController>();
            controller.SetAuthoringReferences(canvasGroup, logoGroup, mark);
            controller.SetWordmarkAuthoringReferences(wordmark, maskRect, null);
            controller.SetSkipLockSecondsForTesting(ShortLockSeconds);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }

        [UnityTest]
        public IEnumerator Skip_DuringLockWindow_IsIgnored()
        {
            bool completed = false;
            controller.Play(() => completed = true);

            yield return null;
            controller.Skip();
            yield return null;

            Assert.That(completed, Is.False,
                "a tap immediately after the intro starts must be ignored, not dismiss it");
            Assert.That(controller.HasCompleted, Is.False);
        }

        [UnityTest]
        public IEnumerator Skip_AfterLockWindow_CompletesTheIntroExactlyOnce()
        {
            int completedCount = 0;
            int fadeOutStartedCount = 0;
            controller.FadeOutStarted += () => fadeOutStartedCount++;
            controller.Play(() => completedCount++);

            yield return new WaitForSecondsRealtime(ShortLockSeconds + 0.05f);

            controller.Skip();

            yield return WaitUntilCompleted();

            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(fadeOutStartedCount, Is.EqualTo(1),
                "FadeOutStarted must fire exactly once as part of the skip handoff.");

            // A second tap after completion must not do anything further.
            controller.Skip();
            yield return null;
            Assert.That(completedCount, Is.EqualTo(1));
        }

        /// <summary>Waits for the controller to complete, failing rather than hanging.</summary>
        private IEnumerator WaitUntilCompleted()
        {
            int frames = 0;

            while (!controller.HasCompleted && frames < FrameBudget)
            {
                frames++;
                yield return null;
            }

            Assert.That(controller.HasCompleted, Is.True,
                "intro did not complete within " + FrameBudget + " frames after a valid skip");
        }

        private static RectTransform Child(Transform parentTransform, string name)
        {
            GameObject child = new GameObject(name);
            RectTransform rect = child.AddComponent<RectTransform>();
            rect.SetParent(parentTransform, false);
            return rect;
        }

        /// <summary>A 2x2 sprite. Content, never appearance — nothing asserts what it looks like.</summary>
        private static Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(2, 2);
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
        }
    }
}
