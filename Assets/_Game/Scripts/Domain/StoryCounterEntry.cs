using System;
using UnityEngine;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The accumulated value of one named story counter, such as an investigation level.
    /// </summary>
    /// <remarks>
    /// A list of these rather than a Dictionary&lt;string,int&gt; for the same reason as
    /// <see cref="CardCooldownEntry"/>: saves go through JsonUtility, which serialises neither
    /// dictionaries nor any other non-list generic.
    /// </remarks>
    [Serializable]
    public sealed class StoryCounterEntry
    {
        [SerializeField] private string id;
        [SerializeField] private int value;

        public StoryCounterEntry()
        {
            id = string.Empty;
        }

        public StoryCounterEntry(string id, int value)
        {
            this.id = id ?? string.Empty;
            this.value = value;
        }

        public string Id => id ?? string.Empty;

        public int Value => value;

        public void Add(int delta)
        {
            value += delta;
        }
    }
}
