using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// The ordered set of slides a <c>PrologueSequenceController</c> plays. Editable entirely from
    /// the Inspector — add, remove, or reorder <see cref="PrologueSlideData"/> entries here without
    /// touching any code, and the controller/scene adapt automatically.
    /// </summary>
    [CreateAssetMenu(menuName = "Royal Decisions/Story/Prologue Sequence", fileName = "DefaultPrologue")]
    public sealed class PrologueSequenceData : ScriptableObject
    {
        [Tooltip("Shown in order, one at a time.")]
        [SerializeField] private PrologueSlideData[] slides = Array.Empty<PrologueSlideData>();

        public IReadOnlyList<PrologueSlideData> Slides => slides ?? Array.Empty<PrologueSlideData>();

        public int SlideCount => Slides.Count;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the prologue content generator and by tests. Compiled
        /// out of player builds so runtime code cannot mutate content.
        /// </summary>
        public void SetAuthoringData(PrologueSlideData[] slideData)
        {
            slides = slideData ?? Array.Empty<PrologueSlideData>();
        }
#endif
    }
}
