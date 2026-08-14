using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>Optional audio/haptic feedback wiring; empty cue IDs remain silent.</summary>
    public sealed class GameFeedbackController : MonoBehaviour
    {
        [SerializeField] private GameSceneController gameSceneController;
        [SerializeField] private CardSwipeController swipeController;
        [SerializeField] private AudioService audioService;
        [SerializeField] private FeedbackCueProfile cues;

        /// <summary>Below this strength a drag has not moved far enough to count as "started".</summary>
        private const float PreviewCueMinimumStrength = 0.05f;

        [Tooltip("A stat at or under this value, or at or over 100 minus this value, counts as "
            + "critical. Mirrors GameUITheme.CriticalBoundary.")]
        [SerializeField] private int criticalBoundary = 15;

        private IHapticService haptics;
        private bool thresholdPulsed;
        private bool subscribed;
        private ChoiceSide? previewCueSide;

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        public void Configure(IHapticService hapticService)
        {
            haptics = hapticService ?? new NoOpHapticService();
        }

        private void Subscribe()
        {
            if (subscribed || swipeController == null)
            {
                return;
            }
            swipeController.ChoicePreviewChanged += HandlePreview;
            swipeController.ChoicePreviewCleared += HandlePreviewCleared;
            swipeController.DecisionConfirmed += HandleDecision;
            swipeController.ExitAnimationCompleted += HandleExit;
            swipeController.SnapBackStarted += HandleSnapBack;
            if (gameSceneController != null && gameSceneController.Session != null)
            {
                gameSceneController.Session.StateChanged += HandleState;
                gameSceneController.Session.CardPresented += HandleCardPresented;
                gameSceneController.Session.StatValueChanged += HandleStatValueChanged;
            }
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }
            if (swipeController != null)
            {
                swipeController.ChoicePreviewChanged -= HandlePreview;
                swipeController.ChoicePreviewCleared -= HandlePreviewCleared;
                swipeController.DecisionConfirmed -= HandleDecision;
                swipeController.ExitAnimationCompleted -= HandleExit;
                swipeController.SnapBackStarted -= HandleSnapBack;
            }
            if (gameSceneController != null && gameSceneController.Session != null)
            {
                gameSceneController.Session.StateChanged -= HandleState;
                gameSceneController.Session.CardPresented -= HandleCardPresented;
                gameSceneController.Session.StatValueChanged -= HandleStatValueChanged;
            }
            subscribed = false;
        }

        private void HandlePreview(ChoiceSide side, float strength)
        {
            if (strength >= 1f && !thresholdPulsed)
            {
                thresholdPulsed = true;
                Play(cues != null ? cues.Threshold : string.Empty);
                haptics?.Pulse();
            }
            else if (strength < 1f)
            {
                thresholdPulsed = false;
            }

            if (strength >= PreviewCueMinimumStrength && previewCueSide != side)
            {
                previewCueSide = side;
                Play(cues != null ? cues.CardPreview : string.Empty);
            }
        }

        private void HandlePreviewCleared()
        {
            thresholdPulsed = false;
            previewCueSide = null;
        }

        private void HandleCardPresented(CardDefinition card) =>
            Play(cues != null ? cues.CardEnter : string.Empty);

        private void HandleSnapBack() => Play(cues != null ? cues.SnapBack : string.Empty);

        /// <summary>
        /// Plays a warning cue the instant a stat crosses into its critical range, and an ordinary
        /// stat cue otherwise. Only the crossing plays the warning — a stat that is already critical
        /// and moves further does not repeat it.
        /// </summary>
        private void HandleStatValueChanged(StatChange change)
        {
            bool becameCritical = IsCritical(change.Current) && !IsCritical(change.Previous);
            if (becameCritical)
            {
                Play(cues != null ? cues.Critical : string.Empty);
                haptics?.Pulse();
                return;
            }

            Play(cues == null ? string.Empty
                : change.Delta > 0 ? cues.StatIncrease : cues.StatDecrease);
        }

        private bool IsCritical(int value) =>
            value <= criticalBoundary || value >= StatBounds.Max - criticalBoundary;

        private void HandleDecision(ChoiceSide side)
        {
            Play(cues == null ? string.Empty
                : side == ChoiceSide.Left ? cues.LeftConfirmation : cues.RightConfirmation);
            haptics?.Pulse();
        }

        private void HandleExit(ChoiceSide side) => Play(cues != null ? cues.Exit : string.Empty);

        private void HandleState(GameSessionState state)
        {
            if (state == GameSessionState.ShowingGameOver)
            {
                Play(cues != null ? cues.GameOver : string.Empty);
                haptics?.Pulse();
            }
        }

        private void Play(string cueId)
        {
            if (!string.IsNullOrEmpty(cueId))
            {
                audioService?.Play(cueId);
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            GameSceneController game,
            CardSwipeController swipe,
            AudioService audio,
            FeedbackCueProfile profile)
        {
            gameSceneController = game;
            swipeController = swipe;
            audioService = audio;
            cues = profile;
        }
#endif
    }
}
