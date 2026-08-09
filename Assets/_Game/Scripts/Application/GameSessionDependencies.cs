using System;
using RoyalDecisions.Data;

namespace RoyalDecisions.Application
{
    /// <summary>
    /// Everything a <see cref="GameSession"/> needs, supplied once at construction.
    /// </summary>
    /// <remarks>
    /// Constructor injection rather than a locator or a singleton, so a test builds a session from
    /// fakes with no global state to reset between cases.
    ///
    /// The catalogue is deliberately allowed to be null: a missing catalogue is a content error the
    /// session reports through its state machine, not an exception thrown during wiring.
    /// </remarks>
    public sealed class GameSessionDependencies
    {
        public GameSessionDependencies(
            ContentCatalogue catalogue,
            IGamePresenter presenter,
            IRunSaveStore runSaveStore,
            ISeedProvider seedProvider,
            IAudioPlayer audioPlayer = null)
        {
            Catalogue = catalogue;
            Presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            RunSaveStore = runSaveStore ?? throw new ArgumentNullException(nameof(runSaveStore));
            SeedProvider = seedProvider ?? throw new ArgumentNullException(nameof(seedProvider));

            // Optional: audio is never required for a decision to resolve.
            AudioPlayer = audioPlayer;
        }

        public ContentCatalogue Catalogue { get; }

        public IGamePresenter Presenter { get; }

        public IRunSaveStore RunSaveStore { get; }

        public ISeedProvider SeedProvider { get; }

        public IAudioPlayer AudioPlayer { get; }
    }
}
