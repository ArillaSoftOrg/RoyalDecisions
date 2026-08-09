namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The single randomness boundary in the game, injected so selection can be driven by a
    /// scripted source in tests and by a seeded stream in play.
    /// </summary>
    /// <remarks>
    /// Nothing outside an implementation of this interface may call a random API — scattering
    /// <c>UnityEngine.Random</c> across systems would make a run impossible to reproduce.
    /// </remarks>
    public interface IRandomSource
    {
        /// <summary>
        /// Returns a value in the range <c>[0, exclusiveMax)</c>.
        /// </summary>
        int NextInt(int exclusiveMax);
    }
}
