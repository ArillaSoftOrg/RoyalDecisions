using NUnit.Framework;
using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// The player-facing counterpart to the development-only DeleteSave command.
    /// </summary>
    [TestFixture]
    public class GameSessionResetProgressTests
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

        private GameSession StartedSession()
        {
            GameSession session = new GameSession(new GameSessionDependencies(
                GameSessionTestContent.Standard(), presenter, store, seeds, audio));
            session.StartNewGame();
            return session;
        }

        [Test]
        public void ResetProgressDeletesTheSaveAndReturnsToUninitialized()
        {
            GameSession session = StartedSession();

            SessionResult result = session.ResetProgress();

            Assert.That(result.Accepted, Is.True);
            Assert.That(store.DeleteCount, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Uninitialized));
            Assert.That(session.CurrentRun, Is.Null);
        }

        [Test]
        public void ResetProgressIsSafeToCallWithoutAnActiveRun()
        {
            GameSession session = new GameSession(new GameSessionDependencies(
                GameSessionTestContent.Standard(), presenter, store, seeds, audio));

            SessionResult result = session.ResetProgress();

            Assert.That(result.Accepted, Is.True);
            Assert.That(store.DeleteCount, Is.EqualTo(1));
        }
    }
}
