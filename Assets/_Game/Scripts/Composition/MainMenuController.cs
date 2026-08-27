using System.Collections;
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

        [Tooltip("Loaded when Yeni Oyun is pressed, instead of the Game scene directly, so the "
            + "prologue plays before a brand-new run begins. Continue always goes straight to Game.")]
        [SerializeField] private string prologueSceneName = "Prologue";

        [Header("Wiring")]
        [SerializeField] private SessionIntent sessionIntent;

        [Tooltip("Optional. Kept in sync with IsContinueAvailable when assigned.")]
        [SerializeField] private Button continueButton;
        [SerializeField] private InterfaceTextDefinition interfaceText;
        [SerializeField] private MainMenuTextView mainMenuTextView;

        [Header("Audio")]
        [Tooltip("Optional. Absent audio is a supported configuration.")]
        [SerializeField] private AudioService audioService;
        [SerializeField] private FeedbackCueProfile cues;

        [Tooltip("Realtime seconds the ui_click cue is given to play before the Game scene loads.")]
        [SerializeField] private float sceneTransitionDelaySeconds = 0.15f;

        [Header("Transitions")]
        [Tooltip("Optional. Fades to a solid cover before the Game scene loads, instead of an "
            + "instant cut; falls back to loading immediately when absent.")]
        [SerializeField] private PanelFadeAnimator transitionOverlay;

        private ISceneLoader sceneLoader;
        private IRunSaveStore runStore;
        private bool isTransitioningToGame;

        /// <summary>True only for a save that is present and structurally loadable.</summary>
        public bool IsContinueAvailable { get; private set; }

        /// <summary>Set when a save exists but cannot be used. Shown to the player, never acted on.</summary>
        public SessionError SaveProblem { get; private set; } = SessionError.None;

        /// <summary>Scene Yeni Oyun transitions to. Exposed for tests/diagnostics.</summary>
        public string NewGameDestinationSceneName => prologueSceneName;

        /// <summary>Scene Continue transitions to. Exposed for tests/diagnostics.</summary>
        public string ContinueDestinationSceneName => gameSceneName;

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

        private void Start()
        {
            PlayMenuMusic();
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

        /// <summary>Wire a Button's OnClick to this. Loads the prologue rather than Game directly,
        /// so a brand-new run always plays it first.</summary>
        public void OnNewGamePressed()
        {
            if (isTransitioningToGame)
            {
                return;
            }

            isTransitioningToGame = true;
            PlayUiClick();
            sessionIntent?.RequestNewGame();
            BeginSceneTransition(prologueSceneName);
        }

        /// <summary>Wire a Button's OnClick to this. Does nothing when Continue is unavailable.
        /// Always goes straight to Game — the prologue never plays for Continue.</summary>
        public void OnContinuePressed()
        {
            if (!IsContinueAvailable || isTransitioningToGame)
            {
                return;
            }

            isTransitioningToGame = true;
            PlayUiClick();
            sessionIntent?.RequestContinue();
            BeginSceneTransition(gameSceneName);
        }

        /// <summary>
        /// Runs the click-cue delay and optional fade before loading, exactly as before this
        /// existed — only the destination now varies by caller. Falls back to loading immediately
        /// when coroutines cannot run (outside Play Mode), the same fail-open pattern every other
        /// sequence controller in this project already uses, so the destination stays directly
        /// testable without a running player loop.
        /// </summary>
        private void BeginSceneTransition(string destinationSceneName)
        {
            if (!CanRunCoroutines())
            {
                sceneLoader?.LoadScene(destinationSceneName);
                return;
            }

            StartCoroutine(LoadSceneAfterClickCue(destinationSceneName));
        }

        private bool CanRunCoroutines()
        {
            return UnityEngine.Application.isPlaying && isActiveAndEnabled;
        }

        private IEnumerator LoadSceneAfterClickCue(string destinationSceneName)
        {
            yield return new WaitForSecondsRealtime(sceneTransitionDelaySeconds);

            if (transitionOverlay != null)
            {
                transitionOverlay.Show(() => sceneLoader?.LoadScene(destinationSceneName));
            }
            else
            {
                sceneLoader?.LoadScene(destinationSceneName);
            }
        }

        private void PlayMenuMusic()
        {
            if (audioService == null || cues == null || string.IsNullOrEmpty(cues.MenuMusic))
            {
                return;
            }

            audioService.PlayMusic(cues.MenuMusic);
        }

        private void PlayUiClick()
        {
            if (audioService == null || cues == null || string.IsNullOrEmpty(cues.UiClick))
            {
                return;
            }

            audioService.Play(cues.UiClick);
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
