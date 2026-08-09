using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Everything that changes during a single playthrough: stats, flags, history and the RNG seed.
    /// </summary>
    /// <remarks>
    /// A plain serialisable class, never a ScriptableObject — player progress must not be written
    /// into project assets. Every field is JsonUtility-compatible: concrete lists and primitives
    /// only, no dictionaries and no polymorphism.
    /// </remarks>
    [Serializable]
    public class RunState
    {
        [SerializeField] private int saveVersion;
        [SerializeField] private int turn;
        [SerializeField] private int seed;
        [SerializeField] private StatValues stats;
        [SerializeField] private List<string> flags;
        [SerializeField] private List<string> shownCardIds;
        [SerializeField] private List<CardCooldownEntry> cooldowns;
        [SerializeField] private string forcedNextCardId;
        [SerializeField] private string currentCardId;
        [SerializeField] private bool isRunActive;

        public RunState()
        {
            saveVersion = GameConstants.CurrentSaveVersion;
            turn = GameConstants.FirstTurn;
            stats = StatValues.CreateInitial();
            flags = new List<string>();
            shownCardIds = new List<string>();
            cooldowns = new List<CardCooldownEntry>();
            forcedNextCardId = string.Empty;
            currentCardId = string.Empty;
        }

        /// <summary>Starts a fresh run driven by the given seed.</summary>
        public static RunState CreateNew(int runSeed)
        {
            RunState state = new RunState();
            state.seed = runSeed;
            state.isRunActive = true;
            return state;
        }

        public int SaveVersion => saveVersion;

        public int Turn => turn;

        public int Seed => seed;

        public StatValues Stats => stats;

        public IReadOnlyList<string> Flags => flags;

        public IReadOnlyList<string> ShownCardIds => shownCardIds;

        public IReadOnlyList<CardCooldownEntry> Cooldowns => cooldowns;

        public string ForcedNextCardId => forcedNextCardId ?? string.Empty;

        public string CurrentCardId => currentCardId ?? string.Empty;

        public bool IsRunActive => isRunActive;

        public bool HasForcedNextCard => !string.IsNullOrEmpty(forcedNextCardId);

        public void SetStats(StatValues value)
        {
            stats = value;
        }

        /// <summary>Adds a flag. Returns false when the run already carried it.</summary>
        public bool AddFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || flags.Contains(flag))
            {
                return false;
            }

            flags.Add(flag);
            return true;
        }

        /// <summary>Removes a flag. Returns false when the run did not carry it.</summary>
        public bool RemoveFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && flags.Remove(flag);
        }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
        }

        public void AdvanceTurn()
        {
            turn++;
        }

        /// <summary>Records that a card has been shown. Repeat calls do not duplicate the entry.</summary>
        public void MarkCardShown(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || shownCardIds.Contains(cardId))
            {
                return;
            }

            shownCardIds.Add(cardId);
        }

        public bool HasShownCard(string cardId)
        {
            return !string.IsNullOrEmpty(cardId) && shownCardIds.Contains(cardId);
        }

        /// <summary>
        /// Blocks the card until <paramref name="availableOnTurn"/>. An existing cooldown is only
        /// ever extended, so a shorter cooldown cannot cut a longer one short.
        /// </summary>
        public void SetCooldown(string cardId, int availableOnTurn)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return;
            }

            CardCooldownEntry existing = FindCooldown(cardId);
            if (existing != null)
            {
                existing.ExtendTo(availableOnTurn);
                return;
            }

            cooldowns.Add(new CardCooldownEntry(cardId, availableOnTurn));
        }

        public bool TryGetCooldownTurn(string cardId, out int availableOnTurn)
        {
            CardCooldownEntry entry = FindCooldown(cardId);
            if (entry == null)
            {
                availableOnTurn = 0;
                return false;
            }

            availableOnTurn = entry.AvailableOnTurn;
            return true;
        }

        public bool IsOnCooldown(string cardId)
        {
            return TryGetCooldownTurn(cardId, out int availableOnTurn) && turn < availableOnTurn;
        }

        public void SetForcedNextCardId(string cardId)
        {
            forcedNextCardId = cardId ?? string.Empty;
        }

        public void ClearForcedNextCardId()
        {
            forcedNextCardId = string.Empty;
        }

        public void SetCurrentCardId(string cardId)
        {
            currentCardId = cardId ?? string.Empty;
        }

        public void EndRun()
        {
            isRunActive = false;
            currentCardId = string.Empty;
            forcedNextCardId = string.Empty;
        }

        /// <summary>
        /// Repairs a state rebuilt by JSON deserialisation, which writes the backing fields
        /// directly and so bypasses every guard the normal API provides.
        /// Returns whether anything actually had to be repaired.
        /// </summary>
        /// <remarks>
        /// JsonUtility does run the parameterless constructor first, so a merely truncated save
        /// arrives with valid collections. The null guards cover a file that names a field
        /// explicitly as null; the rest covers a hand-edited or corrupted one.
        ///
        /// The return value is what lets the save layer tell an intact save from a repaired one,
        /// so damage gets reported rather than silently absorbed.
        /// </remarks>
        public bool SanitizeAfterLoad()
        {
            bool repaired = false;

            if (flags == null)
            {
                flags = new List<string>();
                repaired = true;
            }

            if (shownCardIds == null)
            {
                shownCardIds = new List<string>();
                repaired = true;
            }

            if (cooldowns == null)
            {
                cooldowns = new List<CardCooldownEntry>();
                repaired = true;
            }

            if (forcedNextCardId == null)
            {
                forcedNextCardId = string.Empty;
                repaired = true;
            }

            if (currentCardId == null)
            {
                currentCardId = string.Empty;
                repaired = true;
            }

            // A negative turn would invert every cooldown comparison, quietly making blocked cards
            // drawable for the rest of the run.
            if (turn < GameConstants.FirstTurn)
            {
                turn = GameConstants.FirstTurn;
                repaired = true;
            }

            repaired |= CompactIds(flags);
            repaired |= CompactIds(shownCardIds);
            repaired |= CompactCooldowns();

            StatValues clamped = stats.Sanitized();
            if (clamped.Authority != stats.Authority
                || clamped.People != stats.People
                || clamped.Security != stats.Security
                || clamped.Wealth != stats.Wealth)
            {
                repaired = true;
            }

            stats = clamped;

            return repaired;
        }

        /// <summary>
        /// Drops blank and duplicate IDs in place, keeping the first occurrence of each.
        /// </summary>
        private static bool CompactIds(List<string> ids)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            int write = 0;

            for (int read = 0; read < ids.Count; read++)
            {
                string id = ids[read];

                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    continue;
                }

                ids[write] = id;
                write++;
            }

            if (write == ids.Count)
            {
                return false;
            }

            ids.RemoveRange(write, ids.Count - write);
            return true;
        }

        /// <summary>
        /// Drops null and ID-less cooldown entries, and collapses duplicates onto the later turn so
        /// a repeated entry cannot shorten a cooldown.
        /// </summary>
        private bool CompactCooldowns()
        {
            Dictionary<string, CardCooldownEntry> kept =
                new Dictionary<string, CardCooldownEntry>(StringComparer.Ordinal);

            bool repaired = false;
            int write = 0;

            for (int read = 0; read < cooldowns.Count; read++)
            {
                CardCooldownEntry entry = cooldowns[read];

                if (entry == null || string.IsNullOrWhiteSpace(entry.CardId))
                {
                    repaired = true;
                    continue;
                }

                if (kept.TryGetValue(entry.CardId, out CardCooldownEntry existing))
                {
                    existing.ExtendTo(entry.AvailableOnTurn);
                    repaired = true;
                    continue;
                }

                kept.Add(entry.CardId, entry);
                cooldowns[write] = entry;
                write++;
            }

            if (write != cooldowns.Count)
            {
                cooldowns.RemoveRange(write, cooldowns.Count - write);
            }

            return repaired;
        }

        private CardCooldownEntry FindCooldown(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return null;
            }

            // Indexed loop rather than LINQ: this runs on every card eligibility check.
            for (int i = 0; i < cooldowns.Count; i++)
            {
                if (cooldowns[i].CardId == cardId)
                {
                    return cooldowns[i];
                }
            }

            return null;
        }
    }
}
