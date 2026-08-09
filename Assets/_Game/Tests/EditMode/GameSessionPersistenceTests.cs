using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Resume, repair, refusal and save failure — all against an in-memory store.
    /// </summary>
    /// <remarks>
    /// Nothing here touches persistent data. The fake counts writes, which is what turns "a corrupt
    /// save is never overwritten" into a direct assertion rather than an inference.
    /// </remarks>
    [TestFixture]
    public class GameSessionPersistenceTests
    {
        private const int Seed = 20260731;

        private FakeGamePresenter presenter;
        private FakeRunSaveStore store;
        private FakeSeedProvider seeds;

        [SetUp]
        public void SetUp()
        {
            presenter = new FakeGamePresenter();
            store = new FakeRunSaveStore();
            seeds = new FakeSeedProvider(Seed, Seed + 1);
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        private GameSession Build(ContentCatalogue catalogue, FakeGamePresenter target = null)
        {
            return new GameSession(new GameSessionDependencies(
                catalogue, target ?? presenter, store, seeds, new FakeAudioPlayer()));
        }

        /// <summary>A catalogue with several plain cards, so selection actually has choices.</summary>
        private static ContentCatalogue MultiCardCatalogue()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(id: "card_01_opening"),
                CardTestFactory.Card(id: "card_02_a"),
                CardTestFactory.Card(id: "card_03_b"),
                CardTestFactory.Card(id: "card_04_c"),
                CardTestFactory.Card(id: "card_05_d")
            };

            return GameSessionTestContent.Build(
                cards, CardTestFactory.AllBoundaryEndings(), "card_01_opening");
        }

        // --- 14: deterministic resume -----------------------------------------

        [Test]
        public void ResumeReproducesDeterministicSelection()
        {
            ContentCatalogue catalogue = MultiCardCatalogue();

            GameSession first = Build(catalogue);
            first.StartNewGame();

            for (int turn = 0; turn < 4; turn++)
            {
                first.ConfirmDecision(ChoiceSide.Left);
                first.NotifyCardExitCompleted();
            }

            string expectedNextCard = presenter.LastShownCard.Id;
            int expectedTurn = first.CurrentRun.Turn;
            int expectedSeed = first.CurrentRun.Seed;

            // Rebuild a session from exactly what was persisted.
            FakeGamePresenter resumedPresenter = new FakeGamePresenter();
            GameSession resumed = Build(catalogue, resumedPresenter);

            SessionResult result = resumed.Resume();

            Assert.That(result.Accepted, Is.True, result.ToString());
            Assert.That(resumed.CurrentRun.Seed, Is.EqualTo(expectedSeed));
            Assert.That(resumed.CurrentRun.Turn, Is.EqualTo(expectedTurn));
            Assert.That(resumedPresenter.LastShownCard.Id, Is.EqualTo(expectedNextCard),
                "the stream is a pure function of seed and turn, so no RNG state needs storing");
        }

        [Test]
        public void ResumeContinuesFromTheSavedTurn()
        {
            GameSession first = Build(MultiCardCatalogue());
            first.StartNewGame();
            first.ConfirmDecision(ChoiceSide.Right);
            first.NotifyCardExitCompleted();

            GameSession resumed = Build(MultiCardCatalogue(), new FakeGamePresenter());
            resumed.Resume();

            Assert.That(resumed.CurrentRun.Turn, Is.EqualTo(1));
        }

        [Test]
        public void ResumingAFinishedRunIsRejectedWithoutDeletingIt()
        {
            GameSession first = Build(GameSessionTestContent.WithInstantLoss());
            first.StartNewGame();
            first.ConfirmDecision(ChoiceSide.Left);
            first.NotifyCardExitCompleted();

            FakeGamePresenter resumedPresenter = new FakeGamePresenter();
            GameSession resumed = Build(
                GameSessionTestContent.WithInstantLoss(), resumedPresenter);

            SessionResult result = resumed.Resume();

            Assert.That(result.Accepted, Is.False);
            Assert.That(resumed.State, Is.EqualTo(GameSessionState.Uninitialized));
            Assert.That(resumedPresenter.ShowGameOverCount, Is.Zero);
            Assert.That(store.DeleteCount, Is.Zero);
            Assert.That(resumed.CanResume(), Is.False);
        }

        [Test]
        public void CanResumeReflectsWhetherASaveIsUsable()
        {
            GameSession session = Build(MultiCardCatalogue());
            Assert.That(session.CanResume(), Is.False);

            session.StartNewGame();

            GameSession other = Build(MultiCardCatalogue(), new FakeGamePresenter());
            Assert.That(other.CanResume(), Is.True);
        }

        // --- 15: repaired saves --------------------------------------------------

        [Test]
        public void ARepairedSaveResumesAndIsReportedButNotRewritten()
        {
            GameSession first = Build(MultiCardCatalogue());
            first.StartNewGame();
            int writesAfterStart = store.SaveCount;

            store.ForcedLoadStatus = RunLoadStatus.SuccessAfterRepair;

            GameSession resumed = Build(MultiCardCatalogue(), new FakeGamePresenter());
            SessionResult result = resumed.Resume();

            Assert.That(result.Accepted, Is.True);
            Assert.That(resumed.LastLoadWasRepaired, Is.True, "the repair is exposed diagnostically");
            Assert.That(store.SaveCount, Is.EqualTo(writesAfterStart),
                "loading never rewrites the file; the repair reaches disk at the next decision");
        }

        [Test]
        public void ASaveRecoveredFromBackupResumesNormally()
        {
            GameSession first = Build(MultiCardCatalogue());
            first.StartNewGame();

            store.ForcedLoadStatus = RunLoadStatus.RecoveredFromBackup;

            GameSession resumed = Build(MultiCardCatalogue(), new FakeGamePresenter());

            Assert.That(resumed.Resume().Accepted, Is.True);
            Assert.That(resumed.LastLoadWasRepaired, Is.False);
        }

        // --- 16: unusable saves are never overwritten ---------------------------------

        [TestCase(RunLoadStatus.Corrupt, SessionErrorCode.CorruptSave)]
        [TestCase(RunLoadStatus.UnsupportedVersion, SessionErrorCode.UnsupportedSave)]
        public void AnUnusableSaveBlocksResumeAndIsLeftAlone(
            RunLoadStatus status, SessionErrorCode expectedCode)
        {
            store.ForcedLoadStatus = status;

            GameSession session = Build(MultiCardCatalogue());
            SessionResult result = session.Resume();

            Assert.That(result.Accepted, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.PersistenceError));
            Assert.That(session.LastError.Code, Is.EqualTo(expectedCode));
            Assert.That(store.SaveCount, Is.Zero, "the file must not be rewritten");
            Assert.That(store.DeleteCount, Is.Zero, "and must not be deleted");
        }

        [Test]
        public void AnUnusableSaveMakesResumeUnavailable()
        {
            store.ForcedLoadStatus = RunLoadStatus.Corrupt;

            GameSession session = Build(MultiCardCatalogue());

            Assert.That(session.CanResume(), Is.False);
        }

        [Test]
        public void AReadFailureIsReportedAsRecoverable()
        {
            store.ForcedLoadStatus = RunLoadStatus.ReadFailed;

            GameSession session = Build(MultiCardCatalogue());
            session.Resume();

            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.LoadFailed));
            Assert.That(session.LastError.IsRecoverable, Is.True);
            Assert.That(store.SaveCount, Is.Zero);
        }

        [Test]
        public void ResumingWithNoSaveIsRejectedWithoutError()
        {
            GameSession session = Build(MultiCardCatalogue());

            SessionResult result = session.Resume();

            Assert.That(result.Accepted, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Uninitialized),
                "no save is a normal first launch, not a failure state");
        }

        // --- 17: save failure blocks continuation ------------------------------------------

        [Test]
        public void ASaveFailureStopsTheFlowBeforeTheNextCard()
        {
            GameSession session = Build(MultiCardCatalogue());
            session.StartNewGame();

            int showsBefore = presenter.CountOf("ShowCard");
            store.FailSaves = true;

            SessionResult result = session.ConfirmDecision(ChoiceSide.Left);

            Assert.That(result.Accepted, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.PersistenceError));
            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.SaveFailed));
            Assert.That(session.LastError.IsRecoverable, Is.True);

            session.NotifyCardExitCompleted();

            Assert.That(presenter.CountOf("ShowCard"), Is.EqualTo(showsBefore),
                "the game must not continue as though persistence had succeeded");
        }

        [Test]
        public void RetryingASaveResumesTheFlow()
        {
            GameSession session = Build(MultiCardCatalogue());
            session.StartNewGame();

            store.FailSaves = true;
            session.ConfirmDecision(ChoiceSide.Left);
            Assert.That(session.State, Is.EqualTo(GameSessionState.PersistenceError));

            store.FailSaves = false;
            SessionResult retry = session.RetrySave();

            Assert.That(retry.Accepted, Is.True);
            Assert.That(session.State, Is.EqualTo(GameSessionState.WaitingForCardExit));

            session.NotifyCardExitCompleted();
            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision));
        }

        [Test]
        public void ARetryThatAlsoFailsStaysInThePersistenceErrorState()
        {
            GameSession session = Build(MultiCardCatalogue());
            session.StartNewGame();

            store.FailSaves = true;
            session.ConfirmDecision(ChoiceSide.Left);

            SessionResult retry = session.RetrySave();

            Assert.That(retry.Accepted, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.PersistenceError));
        }

        [Test]
        public void AFailedNewGameSaveBlocksBeforeAnyCardIsShown()
        {
            store.FailSaves = true;

            GameSession session = Build(MultiCardCatalogue());
            SessionResult result = session.StartNewGame();

            Assert.That(result.Accepted, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.PersistenceError));
            Assert.That(presenter.CountOf("ShowCard"), Is.Zero);
        }

        [Test]
        public void RetryingAFailedNewGameSavePresentsTheOpeningCard()
        {
            store.FailSaves = true;
            GameSession session = Build(MultiCardCatalogue());
            session.StartNewGame();

            store.FailSaves = false;
            SessionResult retry = session.RetrySave();

            Assert.That(retry.Accepted, Is.True);
            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision));
            Assert.That(presenter.LastShownCard.Id, Is.EqualTo("card_01_opening"));
        }

        [Test]
        public void RestartEscapesAPersistenceError()
        {
            store.FailSaves = true;
            GameSession session = Build(MultiCardCatalogue());
            session.StartNewGame();

            store.FailSaves = false;
            SessionResult restart = session.Restart();

            Assert.That(restart.Accepted, Is.True);
            Assert.That(session.State, Is.EqualTo(GameSessionState.AwaitingDecision));
        }
    }
}
