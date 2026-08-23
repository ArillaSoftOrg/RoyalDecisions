using System;
using System.Collections.Generic;
using System.Reflection;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Builds in-memory content assets for tests without touching the AssetDatabase.
    /// </summary>
    /// <remarks>
    /// Every instance is tracked so <see cref="DestroyAll"/> can release it in a test teardown;
    /// ScriptableObjects left alive between EditMode tests leak into later fixtures.
    /// </remarks>
    public static class CardTestFactory
    {
        // Qualified: with System in scope, a bare "Object" is ambiguous with System.Object.
        private static readonly List<UnityEngine.Object> Created = new List<UnityEngine.Object>();

        public static ChoiceDefinition Choice(
            string previewText = "Choice",
            int authority = 0,
            int people = 0,
            int security = 0,
            int wealth = 0,
            string[] flagsToAdd = null,
            string[] flagsToRemove = null,
            string forcedNextCardId = "",
            string audioEventId = "",
            CounterDelta[] counterDeltas = null,
            ConditionalChoiceEffect conditionalEffect = null,
            RandomStatOutcome randomOutcome = null,
            CardConditions availability = null)
        {
            return new ChoiceDefinition(
                previewText,
                new StatDeltas(authority, people, security, wealth),
                flagsToAdd,
                flagsToRemove,
                forcedNextCardId,
                audioEventId,
                counterDeltas,
                conditionalEffect,
                randomOutcome,
                availability);
        }

        public static CardDefinition Card(
            string id = "card_test",
            string speaker = "Speaker",
            string bodyText = "Body",
            ChoiceDefinition left = null,
            ChoiceDefinition right = null,
            CardConditions conditions = null,
            int selectionWeight = CardDefinition.DefaultSelectionWeight,
            bool oncePerRun = false,
            int cooldownTurns = 0,
            string forcedNextCardId = "",
            bool forcedChainOnly = false,
            CardVariant[] variants = null)
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.name = id;
            card.SetAuthoringData(
                id,
                speaker,
                bodyText,
                left ?? Choice("Left"),
                right ?? Choice("Right"),
                conditions,
                selectionWeight,
                oncePerRun,
                cooldownTurns,
                forcedNextCardId,
                isForcedChainOnly: forcedChainOnly,
                cardVariants: variants);

            Created.Add(card);
            return card;
        }

        public static EndingDefinition Ending(
            string id = "ending_test",
            string title = "Title",
            string bodyText = "Body",
            StatType triggerStat = StatType.Authority,
            StatBoundary boundary = StatBoundary.Min,
            int priority = 0)
        {
            EndingDefinition ending = ScriptableObject.CreateInstance<EndingDefinition>();
            ending.name = id;
            ending.SetAuthoringData(id, title, bodyText, triggerStat, boundary, priority);

            Created.Add(ending);
            return ending;
        }

        public static CardConditions Conditions(
            string[] requiredFlags = null,
            string[] forbiddenFlags = null,
            StatRange[] statRanges = null)
        {
            return new CardConditions(requiredFlags, forbiddenFlags, statRanges);
        }

        /// <summary>
        /// A card whose left and right choices are genuinely null.
        /// </summary>
        /// <remarks>
        /// <c>SetAuthoringData</c> substitutes empty choices for nulls, so this state cannot be
        /// authored — it only arises from a malformed asset, such as a hand-edited file or a bad
        /// merge. Reflection reproduces it so the validation rule that guards against it can be
        /// tested at all.
        /// </remarks>
        public static CardDefinition CardWithNullChoices(string id)
        {
            CardDefinition card = Card(id: id);

            SetPrivateField(card, "leftChoice", null);
            SetPrivateField(card, "rightChoice", null);

            return card;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        /// <summary>
        /// The full set of eight boundary endings — minimum and maximum for each statistic — named
        /// <c>ending_{stat}_{boundary}</c> in lower case.
        /// </summary>
        public static List<EndingDefinition> AllBoundaryEndings()
        {
            List<EndingDefinition> endings = new List<EndingDefinition>(8);

            foreach (StatType stat in (StatType[])Enum.GetValues(typeof(StatType)))
            {
                foreach (StatBoundary boundary in (StatBoundary[])Enum.GetValues(typeof(StatBoundary)))
                {
                    endings.Add(Ending(
                        id: EndingId(stat, boundary),
                        title: stat + " " + boundary,
                        triggerStat: stat,
                        boundary: boundary));
                }
            }

            return endings;
        }

        public static string EndingId(StatType stat, StatBoundary boundary)
        {
            return string.Format(
                "ending_{0}_{1}",
                stat.ToString().ToLowerInvariant(),
                boundary.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Builds one unconditional card per (id, weight) pair, in the order given.
        /// </summary>
        public static List<CardDefinition> WeightedCards(params (string Id, int Weight)[] entries)
        {
            List<CardDefinition> cards = new List<CardDefinition>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                cards.Add(Card(id: entries[i].Id, selectionWeight: entries[i].Weight));
            }

            return cards;
        }

        /// <summary>
        /// Registers an asset this factory did not create, so it is released by
        /// <see cref="DestroyAll"/> along with everything else.
        /// </summary>
        public static T Track<T>(T asset) where T : UnityEngine.Object
        {
            if (asset != null)
            {
                Created.Add(asset);
            }

            return asset;
        }

        /// <summary>Releases every asset this factory created. Call from a test teardown.</summary>
        public static void DestroyAll()
        {
            for (int i = 0; i < Created.Count; i++)
            {
                if (Created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(Created[i]);
                }
            }

            Created.Clear();
        }
    }
}
