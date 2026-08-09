using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// The set of cards and endings a run draws from, plus the card the run opens on.
    /// </summary>
    /// <remarks>
    /// A container, not a service: it holds references and nothing else. Selection, filtering and
    /// ending lookup all live in the Domain services, which take plain lists so they never depend
    /// on this ScriptableObject.
    /// </remarks>
    public class ContentCatalogue : ScriptableObject
    {
        [Tooltip("Stored pre-sorted by ID so regenerating content produces no spurious asset diff.")]
        [SerializeField] private CardDefinition[] cards = Array.Empty<CardDefinition>();

        [SerializeField] private EndingDefinition[] endings = Array.Empty<EndingDefinition>();

        [Tooltip("Card forced at the start of a run. Leave empty to open on a normal draw.")]
        [SerializeField] private string openingCardId = string.Empty;

        public IReadOnlyList<CardDefinition> Cards => cards ?? Array.Empty<CardDefinition>();

        public IReadOnlyList<EndingDefinition> Endings => endings ?? Array.Empty<EndingDefinition>();

        public string OpeningCardId => openingCardId ?? string.Empty;

        public bool HasOpeningCard => !string.IsNullOrEmpty(openingCardId);

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the placeholder content generator and by tests.
        /// Compiled out of player builds so runtime code cannot mutate content.
        /// </summary>
        public void SetAuthoringData(
            CardDefinition[] catalogueCards,
            EndingDefinition[] catalogueEndings,
            string opening)
        {
            cards = catalogueCards ?? Array.Empty<CardDefinition>();
            endings = catalogueEndings ?? Array.Empty<EndingDefinition>();
            openingCardId = opening ?? string.Empty;
        }
#endif
    }
}
