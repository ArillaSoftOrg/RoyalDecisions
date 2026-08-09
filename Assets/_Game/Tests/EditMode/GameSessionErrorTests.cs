using NUnit.Framework;
using RoyalDecisions.Application;
using RoyalDecisions.Data;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Every controlled error path, each asserted on a code rather than a log line.
    /// </summary>
    [TestFixture]
    public class GameSessionErrorTests
    {
        private FakeGamePresenter presenter;
        private FakeRunSaveStore store;
        private FakeSeedProvider seeds;

        [SetUp]
        public void SetUp()
        {
            presenter = new FakeGamePresenter();
            store = new FakeRunSaveStore();
            seeds = new FakeSeedProvider(7);
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        private GameSession Build(ContentCatalogue catalogue)
        {
            return new GameSession(new GameSessionDependencies(
                catalogue, presenter, store, seeds, new FakeAudioPlayer()));
        }

        // --- Content errors ---------------------------------------------------

        [Test]
        public void AMissingCatalogueIsReportedRatherThanThrown()
        {
            GameSession session = Build(null);

            SessionResult result = session.StartNewGame();

            Assert.That(result.Accepted, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.ContentError));
            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.MissingCatalogue));
            Assert.That(session.LastError.IsRecoverable, Is.False);
        }

        [Test]
        public void AnEmptyCatalogueIsReported()
        {
            GameSession session = Build(GameSessionTestContent.Empty());

            session.StartNewGame();

            Assert.That(session.State, Is.EqualTo(GameSessionState.ContentError));
            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.EmptyCatalogue));
        }

        [Test]
        public void AnOpeningCardThatDoesNotExistRefusesToStart()
        {
            GameSession session = Build(GameSessionTestContent.WithUnknownOpeningCard());

            session.StartNewGame();

            Assert.That(session.State, Is.EqualTo(GameSessionState.ContentError));
            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.InvalidOpeningCard));
            Assert.That(presenter.CountOf("ShowCard"), Is.Zero,
                "a run must not silently open on a random card");
        }

        [Test]
        public void AnAbsentOpeningCardIdRefusesToStart()
        {
            ContentCatalogue catalogue = GameSessionTestContent.Build(
                new System.Collections.Generic.List<CardDefinition>
                {
                    CardTestFactory.Card(id: "card_a")
                },
                CardTestFactory.AllBoundaryEndings(),
                string.Empty);

            GameSession session = Build(catalogue);
            session.StartNewGame();

            Assert.That(session.LastError.Code, Is.EqualTo(SessionErrorCode.InvalidOpeningCard));
        }

        [Test]
        public void AContentErrorWritesNothing()
        {
            GameSession session = Build(GameSessionTestContent.WithUnknownOpeningCard());

            session.StartNewGame();

            Assert.That(store.SaveCount, Is.Zero,
                "a run that never started must not leave a save behind");
        }

        [Test]
        public void RestartEscapesAContentError()
        {
            GameSession session = Build(GameSessionTestContent.Standard());
            session.StartNewGame();
            session.Shutdown();

            GameSession broken = Build(null);
            broken.StartNewGame();
            Assert.That(broken.State, Is.EqualTo(GameSessionState.ContentError));

            SessionResult restart = broken.Restart();

            Assert.That(restart.Accepted, Is.False,
                "restarting with the same broken content fails the same way");
            Assert.That(broken.State, Is.EqualTo(GameSessionState.ContentError));
        }

        // --- Wiring errors -----------------------------------------------------------

        [Test]
        public void ANullPresenterIsRejectedAtConstruction()
        {
            Assert.That(
                () => new GameSessionDependencies(
                    GameSessionTestContent.Standard(), null, store, seeds),
                Throws.ArgumentNullException);
        }

        [Test]
        public void ANullSaveStoreIsRejectedAtConstruction()
        {
            Assert.That(
                () => new GameSessionDependencies(
                    GameSessionTestContent.Standard(), presenter, null, seeds),
                Throws.ArgumentNullException);
        }

        [Test]
        public void ANullSeedProviderIsRejectedAtConstruction()
        {
            Assert.That(
                () => new GameSessionDependencies(
                    GameSessionTestContent.Standard(), presenter, store, null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void NullDependenciesAreRejected()
        {
            Assert.That(() => new GameSession(null), Throws.ArgumentNullException);
        }

        [Test]
        public void AnAbsentAudioPlayerIsAccepted()
        {
            Assert.That(
                () => new GameSessionDependencies(
                    GameSessionTestContent.Standard(), presenter, store, seeds, null),
                Throws.Nothing,
                "audio is optional throughout the MVP");
        }

        // --- Every error carries a code -------------------------------------------------

        [Test]
        public void EveryFailureExposesATestableCode()
        {
            GameSession session = Build(GameSessionTestContent.Empty());
            SessionResult result = session.StartNewGame();

            Assert.That(result.Error.HasError, Is.True);
            Assert.That(result.Error.Code, Is.Not.EqualTo(SessionErrorCode.None));
            Assert.That(result.Error.Message, Is.Not.Empty);
            Assert.That(result.State, Is.EqualTo(GameSessionState.ContentError));
        }

        [Test]
        public void StateChangesAreObservable()
        {
            GameSession session = Build(GameSessionTestContent.Standard());

            System.Collections.Generic.List<GameSessionState> observed =
                new System.Collections.Generic.List<GameSessionState>();
            session.StateChanged += observed.Add;

            session.StartNewGame();

            Assert.That(observed, Does.Contain(GameSessionState.Loading));
            Assert.That(observed, Does.Contain(GameSessionState.AwaitingDecision));
        }
    }
}
