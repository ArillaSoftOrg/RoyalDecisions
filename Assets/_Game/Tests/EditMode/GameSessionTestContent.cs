using System.Collections.Generic;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Builds catalogues for session tests, in memory and without the AssetDatabase.
    /// </summary>
    public static class GameSessionTestContent
    {
        public const string OpeningCardId = "card_01_opening";
        public const string ChainStartId = "card_02_chain_start";
        public const string ChainEndId = "card_03_chain_end";
        public const string PlainCardId = "card_04_plain";

        /// <summary>
        /// A catalogue with an opening card, a two-card chain, and a plain card, plus all eight
        /// boundary endings.
        /// </summary>
        public static ContentCatalogue Standard()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(
                    id: OpeningCardId,
                    left: CardTestFactory.Choice("Left", authority: 5, audioEventId: "sfx_open"),
                    right: CardTestFactory.Choice("Right", people: 5)),

                CardTestFactory.Card(
                    id: ChainStartId,
                    left: CardTestFactory.Choice("Left"),
                    right: CardTestFactory.Choice("Right"),
                    forcedNextCardId: ChainEndId),

                CardTestFactory.Card(id: ChainEndId),
                CardTestFactory.Card(id: PlainCardId)
            };

            return Build(cards, CardTestFactory.AllBoundaryEndings(), OpeningCardId);
        }

        /// <summary>A catalogue whose opening card forces a target that does not exist.</summary>
        public static ContentCatalogue WithMissingForcedTarget()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(
                    id: OpeningCardId,
                    forcedNextCardId: "card_does_not_exist"),
                CardTestFactory.Card(id: PlainCardId)
            };

            return Build(cards, CardTestFactory.AllBoundaryEndings(), OpeningCardId);
        }

        /// <summary>A catalogue holding exactly one card, which is once-per-run.</summary>
        public static ContentCatalogue WithSingleOnceCard()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(id: OpeningCardId, oncePerRun: true)
            };

            return Build(cards, CardTestFactory.AllBoundaryEndings(), OpeningCardId);
        }

        /// <summary>A catalogue whose only card drives Authority straight to its minimum.</summary>
        public static ContentCatalogue WithInstantLoss()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(
                    id: OpeningCardId,
                    left: CardTestFactory.Choice("Doom", authority: -999),
                    right: CardTestFactory.Choice("Doom", authority: -999))
            };

            return Build(cards, CardTestFactory.AllBoundaryEndings(), OpeningCardId);
        }

        /// <summary>Instant loss, but with no ending content to describe it.</summary>
        public static ContentCatalogue WithInstantLossAndNoEndings()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(
                    id: OpeningCardId,
                    left: CardTestFactory.Choice("Doom", authority: -999),
                    right: CardTestFactory.Choice("Doom", authority: -999))
            };

            return Build(cards, new List<EndingDefinition>(), OpeningCardId);
        }

        public static ContentCatalogue Empty()
        {
            return Build(new List<CardDefinition>(), CardTestFactory.AllBoundaryEndings(), string.Empty);
        }

        /// <summary>A catalogue whose opening card's left choice is never available.</summary>
        public static ContentCatalogue WithUnavailableLeftChoice()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(
                    id: OpeningCardId,
                    left: CardTestFactory.Choice("Locked", authority: 5,
                        availability: CardTestFactory.Conditions(requiredFlags: new[] { "never_set" })),
                    right: CardTestFactory.Choice("Right", people: 5))
            };

            return Build(cards, CardTestFactory.AllBoundaryEndings(), OpeningCardId);
        }

        public static ContentCatalogue WithUnknownOpeningCard()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(id: PlainCardId)
            };

            return Build(cards, CardTestFactory.AllBoundaryEndings(), "card_nowhere");
        }

        public static ContentCatalogue Build(
            List<CardDefinition> cards,
            List<EndingDefinition> endings,
            string openingCardId)
        {
            ContentCatalogue catalogue = ScriptableObject.CreateInstance<ContentCatalogue>();
            catalogue.SetAuthoringData(cards.ToArray(), endings.ToArray(), openingCardId);

            CardTestFactory.Track(catalogue);
            return catalogue;
        }
    }
}
