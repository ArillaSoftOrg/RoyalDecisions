using System;
using UnityEngine;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Records the earliest turn a card may be drawn again after being shown.
    /// </summary>
    /// <remarks>
    /// A list of these rather than a Dictionary&lt;string,int&gt; because saves go through
    /// JsonUtility, which serialises neither dictionaries nor any other non-list generic.
    /// </remarks>
    [Serializable]
    public class CardCooldownEntry
    {
        [SerializeField] private string cardId;
        [SerializeField] private int availableOnTurn;

        public CardCooldownEntry()
        {
            cardId = string.Empty;
        }

        public CardCooldownEntry(string cardId, int availableOnTurn)
        {
            this.cardId = cardId ?? string.Empty;
            this.availableOnTurn = availableOnTurn;
        }

        public string CardId => cardId ?? string.Empty;

        public int AvailableOnTurn => availableOnTurn;

        /// <summary>Pushes the cooldown out; never brings an existing cooldown forward.</summary>
        public void ExtendTo(int turn)
        {
            if (turn > availableOnTurn)
            {
                availableOnTurn = turn;
            }
        }
    }
}
