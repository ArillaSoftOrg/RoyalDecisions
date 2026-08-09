using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Covers the parts of the swipe that only exist while frames are running.
    /// </summary>
    /// <remarks>
    /// The state machine and exactly-once rules are proven in EditMode. What is left is genuinely
    /// temporal: coroutines completing, input staying locked across frames, and cancellation
    /// mid-flight.
    ///
    /// Every test asserts <em>arrival</em>, never timing. Durations are shortened to a few frames
    /// and each wait is bounded, so a hang fails with a message instead of stalling the suite.
    /// </remarks>
    [TestFixture]
    public class CardSwipeAnimationPlayModeTests
    {
        private const float ParentWidth = 1000f;
        private const float CardWidth = 600f;
        private const float Threshold = 250f;
        private const float PressX = 500f;
        private const float PressY = 800f;
        private const float ShortDuration = 0.05f;
        private const int FrameBudget = 300;

        private GameObject root;
        private RectTransform parent;
        private RectTransform card;
        private CardView cardView;
        private CardSwipeController controller;

        private List<ChoiceSide> confirmed;
        private List<ChoiceSide> exited;
        private int impactPreviewCleared;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Canvas");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            parent = Child(root.transform, "DragParent");
            parent.sizeDelta = new Vector2(ParentWidth, 1600f);

            card = Child(parent, "Card");
            card.sizeDelta = new Vector2(CardWidth, 900f);
            card.anchoredPosition = Vector2.zero;

            ChoicePreviewView left = Child(card, "Left").gameObject.AddComponent<ChoicePreviewView>();
            left.SetAuthoringReferences(
                ChoiceSide.Left, null, Child(card, "LeftGroup").gameObject.AddComponent<CanvasGroup>());

            ChoicePreviewView right = Child(card, "Right").gameObject.AddComponent<ChoicePreviewView>();
            right.SetAuthoringReferences(
                ChoiceSide.Right, null, Child(card, "RightGroup").gameObject.AddComponent<CanvasGroup>());

            cardView = card.gameObject.AddComponent<CardView>();
            cardView.SetAuthoringReferences(null, null, null, left, right);

            controller = card.gameObject.AddComponent<CardSwipeController>();
            controller.SetAuthoringReferences(
                cardView, parent,
                snapDuration: ShortDuration,
                exitSeconds: ShortDuration);

            confirmed = new List<ChoiceSide>();
            exited = new List<ChoiceSide>();
            impactPreviewCleared = 0;
            controller.DecisionConfirmed += HandleDecisionConfirmed;
            controller.ExitAnimationCompleted += HandleExitCompleted;
            controller.ChoicePreviewCleared += HandleChoicePreviewCleared;
        }

        [TearDown]
        public void TearDown()
        {
            if (controller != null)
            {
                controller.DecisionConfirmed -= HandleDecisionConfirmed;
                controller.ExitAnimationCompleted -= HandleExitCompleted;
                controller.ChoicePreviewCleared -= HandleChoicePreviewCleared;
            }

            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }

        private void HandleDecisionConfirmed(ChoiceSide side)
        {
            confirmed.Add(side);
        }

        private void HandleExitCompleted(ChoiceSide side)
        {
            exited.Add(side);
        }

        private void HandleChoicePreviewCleared()
        {
            impactPreviewCleared++;
        }

        private static RectTransform Child(Transform parentTransform, string name)
        {
            GameObject child = new GameObject(name);
            RectTransform rect = child.AddComponent<RectTransform>();
            rect.SetParent(parentTransform, false);
            return rect;
        }

        private static PointerEventData Pointer(float x)
        {
            return new PointerEventData(EventSystem.current)
            {
                pointerId = 0,
                position = new Vector2(x, PressY)
            };
        }

        private void BeginAndDragTo(float toX)
        {
            PointerEventData data = Pointer(PressX);
            controller.OnBeginDrag(data);

            data.position = new Vector2(toX, PressY);
            controller.OnDrag(data);
            controller.OnEndDrag(data);
        }

        /// <summary>Waits for the controller to settle, failing rather than hanging.</summary>
        private IEnumerator WaitUntilSettled()
        {
            int frames = 0;

            while (controller.IsAnimating && frames < FrameBudget)
            {
                frames++;
                yield return null;
            }

            Assert.That(controller.IsAnimating, Is.False,
                "animation did not complete within " + FrameBudget + " frames");
        }

        // --- Snap back --------------------------------------------------------

        [UnityTest]
        public IEnumerator SnapBackReturnsTheCardToNeutral()
        {
            yield return null;
            card.anchoredPosition = Vector2.zero;

            BeginAndDragTo(PressX + 100f);
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.SnappingBack));

            yield return WaitUntilSettled();

            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
            Assert.That(card.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(0f, card.localRotation.eulerAngles.z),
                Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator InputStaysLockedForTheWholeSnapBack()
        {
            yield return null;

            BeginAndDragTo(PressX + 100f);

            Assert.That(controller.IsInteractable, Is.False, "locked as soon as the drag ends");

            yield return null;
            Assert.That(controller.IsInteractable, Is.False, "still locked mid-animation");

            yield return WaitUntilSettled();
            Assert.That(controller.IsInteractable, Is.True, "available again once settled");
        }

        [UnityTest]
        public IEnumerator SnapBackClearsBothPreviews()
        {
            yield return null;

            BeginAndDragTo(PressX - 100f);
            yield return WaitUntilSettled();

            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Left),
                Is.EqualTo(0f).Within(0.001f));
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right),
                Is.EqualTo(0f).Within(0.001f));
            Assert.That(impactPreviewCleared, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SnapBackEmitsNoEvents()
        {
            yield return null;

            BeginAndDragTo(PressX + 100f);
            yield return WaitUntilSettled();

            Assert.That(confirmed, Is.Empty);
            Assert.That(exited, Is.Empty);
        }

        // --- Exit ---------------------------------------------------------------

        [UnityTest]
        public IEnumerator ExitCompletesAndReportsOnce()
        {
            yield return null;
            card.anchoredPosition = Vector2.zero;

            BeginAndDragTo(PressX + Threshold + 50f);

            Assert.That(confirmed.Count, Is.EqualTo(1), "the decision lands immediately");
            Assert.That(exited, Is.Empty, "the exit has not finished yet");
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Exiting));
            Assert.That(impactPreviewCleared, Is.EqualTo(1));

            yield return WaitUntilSettled();

            Assert.That(exited, Is.EqualTo(new[] { ChoiceSide.Right }));
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Completed));
        }

        [UnityTest]
        public IEnumerator TheCardFinishesCompletelyOutsideTheParent()
        {
            yield return null;
            card.anchoredPosition = Vector2.zero;

            BeginAndDragTo(PressX - Threshold - 50f);
            yield return WaitUntilSettled();

            float leadingEdge = card.anchoredPosition.x + (CardWidth * 0.5f);

            Assert.That(leadingEdge, Is.LessThan(-(ParentWidth * 0.5f)),
                "the trailing edge must clear the parent, not just its centre");
        }

        [UnityTest]
        public IEnumerator TheConfirmedPreviewStaysVisibleThroughoutTheExit()
        {
            yield return null;

            BeginAndDragTo(PressX + Threshold + 50f);

            for (int frame = 0; frame < 3 && controller.IsAnimating; frame++)
            {
                Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(1f));
                Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Left), Is.EqualTo(0f));
                yield return null;
            }

            yield return WaitUntilSettled();

            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator InputStaysLockedForTheWholeExit()
        {
            yield return null;

            BeginAndDragTo(PressX + Threshold + 50f);
            Assert.That(controller.IsInteractable, Is.False);

            yield return null;
            Assert.That(controller.IsInteractable, Is.False);

            yield return WaitUntilSettled();
            Assert.That(controller.IsInteractable, Is.False, "a decided card stays locked");
        }

        // --- Cancellation mid-flight -----------------------------------------------

        [UnityTest]
        public IEnumerator DisablingMidExitSuppressesTheCompletionEvent()
        {
            yield return null;

            BeginAndDragTo(PressX + Threshold + 50f);
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Exiting));

            controller.enabled = false;
            yield return null;
            yield return null;

            Assert.That(exited, Is.Empty, "a cancelled exit must not announce completion");
            Assert.That(confirmed.Count, Is.EqualTo(1), "the decision already went out and stands");
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Completed));
        }

        [UnityTest]
        public IEnumerator ReEnablingAfterADecisionStaysLockedUntilReset()
        {
            yield return null;

            BeginAndDragTo(PressX + Threshold + 50f);
            controller.enabled = false;
            yield return null;

            controller.enabled = true;
            yield return null;

            Assert.That(controller.IsInteractable, Is.False,
                "a decided card must not accept a second swipe on re-enable");

            BeginAndDragTo(PressX + Threshold + 50f);
            Assert.That(confirmed.Count, Is.EqualTo(1));

            controller.ResetForNextCard();
            Assert.That(controller.IsInteractable, Is.True);
        }

        [UnityTest]
        public IEnumerator DisablingMidDragReturnsToNeutralWithoutEvents()
        {
            yield return null;
            card.anchoredPosition = Vector2.zero;

            PointerEventData data = Pointer(PressX);
            controller.OnBeginDrag(data);
            data.position = new Vector2(PressX + 200f, PressY);
            controller.OnDrag(data);

            controller.enabled = false;
            yield return null;

            Assert.That(confirmed, Is.Empty);
            Assert.That(exited, Is.Empty);
            Assert.That(card.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
        }

        [UnityTest]
        public IEnumerator ReEnablingAfterADroppedDragAcceptsANewSwipe()
        {
            yield return null;

            PointerEventData data = Pointer(PressX);
            controller.OnBeginDrag(data);

            controller.enabled = false;
            yield return null;
            controller.enabled = true;
            yield return null;

            // The old pointer is gone, so a fresh interaction must be accepted in full.
            BeginAndDragTo(PressX + Threshold + 50f);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }));
        }

        [UnityTest]
        public IEnumerator ResetDuringAnExitStopsTheAnimation()
        {
            yield return null;

            BeginAndDragTo(PressX + Threshold + 50f);
            controller.ResetForNextCard();

            yield return null;
            yield return null;

            Assert.That(exited, Is.Empty, "a reset exit must not announce completion");
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
            Assert.That(card.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
        }

        // --- Sequential cards -------------------------------------------------------

        [UnityTest]
        public IEnumerator ASecondCardCanBeSwipedAfterReset()
        {
            yield return null;

            BeginAndDragTo(PressX + Threshold + 50f);
            yield return WaitUntilSettled();

            controller.ResetForNextCard();
            Assert.That(card.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));

            BeginAndDragTo(PressX - Threshold - 50f);
            yield return WaitUntilSettled();

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right, ChoiceSide.Left }));
            Assert.That(exited, Is.EqualTo(new[] { ChoiceSide.Right, ChoiceSide.Left }));
        }
    }
}
