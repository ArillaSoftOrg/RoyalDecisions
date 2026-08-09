using RoyalDecisions.Domain;

namespace RoyalDecisions.Application
{
    /// <summary>
    /// The application's port onto settings persistence, kept separate from run data.
    /// </summary>
    /// <remarks>
    /// <see cref="Load"/> never fails: unreadable preferences fall back to defaults rather than
    /// standing between the player and the game.
    /// </remarks>
    public interface ISettingsStore
    {
        GameSettings Load();

        SaveOutcome Save(GameSettings settings);
    }
}
