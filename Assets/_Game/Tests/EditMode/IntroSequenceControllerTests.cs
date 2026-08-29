using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class IntroSequenceControllerTests
    {
        private static IntroSequenceController Build(bool withSprite = true)
        {
            GameObject logo = PresentationTestObjects.CreateObject("Logo");
            CanvasGroup canvasGroup = logo.AddComponent<CanvasGroup>();
            Image image = logo.AddComponent<Image>();
            if (withSprite)
            {
                image.sprite = PresentationTestObjects.CreateSprite("Logo");
            }

            IntroSequenceController controller =
                PresentationTestObjects.CreateComponent<IntroSequenceController>("Intro");
            controller.SetAuthoringReferences(canvasGroup, logo.GetComponent<RectTransform>(), image);
            return controller;
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void Play_OutsidePlayMode_CompletesImmediately()
        {
            IntroSequenceController controller = Build();
            bool completed = false;

            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
            Assert.That(controller.HasCompleted, Is.True);
        }

        [Test]
        public void Play_WithoutLogoSprite_StillCompletesSafely()
        {
            IntroSequenceController controller = Build(withSprite: false);
            bool completed = false;

            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void Play_WithNoReferencesAssigned_StillCompletesSafely()
        {
            IntroSequenceController controller =
                PresentationTestObjects.CreateComponent<IntroSequenceController>("Intro");
            bool completed = false;

            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void Play_CalledTwice_InvokesEachCallersCallbackExactlyOnce()
        {
            IntroSequenceController controller = Build();
            int firstCount = 0;
            int secondCount = 0;

            controller.Play(() => firstCount++);
            controller.Play(() => secondCount++);

            Assert.That(firstCount, Is.EqualTo(1),
                "The first caller's callback must still fire exactly once.");
            Assert.That(secondCount, Is.EqualTo(1),
                "A second Play() call must not silently drop its caller's callback, even though " +
                "no second sequence starts.");
        }

        [Test]
        public void Play_CalledManyTimes_NeverInvokesAnyCallbackMoreThanOnce()
        {
            IntroSequenceController controller = Build();
            int totalInvocations = 0;

            for (int i = 0; i < 5; i++)
            {
                controller.Play(() => totalInvocations++);
            }

            Assert.That(totalInvocations, Is.EqualTo(5));
        }

        [Test]
        public void Skip_AfterCompletion_DoesNotInvokeCompletionAgain()
        {
            IntroSequenceController controller = Build();
            int completedCount = 0;
            controller.Play(() => completedCount++);

            controller.Skip();
            controller.Skip();

            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void Skip_CalledBeforePlay_CompletesExactlyOnceWithNoCallbackYet()
        {
            IntroSequenceController controller = Build();

            controller.Skip();

            Assert.That(controller.HasCompleted, Is.True);
        }

        [Test]
        public void Play_AfterSkipCalledFirst_StillResolvesItsOwnCallback()
        {
            // A tap could in principle reach the click-catcher before Play() has run (e.g. a
            // misbehaving caller invoking Skip programmatically first). Play() must not then start
            // a real sequence whose completion is already suppressed, and must not hang forever —
            // its own callback still has to fire.
            IntroSequenceController controller = Build();
            controller.Skip();
            bool completed = false;

            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void SetReducedMotion_ThenPlay_StillCompletesSafely()
        {
            IntroSequenceController controller = Build();
            bool completed = false;

            controller.SetReducedMotion(true);
            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void SetReducedMotion_ToggledBackOff_StillCompletesSafely()
        {
            IntroSequenceController controller = Build();
            bool completed = false;

            controller.SetReducedMotion(true);
            controller.SetReducedMotion(false);
            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void Play_OutsidePlayMode_FiresFadeOutStartedExactlyOnceBeforeComplete()
        {
            // Outside Play Mode CanAnimate() is false, so Play() resolves synchronously through
            // Complete() without ever running the real fade-out coroutine. Complete() itself still
            // guarantees FadeOutStarted fires — exactly once, and strictly before onComplete — so
            // callers relying on FadeOutStarted to reveal what comes next are never left waiting.
            IntroSequenceController controller = Build();
            int fadeOutStartedCount = 0;
            bool fadeOutStartedBeforeComplete = false;
            controller.FadeOutStarted += () =>
            {
                fadeOutStartedCount++;
                fadeOutStartedBeforeComplete = !controller.HasCompleted;
            };

            controller.Play(() => { });

            Assert.That(fadeOutStartedCount, Is.EqualTo(1));
            Assert.That(fadeOutStartedBeforeComplete, Is.True,
                "FadeOutStarted must fire before HasCompleted becomes true, not after.");
        }

        [Test]
        public void Skip_CalledBeforePlay_FiresFadeOutStartedExactlyOnce()
        {
            IntroSequenceController controller = Build();
            int fadeOutStartedCount = 0;
            controller.FadeOutStarted += () => fadeOutStartedCount++;

            controller.Skip();

            Assert.That(fadeOutStartedCount, Is.EqualTo(1));
        }

        [Test]
        public void FadeOutStarted_NeverFiresTwice_AcrossRepeatedSkipCalls()
        {
            IntroSequenceController controller = Build();
            int fadeOutStartedCount = 0;
            controller.FadeOutStarted += () => fadeOutStartedCount++;

            controller.Play(() => { });
            controller.Skip();
            controller.Skip();
            controller.Skip();

            Assert.That(fadeOutStartedCount, Is.EqualTo(1));
        }
    }
}
