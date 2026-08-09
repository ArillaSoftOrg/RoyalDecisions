using System;
using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Editor
{
    /// <summary>Deterministic incoming/outgoing forced-card link index for authoring UI.</summary>
    public sealed class ContentLinkIndex
    {
        private readonly Dictionary<string, List<string>> incoming =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> outgoing =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public ContentLinkIndex(IReadOnlyList<CardDefinition> cards)
        {
            if (cards == null)
            {
                return;
            }
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrEmpty(card.Id))
                {
                    continue;
                }
                Add(card.Id, card.ForcedNextCardId, "card");
                Add(card.Id, card.LeftChoice?.ForcedNextCardId, "left");
                Add(card.Id, card.RightChoice?.ForcedNextCardId, "right");
            }
            Sort(incoming);
            Sort(outgoing);
        }

        public IReadOnlyList<string> GetIncoming(string cardId) =>
            TryGet(incoming, cardId);

        public IReadOnlyList<string> GetOutgoing(string cardId) =>
            TryGet(outgoing, cardId);

        private void Add(string source, string target, string origin)
        {
            if (string.IsNullOrEmpty(target))
            {
                return;
            }
            AddValue(outgoing, source, target + " (" + origin + ")");
            AddValue(incoming, target, source + " (" + origin + ")");
        }

        private static void AddValue(
            Dictionary<string, List<string>> index,
            string key,
            string value)
        {
            if (!index.TryGetValue(key, out List<string> values))
            {
                values = new List<string>();
                index.Add(key, values);
            }
            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }

        private static void Sort(Dictionary<string, List<string>> index)
        {
            foreach (List<string> values in index.Values)
            {
                values.Sort(StringComparer.Ordinal);
            }
        }

        private static IReadOnlyList<string> TryGet(
            Dictionary<string, List<string>> index,
            string key)
        {
            return key != null && index.TryGetValue(key, out List<string> values)
                ? values
                : Array.Empty<string>();
        }
    }
}
