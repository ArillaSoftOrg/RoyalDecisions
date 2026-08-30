using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Drives the swipe controller by invoking its drag handlers directly.
    /// </summary>
    /// <remarks>
    /// No EventSystem, no raycasting, no frames — so the whole state machine and the exactly-once
    /// guarantee are covered deterministically here, leaving PlayMode for animation only.
    ///
    /// Outside play mode the controller finishes its animations synchronously, which is why the
    /// full event order is observable in an EditMode test.
    /// </remarks>
    [TestFixture]
    public class CardSwipeControllerTests
    {
        private const float ParentWidth = 1000f;
        private const float CardWidth = 600f;
        private const float Threshold = 250f;   // ParentWidth * 0.25
        private const float PressX = 500f;
        private const float PressY = 800f;

        private RectTransform parent;
        private RectTransform card;
        private CardView cardView;
        private CardSwipeController controller;

        private List<ChoiceSide> confirmed;
        private List<ChoiceSide> exited;
        private List<ChoiceSide> previewSides;
        private List<float> previewStrengths;
        private int previewCleared;

        [SetUp]
        public void SetUp()
        {
            parent = PresentationTestObjects.CreateObject("DragParent").GetComponent<RectTransform>();
            parent.sizeDelta = new Vector2(ParentWidth, 1600f);

            GameObject cardObject = PresentationTestObjects.CreateObject("Card");
            card = cardObject.GetComponent<RectTransform>();
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

            cardView = cardObject.AddComponent<CardView>();
            cardView.SetAuthoringReferences(
                PresentationTestObjects.CreateText("Speaker"),
                PresentationTestObjects.CreateText("Body"),
                PresentationTestObjects.CreateImage("Portrait"),
                left,
                right);

            controller = cardObject.AddComponent<CardSwipeController>();
            controller.SetAuthoringReferences(cardView, parent);

            confirmed = new List<ChoiceSide>();
            exited = new List<ChoiceSide>();
            controller.DecisionConfirmed += side => confirmed.Add(side);
            controller.ExitAnimationCompleted += side => exited.Add(side);
            previewSides = new List<ChoiceSide>();
            previewStrengths = new List<float>();
            previewCleared = 0;
            controller.ChoicePreviewChanged += (side, strength) =>
            {
                previewSides.Add(side);
                previewStrengths.Add(strength);
            };
            controller.ChoicePreviewCleared += () => previewCleared++;
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        private static PointerEventData Pointer(int id, float x, float y = PressY)
        {
            return new PointerEventData(null)
            {
                pointerId = id,
                position = new Vector2(x, y)
            };
        }

        /// <summary>Press at the centre, move to <paramref name="toX"/>, release.</summary>
        private void Swipe(float toX, int pointerId = 0)
        {
            PointerEventData data = Pointer(pointerId, PressX);
            controller.OnBeginDrag(data);

            data.position = new Vector2(toX, PressY);
            controller.OnDrag(data);
            controller.OnEndDrag(data);
        }

        // --- Below threshold ------------------------------------------------

        [Test]
        public void ReleasingBelowThresholdEmitsNoDecision()
        {
            Swipe(PressX + 100f);

            Assert.That(confirmed, Is.Empty);
            Assert.That(exited, Is.Empty);
            Assert.That(controller.HasDecision, Is.False);
        }

        [Test]
        public void ReleasingBelowThresholdReturnsToNeutralAndUnlocks()
        {
            Swipe(PressX - 100f);

            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
            Assert.That(controller.IsInteractable, Is.True);
            Assert.That(card.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(card.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Left), Is.EqualTo(0f));
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(0f));
        }

        [Test]
        public void DragPreviewReportsOnlyActiveSideAndClearsAfterSnapBack()
        {
            Swipe(PressX - 100f);

            Assert.That(previewSides, Is.EqualTo(new[] { ChoiceSide.Left }));
            Assert.That(previewStrengths[0], Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(previewCleared, Is.EqualTo(1));
        }

        // --- Confirmation ------------------------------------------------------

        [Test]
        public void DraggingPastThresholdRightConfirmsRight()
        {
            Swipe(PressX + Threshold + 50f);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }));
            Assert.That(controller.ConfirmedSide, Is.EqualTo(ChoiceSide.Right));
        }

        [Test]
        public void ConfirmationClearsImpactPreviewBeforeDecisionCompletes()
        {
            Swipe(PressX + Threshold + 50f);

            Assert.That(previewSides, Is.EqualTo(new[] { ChoiceSide.Right }));
            Assert.That(previewCleared, Is.EqualTo(1));
            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }));
        }

        [Test]
        public void CommittedExitMovesDiagonallyAndRotates()
        {
            Swipe(PressX + Threshold + 50f);

            // The committed exit — unlike the drag itself — moves on both axes and rotates, for a
            // diagonal "thrown off the table" feel rather than a straight sideways slide.
            Assert.That(card.anchoredPosition.x, Is.GreaterThan(0f));
            Assert.That(card.anchoredPosition.y, Is.LessThan(0f));
            Assert.That(card.localRotation, Is.Not.EqualTo(Quaternion.identity));
        }

        [Test]
        public void DraggingPastThresholdLeftConfirmsLeft()
        {
            Swipe(PressX - Threshold - 50f);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Left }));
        }

        [Test]
        public void ReleasingExactlyOnTheThresholdConfirms()
        {
            Swipe(PressX + Threshold);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }),
                "the threshold is inclusive");
        }

        [Test]
        public void ReleasingJustBelowTheThresholdDoesNotConfirm()
        {
            Swipe(PressX + Threshold - 1f);

            Assert.That(confirmed, Is.Empty);
        }

        [Test]
        public void DecisionPrecedesExitCompletion()
        {
            List<string> order = new List<string>();
            controller.DecisionConfirmed += _ => order.Add("decision");
            controller.ExitAnimationCompleted += _ => order.Add("exit");

            Swipe(PressX + Threshold);

            Assert.That(order, Is.EqualTo(new[] { "decision", "exit" }));
        }

        [Test]
        public void InputIsLockedBeforeAnyHandlerRuns()
        {
            CardSwipeState stateInsideHandler = CardSwipeState.Idle;
            bool interactableInsideHandler = true;

            controller.DecisionConfirmed += _ =>
            {
                stateInsideHandler = controller.State;
                interactableInsideHandler = controller.IsInteractable;
            };

            Swipe(PressX + Threshold);

            Assert.That(stateInsideHandler, Is.EqualTo(CardSwipeState.Exiting));
            Assert.That(interactableInsideHandler, Is.False);
        }

        [Test]
        public void TheCardEndsCompletelyOutsideTheParent()
        {
            Swipe(PressX + Threshold);

            float trailingEdge = card.anchoredPosition.x - (CardWidth * 0.5f);

            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Completed));
            Assert.That(trailingEdge, Is.GreaterThan(ParentWidth * 0.5f));
        }

        [Test]
        public void TheConfirmedPreviewStaysLitAndTheOtherStaysDark()
        {
            Swipe(PressX + Threshold);

            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(1f));
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Left), Is.EqualTo(0f));
        }

        // --- Exactly once ----------------------------------------------------------

        [Test]
        public void ASecondEndDragEmitsNothingFurther()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);
            data.position = new Vector2(PressX + Threshold, PressY);
            controller.OnDrag(data);

            controller.OnEndDrag(data);
            controller.OnEndDrag(data);
            controller.OnEndDrag(data);

            Assert.That(confirmed.Count, Is.EqualTo(1));
            Assert.That(exited.Count, Is.EqualTo(1));
        }

        [Test]
        public void RapidRepeatedSwipesOnADecidedCardEmitNothing()
        {
            Swipe(PressX + Threshold);
            confirmed.Clear();
            exited.Clear();

            for (int i = 0; i < 5; i++)
            {
                Swipe(PressX + Threshold);
            }

            Assert.That(confirmed, Is.Empty, "a decided card is locked until it is reset");
            Assert.That(exited, Is.Empty);
        }

        [Test]
        public void AHandlerReenteringEndDragEmitsNothingFurther()
        {
            PointerEventData data = Pointer(0, PressX);

            controller.DecisionConfirmed += _ => controller.OnEndDrag(data);

            controller.OnBeginDrag(data);
            data.position = new Vector2(PressX + Threshold, PressY);
            controller.OnDrag(data);
            controller.OnEndDrag(data);

            Assert.That(confirmed.Count, Is.EqualTo(1));
        }

        [Test]
        public void AHandlerThatResetsPreventsTheExitFromRunning()
        {
            controller.DecisionConfirmed += _ => controller.ResetForNextCard();

            Swipe(PressX + Threshold);

            Assert.That(confirmed.Count, Is.EqualTo(1));
            Assert.That(exited, Is.Empty, "the controller must not finish an exit it no longer owns");
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
        }

        [Test]
        public void ARepeatedBeginDragDuringADragIsIgnored()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);

            data.position = new Vector2(PressX + 100f, PressY);
            controller.OnDrag(data);
            float afterFirstDrag = card.anchoredPosition.x;

            controller.OnBeginDrag(Pointer(0, PressX + 400f));

            Assert.That(card.anchoredPosition.x, Is.EqualTo(afterFirstDrag).Within(0.001f));
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Dragging));
        }

        [TestCase(CardSwipeState.Exiting)]
        [TestCase(CardSwipeState.Completed)]
        public void ANewDragIsRefusedOnceTheCardIsDecided(CardSwipeState unused)
        {
            Swipe(PressX + Threshold);
            Assert.That(controller.IsInteractable, Is.False);

            controller.OnBeginDrag(Pointer(0, PressX));

            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Completed));
        }

        // --- Pointer ownership --------------------------------------------------------

        [Test]
        public void AForeignPointerCannotSteerTheDrag()
        {
            PointerEventData owner = Pointer(0, PressX);
            controller.OnBeginDrag(owner);

            owner.position = new Vector2(PressX + 100f, PressY);
            controller.OnDrag(owner);
            float owned = card.anchoredPosition.x;

            controller.OnDrag(Pointer(1, PressX + 400f));

            Assert.That(card.anchoredPosition.x, Is.EqualTo(owned).Within(0.001f));
        }

        [Test]
        public void AForeignPointerCannotConfirmTheDrag()
        {
            PointerEventData owner = Pointer(0, PressX);
            controller.OnBeginDrag(owner);
            owner.position = new Vector2(PressX + Threshold, PressY);
            controller.OnDrag(owner);

            controller.OnEndDrag(Pointer(1, PressX + Threshold));

            Assert.That(confirmed, Is.Empty);
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Dragging));
        }

        [Test]
        public void TheOwningPointerStillConfirmsAfterAForeignOneIsIgnored()
        {
            PointerEventData owner = Pointer(0, PressX);
            controller.OnBeginDrag(owner);
            owner.position = new Vector2(PressX + Threshold, PressY);
            controller.OnDrag(owner);

            controller.OnEndDrag(Pointer(7, PressX));
            controller.OnEndDrag(owner);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }));
        }

        // --- Live drag behaviour ---------------------------------------------------------

        [Test]
        public void TheCardMovesHorizontallyOnlyWithNoLift()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);

            data.position = new Vector2(PressX + 150f, PressY + 400f);
            controller.OnDrag(data);

            Assert.That(card.anchoredPosition.x, Is.EqualTo(150f).Within(0.001f));
            // Vertical pointer movement (PressY + 400 above) is read and discarded as an input
            // signal — the card must stay a plain rectangle translating on X only, so Y never
            // moves regardless of drag distance.
            Assert.That(card.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DraggingRightRaisesOnlyTheRightPreview()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);

            data.position = new Vector2(PressX + (Threshold * 0.5f), PressY);
            controller.OnDrag(data);

            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right),
                Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Left), Is.EqualTo(0f));
        }

        [Test]
        public void DraggingLeftRaisesOnlyTheLeftPreview()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);

            data.position = new Vector2(PressX - (Threshold * 0.5f), PressY);
            controller.OnDrag(data);

            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Left),
                Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(0f));
        }

        [Test]
        public void TheCardRotatesWithTheDragAndClampsAtTheMaximumWithNoScale()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);

            data.position = new Vector2(PressX + 5000f, PressY);
            controller.OnDrag(data);

            // Well past the confirm threshold: rotation must be clamped at the authored maximum
            // (12 deg, the SetAuthoringReferences default used by this fixture's SetUp) — the same
            // one whole rigid card (mask, portrait, previews together), never scaled/stretched.
            Assert.That(Mathf.DeltaAngle(0f, card.localRotation.eulerAngles.z),
                Is.EqualTo(-12f).Within(0.01f));
            Assert.That(card.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void ThresholdDistanceIsDerivedFromTheParentWidth()
        {
            Assert.That(controller.ThresholdDistance, Is.EqualTo(Threshold).Within(0.001f));

            parent.sizeDelta = new Vector2(2000f, 1600f);

            Assert.That(controller.ThresholdDistance, Is.EqualTo(500f).Within(0.001f),
                "a wider parent means a longer swipe, so the gesture scales with the screen");
        }

        // --- Controls settings -------------------------------------------------------------

        [Test]
        public void ConfirmSideRaisesExactlyOneDecisionForTheGivenSide()
        {
            controller.ConfirmSide(ChoiceSide.Left);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Left }));
            Assert.That(exited, Is.EqualTo(new[] { ChoiceSide.Left }));
            Assert.That(controller.HasDecision, Is.True);
        }

        [Test]
        public void ConfirmSideIsIgnoredWhileADragIsInProgress()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);

            controller.ConfirmSide(ChoiceSide.Right);

            Assert.That(confirmed, Is.Empty,
                "a tap button must not resolve a card mid-drag — only one input wins per card");
        }

        [Test]
        public void ConfirmSideIsIgnoredOnceADecisionIsAlreadyMade()
        {
            controller.ConfirmSide(ChoiceSide.Left);
            controller.ConfirmSide(ChoiceSide.Right);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Left }),
                "the exactly-once guarantee must hold for repeated taps too");
        }

        [Test]
        public void SetInvertRotationFlipsTheLeanDirection()
        {
            controller.SetInvertRotation(true);

            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);
            data.position = new Vector2(PressX + 5000f, PressY);
            controller.OnDrag(data);

            Assert.That(Mathf.DeltaAngle(0f, card.localRotation.eulerAngles.z),
                Is.EqualTo(12f).Within(0.01f),
                "inverted from the default clockwise-on-right-drag rotation");
        }

        [Test]
        public void SetInvertRotationFlipsWhichSideConfirms()
        {
            controller.SetInvertRotation(true);

            Swipe(PressX + Threshold + 50f);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Left }),
                "Invert Swipe Direction: a right drag must resolve as the Left choice");
        }

        [Test]
        public void InvertedDragPreviewShowsTheSideThatWillActuallyBeConfirmed()
        {
            controller.SetInvertRotation(true);

            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);
            data.position = new Vector2(PressX + (Threshold * 0.5f), PressY);
            controller.OnDrag(data);

            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Left),
                Is.EqualTo(0.5f).Within(0.001f),
                "a right drag must preview Left, matching what release will confirm");
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(0f));
        }

        [Test]
        public void InvertedDragStillExitsTowardsThePhysicalDragDirection()
        {
            controller.SetInvertRotation(true);

            Swipe(PressX + Threshold + 50f);

            float trailingEdge = card.anchoredPosition.x - (CardWidth * 0.5f);
            Assert.That(trailingEdge, Is.GreaterThan(ParentWidth * 0.5f),
                "the card must keep flying off to the right it was dragged towards, even though "
                    + "the Left choice was confirmed — reversing mid-flight would look broken");
        }

        [Test]
        public void InvertingTwiceRestoresTheOriginalMapping()
        {
            controller.SetInvertRotation(true);
            controller.SetInvertRotation(false);

            Swipe(PressX + Threshold + 50f);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }));
        }

        [Test]
        public void ConfirmSideIgnoresInversion()
        {
            controller.SetInvertRotation(true);

            controller.ConfirmSide(ChoiceSide.Left);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Left }),
                "a tapped side already says exactly what it means, regardless of the swipe setting");
        }

        [Test]
        public void MaximumSensitivityShortensTheConfirmThreshold()
        {
            controller.SetSwipeSensitivity(1f);

            Assert.That(controller.ThresholdDistance, Is.LessThan(Threshold),
                "the most sensitive setting must need less drag than the authored default");
        }

        [Test]
        public void MinimumSensitivityLengthensTheConfirmThreshold()
        {
            controller.SetSwipeSensitivity(0f);

            Assert.That(controller.ThresholdDistance, Is.GreaterThan(Threshold),
                "the least sensitive setting must need more drag than the authored default");
        }

        [Test]
        public void DefaultSensitivityReproducesTheAuthoredThreshold()
        {
            controller.SetSwipeSensitivity(0.5f);

            Assert.That(controller.ThresholdDistance, Is.EqualTo(Threshold).Within(0.001f));
        }

        [Test]
        public void DisablingSwipeInputSuppressesDragConfirmation()
        {
            controller.SetSwipeInputEnabled(false);

            Swipe(PressX + Threshold + 50f);

            Assert.That(confirmed, Is.Empty,
                "with swipe disabled, a drag must never resolve the card");
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
        }

        [Test]
        public void DisablingSwipeInputStillAllowsConfirmSide()
        {
            controller.SetSwipeInputEnabled(false);

            controller.ConfirmSide(ChoiceSide.Left);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Left }),
                "a tap-button decision must keep working even with the swipe gesture disabled");
        }

        [Test]
        public void ReEnablingSwipeInputRestoresDragConfirmation()
        {
            controller.SetSwipeInputEnabled(false);
            controller.SetSwipeInputEnabled(true);

            Swipe(PressX + Threshold + 50f);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }));
        }

        // --- Reset and cancel -------------------------------------------------------------

        [Test]
        public void ResetRestoresEverything()
        {
            Swipe(PressX + Threshold);

            controller.ResetForNextCard();

            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
            Assert.That(controller.IsInteractable, Is.True);
            Assert.That(controller.HasDecision, Is.False);
            Assert.That(controller.ConfirmedSide, Is.Null);
            Assert.That(card.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(card.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(cardView.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(0f));
        }

        [Test]
        public void ResetIsIdempotent()
        {
            Swipe(PressX + Threshold);

            controller.ResetForNextCard();
            CardSwipeState afterFirst = controller.State;
            Vector2 positionAfterFirst = card.anchoredPosition;

            controller.ResetForNextCard();
            controller.ResetForNextCard();

            Assert.That(controller.State, Is.EqualTo(afterFirst));
            Assert.That(card.anchoredPosition, Is.EqualTo(positionAfterFirst));
            Assert.That(confirmed.Count, Is.EqualTo(1), "resetting emits nothing");
            Assert.That(exited.Count, Is.EqualTo(1));
        }

        [Test]
        public void AResetCardAcceptsANewDecision()
        {
            Swipe(PressX + Threshold);
            controller.ResetForNextCard();

            Swipe(PressX - Threshold);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right, ChoiceSide.Left }));
        }

        [Test]
        public void CancelDuringADragEmitsNothingAndReturnsToNeutral()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);
            data.position = new Vector2(PressX + 200f, PressY);
            controller.OnDrag(data);

            controller.CancelInteraction();

            Assert.That(confirmed, Is.Empty);
            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
            Assert.That(card.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CancelAfterADecisionIsANoOp()
        {
            Swipe(PressX + Threshold);

            controller.CancelInteraction();

            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Completed),
                "a decision already delivered cannot be taken back here");
            Assert.That(controller.HasDecision, Is.True);
        }

        [Test]
        public void ADroppedPointerLeavesTheControllerUsable()
        {
            PointerEventData data = Pointer(0, PressX);
            controller.OnBeginDrag(data);
            controller.CancelInteraction();

            Swipe(PressX + Threshold);

            Assert.That(confirmed, Is.EqualTo(new[] { ChoiceSide.Right }),
                "the abandoned pointer must not block the next interaction");
        }

        // --- Robustness ---------------------------------------------------------------------

        [Test]
        public void AControllerWithNoCardViewDoesNotThrow()
        {
            GameObject bareObject = PresentationTestObjects.CreateObject("Bare");
            RectTransform bare = bareObject.GetComponent<RectTransform>();
            bare.SetParent(parent, false);

            CardSwipeController bareController = bareObject.AddComponent<CardSwipeController>();

            Assert.That(() =>
            {
                PointerEventData data = Pointer(0, PressX);
                bareController.OnBeginDrag(data);
                data.position = new Vector2(PressX + 400f, PressY);
                bareController.OnDrag(data);
                bareController.OnEndDrag(data);
                bareController.ResetForNextCard();
                bareController.CancelInteraction();
            }, Throws.Nothing);
        }

        [Test]
        public void NullPointerDataIsIgnored()
        {
            Assert.That(() =>
            {
                controller.OnBeginDrag(null);
                controller.OnDrag(null);
                controller.OnEndDrag(null);
            }, Throws.Nothing);

            Assert.That(controller.State, Is.EqualTo(CardSwipeState.Idle));
        }

        [Test]
        public void TheControllerDoesNoPerFrameWork()
        {
            // No Update means a card at rest costs nothing, which is CLAUDE.md §10 taken literally.
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Assert.That(typeof(CardSwipeController).GetMethod("Update", flags), Is.Null);
            Assert.That(typeof(CardSwipeController).GetMethod("LateUpdate", flags), Is.Null);
            Assert.That(typeof(CardSwipeController).GetMethod("FixedUpdate", flags), Is.Null);
        }

        [Test]
        public void TheControllerReferencesNoGameplayServices()
        {
            // A compile-time guarantee already, but this fails loudly if someone adds a field.
            foreach (FieldInfo field in typeof(CardSwipeController).GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                string typeName = field.FieldType.Name;

                Assert.That(typeName, Does.Not.Contain("Resolver"));
                Assert.That(typeName, Does.Not.Contain("Service"));
                Assert.That(typeName, Does.Not.Contain("RunState"));
                Assert.That(typeName, Does.Not.Contain("StatSystem"));
            }
        }
    }
}
