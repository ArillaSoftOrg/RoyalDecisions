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

        [Tooltip("Small extra ease once the next portrait reaches full scale.")]
        [SerializeField] private float settleDuration = 0.1f;

        [Header("Secondary motion (restrained)")]
        [SerializeField] private float secondaryScaleYPeak = 1.025f;

        [SerializeField] private float secondaryTiltDegrees = 1f;

        [SerializeField]
        private AnimationCurve firstHalfEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private AnimationCurve secondHalfEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

            // Midpoint: CardBack resets to neutral (invisible — the next portrait, edge-on at
            // scale 0, is about to cover it) and the next card's content replaces the outgoing
            // one's. Until this line, only Card.png has been visible behind the outgoing portrait.
            if (cardBack != null)
            {
                cardBack.localScale = cardBackNeutralScale;
                cardBack.localRotation = cardBackNeutralRotation;
            }

            swipeController.RestoreNeutralGeometry();
            RectTransform portraitRoot = cardView.CardRoot;
            Vector3 portraitNeutralScale = portraitRoot != null ? portraitRoot.localScale : Vector3.one;
            Quaternion portraitNeutralRotation =
                portraitRoot != null ? portraitRoot.localRotation : Quaternion.identity;

            if (portraitRoot != null)
            {
                portraitRoot.localScale = new Vector3(0f, portraitNeutralScale.y, portraitNeutralScale.z);
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
                    float wobble = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

                    portraitRoot.localScale = new Vector3(
                        Mathf.LerpUnclamped(0f, portraitNeutralScale.x, t),
                        Mathf.LerpUnclamped(secondaryScaleYPeak, portraitNeutralScale.y, 1f - wobble),
                        portraitNeutralScale.z);
                    portraitRoot.localRotation = portraitNeutralRotation
                        * Quaternion.Euler(0f, 0f, secondaryTiltDegrees * wobble);

                    float contentAlpha = Mathf.Clamp01(t);
                    cardView.SetContentAlpha(contentAlpha, contentAlpha);

                    yield return null;
                }
            }

            cardView.SetContentAlpha(1f, 1f);

            if (portraitRoot != null && settleDuration > 0f)
            {
                float elapsed = 0f;
                const float SettleStartScale = 0.98f;

                while (elapsed < settleDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / settleDuration);
                    float scale = Mathf.LerpUnclamped(SettleStartScale, 1f, t);

                    portraitRoot.localScale = new Vector3(
                        portraitNeutralScale.x * scale,
                        portraitNeutralScale.y * scale,
                        portraitNeutralScale.z);

                    yield return null;
                }
            }

            running = null;
            IsTransitioning = false;

            // Snaps position/rotation/scale to the authoritative neutral values (this settle
            // approximated the last stretch of it) and unlocks input for the new card.
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
