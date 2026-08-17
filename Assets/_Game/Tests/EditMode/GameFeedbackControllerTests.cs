using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Confirms GameFeedbackController actually reaches a haptic service seeded with the saved
    /// HapticsEnabled preference, and routes drag/decision events to the right feedback tier.
    /// </summary>
    /// <remarks>
    /// Before this wiring existed, no composition root ever called
    /// <see cref="GameFeedbackController.Configure"/>, so its <c>haptics</c> field stayed null and
    /// every <c>haptics?.Pulse()</c> call in the class was a silent no-op regardless of the Settings
    /// toggle. This drives the controller the same way <c>CardSwipeControllerTests</c> drives
    /// <see cref="CardSwipeController"/> — no EventSystem, no frames — to prove the tiers now fire.
    /// </remarks>
    [TestFixture]
    public class GameFeedbackControllerTests
    {
        private const float ParentWidth = 1000f;
        private const float CardWidth = 600f;
        private const float Threshold = 250f; // ParentWidth * 0.25
        private const float PressX = 500f;
        private const float PressY = 800f;

        private CardSwipeController swipeController;
        private GameFeedbackController feedback;
        private FakeHapticService haptics;

        [SetUp]
        public void SetUp()
        {
            RectTransform parent =
                PresentationTestObjects.CreateObject("DragParent").GetComponent<RectTransform>();
            parent.sizeDelta = new Vector2(ParentWidth, 1600f);

            GameObject cardObject = PresentationTestObjects.CreateObject("Card");
            RectTransform card = cardObject.GetComponent<RectTransform>();
            card.SetParent(parent, false);
            card.sizeDelta = new Vector2(CardWidth, 900f);
            card.anchoredPosition = Vector2.zero;

            ChoicePreviewView left = PresentationTestObjects.CreateComponent<ChoicePreviewView>("Left");
            left.SetAuthoringReferences(
                ChoiceSide.Left,
                PresentationTestObjects.CreateText("LeftLabel"),
                PresentationTestObjects.CreateCanvasGroup("LeftGroup"));

            ChoicePreviewView right = PresentationTestObjects.CreateComponent<ChoicePreviewView>("Right");
            right.SetAuthoringReferences(
                ChoiceSide.Right,
                PresentationTestObjects.CreateText("RightLabel"),
                PresentationTestObjects.CreateCanvasGroup("RightGroup"));

            CardView cardView = cardObject.AddComponent<CardView>();
            cardView.SetAuthoringReferences(
                PresentationTestObjects.CreateText("Speaker"),
                PresentationTestObjects.CreateText("Body"),
                PresentationTestObjects.CreateImage("Portrait"),
                left,
                right);

            swipeController = cardObject.AddComponent<CardSwipeController>();
            swipeController.SetAuthoringReferences(cardView, parent);

            // Created inactive, wired, then activated — Awake/OnEnable (and the Subscribe() it
            // triggers) must see the real swipeController and the fake haptics, not defaults.
            GameObject feedbackHost = PresentationTestObjects.CreateObject("Feedback");
            feedbackHost.SetActive(false);
            feedback = feedbackHost.AddComponent<GameFeedbackController>();
            feedback.SetAuthoringReferences(null, swipeController, null, null);

            haptics = new FakeHapticService();
            feedback.Configure(haptics, new FakeSettingsStore());

            feedbackHost.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        private void Swipe(float toX)
        {
            PointerEventData data = new PointerEventData(null)
            {
                pointerId = 0,
                position = new Vector2(PressX, PressY)
            };
            swipeController.OnBeginDrag(data);
            data.position = new Vector2(toX, PressY);
            swipeController.OnDrag(data);
            swipeController.OnEndDrag(data);
        }

        [Test]
        public void Configure_AppliesEnabledPreferenceByDefault()
        {
            Assert.That(haptics.IsEnabled, Is.True);
        }

        [Test]
        public void Configure_AppliesSavedHapticsDisabledPreferenceImmediately()
        {
            GameSettings disabled = GameSettings.CreateDefault();
            disabled.SetHapticsEnabled(false);
            FakeSettingsStore disabledStore = new FakeSettingsStore();
            disabledStore.Save(disabled);

            FakeHapticService service = new FakeHapticService();
            feedback.Configure(service, disabledStore);

            Assert.That(service.IsEnabled, Is.False);
        }

        [Test]
        public void DraggingPastThreshold_PulsesLightExactlyOnce()
        {
            PointerEventData data = new PointerEventData(null)
            {
                pointerId = 0,
                position = new Vector2(PressX, PressY)
            };
            swipeController.OnBeginDrag(data);

            // Two drag updates past the threshold; the second must not re-pulse.
            data.position = new Vector2(PressX + Threshold + 10f, PressY);
            swipeController.OnDrag(data);
            data.position = new Vector2(PressX + Threshold + 40f, PressY);
            swipeController.OnDrag(data);
            swipeController.OnEndDrag(data);

            Assert.That(haptics.Pulses, Has.Exactly(1).EqualTo(HapticFeedbackLevel.Light));
        }

        [Test]
        public void ReleasingBelowThreshold_NeverPulses()
        {
            Swipe(PressX + 50f);

            Assert.That(haptics.Pulses, Is.Empty);
        }

        [Test]
        public void ConfirmingADecision_PulsesStandard()
        {
            Swipe(PressX + Threshold + 50f);

            Assert.That(haptics.Pulses, Does.Contain(HapticFeedbackLevel.Standard));
        }
    }
}
