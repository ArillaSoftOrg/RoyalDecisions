using RoyalDecisions.Application;
using RoyalDecisions.Infrastructure;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Offers New Game and Continue, and records which one the player chose.
    /// </summary>
    /// <remarks>
    /// It never creates or mutates a <see cref="RoyalDecisions.Domain.RunState"/> — it decides only
    /// which scene to load and what the game scene should do when it gets there.
    ///
    /// A save it cannot read makes Continue unavailable with a message. It is never deleted: a file
    /// this build cannot understand may still be readable by another, and destroying a player's run
    /// to tidy up a menu is not a trade worth making.
    /// </remarks>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string gameSceneName = "Game";

        [Header("Wiring")]
        [SerializeField] private SessionIntent sessionIntent;

        [Tooltip("Optional. Kept in sync with IsContinueAvailable when assigned.")]
        [SerializeField] private Button continueButton;
        [SerializeField] private InterfaceTextDefinition interfaceText;
        [SerializeField] private MainMenuTextView mainMenuTextView;

        private ISceneLoader sceneLoader;
        private IRunSaveStore runStore;

        /// <summary>True only for a save that is present and structurally loadable.</summary>
        public bool IsContinueAvailable { get; private set; }

        /// <summary>Set when a save exists but cannot be used. Shown to the player, never acted on.</summary>
        public SessionError SaveProblem { get; private set; } = SessionError.None;

        private void Awake()
        {
            if (runStore == null)
            {
                SavePaths paths = SavePaths.ForPersistentData();
                runStore = new SaveServiceRunStore(new SaveService(new SystemFileSystem(), paths));
            }

            sceneLoader ??= new UnitySceneLoader();

            RefreshContinueAvailability();
        }

        /// <summary>Injection seam for tests, which must never touch persistent data.</summary>
        public void Configure(IRunSaveStore store, ISceneLoader loader, SessionIntent intent)
        {
            runStore = store;
            sceneLoader = loader;
            sessionIntent = intent;

            RefreshContinueAvailability();
        }

        public void RefreshContinueAvailability()
        {
            SaveProblem = SessionError.None;
            IsContinueAvailable = false;

            if (runStore == null || !runStore.HasSave())
            {
                ApplyContinueAvailability();
                return;
            }

            RunLoadOutcome outcome = runStore.Load();

            if (outcome.Succeeded && outcome.HasRun && outcome.RunState.IsRunActive)
            {
                IsContinueAvailable = true;
                ApplyContinueAvailability();
                return;
            }

            SaveProblem = outcome.Status == RunLoadStatus.UnsupportedVersion
                ? SessionError.Terminal(
                    SessionErrorCode.UnsupportedSave,
                    interfaceText != null
                        ? interfaceText.UnsupportedSave
                        : "This save was made by a newer version of the game.")
                : SessionError.Terminal(
                    SessionErrorCode.CorruptSave,
                    interfaceText != null
                        ? interfaceText.CorruptSave
                        : "This save could not be read.");

            ApplyContinueAvailability();
        }

        /// <summary>Wire a Button's OnClick to this.</summary>
        public void OnNewGamePressed()
        {
            sessionIntent?.RequestNewGame();
            LoadGameScene();
        }

        /// <summary>Wire a Button's OnClick to this. Does nothing when Continue is unavailable.</summary>
        public void OnContinuePressed()
        {
            if (!IsContinueAvailable)
            {
                return;
            }

            sessionIntent?.RequestContinue();
            LoadGameScene();
        }

        private void LoadGameScene()
        {
            sceneLoader?.LoadScene(gameSceneName);
        }

        private void ApplyContinueAvailability()
        {
            if (continueButton != null)
            {
                continueButton.interactable = IsContinueAvailable;
            }

            mainMenuTextView?.SetSaveError(SaveProblem.HasError ? SaveProblem.Message : string.Empty);
        }
    }
}
