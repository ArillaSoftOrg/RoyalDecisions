using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Wires the game scene to a session, and translates Unity events into session commands.
    /// </summary>
    /// <remarks>
    /// This is a composition root, not a game rule. It constructs services, owns every
    /// subscription, and forwards events — it calculates nothing and never touches a
    /// <see cref="RunState"/>.
    ///
    /// Subscriptions are taken in <c>OnEnable</c> and released in <c>OnDisable</c>, so a disabled
    /// scene cannot receive a swipe event for a session it no longer drives.
    /// </remarks>
    public sealed class GameSceneController : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private ContentCatalogue catalogue;

        [Header("Views")]
        [SerializeField] private CardView cardView;
        [SerializeField] private HUDView hudView;
        [SerializeField] private GameOverView gameOverView;
        [SerializeField] private CardSwipeController swipeController;
        [SerializeField] private TapChoiceButtonsView tapChoiceButtonsView;
        [SerializeField] private RunStatusView runStatusView;
        [SerializeField] private FooterView footerView;
        [SerializeField] private TutorialCoordinator tutorialCoordinator;

        [Header("Audio")]
        [SerializeField] private AudioService audioService;
        [SerializeField] private FeedbackCueProfile cues;

        [Header("Start mode")]
        [Tooltip("Optional. Set by the main menu; when absent the fallback below is used.")]
        [SerializeField] private SessionIntent sessionIntent;

        [Tooltip("Used when no SessionIntent asset is assigned.")]
        [SerializeField] private SessionStartMode fallbackStartMode = SessionStartMode.NewGame;

        [Header("Transitions")]
        [Tooltip("Optional. Starts covering the screen and fades away once the scene starts, "
            + "hiding the frame where layout settles instead of an instant cut from MainMenu. "
            + "Falls back to an already-clear scene when absent.")]
        [SerializeField] private PanelFadeAnimator transitionOverlay;

        private GameSession session;
        private ISettingsStore settingsStore;
        private IRunSaveStore runStoreOverride;
        private IRunSaveStore runStore;
        private ISeedProvider seedProviderOverride;
        private bool subscribed;

        /// <summary>
        /// Supplies persistence and seeding from outside, instead of building the real services.
        /// </summary>
        /// <remarks>
        /// Must be called before the object becomes active, so create the GameObject inactive,
        /// configure it, then activate. Tests use this so a scene test never writes to the
        /// player's persistent data.
        /// </remarks>
        public void Configure(
            IRunSaveStore runStore,
            ISettingsStore settings,
            ISeedProvider seedProvider)
        {
            runStoreOverride = runStore;
            settingsStore = settings;
            seedProviderOverride = seedProvider;
        }

        /// <summary>The live session. Null until Awake has run.</summary>
        public GameSession Session => session;

        public GameSessionState State =>
            session != null ? session.State : GameSessionState.Uninitialized;

        /// <summary>Set when a required reference is missing; the scene reports rather than throws.</summary>
        public SessionError WiringError { get; private set; } = SessionError.None;

        private void Awake()
        {
            BuildSession();
        }

        private void OnEnable()
        {
            Subscribe();

            // A card whose exit was interrupted by a disable leaves the session waiting. Continue
            // rather than deadlocking: the decision is already resolved and saved.
            if (session != null && session.State == GameSessionState.WaitingForCardExit)
            {
                session.NotifyCardExitCompleted();
            }
        }

        private void Start()
        {
            if (session == null)
            {
                return;
            }

            transitionOverlay?.Hide();
            ApplySettings();
            PlayGameplayMusic();
            StartSession();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentDebugPanel debug = gameObject.GetComponent<DevelopmentDebugPanel>();
            if (debug == null)
            {
                debug = gameObject.AddComponent<DevelopmentDebugPanel>();
            }
            debug.Configure(session);
#endif
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            session?.Shutdown();
        }

        // --- Construction ------------------------------------------------------

        private void BuildSession()
        {
            if (!TryValidateReferences(out SessionError error))
            {
                WiringError = error;
                Debug.LogError("[GameScene] " + error.Message, this);
                return;
            }

            runStore = runStoreOverride;

            if (runStore == null || settingsStore == null)
            {
                SavePaths paths = SavePaths.ForPersistentData();
                IFileSystem fileSystem = new SystemFileSystem();

                runStore ??= new SaveServiceRunStore(new SaveService(fileSystem, paths));
                settingsStore ??= new SettingsServiceStore(
                    new SettingsSaveService(fileSystem, paths));
            }

            IGamePresenter presenter = new UnityGamePresenter(
                cardView, hudView, gameOverView, swipeController, runStatusView, footerView);

            session = new GameSession(new GameSessionDependencies(
                catalogue,
                presenter,
                runStore,
                seedProviderOverride ?? new SystemSeedProvider(),
                new AudioServicePlayer(audioService)));
        }

        private bool TryValidateReferences(out SessionError error)
        {
            if (cardView == null || swipeController == null)
            {
                error = SessionError.Terminal(
                    SessionErrorCode.MissingPresenter,
                    "GameSceneController needs a CardView and a CardSwipeController.");
                return false;
            }

            error = SessionError.None;
            return true;
        }

        private void ApplySettings()
        {
            if (settingsStore == null)
            {
                return;
            }

            GameSettings settings = settingsStore.Load();

            if (audioService != null)
            {
                // Volume and mute go through the audio service's public API only.
                audioService.SetMasterVolume(settings.MasterVolume);
                audioService.SetSfxVolume(settings.SfxVolume);
                audioService.SetMusicVolume(settings.MusicVolume);
                audioService.SetMasterMuted(settings.MasterMuted);
            }

            // The Settings menu lives in MainMenu, this controller in Game — different scenes, so
            // there is no live reference between them. Each scene re-applies its own runtime
            // preferences from the same saved GameSettings on load, exactly like audio above.
            if (swipeController != null)
            {
                swipeController.SetInvertRotation(settings.InvertSwipeRotation);
                swipeController.SetSwipeSensitivity(settings.SwipeSensitivity);
                swipeController.SetSwipeInputEnabled(!settings.DisableSwipe);
            }

            // With swipe disabled, decision buttons are the only way to play — forced visible
            // regardless of the toggle, or a player who also had tap buttons off would be stuck.
            tapChoiceButtonsView?.SetVisible(settings.TapButtonsEnabled || settings.DisableSwipe);
            // Prominence (graphics alpha) is separate from existence/hit-target above: normal
            // swipe-first play keeps the buttons invisible-but-tappable; DisableSwipe is the one
            // signal that unambiguously means tapping is no longer optional, so only it restores
            // full visibility. TapButtonsEnabled alone (the default-on accessibility toggle) does
            // not, since always defaulting to prominent would contradict the new art direction's
            // "swipe first" composition for every player who has not changed anything.
            tapChoiceButtonsView?.SetProminent(settings.DisableSwipe);
        }

        private void PlayGameplayMusic()
        {
            if (audioService == null || cues == null || string.IsNullOrEmpty(cues.GameplayMusic))
            {
                return;
            }

            audioService.PlayMusic(cues.GameplayMusic);
        }

        private void PlayUiClick()
        {
            if (audioService == null || cues == null || string.IsNullOrEmpty(cues.UiClick))
            {
                return;
            }

            audioService.Play(cues.UiClick);
        }

        private void StartSession()
        {
            SessionStartMode mode = sessionIntent != null ? sessionIntent.Mode : fallbackStartMode;

            if (mode == SessionStartMode.Continue && session.CanResume())
            {
                session.Resume();
                return;
            }
            if (mode == SessionStartMode.NewGame
                && tutorialCoordinator != null
                && tutorialCoordinator.TryGateNewGame(settingsStore, () => session.StartNewGame()))
            {
                return;
            }
            session.StartNewGame();
        }

        // --- Subscriptions -------------------------------------------------------

        private void Subscribe()
        {
            // Deliberately independent of the session: the handlers guard for a null one, so
            // subscription order never has to be reasoned about.
            if (subscribed)
            {
                return;
            }

            if (swipeController != null)
            {
                swipeController.DecisionConfirmed += HandleDecisionConfirmed;
                swipeController.ExitAnimationCompleted += HandleExitCompleted;
                swipeController.ChoicePreviewChanged += HandleChoicePreviewChanged;
                swipeController.ChoicePreviewCleared += HandleChoicePreviewCleared;
            }

            if (gameOverView != null)
            {
                gameOverView.RestartRequested += HandleRestartRequested;
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
                swipeController.DecisionConfirmed -= HandleDecisionConfirmed;
                swipeController.ExitAnimationCompleted -= HandleExitCompleted;
                swipeController.ChoicePreviewChanged -= HandleChoicePreviewChanged;
                swipeController.ChoicePreviewCleared -= HandleChoicePreviewCleared;
            }

            if (gameOverView != null)
            {
                gameOverView.RestartRequested -= HandleRestartRequested;
            }

            subscribed = false;
        }

        // --- Event translation ------------------------------------------------------

        private void HandleDecisionConfirmed(ChoiceSide side)
        {
            // The session rejects anything invalid for its state, so no guard is needed here.
            session?.ConfirmDecision(side);
        }

        private void HandleExitCompleted(ChoiceSide side)
        {
            session?.NotifyCardExitCompleted();
        }

        private void HandleRestartRequested()
        {
            PlayUiClick();
            session?.Restart();
        }

        private void HandleChoicePreviewChanged(ChoiceSide side, float strength)
        {
            CardDefinition card = session?.CurrentCard;
            if (card == null || hudView == null)
            {
                hudView?.ClearChoiceImpact();
                return;
            }

            ChoiceDefinition choice = side == ChoiceSide.Left ? card.LeftChoice : card.RightChoice;
            if (choice == null)
            {
                hudView.ClearChoiceImpact();
                return;
            }

            hudView.ShowChoiceImpact(choice.Deltas, strength);
        }

        private void HandleChoicePreviewCleared()
        {
            hudView?.ClearChoiceImpact();
        }

        /// <summary>Coalesced pause/focus-loss entry point; never creates an extra save.</summary>
        public void HandleApplicationInterrupted()
        {
            if (session == null)
            {
                swipeController?.CancelInteraction();
                return;
            }

            if (session.State == GameSessionState.WaitingForCardExit)
            {
                // The decision was already persisted. Completing presentation is idempotent at
                // the GameSession boundary and deliberately performs no additional save.
                session.NotifyCardExitCompleted();
                return;
            }

            swipeController?.CancelInteraction();
            hudView?.ClearChoiceImpact();
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by scene setup and tests.</summary>
        public void SetAuthoringReferences(
            ContentCatalogue contentCatalogue,
            CardView card,
            HUDView hud,
            GameOverView gameOver,
            CardSwipeController swipe,
            AudioService audio = null,
            SessionStartMode startMode = SessionStartMode.NewGame,
            RunStatusView status = null,
            FooterView footer = null,
            TutorialCoordinator tutorial = null,
            TapChoiceButtonsView tapChoices = null)
        {
            catalogue = contentCatalogue;
            cardView = card;
            hudView = hud;
            gameOverView = gameOver;
            swipeController = swipe;
            audioService = audio;
            fallbackStartMode = startMode;
            runStatusView = status;
            footerView = footer;
            tutorialCoordinator = tutorial;
            tapChoiceButtonsView = tapChoices;
        }
#endif
    }
}
