using System;
using RoyalDecisions.Application;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Produces run seeds from the system clock.
    /// </summary>
    /// <remarks>
    /// The only clock read in the whole project. Gameplay receives seeds through
    /// <see cref="ISeedProvider"/>, so a test can make an entire run reproducible by supplying a
    /// fixed sequence instead.
    /// </remarks>
    public sealed class SystemSeedProvider : ISeedProvider
    {
        public int NextSeed()
        {
            // Ticks rather than seconds: two runs started in the same second must not share a seed.
            long ticks = DateTime.UtcNow.Ticks;
            return unchecked((int)(ticks ^ (ticks >> 32)));
        }
    }
}
