using System;
using System.Collections.Generic;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Builds the "Sığınak: Saltanat Günlükleri" story content in memory, with no asset I/O.
    /// </summary>
    /// <remarks>
    /// Transcribes the full 250-card narrative specification (root-level <c>Hıkaye.md</c>) into the
    /// existing card data model, using one general-purpose authoring helper per card rather than a
    /// bespoke type per card (CLAUDE.md §4/§7 forbid the latter). Split into one partial-class file
    /// per chapter (<c>StoryChapter1Cards.cs</c> .. <c>StoryChapter6Cards.cs</c>) purely to keep any
    /// one file readable — all six contribute to this same type and share the helpers below.
    ///
    /// <para>
    /// Stat mapping. The specification's four resources are the same shape as the engine's four
    /// core statistics, relabelled for this setting:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="StatType.Wealth"/> = Erzak (food/supplies)</item>
    /// <item><see cref="StatType.Security"/> = Barınak (shelter/defence)</item>
    /// <item><see cref="StatType.People"/> = Toplum Sağlığı (community health)</item>
    /// <item><see cref="StatType.Authority"/> = Toplum Morali (community morale / the leader's standing)</item>
    /// </list>
    /// <para>
    /// Leader health (👑) is not one of the four — it is a separate, narrower measure that resets
    /// on succession. See <see cref="LeaderHealthBounds"/> and <c>RunState.LeaderHealth</c>.
    /// </para>
    /// <para>
    /// Every card in this library is authored with <c>isForcedChainOnly: true</c>: the whole story
    /// is a hand-authored forced-next graph (<see cref="ChoiceDefinition.ForcedNextCardId"/>), and
    /// normal (non-forced) weighted selection must never surface a card from a branch the current
    /// run did not take. See <see cref="CardDefinition.ForcedChainOnly"/>.
    /// </para>
    /// <para>
    /// Where the specification shows a card's text or choices changing based on an earlier flag
    /// (its "*Eğer X ise:* ... *değilse:*" pattern), the "otherwise" branch is authored as the
    /// card's own base fields and the flagged branch as a single <see cref="CardVariant"/> — see
    /// each such card's comment for which flag selects it.
    /// </para>
    /// </remarks>
    public static partial class StoryContentLibrary
    {
        public const string OpeningCardId = "story_k001";

        // --- Story counters (see RunState.AddToCounter/GetCounter) --------------
        public const string CounterVertakIpucu = "vertak_ipucu";
        public const string CounterPharmaArastirma = "pharma_arastirma";

        /// <summary>All 250 specification cards plus the terminal card after K250, ID-sorted.</summary>
        public static List<CardDefinition> CreateCards()
        {
            List<CardDefinition> cards = new List<CardDefinition>(251);
            cards.AddRange(CreateChapter1Cards());
            cards.AddRange(CreateChapter2Cards());
            cards.AddRange(CreateChapter3Cards());
            cards.AddRange(CreateChapter4Cards());
            cards.AddRange(CreateChapter5Cards());
            cards.AddRange(CreateChapter6Cards());

            cards.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            return cards;
        }

        /// <summary>
        /// One ending per statistic per boundary, themed to the shelter setting. Reachable only if
        /// a run's stat drift (accumulated across up to 250 authored decisions, plus whatever a
        /// player's own choices add) happens to reach 0 or 100 — required regardless, since
        /// <see cref="RoyalDecisions.Domain.GameOverEvaluator"/> checks every stat after every
        /// decision in every catalogue.
        /// </summary>
        public static List<EndingDefinition> CreateEndings()
        {
            return new List<EndingDefinition>(8)
            {
                Ending(StatType.Wealth, StatBoundary.Min, "Son Konserve",
                    "Ambarlar boşaldı; tek bir kutu bile kalmadı. Sığınak sessizliğe gömüldü."),
                Ending(StatType.Wealth, StatBoundary.Max, "Taşan Depo",
                    "Depolar öyle doldu ki artık kimse ne sakladığını hatırlamıyor."),
                Ending(StatType.Security, StatBoundary.Min, "Düşen Duvarlar",
                    "Barınağın son duvarı da çöktü; dışarısı artık içeri kadar geldi."),
                Ending(StatType.Security, StatBoundary.Max, "Mühürlenmiş Sığınak",
                    "Her açıklık kapatıldı, her giriş mühürlendi. İçeride kalanlar da bir daha çıkmadı."),
                Ending(StatType.People, StatBoundary.Min, "Sessiz Koğuşlar",
                    "Revirde artık kimse yatmıyor; bakacak kimse de kalmadı."),
                Ending(StatType.People, StatBoundary.Max, "Aşırı Tedbir",
                    "Herkes o kadar sıkı korundu ki sığınak bir hastaneye, sonra bir hapishaneye döndü."),
                Ending(StatType.Authority, StatBoundary.Min, "Unutulan Lider",
                    "Kararlar artık kimseye sorulmuyor; sözünüz duvarların ötesine geçmiyor."),
                Ending(StatType.Authority, StatBoundary.Max, "Sorgulanmayan Söz",
                    "Kimse bir daha itiraz etmedi. Kimse bir daha da gerçeği söylemedi.")
            };
        }

        public static string EndingId(StatType stat, StatBoundary boundary)
        {
            return string.Format(
                "story_ending_{0}_{1}",
                stat.ToString().ToLowerInvariant(),
                boundary.ToString().ToLowerInvariant());
        }

        // --- Shared construction helpers, used by every chapter file ------------------

        /// <summary>Card IDs as "story_k001".."story_k250", matching the specification's Kn.</summary>
        internal static string K(int n)
        {
            return string.Format("story_k{0:D3}", n);
        }

        internal static CardDefinition Card(
            int number,
            string speaker,
            string bodyText,
            ChoiceDefinition left,
            ChoiceDefinition right,
            CardVariant[] variants = null)
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            string id = K(number);
            card.name = id;
            card.SetAuthoringData(
                id, speaker, bodyText, left, right,
                isOncePerRun: true, isForcedChainOnly: true, cardVariants: variants);
            return card;
        }

        /// <summary>
        /// Builds one choice. <paramref name="forcedNext"/> names a card by number (matching the
        /// specification's "→Kn"); use 0 for a choice authored with no forced-next at all (only the
        /// closing card of the whole story does this).
        /// </summary>
        internal static ChoiceDefinition Choice(
            string previewText,
            int authority = 0,
            int people = 0,
            int security = 0,
            int wealth = 0,
            string[] flagsAdd = null,
            string[] flagsRemove = null,
            int forcedNext = 0,
            CounterDelta[] counterDeltas = null,
            ConditionalChoiceEffect conditionalEffect = null,
            RandomStatOutcome randomOutcome = null,
            CardConditions availability = null)
        {
            string forcedNextCardId = forcedNext > 0 ? K(forcedNext) : string.Empty;

            return new ChoiceDefinition(
                previewText,
                new StatDeltas(authority, people, security, wealth),
                flagsAdd,
                flagsRemove,
                forcedNextCardId,
                audioEventId: "",
                counterDeltas: counterDeltas,
                conditionalEffect: conditionalEffect,
                randomOutcome: randomOutcome,
                availability: availability);
        }

        /// <summary>A leader-risk choice: fatal below <see cref="LeaderHealthBounds.CriticalThreshold"/>.</summary>
        internal static ConditionalChoiceEffect LeaderRisk(
            int leaderHealthDeltaWhenFalse, StatDeltas deltasWhenFalse = default)
        {
            return new ConditionalChoiceEffect(
                new NumericCondition(
                    NumericSource.LeaderHealth, NumericComparison.LessThan,
                    LeaderHealthBounds.CriticalThreshold),
                deltasWhenFalse: deltasWhenFalse,
                leaderHealthDeltaWhenFalse: leaderHealthDeltaWhenFalse,
                triggersSuccessionWhenTrue: true);
        }

        /// <summary>An unconditional (or otherwise-gated) reign-ending effect for a "Yıkıcı" choice.</summary>
        internal static ConditionalChoiceEffect Reign(NumericCondition condition, StatType resetStat)
        {
            return new ConditionalChoiceEffect(condition, triggersSuccessionWhenTrue: true,
                successionResetStat: resetStat);
        }

        /// <summary>A stat-threshold "Yıkıcı" choice: destructive only once that stat is already critical.</summary>
        internal static ConditionalChoiceEffect ReignIfCritical(
            StatType stat, int atOrBelow, StatDeltas deltasWhenSafe, StatType resetStat)
        {
            return new ConditionalChoiceEffect(
                new NumericCondition(NumericSource.Stat, NumericComparison.LessOrEqual, atOrBelow, stat: stat),
                deltasWhenFalse: deltasWhenSafe,
                triggersSuccessionWhenTrue: true,
                successionResetStat: resetStat);
        }

        /// <summary>A plain, unconditional leader-health delta — no risk, no succession.</summary>
        internal static ConditionalChoiceEffect AlwaysLeaderHealth(int delta)
        {
            return new ConditionalChoiceEffect(NumericCondition.Always(), leaderHealthDeltaWhenTrue: delta);
        }

        internal static string[] Flags(params string[] flags)
        {
            return flags;
        }

        /// <summary>Required-flag condition, for a <see cref="CardVariant"/> or choice availability.</summary>
        internal static CardConditions RequiresFlag(string flag)
        {
            return new CardConditions(new[] { flag }, null, null);
        }

        internal static CardConditions RequiresCounterAtLeast(string counterId, int minimum)
        {
            return new CardConditions(null, null, null, new[]
            {
                new NumericCondition(NumericSource.Counter, NumericComparison.GreaterOrEqual, minimum,
                    counterId: counterId)
            });
        }

        /// <summary>A variant selected when <paramref name="flag"/> is present, overriding body text and both choices.</summary>
        internal static CardVariant VariantIfFlag(
            string flag, string bodyText, ChoiceDefinition left, ChoiceDefinition right, string speaker = null)
        {
            return new CardVariant(RequiresFlag(flag), speaker, bodyText, left, right);
        }

        internal static CounterDelta[] Counter(string counterId, int delta)
        {
            return new[] { new CounterDelta(counterId, delta) };
        }

        private static EndingDefinition Ending(StatType stat, StatBoundary boundary, string title, string bodyText)
        {
            string id = EndingId(stat, boundary);

            EndingDefinition ending = ScriptableObject.CreateInstance<EndingDefinition>();
            ending.name = id;
            ending.SetAuthoringData(id, title, bodyText, stat, boundary);
            return ending;
        }
    }
}
