using System;
using System.Collections.Generic;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// An <see cref="IRandomSource"/> that returns pre-scripted rolls and records how it was used.
    /// </summary>
    /// <remarks>
    /// <see cref="CallCount"/> exists so a test can prove randomness was *not* consumed — a forced
    /// draw must leave the stream untouched, and only a counter can demonstrate that.
    /// </remarks>
    public sealed class FakeRandomSource : IRandomSource
    {
        private readonly Queue<int> rolls;

        public FakeRandomSource(params int[] queuedRolls)
        {
            rolls = new Queue<int>(queuedRolls ?? Array.Empty<int>());
        }

        public int CallCount { get; private set; }

        /// <summary>The exclusive maximum of the most recent call, or -1 if never called.</summary>
        public int LastExclusiveMax { get; private set; } = -1;

        public int NextInt(int exclusiveMax)
        {
            CallCount++;
            LastExclusiveMax = exclusiveMax;

            if (rolls.Count == 0)
            {
                throw new InvalidOperationException(
                    "FakeRandomSource was asked for more rolls than were queued.");
            }

            return rolls.Dequeue();
        }
    }
}
