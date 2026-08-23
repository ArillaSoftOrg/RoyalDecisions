using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// One of several equally-likely <see cref="StatDeltas"/> outcomes, picked through the run's
    /// seeded random source at resolution time.
    /// </summary>
    /// <remarks>
    /// For story content authored as "variable" (a choice whose outcome is described as one thing
    /// most of the time and another sometimes) rather than as a fixed number. Deliberately routed
    /// through <c>IRandomSource</c> rather than <c>UnityEngine.Random</c> so the same run seed still
    /// reproduces the same run — see CLAUDE.md's ban on ungoverned randomness.
    /// </remarks>
    [Serializable]
    public sealed class RandomStatOutcome
    {
        [SerializeField] private StatDeltas[] options = Array.Empty<StatDeltas>();

        public RandomStatOutcome(params StatDeltas[] options)
        {
            this.options = options ?? Array.Empty<StatDeltas>();
        }

        public IReadOnlyList<StatDeltas> Options => options ?? Array.Empty<StatDeltas>();

        public bool HasOptions => Options.Count > 0;
    }
}
