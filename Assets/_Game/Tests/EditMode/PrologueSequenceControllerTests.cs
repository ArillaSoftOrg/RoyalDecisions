using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class PrologueSequenceControllerTests
    {
        private readonly List<PrologueSequenceData> createdData = new List<PrologueSequenceData>();

        private PrologueSequenceController Build(int slideCount = 3, bool withSprites = true)
        {
            PrologueSequenceData data = ScriptableObject.CreateInstance<PrologueSequenceData>();
            createdData.Add(data);

            PrologueSlideData[] slides = new PrologueSlideData[slideCount];
            for (int i = 0; i < slideCount; i++)
            {
                Sprite sprite = withSprites ? PresentationTestObjects.CreateSprite("Slide" + i) : null;
                slides[i] = new PrologueSlideData(sprite, "Subtitle " + i);
            }
            data.SetAuthoringData(slides);

            CanvasGroup storyGroup = PresentationTestObjects.CreateCanvasGroup("StoryGroup");
            TextMeshProUGUI storyText = PresentationTestObjects.CreateText("StoryText");
            Image layerA = PresentationTestObjects.CreateImage("LayerA");
            AspectRatioFitter fitterA = PresentationTestObjects.CreateComponent<AspectRatioFitter>("FitterA");
            Image layerB = PresentationTestObjects.CreateImage("LayerB");
            AspectRatioFitter fitterB = PresentationTestObjects.CreateComponent<AspectRatioFitter>("FitterB");
            TextMeshProUGUI continueText = PresentationTestObjects.CreateText("Continue");
            TextMeshProUGUI skipLabel = PresentationTestObjects.CreateText("SkipLabel");
            CanvasGroup fadeOverlay = PresentationTestObjects.CreateCanvasGroup("FadeOverlay");

            PrologueSequenceController controller =
                PresentationTestObjects.CreateComponent<PrologueSequenceController>("Prologue");
            controller.SetAuthoringReferences(
                data, layerA, fitterA, layerB, fitterB, storyGroup, storyText, continueText, skipLabel,
                fadeOverlay);
            return controller;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (PrologueSequenceData data in createdData)
            {
                if (data != null)
                {
                    Object.DestroyImmediate(data);
                }
            }
            createdData.Clear();

            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void Play_EmptySlideList_FailsOpenAndCompletesImmediately()
        {
            PrologueSequenceController controller = Build(slideCount: 0);
            bool completed = false;

            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
            Assert.That(controller.HasCompleted, Is.True);
        }

        [Test]
        public void Play_MissingDataAsset_FailsOpenAndCompletesImmediately()
        {
            PrologueSequenceController controller =
                PresentationTestObjects.CreateComponent<PrologueSequenceController>("Prologue");
            bool completed = false;

            controller.Play(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void Play_OutsidePlayMode_ShowsFirstSlideWithoutCompleting()
        {
            PrologueSequenceController controller = Build(slideCount: 5);

            controller.Play(() => { });

            Assert.That(controller.CurrentSlideIndex, Is.EqualTo(0));
            Assert.That(controller.HasCompleted, Is.False);
        }

        [Test]
        public void OnTapAdvance_StepsThroughEveryRemainingSlide()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            controller.Play(() => { });

            for (int expected = 1; expected < 5; expected++)
            {
                controller.OnTapAdvance();
                Assert.That(controller.CurrentSlideIndex, Is.EqualTo(expected));
                Assert.That(controller.HasCompleted, Is.False);
            }
        }

        [Test]
        public void OnTapAdvance_PastFinalSlide_CompletesExactlyOnce()
        {
            PrologueSequenceController controller = Build(slideCount: 3);
            int completedCount = 0;
            controller.Play(() => completedCount++);

            controller.OnTapAdvance(); // -> slide 1
            controller.OnTapAdvance(); // -> slide 2 (last)
            controller.OnTapAdvance(); // -> completes

            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(controller.HasCompleted, Is.True);
        }

        [Test]
        public void OnTapAdvance_AfterCompletion_DoesNothingHarmful()
        {
            PrologueSequenceController controller = Build(slideCount: 1);
            int completedCount = 0;
            controller.Play(() => completedCount++);

            controller.OnTapAdvance(); // single slide -> completes
            controller.OnTapAdvance(); // repeated tap after completion

            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void Skip_CompletesExactlyOnce()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            int completedCount = 0;
            controller.Play(() => completedCount++);

            controller.Skip();
            controller.Skip();

            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void Skip_CalledBeforePlay_CompletesExactlyOnceWithNoCallbackYet()
        {
            PrologueSequenceController controller = Build(slideCount: 5);

            controller.Skip();

            Assert.That(controller.HasCompleted, Is.True);
        }

        [Test]
        public void Skip_AfterPartialProgress_RepeatedSkipAndTapCannotDoubleComplete()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            int completedCount = 0;
            controller.Play(() => completedCount++);
            controller.OnTapAdvance();
            controller.OnTapAdvance();

            controller.Skip();
            controller.Skip();
            controller.OnTapAdvance();

            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void Play_CalledTwice_InvokesSecondCallersCallbackImmediately()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            int firstCount = 0;
            int secondCount = 0;

            controller.Play(() => firstCount++);
            controller.Play(() => secondCount++);

            Assert.That(firstCount, Is.EqualTo(0), "The first sequence has not completed yet.");
            Assert.That(secondCount, Is.EqualTo(1),
                "A second Play() call must not silently drop its caller's callback, even though no "
                + "second sequence starts.");
        }

        [Test]
        public void MissingIllustrationSprite_StillAllowsProgressionToCompletion()
        {
            PrologueSequenceController controller = Build(slideCount: 3, withSprites: false);
            int completedCount = 0;
            controller.Play(() => completedCount++);

            controller.OnTapAdvance();
            controller.OnTapAdvance();
            controller.OnTapAdvance();

            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void SetReducedMotion_ThenPlayAndAdvance_StillWorksNormally()
        {
            PrologueSequenceController controller = Build(slideCount: 3);
            int completedCount = 0;

            controller.SetReducedMotion(true);
            controller.Play(() => completedCount++);
            controller.OnTapAdvance();
            controller.OnTapAdvance();
            controller.OnTapAdvance();

            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void SetReducedMotion_ToggledBackOff_StillWorksNormally()
        {
            PrologueSequenceController controller = Build(slideCount: 1);
            int completedCount = 0;

            controller.SetReducedMotion(true);
            controller.SetReducedMotion(false);
            controller.Play(() => completedCount++);
            controller.OnTapAdvance();

            Assert.That(completedCount, Is.EqualTo(1));
        }
    }
}
