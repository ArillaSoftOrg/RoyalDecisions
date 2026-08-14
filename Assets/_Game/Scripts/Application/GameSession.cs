using System;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Application
{
    /// <summary>
    /// Orchestrates a play session: start, present, resolve, save, end, restart.
    /// </summary>
    /// <remarks>
    /// Pure C# with no MonoBehaviour, no scene and no file I/O, so the whole flow is testable
    /// without Unity. It owns <em>ordering</em> and nothing else — every rule belongs to the domain
    /// services it calls, and every pixel belongs to the presenter it drives.
    ///
    /// Commands are pushed in by a controller rather than subscribed to, which keeps Unity events
    /// out of the application layer entirely.
    /// </remarks>
    public sealed class GameSession
    {
        private readonly ContentCatalogue catalogue;
        private readonly IGamePresenter presenter;
        private readonly IRunSaveStore runSaveStore;
        private readonly ISeedProvider seedProvider;
        private readonly IAudioPlayer audioPlayer;

        private readonly CardDeckService deckService = new CardDeckService(new ConditionEvaluator());
        private readonly GameOverEvaluator gameOverEvaluator = new GameOverEvaluator();

        private RunState runState;
        private StatSystem statSystem;
        private ChoiceResolver choiceResolver;
        private CardDefinition currentCard;

        private GameOverResult pendingGameOver;
        private bool hasPendingGameOver;
        private bool statsBound;

        /// <summary>Where to continue from once a failed save finally succeeds.</summary>
        private GameSessionState resumeStateAfterSave = GameSessionState.PresentingCard;

        public GameSession(GameSessionDependencies dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            catalogue = dependencies.Catalogue;
            presenter = dependencies.Presenter;
            runSaveStore = dependencies.RunSaveStore;
            seedProvider = dependencies.SeedProvider;
            audioPlayer = dependencies.AudioPlayer;
        }

        public event Action<GameSessionState> StateChanged;

        /// <summary>Raised once per card, right after it becomes the one shown to the player.</summary>
        public event Action<CardDefinition> CardPresented;

        /// <summary>Forwards <see cref="StatSystem.StatChanged"/> for the currently bound run.</summary>
        public event Action<StatChange> StatValueChanged;

        public GameSessionState State { get; private set; } = GameSessionState.Uninitialized;

        public RunState CurrentRun => runState;

        public CardDefinition CurrentCard => currentCard;

        public SessionError LastError { get; private set; } = SessionError.None;

        /// <summary>Diagnostic: the save that was resumed had to be repaired on load.</summary>
        public bool LastLoadWasRepaired { get; private set; }

        /// <summary>Diagnostic: a forced-next ID that matched no card, since cleared.</summary>
        public string LastMissingForcedCardId { get; private set; } = string.Empty;

        // --- Commands --------------------------------------------------------

        /// <summary>True only for a save that is present and structurally loadable.</summary>
        public bool CanResume()
        {
            if (!runSaveStore.HasSave())
            {
                return false;
            }

            RunLoadOutcome outcome = runSaveStore.Load();
            return outcome.Succeeded && outcome.HasRun && outcome.RunState.IsRunActive;
        }

        public SessionResult StartNewGame()
        {
            if (!AcceptsStart())
            {
                return Reject("A run is already in progress.");
            }

            return BeginRun(null);
        }

        public SessionResult Resume()
        {
            if (!AcceptsStart())
            {
                return Reject("A run is already in progress.");
            }

            SetState(GameSessionState.Loading);

            RunLoadOutcome outcome = runSaveStore.Load();
            LastLoadWasRepaired = outcome.WasRepaired;

            switch (outcome.Status)
            {
                case RunLoadStatus.NoSave:
                    // Not an error: there is simply nothing to resume.
                    SetState(GameSessionState.Uninitialized);
                    return Rejected(SessionErrorCode.LoadFailed, "There is no save to resume.");

                case RunLoadStatus.Corrupt:
                    return FailPersistence(SessionError.Terminal(
                        SessionErrorCode.CorruptSave,
                        "The save could not be read and has been left untouched. " + outcome.Message));

                case RunLoadStatus.UnsupportedVersion:
                    return FailPersistence(SessionError.Terminal(
                        SessionErrorCode.UnsupportedSave,
                        "The save was written by a newer build and has been left untouched. "
                        + outcome.Message));

                case RunLoadStatus.ReadFailed:
                    return FailPersistence(SessionError.Recoverable(
                        SessionErrorCode.LoadFailed, outcome.Message));
            }

            if (!outcome.HasRun)
            {
                return FailPersistence(SessionError.Terminal(
                    SessionErrorCode.LoadFailed, "The save produced no run."));
            }

            if (!outcome.RunState.IsRunActive)
            {
                SetState(GameSessionState.Uninitialized);
                return Rejected(
                    SessionErrorCode.LoadFailed,
                    "The saved run has ended and cannot be continued.");
            }

            return BeginRun(outcome.RunState);
        }

        public SessionResult ConfirmDecision(ChoiceSide side)
        {
            if (State != GameSessionState.AwaitingDecision)
            {
                return Reject("No card is awaiting a decision.");
            }

            SetState(GameSessionState.ResolvingDecision);

            CardDefinition card = currentCard;
            ChoiceDefinition choice = card == null
                ? null
                : (side == ChoiceSide.Left ? card.LeftChoice : card.RightChoice);

            ChoiceResolution resolution = choiceResolver.Resolve(runState, card, side);

            if (!resolution.Succeeded)
            {
                // The domain's own duplicate guard refused this. The card is still on screen, so
                // return to awaiting rather than stranding the session.
                SetState(GameSessionState.AwaitingDecision);
                return Rejected(
                    SessionErrorCode.DecisionRejected,
                    "The decision was refused: " + resolution.Status);
            }

            presenter.RefreshStats(statSystem.Current);
            PlayChoiceAudio(choice);

            // Game over is evaluated before the save, so the ended run is what reaches disk. Saving
            // first would persist a file claiming an active run that has actually finished, and
            // Continue would resume a dead game.
            EvaluateGameOver();

            SaveOutcome saveOutcome = runSaveStore.Save(runState);
            if (!saveOutcome.Succeeded)
            {
                resumeStateAfterSave = GameSessionState.WaitingForCardExit;
                return FailPersistence(SessionError.Recoverable(
                    SessionErrorCode.SaveFailed, saveOutcome.Message));
            }

            // Gameplay is resolved and persisted; the card is still leaving the screen.
            SetState(GameSessionState.WaitingForCardExit);
            return Ok();
        }

        public SessionResult NotifyCardExitCompleted()
        {
            if (State != GameSessionState.WaitingForCardExit)
            {
                return Reject("No card is leaving the screen.");
            }

            presenter.ClearCard();
            currentCard = null;

            if (hasPendingGameOver)
            {
                presenter.ShowGameOver(pendingGameOver);
                hasPendingGameOver = false;
                SetState(GameSessionState.ShowingGameOver);
                return Ok();
            }

            return PresentNextCard();
        }

        public SessionResult Restart()
        {
            if (State != GameSessionState.ShowingGameOver
                && State != GameSessionState.PersistenceError
                && State != GameSessionState.ContentError)
            {
                // A second restart arrives here: the first one already left ShowingGameOver.
                return Reject("Restart is not available right now.");
            }

            presenter.CancelInput();
            presenter.HideGameOver();
            presenter.ClearCard();

            hasPendingGameOver = false;
            currentCard = null;

            return BeginRun(null);
        }

        public SessionResult RetrySave()
        {
            if (State != GameSessionState.PersistenceError)
            {
                return Reject("There is no failed save to retry.");
            }

            if (runState == null)
            {
                return Reject("There is no run to save.");
            }

            SaveOutcome outcome = runSaveStore.Save(runState);
            if (!outcome.Succeeded)
            {
                return FailPersistence(SessionError.Recoverable(
                    SessionErrorCode.SaveFailed, outcome.Message));
            }

            LastError = SessionError.None;

            if (resumeStateAfterSave == GameSessionState.WaitingForCardExit)
            {
                SetState(GameSessionState.WaitingForCardExit);
                return Ok();
            }

            return PresentNextCard();
        }

        /// <summary>Releases subscriptions and drops the run. Safe to call repeatedly.</summary>
        public void Shutdown()
        {
            UnbindStats();

            presenter.CancelInput();

            runState = null;
            statSystem = null;
            choiceResolver = null;
            currentCard = null;
            hasPendingGameOver = false;

            SetState(GameSessionState.Uninitialized);
        }

        /// <summary>
        /// Deletes the run save and returns to <see cref="GameSessionState.Uninitialized"/>. The
        /// player-facing counterpart to the development-only DeleteSave command — settings are a
        /// separate file and are never touched by this call.
        /// </summary>
        public SessionResult ResetProgress()
        {
            Shutdown();
            SaveOutcome deleted = runSaveStore.Delete();
            return deleted.Succeeded
                ? Ok()
                : FailPersistence(SessionError.Recoverable(SessionErrorCode.SaveFailed, deleted.Message));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public SessionResult ExecuteDevelopmentCommand(DevelopmentSessionCommand command)
        {
            switch (command)
            {
                case DevelopmentSessionCommand.NewGame:
                    Shutdown();
                    return StartNewGame();
                case DevelopmentSessionCommand.DeleteSave:
                    return ResetProgress();
                case DevelopmentSessionCommand.ChooseLeft:
                    return DevelopmentChooseAndComplete(ChoiceSide.Left);
                case DevelopmentSessionCommand.ChooseRight:
                    return DevelopmentChooseAndComplete(ChoiceSide.Right);
                default:
                    return Reject("Unknown development command.");
            }
        }

        public SessionResult DevelopmentSetStats(StatValues values)
        {
            if (runState == null || statSystem == null)
            {
                return Reject("No development run is active.");
            }
            runState.SetStats(values.Sanitized());
            if (statSystem != null)
            {
                statSystem.StatChanged -= HandleStatSystemChanged;
            }
            statSystem = new StatSystem(runState);
            statSystem.StatChanged += HandleStatSystemChanged;
            choiceResolver = new ChoiceResolver(statSystem);
            presenter.UnbindStats();
            presenter.BindStats(statSystem);
            presenter.RefreshStats(statSystem.Current);
            SaveOutcome outcome = runSaveStore.Save(runState);
            return outcome.Succeeded
                ? Ok()
                : FailPersistence(SessionError.Recoverable(
                    SessionErrorCode.SaveFailed, outcome.Message));
        }

        public SessionResult DevelopmentSetFlag(string flag, bool present)
        {
            if (runState == null || string.IsNullOrWhiteSpace(flag))
            {
                return Reject("A run and non-empty flag are required.");
            }
            if (present) runState.AddFlag(flag); else runState.RemoveFlag(flag);
            SaveOutcome outcome = runSaveStore.Save(runState);
            return outcome.Succeeded
                ? Ok()
                : FailPersistence(SessionError.Recoverable(
                    SessionErrorCode.SaveFailed, outcome.Message));
        }

        private SessionResult DevelopmentChooseAndComplete(ChoiceSide side)
        {
            SessionResult decision = ConfirmDecision(side);
            return decision.Accepted ? NotifyCardExitCompleted() : decision;
        }
#endif

        // --- Run lifecycle ------------------------------------------------------

        private SessionResult BeginRun(RunState restored)
        {
            SetState(GameSessionState.Loading);

            if (!TryValidateContent(out SessionError contentError))
            {
                return FailContent(contentError);
            }

            // Release the previous run's subscription before building a new one, so an event from a
            // discarded StatSystem can never reach the HUD of a run that replaced it.
            UnbindStats();

            if (restored == null)
            {
                if (!TryCreateNewRun(out SessionError openingError))
                {
                    return FailContent(openingError);
                }
            }
            else
            {
                runState = restored;
            }

            statSystem = new StatSystem(runState);
            choiceResolver = new ChoiceResolver(statSystem);

            presenter.BindStats(statSystem);
            statSystem.StatChanged += HandleStatSystemChanged;
            statsBound = true;

            presenter.HideGameOver();
            presenter.RefreshStats(statSystem.Current);

            if (restored == null)
            {
                // Saved immediately, so Continue can never point at a run the player abandoned by
                // starting a new one.
                SaveOutcome outcome = runSaveStore.Save(runState);
                if (!outcome.Succeeded)
                {
                    resumeStateAfterSave = GameSessionState.PresentingCard;
                    return FailPersistence(SessionError.Recoverable(
                        SessionErrorCode.SaveFailed, outcome.Message));
                }
            }
            return PresentNextCard();
        }

        private bool TryCreateNewRun(out SessionError error)
        {
            string openingCardId = catalogue.OpeningCardId;

            if (string.IsNullOrEmpty(openingCardId) || FindCard(openingCardId) == null)
            {
                // Refuse rather than silently opening on a random card: a run that does not start
                // where the content says it starts is a content bug, not a playable variation.
                error = SessionError.Terminal(
                    SessionErrorCode.InvalidOpeningCard,
                    "The catalogue does not name a usable opening card.");
                runState = null;
                return false;
            }

            runState = RunState.CreateNew(seedProvider.NextSeed());

            // Forced cards bypass conditions and consume no randomness, so the opening card is
            // guaranteed to be first and the run's RNG stream begins untouched.
            runState.SetForcedNextCardId(openingCardId);

            error = SessionError.None;
            return true;
        }

        private SessionResult PresentNextCard()
        {
            SetState(GameSessionState.PresentingCard);

            IRandomSource random = SeededRandomSource.ForTurn(runState.Seed, runState.Turn);
            CardSelectionResult selection = deckService.SelectCard(runState, CardList, random);

            switch (selection.Status)
            {
                case CardSelectionStatus.EmptyCatalogue:
                    return FailContent(SessionError.Terminal(
                        SessionErrorCode.EmptyCatalogue, "The catalogue holds no cards."));

                case CardSelectionStatus.NoEligibleCard:
                    // Terminal for this run: eligibility cannot change without a decision, so
                    // retrying would loop forever.
                    return FailContent(SessionError.Terminal(
                        SessionErrorCode.NoEligibleCard,
                        "No card is currently drawable."));

                case CardSelectionStatus.ForcedCardMissing:
                    LastMissingForcedCardId = runState.ForcedNextCardId;

                    // Cleared before anything else, so the same broken target is never retried.
                    runState.ClearForcedNextCardId();

                    if (!selection.HasCard)
                    {
                        return FailContent(SessionError.Terminal(
                            SessionErrorCode.NoEligibleCard,
                            "A forced card was missing and nothing else was drawable."));
                    }

                    break;

                case CardSelectionStatus.Forced:
                    runState.ClearForcedNextCardId();
                    break;
            }

            currentCard = selection.Card;

            // Armed before input is enabled: this is the token the domain's duplicate guard reads.
            runState.SetCurrentCardId(currentCard.Id);

            presenter.ShowTurn(runState.Turn + 1);
            presenter.ShowCard(currentCard);
            CardPresented?.Invoke(currentCard);
            presenter.PrepareForInput();

            SetState(GameSessionState.AwaitingDecision);
            return Ok();
        }

        private void EvaluateGameOver()
        {
            GameOverResult over = gameOverEvaluator.Evaluate(runState, EndingList);

            if (!over.IsGameOver)
            {
                return;
            }

            runState.EndRun();
            pendingGameOver = over;
            hasPendingGameOver = true;

            if (!over.HasEnding)
            {
                // Recorded, not fatal: the ending view shows its configured generic fallback.
                LastError = SessionError.Recoverable(
                    SessionErrorCode.MissingEnding,
                    "No ending covers " + over.TriggerStat + "/" + over.Boundary + ".");
            }
        }

        private void PlayChoiceAudio(ChoiceDefinition choice)
        {
            if (audioPlayer == null || choice == null || !choice.HasAudioEvent)
            {
                return;
            }

            audioPlayer.Play(choice.AudioEventId);
        }

        // --- Content -------------------------------------------------------------

        private bool TryValidateContent(out SessionError error)
        {
            if (catalogue == null)
            {
                error = SessionError.Terminal(
                    SessionErrorCode.MissingCatalogue, "No content catalogue was supplied.");
                return false;
            }

            if (catalogue.Cards.Count == 0)
            {
                error = SessionError.Terminal(
                    SessionErrorCode.EmptyCatalogue, "The catalogue holds no cards.");
                return false;
            }

            error = SessionError.None;
            return true;
        }

        private System.Collections.Generic.IReadOnlyList<CardDefinition> CardList =>
            catalogue.Cards;

        private System.Collections.Generic.IReadOnlyList<EndingDefinition> EndingList =>
            catalogue.Endings;

        private CardDefinition FindCard(string cardId)
        {
            System.Collections.Generic.IReadOnlyList<CardDefinition> cards = catalogue.Cards;

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];

                if (card != null && string.Equals(card.Id, cardId, StringComparison.Ordinal))
                {
                    return card;
                }
            }

            return null;
        }

        // --- State plumbing ---------------------------------------------------------

        private bool AcceptsStart()
        {
            return State == GameSessionState.Uninitialized;
        }

        private void UnbindStats()
        {
            if (!statsBound)
            {
                return;
            }

            presenter.UnbindStats();
            if (statSystem != null)
            {
                statSystem.StatChanged -= HandleStatSystemChanged;
            }
            statsBound = false;
        }

        private void HandleStatSystemChanged(StatChange change) => StatValueChanged?.Invoke(change);

        private void SetState(GameSessionState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(next);
        }

        private SessionResult Ok()
        {
            return SessionResult.Ok(State);
        }

        private SessionResult Reject(string message)
        {
            return SessionResult.Rejected(State, SessionErrorCode.InvalidStateForCommand, message);
        }

        private SessionResult Rejected(SessionErrorCode code, string message)
        {
            return SessionResult.Rejected(State, code, message);
        }

        private SessionResult FailContent(SessionError error)
        {
            LastError = error;
            SetState(GameSessionState.ContentError);
            return SessionResult.Failed(State, error);
        }

        private SessionResult FailPersistence(SessionError error)
        {
            LastError = error;
            SetState(GameSessionState.PersistenceError);
            return SessionResult.Failed(State, error);
        }
    }
}
