using System;
using System.Collections.Generic;
using RoyalDecisions.Application;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>Hands out a scripted seed sequence, so an entire run is reproducible.</summary>
    public sealed class FakeSeedProvider : ISeedProvider
    {
        private readonly Queue<int> seeds;
        private readonly int fallback;

        public FakeSeedProvider(params int[] scripted)
        {
            seeds = new Queue<int>(scripted ?? Array.Empty<int>());
            fallback = 1;
        }

        public int CallCount { get; private set; }

        public int LastSeed { get; private set; }

        public int NextSeed()
        {
            CallCount++;
            LastSeed = seeds.Count > 0 ? seeds.Dequeue() : fallback;
            return LastSeed;
        }
    }
}
