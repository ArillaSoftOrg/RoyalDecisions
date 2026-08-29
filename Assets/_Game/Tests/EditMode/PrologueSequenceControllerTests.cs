using System;
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

        private PrologueSequenceController Build(
            int slideCount = 3, bool withSprites = true, Func<int, string> accentCueIdForSlide = null)
        {
            PrologueSequenceData data = ScriptableObject.CreateInstance<PrologueSequenceData>();
            createdData.Add(data);

            PrologueSlideData[] slides = new PrologueSlideData[slideCount];
            for (int i = 0; i < slideCount; i++)
            {
                Sprite sprite = withSprites ? PresentationTestObjects.CreateSprite("Slide" + i) : null;
                slides[i] = new PrologueSlideData(
                    sprite, "Subtitle " + i, accentCueId: accentCueIdForSlide?.Invoke(i));
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
                    UnityEngine.Object.DestroyImmediate(data);
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

        // --- Final-slide completion (regression coverage for the "extra black screen after the "
        // --- final slide requires a second tap" bug) -------------------------------------------

        [Test]
        public void OnTapAdvance_FromIndexZero_MovesToIndexOne()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            controller.Play(() => { });

            controller.OnTapAdvance();

            Assert.That(controller.CurrentSlideIndex, Is.EqualTo(1));
        }

        [Test]
        public void OnTapAdvance_FromIndexThree_MovesToIndexFour()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            controller.Play(() => { });
            controller.OnTapAdvance(); // -> 1
            controller.OnTapAdvance(); // -> 2
            controller.OnTapAdvance(); // -> 3

            controller.OnTapAdvance(); // -> 4

            Assert.That(controller.CurrentSlideIndex, Is.EqualTo(4));
        }

        [Test]
        public void OnTapAdvance_FromFinalIndexFour_CompletesWithASingleTapAndNoSecondTap()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            int completedCount = 0;
            controller.Play(() => completedCount++);
            controller.OnTapAdvance(); // -> 1
            controller.OnTapAdvance(); // -> 2
            controller.OnTapAdvance(); // -> 3
            controller.OnTapAdvance(); // -> 4 (final slide now showing)

            controller.OnTapAdvance(); // the single tap that must complete the prologue

            Assert.That(completedCount, Is.EqualTo(1),
                "A single tap on the final slide must complete the prologue — no second tap required.");
            Assert.That(controller.HasCompleted, Is.True);
            Assert.That(controller.CurrentSlideIndex, Is.EqualTo(4),
                "Completing must never advance the index past the last real slide.");
        }

        [Test]
        public void OnTapAdvance_RepeatedlyOnFiveSlides_NeverAdvancesToAUserVisibleFifthIndex()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            controller.Play(() => { });

            for (int i = 0; i < 10; i++)
            {
                controller.OnTapAdvance();
                Assert.That(controller.CurrentSlideIndex, Is.LessThanOrEqualTo(4),
                    "CurrentSlideIndex must never reach an out-of-range 'phantom' slide beyond the "
                    + "last real one (index 4 of 5).");
            }
        }

        [Test]
        public void OnTapAdvance_AfterFinalSlideCompletion_NeverCompletesTwice()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            int completedCount = 0;
            controller.Play(() => completedCount++);

            for (int i = 0; i < 4; i++)
            {
                controller.OnTapAdvance();
            }

            controller.OnTapAdvance(); // completes
            controller.OnTapAdvance(); // further taps must do nothing
            controller.OnTapAdvance();

            Assert.That(completedCount, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(4)]
        public void Skip_FromAnySlidePosition_CompletesExactlyOnce(int tapsBeforeSkip)
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            int completedCount = 0;
            controller.Play(() => completedCount++);

            for (int i = 0; i < tapsBeforeSkip; i++)
            {
                controller.OnTapAdvance();
            }

            controller.Skip();
            controller.Skip();

            Assert.That(completedCount, Is.EqualTo(1));
        }

        // --- Audio (ambient + optional slide accents) ------------------------------------------

        [Test]
        public void Play_RequestsAmbientExactlyOnce()
        {
            PrologueSequenceController controller = Build(slideCount: 3);
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);

            controller.Play(() => { });

            Assert.That(audio.MusicRequests, Has.Count.EqualTo(1));
        }

        [Test]
        public void Play_EmptySlideList_NeverStartsAmbient()
        {
            PrologueSequenceController controller = Build(slideCount: 0);
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);

            controller.Play(() => { });

            Assert.That(audio.MusicRequests, Is.Empty,
                "A misconfigured (empty) sequence must not even briefly start the ambient before "
                + "immediately completing.");
        }

        [Test]
        public void Play_FirstSlideAccentCue_PlaysOnceWithoutRequiringATap()
        {
            PrologueSequenceController controller =
                Build(slideCount: 3, accentCueIdForSlide: i => "slide_" + i + "_cue");
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);

            controller.Play(() => { });

            Assert.That(audio.PlayedCues, Is.EqualTo(new[] { "slide_0_cue" }),
                "The first slide's accent must play as soon as the sequence begins, before any tap.");
        }

        [Test]
        public void OnTapAdvance_PlaysEachReachedSlidesAccentCueExactlyOnce()
        {
            PrologueSequenceController controller =
                Build(slideCount: 3, accentCueIdForSlide: i => "slide_" + i + "_cue");
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);
            controller.Play(() => { });

            controller.OnTapAdvance(); // -> slide 1
            controller.OnTapAdvance(); // -> slide 2 (last)

            Assert.That(audio.PlayedCues, Is.EqualTo(new[] { "slide_0_cue", "slide_1_cue", "slide_2_cue" }));
        }

        [Test]
        public void OnTapAdvance_SpamTappingPastCompletion_NeverDuplicatesAnAccentCue()
        {
            PrologueSequenceController controller =
                Build(slideCount: 3, accentCueIdForSlide: i => "slide_" + i + "_cue");
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);
            controller.Play(() => { });

            for (int i = 0; i < 10; i++)
            {
                controller.OnTapAdvance();
            }

            Assert.That(audio.PlayedCues, Is.EqualTo(new[] { "slide_0_cue", "slide_1_cue", "slide_2_cue" }),
                "Each slide's accent must play exactly once no matter how many extra taps arrive "
                + "once the sequence has advanced past it or completed.");
        }

        [Test]
        public void OnTapAdvance_SlideWithNoAccentCue_PlaysNothingForThatSlideButStillAdvances()
        {
            PrologueSequenceController controller =
                Build(slideCount: 3, accentCueIdForSlide: i => i == 1 ? null : "slide_" + i + "_cue");
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);
            controller.Play(() => { });

            controller.OnTapAdvance(); // -> slide 1, which has no accent cue
            controller.OnTapAdvance(); // -> slide 2

            Assert.That(audio.PlayedCues, Is.EqualTo(new[] { "slide_0_cue", "slide_2_cue" }));
            Assert.That(controller.CurrentSlideIndex, Is.EqualTo(2),
                "A missing optional accent cue must be harmless and never block progression.");
        }

        [Test]
        public void Complete_ViaFinalSlideTap_StopsAmbientAndSfx()
        {
            PrologueSequenceController controller = Build(slideCount: 2);
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);
            controller.Play(() => { });

            controller.OnTapAdvance(); // -> slide 1 (last)
            controller.OnTapAdvance(); // -> completes

            Assert.That(audio.StopMusicCount, Is.EqualTo(1));
            Assert.That(audio.StopSfxCount, Is.EqualTo(1));
        }

        [Test]
        public void Skip_StopsAmbientAndSfxImmediately()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);
            controller.Play(() => { });

            controller.Skip();

            Assert.That(audio.StopMusicCount, Is.EqualTo(1));
            Assert.That(audio.StopSfxCount, Is.EqualTo(1));
        }

        [Test]
        public void Skip_CalledTwice_StopsAudioExactlyOnce()
        {
            PrologueSequenceController controller = Build(slideCount: 5);
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);
            controller.Play(() => { });

            controller.Skip();
            controller.Skip();

            Assert.That(audio.StopMusicCount, Is.EqualTo(1));
            Assert.That(audio.StopSfxCount, Is.EqualTo(1));
        }

        [Test]
        public void ApplyAudioSettings_StillPlaysThroughToService_MuteIsTheServicesResponsibility()
        {
            // PrologueSequenceController never special-cases mute itself — it always calls through
            // to whatever IAudioService it has, exactly like every other IAudioService caller in
            // this project; suppressing playback when muted is AudioService's own job (see
            // AudioServiceTests), not something reimplemented here.
            PrologueSequenceController controller = Build(slideCount: 1);
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);

            controller.ApplyAudioSettings(masterVolume: 0.5f, musicVolume: 0.4f, sfxVolume: 0.6f, masterMuted: true);
            controller.Play(() => { });

            Assert.That(audio.MasterVolume, Is.EqualTo(0.5f));
            Assert.That(audio.MusicVolume, Is.EqualTo(0.4f));
            Assert.That(audio.Volume, Is.EqualTo(0.6f));
            Assert.That(audio.IsMuted, Is.True);
            Assert.That(audio.MusicRequests, Has.Count.EqualTo(1),
                "The ambient is still requested even while muted — the service decides whether it "
                + "is actually audible.");
        }

        [Test]
        public void SetReducedMotion_DoesNotStartOrStopAudio()
        {
            PrologueSequenceController controller = Build(slideCount: 1);
            FakeAudioService audio = new FakeAudioService();
            controller.SetAudioService(audio);

            controller.SetReducedMotion(true);

            Assert.That(audio.MusicRequests, Is.Empty);
            Assert.That(audio.StopMusicCount, Is.Zero);
            Assert.That(audio.IsMuted, Is.False,
                "Reduced Motion is a visual accessibility setting and must never mute audio.");
        }
    }
}
