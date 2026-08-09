using System;
using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>Gates New Game before RunState/save/RNG creation; Continue bypasses it.</summary>
    public sealed class TutorialCoordinator : MonoBehaviour
    {
        [SerializeField] private TutorialOverlayView view;

        private ISettingsStore store;
        private GameSettings settings;
        private Action completion;
        private int step;
        private bool subscribed;

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        public bool TryGateNewGame(ISettingsStore settingsStore, Action startNewGame)
        {
            store = settingsStore;
            settings = store != null ? store.Load() : GameSettings.CreateDefault();
            if (settings.TutorialCompleted || view == null || view.StepCount == 0)
            {
                return false;
            }
            completion = startNewGame;
            step = 0;
            Subscribe();
            view.ShowStep(step);
            return true;
        }

        public bool CloseIfOpen()
        {
            if (view == null || !view.IsOpen)
            {
                return false;
            }
            Complete();
            return true;
        }

        public void Advance()
        {
            if (view == null || !view.IsOpen)
            {
                return;
            }
            step++;
            if (step >= view.StepCount)
            {
                Complete();
            }
            else
            {
                view.ShowStep(step);
            }
        }

        public void Skip() => Complete();

        private void Complete()
        {
            if (completion == null)
            {
                view?.Hide();
                return;
            }
            settings ??= GameSettings.CreateDefault();
            settings.SetTutorialCompleted(true);
            if (store != null)
            {
                SaveOutcome outcome = store.Save(settings);
                if (!outcome.Succeeded)
                {
                    Debug.LogWarning("[Tutorial] Completion could not be saved: " + outcome.Message, this);
                }
            }
            view?.Hide();
            Action start = completion;
            completion = null;
            start.Invoke();
        }

        private void Subscribe()
        {
            if (subscribed || view == null)
            {
                return;
            }
            view.NextRequested += Advance;
            view.SkipRequested += Skip;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || view == null)
            {
                return;
            }
            view.NextRequested -= Advance;
            view.SkipRequested -= Skip;
            subscribed = false;
        }

#if UNITY_EDITOR
        public void SetAuthoringReference(TutorialOverlayView tutorialView) => view = tutorialView;
#endif
    }
}
