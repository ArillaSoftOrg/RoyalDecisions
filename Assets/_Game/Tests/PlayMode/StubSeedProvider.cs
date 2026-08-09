using RoyalDecisions.Application;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>A fixed seed, so a scene test produces the same run every time.</summary>
    public sealed class StubSeedProvider : ISeedProvider
    {
        private readonly int seed;

        public StubSeedProvider(int seed)
        {
            this.seed = seed;
        }

        public int CallCount { get; private set; }

        public int NextSeed()
        {
            CallCount++;
            return seed;
        }
    }
}
