using System;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// A deterministic xorshift32 pseudo-random stream.
    /// </summary>
    /// <remarks>
    /// Deliberately hand-rolled rather than wrapping <c>System.Random</c>: that type's algorithm has
    /// changed between .NET runtimes, so the same seed would not reproduce the same run after a
    /// Unity upgrade. xorshift32 is fully specified in five lines and is stable everywhere.
    /// </remarks>
    public sealed class SeededRandomSource : IRandomSource
    {
        /// <summary>
        /// Substituted for a zero state. xorshift is absorbing at zero — a zero state would emit
        /// zero forever, silently turning "random" selection into "always the first card".
        /// </summary>
        private const uint FallbackState = 0x9E3779B9u;

        private uint state;

        public SeededRandomSource(int seed)
        {
            state = Normalize(unchecked((uint)seed));
        }

        /// <summary>
        /// Builds the stream belonging to one turn of one run.
        /// </summary>
        /// <remarks>
        /// Because the stream is a pure function of (seed, turn) rather than a long-lived cursor,
        /// a resumed save reproduces exactly the same draw without storing any RNG state in the
        /// save file.
        /// </remarks>
        public static SeededRandomSource ForTurn(int runSeed, int turn)
        {
            uint mixed = Mix(unchecked((uint)runSeed), unchecked((uint)turn));
            return new SeededRandomSource(unchecked((int)mixed));
        }

        /// <summary>
        /// Distinct salt for <see cref="ForTurn"/>'s <c>turn</c> input, so a choice's random outcome
        /// draws from a different stream than the same turn's card selection despite sharing a turn
        /// number.
        /// </summary>
        private const uint ChoiceResolutionSalt = 0x5BD1E995u;

        /// <summary>
        /// Builds the stream used to resolve a "variable" choice outcome (see
        /// <see cref="RoyalDecisions.Data.RandomStatOutcome"/>) on one turn of one run.
        /// </summary>
        public static SeededRandomSource ForChoiceResolution(int runSeed, int turn)
        {
            uint mixed = Mix(unchecked((uint)runSeed), unchecked((uint)turn) ^ ChoiceResolutionSalt);
            return new SeededRandomSource(unchecked((int)mixed));
        }

        /// <summary>Current internal state, exposed so a later phase could persist a stream.</summary>
        public uint State => state;

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax),
                    exclusiveMax,
                    "Exclusive maximum must be at least 1.");
            }

            // Modulo folding. The bias against a 32-bit state at deck-sized ranges is below one
            // part per million, which is immaterial for card selection and costs no branches.
            return (int)(NextUInt() % (uint)exclusiveMax);
        }

        private uint NextUInt()
        {
            unchecked
            {
                uint x = state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                state = x;
                return x;
            }
        }

        /// <summary>
        /// SplitMix32-style finaliser. The additive constant keeps the (0, 0) case away from the
        /// absorbing zero state before <see cref="Normalize"/> is even consulted.
        /// </summary>
        private static uint Mix(uint runSeed, uint turn)
        {
            unchecked
            {
                uint z = runSeed + FallbackState + (turn * 0x85EBCA6Bu);
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return z ^ (z >> 16);
            }
        }

        private static uint Normalize(uint value)
        {
            return value == 0u ? FallbackState : value;
        }
    }
}
