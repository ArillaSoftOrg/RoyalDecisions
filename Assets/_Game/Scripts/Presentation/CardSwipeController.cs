using System;
using System.Collections;
using RoyalDecisions.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Turns a drag into a decision. Reports it, and does nothing about it.
    /// </summary>
    /// <remarks>
    /// CLAUDE.md §9: "No story, stat, save, or ending logic belongs in CardSwipeController." This
    /// component moves a RectTransform, drives preview strengths through <see cref="CardView"/>, and
    /// raises two events. Phase 7 subscribes and decides what a decision means.
    ///
    /// There is deliberately no <c>Update</c>. Drags arrive as pointer callbacks and animations run
    /// as coroutines, so a card sitting at rest costs nothing per frame.
    /// </remarks>
    public sealed class CardSwipeController : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private const int NoPointer = int.MinValue;

        [Header("References")]
        [SerializeField] private CardView cardView;

        [Tooltip("Space the drag is measured in. Defaults to the card's parent.")]
        [SerializeField] private RectTransform dragParent;

        [Header("Threshold")]
        [Tooltip("Fraction of the parent's width the card must cross to confirm.")]
        [Range(0.05f, 0.9f)]
        [SerializeField] private float thresholdRatio = 0.25f;

        [Tooltip("Floor in UI units, so an unlaid-out parent cannot produce a zero threshold.")]
        [SerializeField] private float minimumThresholdDistance = 40f;

        [Header("Motion")]
        [Range(0.1f, 3f)]
        [SerializeField] private float movementMultiplier = 1f;

        [Range(0f, 90f)]
        [SerializeField] private float maxRotationDegrees = 12f;

        [SerializeField] private bool rotateClockwiseOnRightDrag = true;

        [Header("Animation")]
        [SerializeField] private float snapBackDuration = 0.18f;

        [SerializeField] private float exitDuration = 0.25f;

        [SerializeField] private AnimationCurve snapBackEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField] private AnimationCurve exitEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Extra card widths travelled past the parent edge.")]
        [SerializeField] private float exitMarginMultiplier = 1f;

        /// <summary>Raised once per card, the instant a release is confirmed.</summary>
        public event Action<ChoiceSide> DecisionConfirmed;

        /// <summary>Raised once the card has finished leaving the screen.</summary>
        public event Action<ChoiceSide> ExitAnimationCompleted;

        /// <summary>Reports the active drag side and normalized visual strength.</summary>
        public event Action<ChoiceSide, float> ChoicePreviewChanged;

        /// <summary>Reports that no choice-impact preview should remain visible.</summary>
        public event Action ChoicePreviewCleared;

        /// <summary>Raised when a released drag starts returning to center without a decision.</summary>
        public event Action SnapBackStarted;

        private int activePointerId = NoPointer;
        private Vector2 pressLocalPoint;
        private Vector2 initialAnchoredPosition;
        private Quaternion initialRotation = Quaternion.identity;
        private bool hasCapturedNeutral;
        private float currentDisplacement;
        private Coroutine runningAnimation;
        private ChoiceSide? confirmedSide;
        private bool hasPublishedChoicePreview;
        private bool accessibilityDefaultsCaptured;
        private float defaultMaxRotation;
        private float defaultSnapBackDuration;
        private float defaultExitDuration;
        private bool controlsDefaultsCaptured;
        private bool defaultRotateClockwiseOnRightDrag;

        public CardSwipeState State { get; private set; } = CardSwipeState.Idle;

        public bool IsInteractable => State == CardSwipeState.Idle;

        public bool IsAnimating =>
            State == CardSwipeState.SnappingBack || State == CardSwipeState.Exiting;

        /// <summary>True once a decision has been emitted, until the card is reset.</summary>
        public bool HasDecision => confirmedSide.HasValue;

        public ChoiceSide? ConfirmedSide => confirmedSide;

        public float CurrentDisplacement => currentDisplacement;

        public float ThresholdDistance => SwipeMath.ThresholdDistance(
            ParentWidth, thresholdRatio, minimumThresholdDistance);

        private float ParentWidth
        {
            get
            {
                RectTransform parent = ResolveDragParent();
                return parent != null ? parent.rect.width : 0f;
            }
        }

        private float CardWidth
        {
            get
            {
                RectTransform card = ResolveCardRoot();
                return card != null ? card.rect.width : 0f;
            }
        }

        private void Awake()
        {
            CaptureNeutralOnce();
        }

        // --- Pointer handling -------------------------------------------------

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null || State != CardSwipeState.Idle || activePointerId != NoPointer)
            {
                return;
            }

            RectTransform parent = ResolveDragParent();
            if (parent == null || !TryGetLocalPoint(parent, eventData, out Vector2 local))
            {
                return;
            }

            CaptureNeutralOnce();

            activePointerId = eventData.pointerId;
            pressLocalPoint = local;
            currentDisplacement = 0f;
            State = CardSwipeState.Dragging;

            ApplyDisplacement(0f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsActivePointer(eventData) || State != CardSwipeState.Dragging)
            {
                return;
            }

            RectTransform parent = ResolveDragParent();
            if (parent == null || !TryGetLocalPoint(parent, eventData, out Vector2 local))
            {
                return;
            }

            // Horizontal only for the MVP: the vertical component is read and discarded.
            currentDisplacement = local.x - pressLocalPoint.x;
            ApplyDisplacement(currentDisplacement);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsActivePointer(eventData) || State != CardSwipeState.Dragging)
            {
                return;
            }

            activePointerId = NoPointer;

            if (!HasDecision && SwipeMath.IsConfirmed(currentDisplacement, ThresholdDistance))
            {
                Confirm(SwipeMath.SideFor(currentDisplacement));
                return;
            }

            BeginSnapBack();
        }

        /// <summary>Only the pointer that began the interaction is honoured.</summary>
        private bool IsActivePointer(PointerEventData eventData)
        {
            return eventData != null
                && activePointerId != NoPointer
                && eventData.pointerId == activePointerId;
        }

        // --- Public control ----------------------------------------------------

        /// <summary>
        /// Prepares the card for the next presentation. Safe to call repeatedly.
        /// </summary>
        /// <remarks>
        /// Named for its purpose rather than <c>Reset</c>, which Unity reserves as a message sent
        /// when a component is reset from the Inspector.
        /// </remarks>
        public void ResetForNextCard()
        {
            StopRunningAnimation();

            activePointerId = NoPointer;
            currentDisplacement = 0f;
            confirmedSide = null;

            CaptureNeutralOnce();
            RestoreNeutral();

            State = CardSwipeState.Idle;
        }

        /// <summary>
        /// Abandons an in-flight drag or snap-back without emitting anything. A no-op once a
        /// decision has been made — that cannot be taken back here.
        /// </summary>
        public void CancelInteraction()
        {
            if (State == CardSwipeState.Completed || State == CardSwipeState.Exiting)
            {
                return;
            }

            StopRunningAnimation();

            activePointerId = NoPointer;
            currentDisplacement = 0f;
            RestoreNeutral();

            State = CardSwipeState.Idle;
        }

        public void SetReducedMotion(bool enabled)
        {
            if (!accessibilityDefaultsCaptured)
            {
                defaultMaxRotation = maxRotationDegrees;
                defaultSnapBackDuration = snapBackDuration;
                defaultExitDuration = exitDuration;
                accessibilityDefaultsCaptured = true;
            }
            maxRotationDegrees = enabled ? 4f : defaultMaxRotation;
            snapBackDuration = enabled ? Mathf.Min(defaultSnapBackDuration, 0.05f)
                : defaultSnapBackDuration;
            exitDuration = enabled ? Mathf.Min(defaultExitDuration, 0.05f)
                : defaultExitDuration;
        }

        /// <summary>
        /// Confirms a choice directly — an alternate input to a physical drag, e.g. an on-screen
        /// tap button. Reuses the same single-resolution <see cref="Confirm"/> path as a completed
        /// drag, so a tap and a swipe can never both resolve the same card.
        /// </summary>
        public void ConfirmSide(ChoiceSide side)
        {
            if (!IsInteractable)
            {
                return;
            }

            CaptureNeutralOnce();
            Confirm(side);
        }

        /// <summary>
        /// Flips the card's visual lean direction only. Which side a drag confirms is unaffected —
        /// that mapping lives in <see cref="SwipeMath.SideFor"/> and CLAUDE.md §9 pins it down.
        /// </summary>
        public void SetInvertRotation(bool invert)
        {
            CaptureControlsDefaultsOnce();
            rotateClockwiseOnRightDrag = invert
                ? !defaultRotateClockwiseOnRightDrag
                : defaultRotateClockwiseOnRightDrag;
        }

        private void CaptureControlsDefaultsOnce()
        {
            if (controlsDefaultsCaptured)
            {
                return;
            }

            defaultRotateClockwiseOnRightDrag = rotateClockwiseOnRightDrag;
            controlsDefaultsCaptured = true;
        }

        // --- Confirmation --------------------------------------------------------

        private void Confirm(ChoiceSide side)
        {
            // Locked before any external handler runs: a subscriber that calls back in finds a
            // controller that has already left Idle and already recorded its decision.
            confirmedSide = side;
            State = CardSwipeState.Exiting;

            // The confirmed side stays lit while the card flies out.
            if (cardView != null)
            {
                cardView.SetChoicePreviews(
                    side == ChoiceSide.Left ? 1f : 0f,
                    side == ChoiceSide.Right ? 1f : 0f);
            }

            ClearPublishedChoicePreview();

            DecisionConfirmed?.Invoke(side);

            // A handler may have reset or disabled us; do not start an exit we no longer own.
            if (State != CardSwipeState.Exiting)
            {
                return;
            }

            BeginExit(side);
        }

        // --- Animation ------------------------------------------------------------

        private void BeginSnapBack()
        {
            State = CardSwipeState.SnappingBack;
            SnapBackStarted?.Invoke();

            if (!CanRunCoroutines() || snapBackDuration <= 0f)
            {
                FinishSnapBack();
                return;
            }

            runningAnimation = StartCoroutine(SnapBackRoutine());
        }

        private IEnumerator SnapBackRoutine()
        {
            RectTransform card = ResolveCardRoot();

            Vector2 startPosition = card != null ? card.anchoredPosition : initialAnchoredPosition;
            Quaternion startRotation = card != null ? card.localRotation : initialRotation;
            float startLeft = PreviewStrength(ChoiceSide.Left);
            float startRight = PreviewStrength(ChoiceSide.Right);

            float elapsed = 0f;

            while (elapsed < snapBackDuration)
            {
                // Unscaled: presentation feedback must keep moving even when gameplay time does not.
                elapsed += Time.unscaledDeltaTime;
                float t = Evaluate(snapBackEase, elapsed / snapBackDuration);

                if (card != null)
                {
                    card.anchoredPosition = Vector2.Lerp(startPosition, initialAnchoredPosition, t);
                    card.localRotation = Quaternion.Slerp(startRotation, initialRotation, t);
                }

                if (cardView != null)
                {
                    float left = Mathf.Lerp(startLeft, 0f, t);
                    float right = Mathf.Lerp(startRight, 0f, t);
                    cardView.SetChoicePreviews(left, right);
                    PublishChoicePreview(left, right);
                }

                yield return null;
            }

            FinishSnapBack();
        }

        private void FinishSnapBack()
        {
            RestoreNeutral();

            runningAnimation = null;
            currentDisplacement = 0f;
            State = CardSwipeState.Idle;
        }

        private void BeginExit(ChoiceSide side)
        {
            if (!CanRunCoroutines() || exitDuration <= 0f)
            {
                FinishExit(side);
                return;
            }

            runningAnimation = StartCoroutine(ExitRoutine(side));
        }

        private IEnumerator ExitRoutine(ChoiceSide side)
        {
            RectTransform card = ResolveCardRoot();

            Vector2 startPosition = card != null ? card.anchoredPosition : initialAnchoredPosition;
            Vector2 endPosition = ExitTarget(side);

            float elapsed = 0f;

            while (elapsed < exitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Evaluate(exitEase, elapsed / exitDuration);

                if (card != null)
                {
                    card.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, t);
                }

                yield return null;
            }

            FinishExit(side);
        }

        private void FinishExit(ChoiceSide side)
        {
            RectTransform card = ResolveCardRoot();
            if (card != null)
            {
                card.anchoredPosition = ExitTarget(side);
            }

            runningAnimation = null;

            // Completed before the event, so a re-entrant handler finds a settled controller.
            State = CardSwipeState.Completed;

            ExitAnimationCompleted?.Invoke(side);
        }

        private Vector2 ExitTarget(ChoiceSide side)
        {
            float x = SwipeMath.ExitTargetX(
                initialAnchoredPosition.x, side, ParentWidth, CardWidth, exitMarginMultiplier);

            return new Vector2(x, initialAnchoredPosition.y);
        }

        private void StopRunningAnimation()
        {
            if (runningAnimation == null)
            {
                return;
            }

            if (CanRunCoroutines())
            {
                StopCoroutine(runningAnimation);
            }

            runningAnimation = null;
        }

        private bool CanRunCoroutines()
        {
            return Application.isPlaying && isActiveAndEnabled;
        }

        private static float Evaluate(AnimationCurve curve, float rawProgress)
        {
            float t = Mathf.Clamp01(rawProgress);
            return curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
        }

        // --- Applying state to the view ----------------------------------------------

        private void ApplyDisplacement(float displacement)
        {
            float threshold = ThresholdDistance;
            RectTransform card = ResolveCardRoot();

            if (card != null)
            {
                card.anchoredPosition = new Vector2(
                    initialAnchoredPosition.x + (displacement * movementMultiplier),
                    initialAnchoredPosition.y);

                card.localRotation = initialRotation * Quaternion.Euler(
                    0f,
                    0f,
                    SwipeMath.Rotation(
                        displacement, threshold, maxRotationDegrees, rotateClockwiseOnRightDrag));
            }

            SwipeMath.PreviewStrengths(displacement, threshold, out float left, out float right);

            if (cardView != null)
            {
                cardView.SetChoicePreviews(left, right);
            }

            PublishChoicePreview(left, right);
        }

        private void RestoreNeutral()
        {
            RectTransform card = ResolveCardRoot();
            if (card != null)
            {
                card.anchoredPosition = initialAnchoredPosition;
                card.localRotation = initialRotation;
            }

            if (cardView != null)
            {
                cardView.ClearChoicePreviews();
            }

            ClearPublishedChoicePreview();
        }

        private void PublishChoicePreview(float left, float right)
        {
            float strength = Mathf.Max(left, right);
            if (strength <= 0f)
            {
                ClearPublishedChoicePreview();
                return;
            }

            hasPublishedChoicePreview = true;
            ChoicePreviewChanged?.Invoke(
                left > right ? ChoiceSide.Left : ChoiceSide.Right,
                Mathf.Clamp01(strength));
        }

        private void ClearPublishedChoicePreview()
        {
            if (!hasPublishedChoicePreview)
            {
                return;
            }

            hasPublishedChoicePreview = false;
            ChoicePreviewCleared?.Invoke();
        }

        private float PreviewStrength(ChoiceSide side)
        {
            return cardView != null ? cardView.GetChoicePreviewStrength(side) : 0f;
        }

        private void CaptureNeutralOnce()
        {
            if (hasCapturedNeutral)
            {
                return;
            }

            RectTransform card = ResolveCardRoot();
            if (card == null)
            {
                return;
            }

            initialAnchoredPosition = card.anchoredPosition;
            initialRotation = card.localRotation;
            hasCapturedNeutral = true;
        }

        private RectTransform ResolveCardRoot()
        {
            if (cardView != null)
            {
                return cardView.CardRoot;
            }

            return transform as RectTransform;
        }

        private RectTransform ResolveDragParent()
        {
            if (dragParent != null)
            {
                return dragParent;
            }

            RectTransform card = ResolveCardRoot();
            return card != null ? card.parent as RectTransform : null;
        }

        private static bool TryGetLocalPoint(
            RectTransform parent,
            PointerEventData eventData,
            out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out local);
        }

        // --- Lifecycle ------------------------------------------------------------------

        private void OnDisable()
        {
            StopRunningAnimation();
            activePointerId = NoPointer;

            switch (State)
            {
                case CardSwipeState.Dragging:
                case CardSwipeState.SnappingBack:
                    // Nothing was decided, so the card returns to neutral and becomes available.
                    RestoreNeutral();
                    currentDisplacement = 0f;
                    State = CardSwipeState.Idle;
                    break;

                case CardSwipeState.Exiting:
                    // The decision already went out. Stay locked: ExitAnimationCompleted must not
                    // fire, and this card must never accept a second swipe.
                    State = CardSwipeState.Completed;
                    break;
            }
        }

        private void OnValidate()
        {
            thresholdRatio = Mathf.Clamp(thresholdRatio, 0.05f, 0.9f);
            minimumThresholdDistance = Mathf.Max(
                SwipeMath.AbsoluteMinimumThreshold, minimumThresholdDistance);
            movementMultiplier = Mathf.Clamp(movementMultiplier, 0.1f, 3f);
            maxRotationDegrees = Mathf.Clamp(maxRotationDegrees, 0f, 90f);
            snapBackDuration = Mathf.Max(0f, snapBackDuration);
            exitDuration = Mathf.Max(0f, exitDuration);
            exitMarginMultiplier = Mathf.Max(0f, exitMarginMultiplier);
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by prefab setup and tests.</summary>
        public void SetAuthoringReferences(
            CardView view,
            RectTransform parent,
            float threshold = 0.25f,
            float minimumThreshold = 40f,
            float snapDuration = 0.18f,
            float exitSeconds = 0.25f,
            float maxRotation = 12f,
            float movement = 1f,
            float exitMargin = 1f)
        {
            cardView = view;
            dragParent = parent;
            thresholdRatio = threshold;
            minimumThresholdDistance = minimumThreshold;
            snapBackDuration = snapDuration;
            exitDuration = exitSeconds;
            maxRotationDegrees = maxRotation;
            movementMultiplier = movement;
            exitMarginMultiplier = exitMargin;
        }
#endif
    }
}
