using System;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// A change a choice makes to one named story counter, such as an investigation level.
    /// </summary>
    /// <remarks>
    /// Counters are the general-purpose equivalent of flags for values that accumulate rather than
    /// merely being present or absent (a flag cannot express "seen this clue three times"). See
    /// <c>RunState.AddToCounter</c>.
    /// </remarks>
    [Serializable]
    public sealed class CounterDelta
    {
        [SerializeField] private string counterId = string.Empty;
        [SerializeField] private int delta;

        public CounterDelta()
        {
        }

        public CounterDelta(string counterId, int delta)
        {
            this.counterId = counterId ?? string.Empty;
            this.delta = delta;
        }

        public string CounterId => counterId ?? string.Empty;

        public int Delta => delta;
    }
}
