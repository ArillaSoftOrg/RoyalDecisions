using NUnit.Framework;
using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// The core play loop, driven entirely through the session with fakes for everything else.
    /// </summary>
    /// <remarks>
    /// No Unity scene, no frames, no MonoBehaviour — the session is synchronous, so ordering is
    /// asserted by comparing indices in the presenter's call log rather than by waiting.
    /// </remarks>
    [TestFixture]
    public class GameSessionFlowTests
    {
        private const int Seed = 4242;

        private FakeGamePresenter presenter;
        private FakeRunSaveStore store;
        private FakeSeedProvider seeds;
        private FakeAudioPlayer audio;

        [SetUp]
        public void SetUp()
        {
            presenter = new FakeGamePresenter();
            store = new FakeRunSaveStore();
            seeds = new FakeSeedProvider(Seed, Seed + 1, Seed + 2);
            audio = new FakeAudioPlayer();
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        private GameSession Build(ContentCatalogue catalogue)
        {
            return new GameSession(new GameSessionDependencies(
                catalogue, presenter, store, seeds, audio));
        }

        private GameSession StartedSession(ContentCatalogue catalogue = null)
        {
            GameSession session = Build(catalogue ?? GameSessionTestContent.Standard());
            session.StartNewGame();
            return session;
        }

        /// <summary>Confirms a decision and lets the card finish leaving.</summary>
        private static void ResolveAndFinishExit(GameSession session, ChoiceSide side)
        {
            session.ConfirmDecision(side);
            session.NotifyCardExitCompleted();
        }

        // --- Choice availability -------------------------------------------------

        [Test]
        public void ConfirmDecision_OnAnUnavailableSide_IsRejectedAndAppliesNothing()
        {
            GameSession session = StartedSession(GameSessionTestContent.WithUnavailableLeftChoice());
            StatValues before = session.CurrentRun.Stats;

            SessionResult result = session.ConfirmDecision(ChoiceSide.Left);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(SessionErrorCode.ChoiceUnavailable));
            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision),
                "the card must still be awaiting a decision, not stranded");
            Assert.That(session.CurrentRun.Stats.Authority, Is.EqualTo(before.Authority));
        }

        [Test]
        public void ConfirmDecision_OnAnUnavailableSide_StillAllowsTheOtherSide()
        {
            GameSession session = StartedSession(GameSessionTestContent.WithUnavailableLeftChoice());

            session.ConfirmDecision(ChoiceSide.Left);
            SessionResult result = session.ConfirmDecision(ChoiceSide.Right);

            Assert.That(result.Accepted, Is.True);
            Assert.That(session.CurrentRun.Stats.People, Is.EqualTo(StatBounds.Initial + 5));
        }

        [Test]
        public void DevelopmentChoiceUsesRealExactlyOnceSessionFlow()
        {
            GameSession session = StartedSession();
            int savesBefore = store.SaveCount;

            SessionResult result = session.ExecuteDevelopmentCommand(
                DevelopmentSessionCommand.ChooseLeft);

            Assert.That(result.Accepted, Is.True);
            Assert.That(session.CurrentRun.Turn, Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1));
            Assert.That(session.ExecuteDevelopmentCommand(
                DevelopmentSessionCommand.ChooseLeft).Accepted, Is.True,
                "the second command is a decision for the newly presented card, not a duplicate");
        }

        [Test]
        public void DevelopmentStatMutationRefreshesAndPersistsThroughPorts()
        {
            GameSession session = StartedSession();
            int savesBefore = store.SaveCount;

            SessionResult result = session.DevelopmentSetStats(
                new StatValues(60, 61, 62, 63));

            Assert.That(result.Accepted, Is.True);
            Assert.That(session.CurrentRun.Stats.Authority, Is.EqualTo(60));
            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1));
            Assert.That(presenter.LastStats.Authority, Is.EqualTo(60));
        }

        // --- 1, 2: the opening card ------------------------------------------

        [Test]
        public void NewRunOpensOnTheCatalogueOpeningCard()
        {
            GameSession session = StartedSession();

            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision));
            Assert.That(presenter.LastShownCard.Id,
                Is.EqualTo(GameSessionTestContent.OpeningCardId));
            Assert.That(presenter.LastTurn, Is.EqualTo(1));
            Assert.That(presenter.IndexOf("ShowTurn:1"),
                Is.LessThan(presenter.IndexOf("ShowCard")));
        }

        [Test]
        public void TheOpeningCardConsumesNoRandomness()
        {
            // A forced card bypasses weighted selection entirely, so the run's RNG stream begins
            // untouched and turn 0 is reproducible regardless of catalogue size.
            GameSession session = StartedSession();

            Assert.That(session.CurrentRun.HasForcedNextCard, Is.False,
                "the forced ID is consumed once the card is presented");
            Assert.That(presenter.LastShownCard.Id,
                Is.EqualTo(GameSessionTestContent.OpeningCardId));
        }

        [Test]
        public void ANewRunUsesTheInjectedSeed()
        {
            GameSession session = StartedSession();

            Assert.That(session.CurrentRun.Seed, Is.EqualTo(Seed));
            Assert.That(seeds.CallCount, Is.EqualTo(1), "no clock is read anywhere in the flow");
        }

        // --- 3: arming order ----------------------------------------------------

        [Test]
        public void CurrentCardIdIsSetBeforeInputIsEnabled()
        {
            GameSession session = StartedSession();

            Assert.That(session.CurrentRun.CurrentCardId,
                Is.EqualTo(GameSessionTestContent.OpeningCardId));
            Assert.That(presenter.IndexOf("ShowCard"),
                Is.LessThan(presenter.IndexOf("PrepareForInput")),
                "the card must be on screen before the swipe is armed");
        }

        // --- 4: decisions resolve exactly once -------------------------------------

        [TestCase(ChoiceSide.Left)]
        [TestCase(ChoiceSide.Right)]
        public void ADecisionResolvesExactlyOnce(ChoiceSide side)
        {
            GameSession session = StartedSession();
            int turnBefore = session.CurrentRun.Turn;

            SessionResult first = session.ConfirmDecision(side);
            SessionResult second = session.ConfirmDecision(side);

            Assert.That(first.Accepted, Is.True);
            Assert.That(second.Accepted, Is.False, "the session is no longer awaiting a decision");
            Assert.That(session.CurrentRun.Turn, Is.EqualTo(turnBefore + 1));
            Assert.That(store.SaveCount, Is.EqualTo(2), "the new-run save, then one decision save");
        }

        [Test]
        public void ADecisionAppliesItsStatChanges()
        {
            GameSession session = StartedSession();
            int before = session.CurrentRun.Stats.Authority;

            session.ConfirmDecision(ChoiceSide.Left);

            Assert.That(session.CurrentRun.Stats.Authority, Is.EqualTo(before + 5));
        }

        // --- 5: HUD ------------------------------------------------------------------

        [Test]
        public void TheHudIsBoundToTheRunAndRefreshedFromAuthoritativeValues()
        {
            GameSession session = StartedSession();

            Assert.That(presenter.BindCount, Is.EqualTo(1));
            Assert.That(presenter.IsBound, Is.True);

            session.ConfirmDecision(ChoiceSide.Left);

            Assert.That(presenter.LastStats.Authority,
                Is.EqualTo(session.CurrentRun.Stats.Authority),
                "the HUD is refreshed from the run, not from a copy the session kept");
        }

        // --- 6, 7: saving -------------------------------------------------------------

        [Test]
        public void ADecisionSavesExactlyOnce()
        {
            GameSession session = StartedSession();
            int afterStart = store.SaveCount;

            session.ConfirmDecision(ChoiceSide.Right);

            Assert.That(store.SaveCount, Is.EqualTo(afterStart + 1));
        }

        [Test]
        public void TheSaveHappensBeforeTheNextCardIsPresented()
        {
            GameSession session = StartedSession();
            presenter.Calls.Clear();
            store.Calls.Clear();

            session.ConfirmDecision(ChoiceSide.Right);
            Assert.That(store.Calls, Does.Contain("Save"), "saved while the card is still leaving");

            int showsBeforeExit = presenter.CountOf("ShowCard");
            session.NotifyCardExitCompleted();

            Assert.That(presenter.CountOf("ShowCard"), Is.GreaterThan(showsBeforeExit));
        }

        [Test]
        public void ANewRunIsSavedImmediately()
        {
            StartedSession();

            Assert.That(store.SaveCount, Is.EqualTo(1),
                "Continue must never point at a run the player abandoned by starting a new one");
        }

        // --- 8: sequencing on the exit animation -----------------------------------------

        [Test]
        public void TheNextCardWaitsForTheExitAnimation()
        {
            GameSession session = StartedSession();
            CardDefinition first = presenter.LastShownCard;

            session.ConfirmDecision(ChoiceSide.Right);

            Assert.That(session.State, Is.EqualTo(GameSessionState.WaitingForCardExit));
            Assert.That(presenter.LastShownCard, Is.SameAs(first), "no new card yet");

            session.NotifyCardExitCompleted();

            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision));
        }

        [Test]
        public void ExitCompletionIsIgnoredWhenNoCardIsLeaving()
        {
            GameSession session = StartedSession();

            SessionResult result = session.NotifyCardExitCompleted();

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(SessionErrorCode.InvalidStateForCommand));
        }

        // --- 9, 10: game over --------------------------------------------------------------

        [Test]
        public void TheEndedRunIsPersistedAsInactive()
        {
            GameSession session = Build(GameSessionTestContent.WithInstantLoss());
            session.StartNewGame();

            session.ConfirmDecision(ChoiceSide.Left);

            Assert.That(store.LastSaved.IsRunActive, Is.False,
                "game over is evaluated before the save, so the finished run is what reaches disk");
        }

        [Test]
        public void TheGameOverScreenWaitsForTheCardToLeave()
        {
            GameSession session = Build(GameSessionTestContent.WithInstantLoss());
            session.StartNewGame();

            session.ConfirmDecision(ChoiceSide.Left);
            Assert.That(presenter.ShowGameOverCount, Is.Zero, "the card is still on screen");

            session.NotifyCardExitCompleted();

            Assert.That(presenter.ShowGameOverCount, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(GameSessionState.ShowingGameOver));
        }

        [Test]
        public void AMissingEndingStillEndsTheRun()
        {
            GameSession session = Build(GameSessionTestContent.WithInstantLossAndNoEndings());
            session.StartNewGame();

            ResolveAndFinishExit(session, ChoiceSide.Left);

            Assert.That(session.State, Is.EqualTo(GameSessionState.ShowingGameOver));
            Assert.That(presenter.LastGameOver.IsGameOver, Is.True);
            Assert.That(presenter.LastGameOver.HasEnding, Is.False,
                "the view shows its generic fallback rather than nothing");
            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.MissingEnding));
        }

        // --- 11, 12: forced cards ------------------------------------------------------------

        [Test]
        public void AForcedChainPresentsItsTargetNext()
        {
            ContentCatalogue catalogue = GameSessionTestContent.Build(
                new System.Collections.Generic.List<CardDefinition>
                {
                    CardTestFactory.Card(
                        id: GameSessionTestContent.OpeningCardId,
                        forcedNextCardId: GameSessionTestContent.ChainEndId),
                    CardTestFactory.Card(id: GameSessionTestContent.ChainEndId)
                },
                CardTestFactory.AllBoundaryEndings(),
                GameSessionTestContent.OpeningCardId);

            GameSession session = Build(catalogue);
            session.StartNewGame();

            ResolveAndFinishExit(session, ChoiceSide.Left);

            Assert.That(presenter.LastShownCard.Id,
                Is.EqualTo(GameSessionTestContent.ChainEndId));
        }

        [Test]
        public void AMissingForcedTargetIsClearedAndFallsBack()
        {
            GameSession session = Build(GameSessionTestContent.WithMissingForcedTarget());
            session.StartNewGame();

            ResolveAndFinishExit(session, ChoiceSide.Left);

            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision),
                "a broken chain falls back rather than dead-ending the run");
            Assert.That(session.LastMissingForcedCardId, Is.EqualTo("card_does_not_exist"));
            Assert.That(session.CurrentRun.HasForcedNextCard, Is.False,
                "cleared, so the same broken target is never retried");
        }

        [Test]
        public void AMissingForcedTargetIsNotRetriedOnTheFollowingTurn()
        {
            GameSession session = Build(GameSessionTestContent.WithMissingForcedTarget());
            session.StartNewGame();

            ResolveAndFinishExit(session, ChoiceSide.Left);
            string firstFallback = presenter.LastShownCard.Id;

            ResolveAndFinishExit(session, ChoiceSide.Left);

            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision));
            Assert.That(firstFallback, Is.Not.Empty);
        }

        // --- 13: no eligible card ----------------------------------------------------------------

        [Test]
        public void RunningOutOfEligibleCardsEntersAControlledErrorState()
        {
            GameSession session = Build(GameSessionTestContent.WithSingleOnceCard());
            session.StartNewGame();

            session.ConfirmDecision(ChoiceSide.Left);
            SessionResult result = session.NotifyCardExitCompleted();

            Assert.That(session.State, Is.EqualTo(GameSessionState.ContentError));
            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.NoEligibleCard));
            Assert.That(session.LastError.IsRecoverable, Is.False);
            Assert.That(result.Accepted, Is.False);
        }

        [Test]
        public void AContentErrorDoesNotRetrySelection()
        {
            GameSession session = Build(GameSessionTestContent.WithSingleOnceCard());
            session.StartNewGame();
            ResolveAndFinishExit(session, ChoiceSide.Left);

            int showsAfterError = presenter.CountOf("ShowCard");
            session.NotifyCardExitCompleted();
            session.ConfirmDecision(ChoiceSide.Left);

            Assert.That(presenter.CountOf("ShowCard"), Is.EqualTo(showsAfterError),
                "eligibility cannot change without a decision, so retrying would loop forever");
        }

        // --- 18, 19: restart -------------------------------------------------------------------------

        [Test]
        public void RestartCreatesANewSeedAndReopensOnTheOpeningCard()
        {
            GameSession session = Build(GameSessionTestContent.WithInstantLoss());
            session.StartNewGame();
            int firstSeed = session.CurrentRun.Seed;

            ResolveAndFinishExit(session, ChoiceSide.Left);
            Assert.That(session.State, Is.EqualTo(GameSessionState.ShowingGameOver));

            SessionResult restart = session.Restart();

            Assert.That(restart.Accepted, Is.True);
            Assert.That(session.CurrentRun.Seed, Is.Not.EqualTo(firstSeed));
            Assert.That(presenter.LastShownCard.Id,
                Is.EqualTo(GameSessionTestContent.OpeningCardId));
            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision));
        }

        [Test]
        public void RapidRestartRequestsDoNotCreateOverlappingRuns()
        {
            GameSession session = Build(GameSessionTestContent.WithInstantLoss());
            session.StartNewGame();
            ResolveAndFinishExit(session, ChoiceSide.Left);

            session.Restart();
            int seedAfterFirst = session.CurrentRun.Seed;
            int seedCallsAfterFirst = seeds.CallCount;

            session.Restart();
            session.Restart();

            Assert.That(session.CurrentRun.Seed, Is.EqualTo(seedAfterFirst));
            Assert.That(seeds.CallCount, Is.EqualTo(seedCallsAfterFirst),
                "later restarts are rejected, so no second run is built");
        }

        [Test]
        public void RestartRebindsTheHudToTheNewRun()
        {
            GameSession session = Build(GameSessionTestContent.WithInstantLoss());
            session.StartNewGame();
            ResolveAndFinishExit(session, ChoiceSide.Left);

            int unbindsBefore = presenter.UnbindCount;
            session.Restart();

            Assert.That(presenter.UnbindCount, Is.GreaterThan(unbindsBefore),
                "the old StatSystem must be released before the new one is bound");
            Assert.That(presenter.IsBound, Is.True);
        }

        // --- 20, 21: invalid events and shutdown ----------------------------------------------------

        [Test]
        public void CommandsInvalidForTheCurrentStateAreIgnored()
        {
            GameSession session = Build(GameSessionTestContent.Standard());

            Assert.That(session.ConfirmDecision(ChoiceSide.Left).Accepted, Is.False);
            Assert.That(session.NotifyCardExitCompleted().Accepted, Is.False);
            Assert.That(session.Restart().Accepted, Is.False);
            Assert.That(session.RetrySave().Accepted, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Uninitialized));
        }

        [Test]
        public void StartingTwiceIsRejected()
        {
            GameSession session = StartedSession();

            SessionResult second = session.StartNewGame();

            Assert.That(second.Accepted, Is.False);
            Assert.That(seeds.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void ShutdownReleasesTheHudAndStopsAcceptingDecisions()
        {
            GameSession session = StartedSession();

            session.Shutdown();

            Assert.That(presenter.IsBound, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Uninitialized));
            Assert.That(session.ConfirmDecision(ChoiceSide.Left).Accepted, Is.False,
                "a stale swipe callback after shutdown must not resolve anything");
        }

        [Test]
        public void ShutdownIsIdempotent()
        {
            GameSession session = StartedSession();

            session.Shutdown();
            session.Shutdown();

            Assert.That(session.State, Is.EqualTo(GameSessionState.Uninitialized));
        }

        // --- 22: audio -------------------------------------------------------------------------------

        [Test]
        public void AChoiceAudioCueIsPlayedWhenPresent()
        {
            GameSession session = StartedSession();

            session.ConfirmDecision(ChoiceSide.Left);

            Assert.That(audio.Played, Does.Contain("sfx_open"));
        }

        [Test]
        public void AChoiceWithNoAudioPlaysNothing()
        {
            GameSession session = StartedSession();

            session.ConfirmDecision(ChoiceSide.Right);

            Assert.That(audio.Played, Is.Empty);
        }

        [Test]
        public void AbsentAudioIsNonFatal()
        {
            GameSession session = new GameSession(new GameSessionDependencies(
                GameSessionTestContent.Standard(), presenter, store, seeds, audioPlayer: null));

            session.StartNewGame();

            Assert.That(() => session.ConfirmDecision(ChoiceSide.Left), Throws.Nothing);
            Assert.That(session.State, Is.EqualTo(GameSessionState.WaitingForCardExit));
        }

        // --- Multi-turn -------------------------------------------------------------------------------

        [Test]
        public void SeveralTurnsPlayThroughCleanly()
        {
            GameSession session = StartedSession();

            for (int turn = 0; turn < 8; turn++)
            {
                Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision),
                    "turn " + turn);
                ResolveAndFinishExit(session, turn % 2 == 0 ? ChoiceSide.Left : ChoiceSide.Right);
            }

            Assert.That(session.CurrentRun.Turn, Is.EqualTo(8));
            Assert.That(store.SaveCount, Is.EqualTo(9), "one per decision, plus the new-run save");
        }
    }
}
