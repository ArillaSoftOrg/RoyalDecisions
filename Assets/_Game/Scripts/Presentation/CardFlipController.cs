using System.Collections;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Plays the CardBack-to-next-portrait flip between a committed card's exit and the next
    /// card's settle. Presentation only: it reads a resolved card it is handed and drives
    /// <see cref="CardView"/>/<see cref="CardSwipeController"/> transforms, and never calls into
    /// Application or Domain code itself.
    /// </summary>
    /// <remarks>
    /// Owned by <see cref="RoyalDecisions.Composition.UnityGamePresenter"/>, which arms it (via
    /// <see cref="Arm"/>) only for a genuine card-to-card transition — never for the first card of
    /// a new or restarted run, which has no outgoing card to throw and no CardBack moment to flip
    /// through, and so still appears the old, immediate way. See <see cref="IsArmed"/>.
    /// </remarks>
    public sealed class CardFlipController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardView cardView;
        [SerializeField] private CardSwipeController swipeController;

        [Header("Timing (seconds)")]
        [Tooltip("Very short pause after the outgoing card has fully exited, before the flip "
            + "begins, so CardBack reads as genuinely exposed rather than flipping instantly.")]
        [SerializeField] private float exposureDuration = 0.045f;

        [SerializeField] private float flipDuration = 0.32f;

        [Header("Secondary motion (restrained)")]
        [Tooltip("Used only by the first half — CardBack's own shrink-to-edge-on flip.")]
        [SerializeField] private float secondaryScaleYPeak = 1.025f;

        [Tooltip("Used only by the first half — CardBack's own shrink-to-edge-on flip.")]
        [SerializeField] private float secondaryTiltDegrees = 1f;

        [SerializeField]
        private AnimationCurve firstHalfEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private AnimationCurve secondHalfEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Diagonal entry (second half — incoming portrait)")]
        [Tooltip("Where the incoming portrait starts, relative to its settled centre, as a "
            + "fraction of its own width/height — e.g. (0.55, 0.45) starts it up and to the "
            + "right, sliding down-left into place as rotation eases to 0.")]
        [SerializeField] private Vector2 entryOffsetFraction = new Vector2(0.55f, 0.45f);

        [Tooltip("Tilt the incoming portrait starts at; eases to 0 by the time it settles at "
            + "centre.")]
        [SerializeField] private float entryTiltDegrees = 10f;

        private Coroutine running;

        /// <summary>
        /// True once <see cref="Arm"/> has been called for the card currently leaving the screen,
        /// until the transition it arms either begins (<see cref="BeginTransition"/>) or is
        /// abandoned (<see cref="Disarm"/>) — e.g. because the run ended instead of continuing.
        /// </summary>
        public bool IsArmed { get; private set; }

        /// <summary>True from <see cref="BeginTransition"/> until the flip and settle finish and
        /// input is re-armed.</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Marks the next <c>ShowCard</c> as a genuine card-to-card transition. Called once, right
        /// before the session is told the outgoing card's exit finished.
        /// </summary>
        public void Arm()
        {
            IsArmed = true;
        }

        /// <summary>Cancels a pending arm without playing anything — the run ended instead of
        /// continuing to another card.</summary>
        public void Disarm()
        {
            IsArmed = false;
        }

        /// <summary>
        /// Plays the transition and, at its end, applies <paramref name="card"/>/
        /// <paramref name="resolved"/> to <see cref="CardView"/> and re-arms
        /// <see cref="CardSwipeController"/> for input — the caller must not also call
        /// <c>CardView.Show</c> or <c>CardSwipeController.ResetForNextCard</c> itself.
        /// </summary>
        public void BeginTransition(CardDefinition card, ResolvedCard resolved)
        {
            IsArmed = false;

            if (cardView == null || swipeController == null || !CanRunCoroutines())
            {
                // No transition possible: fall back to the immediate, non-animated presentation.
                cardView?.Show(card, resolved);
                swipeController?.SetSideAvailability(resolved.LeftAvailable, resolved.RightAvailable);
                swipeController?.ResetForNextCard();
                return;
            }

            if (running != null)
            {
                StopCoroutine(running);
            }

            IsTransitioning = true;
            cardView.ForceVisible();
            // Shown for exactly this transition's duration — see SetCardBackVisible and the
            // matching hide once the incoming portrait is about to cover it, below.
            cardView.SetCardBackVisible(true);
            running = StartCoroutine(TransitionRoutine(card, resolved));
        }

        private IEnumerator TransitionRoutine(CardDefinition card, ResolvedCard resolved)
        {
            if (exposureDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(exposureDuration);
            }

            RectTransform cardBack = cardView.CardBackTransform;
            float half = flipDuration * 0.5f;

            Vector3 cardBackNeutralScale = cardBack != null ? cardBack.localScale : Vector3.one;
            Quaternion cardBackNeutralRotation =
                cardBack != null ? cardBack.localRotation : Quaternion.identity;

            if (cardBack != null && half > 0f)
            {
                float elapsed = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Evaluate(firstHalfEase, elapsed / half);
                    float wobble = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

                    cardBack.localScale = new Vector3(
                        Mathf.LerpUnclamped(1f, 0f, t),
                        Mathf.LerpUnclamped(1f, secondaryScaleYPeak, wobble),
                        cardBackNeutralScale.z);
                    cardBack.localRotation = cardBackNeutralRotation
                        * Quaternion.Euler(0f, 0f, -secondaryTiltDegrees * wobble);

                    yield return null;
                }
            }

            // Midpoint: CardBack resets to neutral and is explicitly hidden again (rather than
            // relying on the next portrait, edge-on at scale 0, to cover it) and the next card's
            // content replaces the outgoing one's. Until this line, only Card.png has been
            // visible behind the outgoing portrait.
            if (cardBack != null)
            {
                cardBack.localScale = cardBackNeutralScale;
                cardBack.localRotation = cardBackNeutralRotation;
            }
            cardView.SetCardBackVisible(false);

            swipeController.RestoreNeutralGeometry();
            RectTransform portraitRoot = cardView.CardRoot;
            Vector2 portraitNeutralPosition =
                portraitRoot != null ? portraitRoot.anchoredPosition : Vector2.zero;
            Quaternion portraitNeutralRotation =
                portraitRoot != null ? portraitRoot.localRotation : Quaternion.identity;

            // Diagonal entry: the incoming portrait starts offset up-and-right of its settled
            // centre and tilted, then translates + de-rotates into place — translateX, translateY,
            // and rotate together, no scale, matching the diagonal "thrown into place" feel.
            Vector2 entryStartPosition = portraitNeutralPosition;
            Quaternion entryStartRotation = portraitNeutralRotation;
            if (portraitRoot != null)
            {
                Vector2 size = portraitRoot.rect.size;
                Vector2 entryOffset = new Vector2(
                    entryOffsetFraction.x * size.x, entryOffsetFraction.y * size.y);
                entryStartPosition = portraitNeutralPosition + entryOffset;
                entryStartRotation = portraitNeutralRotation * Quaternion.Euler(0f, 0f, entryTiltDegrees);

                portraitRoot.anchoredPosition = entryStartPosition;
                portraitRoot.localRotation = entryStartRotation;
            }

            cardView.Show(card, resolved);
            cardView.SetContentAlpha(0f, 0f);
            swipeController.SetSideAvailability(resolved.LeftAvailable, resolved.RightAvailable);

            if (portraitRoot != null && half > 0f)
            {
                float elapsed = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Evaluate(secondHalfEase, elapsed / half);

                    portraitRoot.anchoredPosition =
                        Vector2.LerpUnclamped(entryStartPosition, portraitNeutralPosition, t);
                    portraitRoot.localRotation =
                        Quaternion.SlerpUnclamped(entryStartRotation, portraitNeutralRotation, t);

                    float contentAlpha = Mathf.Clamp01(t);
                    cardView.SetContentAlpha(contentAlpha, contentAlpha);

                    yield return null;
                }
            }

            cardView.SetContentAlpha(1f, 1f);

            if (portraitRoot != null)
            {
                portraitRoot.anchoredPosition = portraitNeutralPosition;
                portraitRoot.localRotation = portraitNeutralRotation;
            }

            running = null;
            IsTransitioning = false;

            // Snaps position/rotation/scale to the authoritative neutral values (already reached
            // above, just before this) and unlocks input for the new card.
            swipeController.ResetForNextCard();
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

        private void OnDisable()
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
                // The coroutine may have been stopped mid-flip, with CardBack still shown from
                // BeginTransition — never leave it stuck visible behind whatever comes next.
                cardView?.SetCardBackVisible(false);
            }

            IsTransitioning = false;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by scene setup and tests.</summary>
        public void SetAuthoringReferences(CardView view, CardSwipeController swipe)
        {
            cardView = view;
            swipeController = swipe;
        }
#endif
    }
}
