using NUnit.Framework;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers the parts of <see cref="StartupLoadingController"/> that are reachable without a
    /// running coroutine. Outside Play Mode (which is where EditMode tests always run) every
    /// animated path resolves synchronously — the same reasoning
    /// <see cref="IntroSequenceControllerTests"/> and <see cref="PanelFadeAnimatorTests"/> rely on —
    /// so the smoothing/hold/fade timing itself is covered by
    /// <see cref="StartupLoadingProgressMathTests"/> as pure functions instead of brittle frame-timed
    /// PlayMode tests. The same limitation means the leading edge's time-based wobble and the 100%
    /// completion pulse cannot be observed running here either (both live inside coroutines that only
    /// execute in Play Mode) — what is covered instead is that both wire up safely, never block
    /// completion, and settle to their at-rest state once displayed progress reaches 100%.
    /// </summary>
    [TestFixture]
    public class StartupLoadingControllerTests
    {
        // Deliberately not round numbers, so a test that accidentally reads the wrong field still
        // fails instead of coincidentally matching.
        private const float TubeInteriorWidth = 200f;
        private const float TubeInteriorHeight = 60f;
        private const float BloodMaskLeftInset = 10f;
        private const float InnerWidth = TubeInteriorWidth - (2f * BloodMaskLeftInset);

        private static StartupLoadingController Build(
            out RectTransform bloodMask,
            out BloodFillGraphic bloodFill,
            out Graphic bloodLeadingEdge,
            out RectTransform tubeInterior,
            out TextMeshProUGUI percentage,
            bool withBackgroundSprite = true,
            bool showPercentage = true,
            bool withBloodTube = true)
        {
            CanvasGroup group = PresentationTestObjects.CreateCanvasGroup("Group");
            Image background = PresentationTestObjects.CreateImage("Background");
            if (withBackgroundSprite)
            {
                background.sprite = PresentationTestObjects.CreateSprite("BackgroundArt");
            }

            AspectRatioFitter fitter = PresentationTestObjects.CreateComponent<AspectRatioFitter>("Fitter");
            TextMeshProUGUI status = PresentationTestObjects.CreateText("Status");
            percentage = PresentationTestObjects.CreateText("Percentage");

            StartupLoadingController controller =
                PresentationTestObjects.CreateComponent<StartupLoadingController>("Loading");
            controller.SetAuthoringReferences(
                group, background, fitter, status, percentage, showPercentageValue: showPercentage);

            if (withBloodTube)
            {
                tubeInterior = PresentationTestObjects.CreateObject("TubeInterior").GetComponent<RectTransform>();
                tubeInterior.sizeDelta = new Vector2(TubeInteriorWidth, TubeInteriorHeight);

                bloodMask = PresentationTestObjects.CreateComponent<RectMask2D>("BloodMask")
                    .GetComponent<RectTransform>();
                bloodMask.anchoredPosition = new Vector2(BloodMaskLeftInset, 0f);

                bloodFill = PresentationTestObjects.CreateComponent<BloodFillGraphic>("BloodFill");
                bloodLeadingEdge = PresentationTestObjects.CreateImage("LeadingEdge");

                controller.SetBloodTubeAuthoringReferences(bloodMask, bloodFill, bloodLeadingEdge, tubeInterior);
            }
            else
            {
                bloodMask = null;
                bloodFill = null;
                bloodLeadingEdge = null;
                tubeInterior = null;
            }

            return controller;
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void CompleteLoading_OutsidePlayMode_CompletesImmediately()
        {
            StartupLoadingController controller = Build(out _, out _, out _, out _, out _);
            bool completed = false;

            controller.BeginLoading();
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
            Assert.That(controller.HasCompleted, Is.True);
            Assert.That(controller.DisplayedProgress, Is.EqualTo(1f));
            Assert.That(controller.DisplayedPercentage, Is.EqualTo(100));
        }

        [Test]
        public void CompleteLoading_WithoutBeginLoadingFirst_StillCompletesSafely()
        {
            StartupLoadingController controller = Build(out _, out _, out _, out _, out _);
            bool completed = false;

            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void CompleteLoading_MissingBackgroundSprite_StillCompletesSafely()
        {
            StartupLoadingController controller =
                Build(out _, out _, out _, out _, out _, withBackgroundSprite: false);
            bool completed = false;

            controller.BeginLoading();
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void CompleteLoading_WithNoReferencesAssignedAtAll_StillCompletesSafely()
        {
            StartupLoadingController controller =
                PresentationTestObjects.CreateComponent<StartupLoadingController>("Loading");
            bool completed = false;

            controller.BeginLoading();
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void CompleteLoading_MissingBloodTubeReferences_StillCompletesSafely()
        {
            // Covers a controller wired for background/status/percentage only — e.g. before the
            // blood-tube hierarchy exists in a scene yet, or if a designer intentionally leaves it
            // unassigned. Every blood-tube field is optional; none of them may block startup.
            StartupLoadingController controller =
                Build(out _, out _, out _, out _, out _, withBloodTube: false);
            bool completed = false;

            controller.BeginLoading();
            controller.ReportProgress(0.5f);
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void CompleteLoading_CalledTwice_InvokesEachCallersCallbackExactlyOnce()
        {
            StartupLoadingController controller = Build(out _, out _, out _, out _, out _);
            int firstCount = 0;
            int secondCount = 0;

            controller.CompleteLoading(() => firstCount++);
            controller.CompleteLoading(() => secondCount++);

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(secondCount, Is.EqualTo(1),
                "A second CompleteLoading() call must not silently drop its caller's callback, even "
                + "though the sequence already finished.");
        }

        [Test]
        public void CompleteLoading_CalledManyTimes_NeverInvokesAnyCallbackMoreThanOnce()
        {
            StartupLoadingController controller = Build(out _, out _, out _, out _, out _);
            int totalInvocations = 0;

            for (int i = 0; i < 5; i++)
            {
                controller.CompleteLoading(() => totalInvocations++);
            }

            Assert.That(totalInvocations, Is.EqualTo(5));
        }

        [Test]
        public void ReportProgress_ZeroProgress_MaskWidthIsZero()
        {
            StartupLoadingController controller = Build(out RectTransform bloodMask, out _, out _, out _, out _);

            controller.ReportProgress(0f);

            Assert.That(controller.DisplayedProgress, Is.EqualTo(0f));
            Assert.That(bloodMask.sizeDelta.x, Is.EqualTo(0f));
        }

        [Test]
        public void ReportProgress_HalfProgress_MaskWidthIsHalfInnerWidth()
        {
            StartupLoadingController controller = Build(out RectTransform bloodMask, out _, out _, out _, out _);

            controller.ReportProgress(0.5f);

            Assert.That(bloodMask.sizeDelta.x, Is.EqualTo(InnerWidth * 0.5f).Within(0.001f));
        }

        [Test]
        public void ReportProgress_FullProgress_MaskWidthIsFullInnerWidth()
        {
            StartupLoadingController controller = Build(out RectTransform bloodMask, out _, out _, out _, out _);

            controller.ReportProgress(1f);

            Assert.That(bloodMask.sizeDelta.x, Is.EqualTo(InnerWidth).Within(0.001f));
        }

        [Test]
        public void ReportProgress_AnyProgress_BloodFillStaysAtFullInnerWidth()
        {
            // BloodFill must never be horizontally scaled/stretched by progress — only BloodMask's
            // width changes. This holds at an arbitrary mid-range value specifically to catch a bug
            // where fill width was accidentally driven by displayedProgress too.
            StartupLoadingController controller =
                Build(out _, out BloodFillGraphic bloodFill, out _, out _, out _);

            controller.ReportProgress(0.3f);

            Assert.That(bloodFill.rectTransform.sizeDelta.x, Is.EqualTo(InnerWidth).Within(0.001f));
        }

        [Test]
        public void ReportProgress_ClampsBelowZero()
        {
            StartupLoadingController controller = Build(out RectTransform bloodMask, out _, out _, out _, out _);

            controller.ReportProgress(-5f);

            Assert.That(controller.DisplayedProgress, Is.EqualTo(0f));
            Assert.That(bloodMask.sizeDelta.x, Is.EqualTo(0f));
        }

        [Test]
        public void ReportProgress_ClampsAboveOne()
        {
            StartupLoadingController controller = Build(out RectTransform bloodMask, out _, out _, out _, out _);

            controller.ReportProgress(5f);

            Assert.That(controller.DisplayedProgress, Is.EqualTo(1f));
            Assert.That(bloodMask.sizeDelta.x, Is.EqualTo(InnerWidth).Within(0.001f));
        }

        [Test]
        public void ReportProgress_HalfProgress_LeadingEdgeTracksFillBoundary()
        {
            StartupLoadingController controller = Build(
                out RectTransform bloodMask, out _, out Graphic leadingEdge, out _, out _);

            controller.ReportProgress(0.5f);

            // The leading edge shares bloodMask's own left inset as its coordinate origin (both are
            // children of the same tubeInterior) — its right edge in that shared space is
            // leftInset + maskWidth, not maskWidth alone.
            float expectedX = bloodMask.anchoredPosition.x + (InnerWidth * 0.5f);
            Assert.That(leadingEdge.rectTransform.anchoredPosition.x, Is.EqualTo(expectedX).Within(0.001f));
        }

        [Test]
        public void ReportProgress_UpdatesDisplayedPercentageAndText()
        {
            StartupLoadingController controller =
                Build(out _, out _, out _, out _, out TextMeshProUGUI percentage);

            controller.ReportProgress(0.5f);

            Assert.That(controller.DisplayedPercentage, Is.EqualTo(50));
            Assert.That(percentage.text, Is.EqualTo("50%"));
        }

        [TestCase(0f, "0%")]
        [TestCase(0.004f, "0%")]
        [TestCase(0.999f, "100%")]
        [TestCase(1f, "100%")]
        public void ReportProgress_FormatsPercentageText(float progress, string expected)
        {
            StartupLoadingController controller =
                Build(out _, out _, out _, out _, out TextMeshProUGUI percentage);

            controller.ReportProgress(progress);

            Assert.That(percentage.text, Is.EqualTo(expected));
        }

        [Test]
        public void Build_ShowPercentageFalse_HidesPercentageTextImmediately()
        {
            StartupLoadingController controller = Build(
                out _, out _, out _, out _, out TextMeshProUGUI percentage, showPercentage: false);

            Assert.That(percentage.gameObject.activeSelf, Is.False);
            // The controller is still fully functional with it hidden.
            Assert.That(controller.DisplayedPercentage, Is.EqualTo(0));
        }

        [Test]
        public void Build_ShowPercentageTrue_KeepsPercentageTextVisible()
        {
            StartupLoadingController controller = Build(
                out _, out _, out _, out _, out TextMeshProUGUI percentage, showPercentage: true);

            Assert.That(percentage.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void CompleteLoading_ShowPercentageFalse_StillCompletesSafely()
        {
            StartupLoadingController controller =
                Build(out _, out _, out _, out _, out _, showPercentage: false);
            bool completed = false;

            controller.BeginLoading();
            controller.ReportProgress(0.4f);
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void SetReducedMotion_ThenCompleteLoading_StillCompletesSafely()
        {
            StartupLoadingController controller = Build(out _, out _, out _, out _, out _);
            bool completed = false;

            controller.SetReducedMotion(true);
            controller.BeginLoading();
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void SetReducedMotion_ToggledBackOff_StillCompletesSafely()
        {
            StartupLoadingController controller = Build(out _, out _, out _, out _, out _);
            bool completed = false;

            controller.SetReducedMotion(true);
            controller.SetReducedMotion(false);
            controller.BeginLoading();
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void ReducedMotion_CompletionSettlesLeadingEdgeWobbleToZero()
        {
            // The wobble's own time-based motion only advances inside DriveRoutine, a coroutine that
            // cannot run outside Play Mode, so it cannot be observed "in motion" here (see the class
            // doc comment). What is covered: regardless of reduced motion, once displayed progress
            // reaches 100% the wobble contract requires it to have settled back to zero, not frozen
            // mid-wobble.
            StartupLoadingController controller = Build(
                out _, out _, out Graphic leadingEdge, out _, out _);
            controller.SetReducedMotion(true);
            bool completed = false;

            controller.BeginLoading();
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
            Assert.That(leadingEdge.rectTransform.anchoredPosition.y, Is.EqualTo(0f));
        }

        [Test]
        public void BeginLoading_CalledTwice_DoesNotThrowAndStillCompletes()
        {
            StartupLoadingController controller = Build(out _, out _, out _, out _, out _);
            bool completed = false;

            controller.BeginLoading();
            controller.BeginLoading();
            controller.CompleteLoading(() => completed = true);

            Assert.That(completed, Is.True);
        }
    }
}
