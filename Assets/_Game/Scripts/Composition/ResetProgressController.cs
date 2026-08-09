using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>Deletes the run save on the player's explicit confirmation from the Settings menu.</summary>
    /// <remarks>
    /// No live <see cref="RoyalDecisions.Application.GameSession"/> exists in this scene — the
    /// settings panel lives in MainMenu, the session in Game — so this talks to the run save file
    /// directly, the same way <see cref="SettingsController"/> talks to the settings file directly
    /// rather than through a session.
    /// </remarks>
    public sealed class ResetProgressController : MonoBehaviour
    {
        [SerializeField] private SettingsPanelView view;

        private IRunSaveStore runStore;

        private void Awake()
        {
            if (runStore == null)
            {
                SavePaths paths = SavePaths.ForPersistentData();
                runStore = new SaveServiceRunStore(new SaveService(new SystemFileSystem(), paths));
            }
        }

        private void OnEnable()
        {
            if (view != null) view.ResetProgressConfirmed += HandleResetProgressConfirmed;
        }

        private void OnDisable()
        {
            if (view != null) view.ResetProgressConfirmed -= HandleResetProgressConfirmed;
        }

        /// <summary>Injection seam for tests, which must never touch persistent data.</summary>
        public void Configure(IRunSaveStore store)
        {
            runStore = store;
        }

        private void HandleResetProgressConfirmed()
        {
            SaveOutcome outcome = runStore.Delete();
            if (!outcome.Succeeded)
            {
                Debug.LogWarning(
                    "[ResetProgress] Could not delete the run save: " + outcome.Message, this);
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(SettingsPanelView settingsView)
        {
            view = settingsView;
        }
#endif
    }
}
