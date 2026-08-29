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

        /// <summary>At sensitivity 0, the confirm threshold is this many times the authored default.</summary>
        private const float LeastSensitiveRatioScale = 1.4f;

        /// <summary>At sensitivity 1, the confirm threshold is this many times the authored default.</summary>
        private const float MostSensitiveRatioScale = 0.6f;

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

        [Tooltip("Ease exponent applied to signed drag progress before it drives rotation — above "
            + "1 holds back early in the drag (~2.5-3.5 deg around 50% progress) then ramps up to "
            + "the full maxRotationDegrees near the threshold, reading as a tactile physical tilt "
            + "rather than a mechanically linear one. SwipeMath.Rotation's own formula, and the "
            + "confirm threshold, are untouched by this — it only pre-warps its input.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float rotationEaseExponent = 1.15f;

        [Tooltip("Small vertical rise as the card is dragged toward either side.")]
        [SerializeField] private float maxDragLift = 18f;

        [Tooltip("Subtle scale-up while dragging toward either side. 1 = no scale response.")]
        [Range(1f, 1.1f)]
        [SerializeField] private float maxDragScale = 1.02f;

        [Header("Animation")]
        [SerializeField] private float snapBackDuration = 0.18f;

        [SerializeField] private float exitDuration = 0.25f;

        [SerializeField] private AnimationCurve snapBackEase = BuildSnapBackSpringCurve();

        [SerializeField] private AnimationCurve exitEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Extra card widths travelled past the parent edge.")]
        [SerializeField] private float exitMarginMultiplier = 1f;

        [Header("Committed Exit")]
        [Tooltip("Rotation the card reaches by the end of its exit — independent of the drag-time "
            + "maxRotationDegrees, so the \"thrown card\" can lean further than the drag itself "
            + "ever visually reached.")]
        [SerializeField] private float exitRotationDegrees = 12f;

        [Tooltip("Small upward arc during the exit throw, peaking mid-flight.")]
        [SerializeField] private float exitArcHeight = 36f;

        [Tooltip("Subtle scale-up during the exit throw.")]
        [Range(1f, 1.15f)]
        [SerializeField] private float exitScale = 1.04f;

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
        private Vector3 initialScale = Vector3.one;
        private bool hasCapturedNeutral;
        private float currentDisplacement;
        private Coroutine runningAnimation;
        private ChoiceSide? confirmedSide;
        private bool hasPublishedChoicePreview;
        private bool accessibilityDefaultsCaptured;
        private float defaultMaxRotation;
        private float defaultSnapBackDuration;
        private float defaultExitDuration;
        private float defaultMaxDragLift;
        private float defaultMaxDragScale;
        private float defaultExitRotationDegrees;
        private float defaultExitArcHeight;
        private float defaultExitScale;
        private bool controlsDefaultsCaptured;
        private bool defaultRotateClockwiseOnRightDrag;
        private bool sensitivityDefaultCaptured;
        private float defaultThresholdRatio;
        private bool swipeInputEnabled = true;
        private bool decisionMappingInverted;
        private bool leftSideAvailable = true;
        private bool rightSideAvailable = true;

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
            if (!swipeInputEnabled
                || eventData == null
                || State != CardSwipeState.Idle
                || activePointerId != NoPointer)
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
                // The card keeps flying off in the direction it was physically dragged (exitSide);
                // which ChoiceDefinition that resolves to (logicalSide) is what Invert Swipe
                // Direction flips. Splitting the two keeps the exit animation and the confirmed
                // decision each internally consistent instead of the card reversing mid-flight.
                ChoiceSide physicalSide = SwipeMath.SideFor(currentDisplacement);
                ChoiceSide logicalSide = LogicalSide(physicalSide);

                // An unavailable side never confirms, drag or not — it always snaps back, the same
                // as a drag that never reached the threshold.
                if (IsSideAvailable(logicalSide))
                {
                    Confirm(logicalSide, physicalSide);
                    return;
                }
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
        /// Restores the card root's neutral position/rotation/scale without touching
        /// <see cref="State"/> or re-arming input. For a presentation-only transition (the card
        /// flip) that must reposition the card root mid-sequence while keeping the controller
        /// locked until it finishes and calls <see cref="ResetForNextCard"/> itself.
        /// </summary>
        public void RestoreNeutralGeometry()
        {
            RestoreNeutral();
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
                defaultMaxDragLift = maxDragLift;
                defaultMaxDragScale = maxDragScale;
                defaultExitRotationDegrees = exitRotationDegrees;
                defaultExitArcHeight = exitArcHeight;
                defaultExitScale = exitScale;
                accessibilityDefaultsCaptured = true;
            }
            maxRotationDegrees = enabled ? 4f : defaultMaxRotation;
            snapBackDuration = enabled ? Mathf.Min(defaultSnapBackDuration, 0.05f)
                : defaultSnapBackDuration;
            exitDuration = enabled ? Mathf.Min(defaultExitDuration, 0.05f)
                : defaultExitDuration;
            // The arc/scale flourishes are exactly the kind of non-essential motion Reduced
            // Motion is meant to strip out — suppressed the same way rotation and duration are.
            maxDragLift = enabled ? 0f : defaultMaxDragLift;
            maxDragScale = enabled ? 1f : defaultMaxDragScale;
            exitRotationDegrees = enabled
                ? Mathf.Min(defaultExitRotationDegrees, 4f)
                : defaultExitRotationDegrees;
            exitArcHeight = enabled ? 0f : defaultExitArcHeight;
            exitScale = enabled ? 1f : defaultExitScale;
        }

        /// <summary>
        /// Confirms a choice directly — an alternate input to a physical drag, e.g. an on-screen
        /// tap button. Reuses the same single-resolution <see cref="Confirm(ChoiceSide)"/> path as
        /// a completed drag, so a tap and a swipe can never both resolve the same card. Never
        /// affected by Invert Swipe Direction: a tapped side already says exactly what it means.
        /// </summary>
        public void ConfirmSide(ChoiceSide side)
        {
            if (!IsInteractable || !IsSideAvailable(side))
            {
                return;
            }

            CaptureNeutralOnce();
            Confirm(side);
        }

        /// <summary>
        /// Invert Swipe Direction: flips the card's visual lean <em>and</em> which
        /// <see cref="ChoiceSide"/> a drag confirms (a right drag confirms Left, and vice versa),
        /// so the tilt and the outcome always agree with each other. The card's physical position
        /// and exit trajectory stay tied to the actual drag direction regardless — inverting where
        /// the card itself moves relative to the player's finger would feel broken, not accessible.
        /// See <see cref="LogicalSide"/> and <see cref="Confirm(ChoiceSide, ChoiceSide)"/>.
        /// </summary>
        public void SetInvertRotation(bool invert)
        {
            CaptureControlsDefaultsOnce();
            rotateClockwiseOnRightDrag = invert
                ? !defaultRotateClockwiseOnRightDrag
                : defaultRotateClockwiseOnRightDrag;
            decisionMappingInverted = invert;
        }

        /// <summary>Applies the Invert Swipe Direction setting to a raw (physical) drag side.</summary>
        private ChoiceSide LogicalSide(ChoiceSide physicalSide)
        {
            if (!decisionMappingInverted)
            {
                return physicalSide;
            }

            return physicalSide == ChoiceSide.Left ? ChoiceSide.Right : ChoiceSide.Left;
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

        /// <summary>
        /// Maps a normalized 0..1 sensitivity onto the confirm threshold: 0 needs the widest drag
        /// to confirm, 1 the shortest. The authored inspector value is captured once and treated as
        /// the midpoint (0.5, the settings default), so the default setting reproduces today's
        /// authored feel exactly instead of a hardcoded absolute.
        /// </summary>
        public void SetSwipeSensitivity(float normalizedValue)
        {
            CaptureSensitivityDefaultOnce();

            float clamped = Mathf.Clamp01(normalizedValue);
            float leastSensitiveRatio = defaultThresholdRatio * LeastSensitiveRatioScale;
            float mostSensitiveRatio = defaultThresholdRatio * MostSensitiveRatioScale;
            thresholdRatio = Mathf.Clamp(
                Mathf.Lerp(leastSensitiveRatio, mostSensitiveRatio, clamped), 0.05f, 0.9f);
        }

        /// <summary>
        /// Suppresses drag-driven decisions entirely; <see cref="ConfirmSide"/> (a tap button, say)
        /// keeps working regardless, since it does not go through <see cref="OnBeginDrag"/>.
        /// </summary>
        public void SetSwipeInputEnabled(bool enabled)
        {
            swipeInputEnabled = enabled;
        }

        /// <summary>
        /// Tells the controller which sides of the current card may currently be confirmed (see
        /// <c>ChoiceDefinition.Availability</c>). An unavailable side never confirms — a drag or tap
        /// towards it always snaps back — regardless of distance or a tap button's own state.
        /// Defaults to both available, and is meant to be set once per presented card, before
        /// <see cref="ResetForNextCard"/> arms input for it; it is deliberately untouched by
        /// <see cref="ResetForNextCard"/> itself; so the caller that just set it is not undone by
        /// the very call that follows it in the normal present-card sequence.
        /// </summary>
        public void SetSideAvailability(bool leftAvailable, bool rightAvailable)
        {
            leftSideAvailable = leftAvailable;
            rightSideAvailable = rightAvailable;
        }

        private bool IsSideAvailable(ChoiceSide side)
        {
            return side == ChoiceSide.Left ? leftSideAvailable : rightSideAvailable;
        }

        private void CaptureSensitivityDefaultOnce()
        {
            if (sensitivityDefaultCaptured)
            {
                return;
            }

            defaultThresholdRatio = thresholdRatio;
            sensitivityDefaultCaptured = true;
        }

        // --- Confirmation --------------------------------------------------------

        /// <summary>Single-side entry point: the same side is both the confirmed decision and the
        /// exit direction. Used by <see cref="ConfirmSide"/> (a tap button), where there is no
        /// physical drag direction for Invert Swipe Direction to act on.</summary>
        private void Confirm(ChoiceSide side) => Confirm(side, side);

        /// <param name="logicalSide">The <see cref="ChoiceDefinition"/> this decision resolves to —
        /// what is reported via <see cref="DecisionConfirmed"/> and lit up on the card.</param>
        /// <param name="exitSide">Which edge the card physically exits towards. Equal to
        /// <paramref name="logicalSide"/> unless Invert Swipe Direction is on, in which case it is
        /// the side the player actually dragged towards.</param>
        private void Confirm(ChoiceSide logicalSide, ChoiceSide exitSide)
        {
            // Locked before any external handler runs: a subscriber that calls back in finds a
            // controller that has already left Idle and already recorded its decision.
            confirmedSide = logicalSide;
            State = CardSwipeState.Exiting;

            // The confirmed side stays lit while the card flies out.
            if (cardView != null)
            {
                cardView.SetChoicePreviews(
                    logicalSide == ChoiceSide.Left ? 1f : 0f,
                    logicalSide == ChoiceSide.Right ? 1f : 0f);
            }

            ClearPublishedChoicePreview();

            DecisionConfirmed?.Invoke(logicalSide);

            // A handler may have reset or disabled us; do not start an exit we no longer own.
            if (State != CardSwipeState.Exiting)
            {
                return;
            }

            BeginExit(exitSide);
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
            Vector3 startScale = card != null ? card.localScale : initialScale;
            float startLeft = PreviewStrength(ChoiceSide.Left);
            float startRight = PreviewStrength(ChoiceSide.Right);

            float elapsed = 0f;

            while (elapsed < snapBackDuration)
            {
                // Unscaled: presentation feedback must keep moving even when gameplay time does not.
                elapsed += Time.unscaledDeltaTime;
                // Unclamped: the default snapBackEase curve overshoots past 1 before settling back
                // to it, for a short spring feel — a plain Lerp/Slerp would silently clamp that
                // overshoot away.
                float t = Evaluate(snapBackEase, elapsed / snapBackDuration);

                if (card != null)
                {
                    card.anchoredPosition = Vector2.LerpUnclamped(startPosition, initialAnchoredPosition, t);
                    card.localRotation = Quaternion.SlerpUnclamped(startRotation, initialRotation, t);
                    card.localScale = Vector3.LerpUnclamped(startScale, initialScale, t);
                }

                if (cardView != null)
                {
                    // Clamped here specifically: the preview should fade out smoothly, not flicker
                    // with the position/rotation overshoot above.
                    float clampedT = Mathf.Clamp01(t);
                    float left = Mathf.Lerp(startLeft, 0f, clampedT);
                    float right = Mathf.Lerp(startRight, 0f, clampedT);
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
            Quaternion startRotation = card != null ? card.localRotation : initialRotation;
            Quaternion endRotation = initialRotation * Quaternion.Euler(0f, 0f, ExitRotationAngle(side));
            Vector3 startScale = card != null ? card.localScale : initialScale;
            Vector3 endScale = initialScale * exitScale;

            float elapsed = 0f;

            while (elapsed < exitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Evaluate(exitEase, elapsed / exitDuration);

                if (card != null)
                {
                    Vector2 position = Vector2.LerpUnclamped(startPosition, endPosition, t);
                    // Small upward arc during the throw, peaking mid-flight rather than a straight
                    // line to the exit target.
                    position.y += exitArcHeight * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    card.anchoredPosition = position;
                    card.localRotation = Quaternion.SlerpUnclamped(startRotation, endRotation, t);
                    card.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
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
                card.localRotation = initialRotation * Quaternion.Euler(0f, 0f, ExitRotationAngle(side));
                card.localScale = initialScale * exitScale;
            }

            runningAnimation = null;

            // Completed before the event, so a re-entrant handler finds a settled controller.
            State = CardSwipeState.Completed;

            ExitAnimationCompleted?.Invoke(side);
        }

        /// <summary>The rotation the card reaches by the end of its exit throw — a fixed lean in
        /// the exit direction, independent of the drag-time <see cref="maxRotationDegrees"/>.
        /// Mirrors <see cref="SwipeMath.Rotation"/>'s own sign convention.</summary>
        private float ExitRotationAngle(ChoiceSide side)
        {
            float signed = side == ChoiceSide.Right ? 1f : -1f;
            float angle = -signed * Mathf.Abs(exitRotationDegrees);
            return rotateClockwiseOnRightDrag ? angle : -angle;
        }

        /// <summary>
        /// Fast initial return, a slight overshoot past the neutral value, then a settle — a short
        /// spring feel instead of a linear or pure ease-out glide back to centre. Single overshoot
        /// hump only (no oscillation), so it always settles within one pass. Public so Editor
        /// scene setup can wire this exact curve into an already-serialized component, not just
        /// rely on it as a fresh-component default.
        /// </summary>
        public static AnimationCurve BuildSnapBackSpringCurve()
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.6f, 1.08f),
                new Keyframe(1f, 1f));
            for (int i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0.3f);
            }

            return curve;
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
                float lift = SwipeMath.ArcLift(displacement, threshold, maxDragLift);
                float scale = SwipeMath.DragScale(displacement, threshold, maxDragScale);

                card.anchoredPosition = new Vector2(
                    initialAnchoredPosition.x + (displacement * movementMultiplier),
                    initialAnchoredPosition.y + lift);

                card.localRotation = initialRotation * Quaternion.Euler(
                    0f,
                    0f,
                    SwipeMath.Rotation(
                        EasedDisplacement(displacement, threshold), threshold, maxRotationDegrees,
                        rotateClockwiseOnRightDrag));
                card.localScale = initialScale * scale;
            }

            SwipeMath.PreviewStrengths(displacement, threshold, out float left, out float right);

            // The preview must show what will actually be confirmed, not just which way the
            // finger is moving — otherwise a right drag would light up "Right" while Invert Swipe
            // Direction is about to resolve it as the Left choice on release.
            if (decisionMappingInverted)
            {
                (left, right) = (right, left);
            }

            if (cardView != null)
            {
                cardView.SetChoicePreviews(left, right);
            }

            PublishChoicePreview(left, right);
        }

        /// <summary>
        /// Pre-warps <paramref name="displacement"/> through a power curve so the existing, tested
        /// <see cref="SwipeMath.Rotation"/> — linear in its own input — reads as a smooth nonlinear
        /// ramp instead: most of the tilt happens early, then flattens approaching the threshold.
        /// Presentation only; <see cref="SwipeMath.Rotation"/> itself, and the confirm threshold,
        /// are untouched.
        /// </summary>
        private float EasedDisplacement(float displacement, float thresholdDistance)
        {
            if (thresholdDistance <= 0f)
            {
                return displacement;
            }

            float signedProgress = Mathf.Clamp(displacement / thresholdDistance, -1f, 1f);
            float eased = Mathf.Sign(signedProgress)
                * Mathf.Pow(Mathf.Abs(signedProgress), rotationEaseExponent);
            return eased * thresholdDistance;
        }

        private void RestoreNeutral()
        {
            RectTransform card = ResolveCardRoot();
            if (card != null)
            {
                card.anchoredPosition = initialAnchoredPosition;
                card.localRotation = initialRotation;
                card.localScale = initialScale;
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
            initialScale = card.localScale;
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
            rotationEaseExponent = Mathf.Clamp(rotationEaseExponent, 0.3f, 1.5f);
            maxDragLift = Mathf.Max(0f, maxDragLift);
            maxDragScale = Mathf.Clamp(maxDragScale, 1f, 1.1f);
            snapBackDuration = Mathf.Max(0f, snapBackDuration);
            exitDuration = Mathf.Max(0f, exitDuration);
            exitMarginMultiplier = Mathf.Max(0f, exitMarginMultiplier);
            exitRotationDegrees = Mathf.Clamp(exitRotationDegrees, 0f, 90f);
            exitArcHeight = Mathf.Max(0f, exitArcHeight);
            exitScale = Mathf.Clamp(exitScale, 1f, 1.15f);
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
