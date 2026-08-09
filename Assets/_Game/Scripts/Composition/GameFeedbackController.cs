using RoyalDecisions.Application;
using RoyalDecisions.Data;
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

        private IHapticService haptics;
        private bool thresholdPulsed;
        private bool subscribed;

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
            if (gameSceneController != null && gameSceneController.Session != null)
            {
                gameSceneController.Session.StateChanged += HandleState;
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
            }
            if (gameSceneController != null && gameSceneController.Session != null)
            {
                gameSceneController.Session.StateChanged -= HandleState;
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
        }

        private void HandlePreviewCleared() => thresholdPulsed = false;

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
