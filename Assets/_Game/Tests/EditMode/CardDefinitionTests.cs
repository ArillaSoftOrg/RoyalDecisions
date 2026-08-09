using NUnit.Framework;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class CardDefinitionTests
    {
        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        [Test]
        public void FreshInstance_HasUsableChoicesAndConditions()
        {
            // A card created without authoring must not hand out nulls, or every consumer would
            // need its own null guard.
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            try
            {
                Assert.That(card.LeftChoice, Is.Not.Null);
                Assert.That(card.RightChoice, Is.Not.Null);
                Assert.That(card.Conditions, Is.Not.Null);
                Assert.That(card.Conditions.IsEmpty, Is.True);
                Assert.That(card.Id, Is.Empty);
                Assert.That(card.HasForcedNextCard, Is.False);
                Assert.That(card.Portrait, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(card);
            }
        }

        [Test]
        public void SetAuthoringData_PopulatesEveryField()
        {
            CardDefinition card = CardTestFactory.Card(
                id: "card_opening",
                speaker: "The Chancellor",
                bodyText: "The treasury is empty, your Majesty.",
                left: CardTestFactory.Choice("Refuse", authority: 5, wealth: -10),
                right: CardTestFactory.Choice("Agree", people: 8),
                oncePerRun: true,
                cooldownTurns: 4,
                forcedNextCardId: "card_opening_followup");

            Assert.That(card.Id, Is.EqualTo("card_opening"));
            Assert.That(card.Speaker, Is.EqualTo("The Chancellor"));
            Assert.That(card.BodyText, Is.EqualTo("The treasury is empty, your Majesty."));
            Assert.That(card.LeftChoice.PreviewText, Is.EqualTo("Refuse"));
            Assert.That(card.LeftChoice.Deltas.Authority, Is.EqualTo(5));
            Assert.That(card.LeftChoice.Deltas.Wealth, Is.EqualTo(-10));
            Assert.That(card.RightChoice.Deltas.People, Is.EqualTo(8));
            Assert.That(card.OncePerRun, Is.True);
            Assert.That(card.CooldownTurns, Is.EqualTo(4));
            Assert.That(card.HasCooldown, Is.True);
            Assert.That(card.ForcedNextCardId, Is.EqualTo("card_opening_followup"));
            Assert.That(card.HasForcedNextCard, Is.True);
        }

        [Test]
        public void SelectionWeight_FallsBackToOneSoAnUnweightedCardStaysDrawable()
        {
            Assert.That(CardTestFactory.Card(selectionWeight: 0).SelectionWeight,
                Is.EqualTo(CardDefinition.DefaultSelectionWeight));
            Assert.That(CardTestFactory.Card(selectionWeight: -5).SelectionWeight,
                Is.EqualTo(CardDefinition.DefaultSelectionWeight));
            Assert.That(CardTestFactory.Card(selectionWeight: 7).SelectionWeight,
                Is.EqualTo(7));
        }

        [Test]
        public void CooldownTurns_NeverReportsANegativeCooldown()
        {
            CardDefinition card = CardTestFactory.Card(cooldownTurns: -3);

            Assert.That(card.CooldownTurns, Is.Zero);
            Assert.That(card.HasCooldown, Is.False);
        }

        [Test]
        public void Conditions_StoreFlagAndStatRequirements()
        {
            CardDefinition card = CardTestFactory.Card(
                conditions: CardTestFactory.Conditions(
                    requiredFlags: new[] { "war_declared" },
                    forbiddenFlags: new[] { "treaty_signed" },
                    statRanges: new[] { new StatRange(StatType.People, StatBounds.Min, 25) }));

            Assert.That(card.Conditions.IsEmpty, Is.False);
            Assert.That(card.Conditions.RequiredFlags, Is.EquivalentTo(new[] { "war_declared" }));
            Assert.That(card.Conditions.ForbiddenFlags, Is.EquivalentTo(new[] { "treaty_signed" }));
            Assert.That(card.Conditions.StatRanges.Count, Is.EqualTo(1));
            Assert.That(card.Conditions.StatRanges[0].Stat, Is.EqualTo(StatType.People));
            Assert.That(card.Conditions.StatRanges[0].Max, Is.EqualTo(25));
        }

        [Test]
        public void NewStatRange_DefaultsToUnrestrictedRatherThanZeroToZero()
        {
            // An author adding a range in the Inspector gets "no restriction", not "must be 0".
            StatRange range = new StatRange();

            Assert.That(range.Min, Is.EqualTo(StatBounds.Min));
            Assert.That(range.Max, Is.EqualTo(StatBounds.Max));
            Assert.That(range.Contains(StatBounds.Initial), Is.True);
        }

        [Test]
        public void StatRange_ContainsIsInclusiveAtBothEnds()
        {
            StatRange range = new StatRange(StatType.Wealth, 10, 25);

            Assert.That(range.Contains(9), Is.False);
            Assert.That(range.Contains(10), Is.True);
            Assert.That(range.Contains(25), Is.True);
            Assert.That(range.Contains(26), Is.False);
        }

        [Test]
        public void Choice_ExposesForcedCardAndAudioOnlyWhenAuthored()
        {
            ChoiceDefinition bare = CardTestFactory.Choice();
            Assert.That(bare.HasForcedNextCard, Is.False);
            Assert.That(bare.HasAudioEvent, Is.False);
            Assert.That(bare.FlagsToAdd, Is.Empty);
            Assert.That(bare.FlagsToRemove, Is.Empty);

            ChoiceDefinition full = CardTestFactory.Choice(
                flagsToAdd: new[] { "war_declared" },
                flagsToRemove: new[] { "peace" },
                forcedNextCardId: "card_next",
                audioEventId: "sfx_seal");

            Assert.That(full.HasForcedNextCard, Is.True);
            Assert.That(full.HasAudioEvent, Is.True);
            Assert.That(full.FlagsToAdd, Is.EquivalentTo(new[] { "war_declared" }));
            Assert.That(full.FlagsToRemove, Is.EquivalentTo(new[] { "peace" }));
        }

        [Test]
        public void EndingDefinition_StoresItsTriggerBoundary()
        {
            EndingDefinition ending = CardTestFactory.Ending(
                id: "ending_people_min",
                title: "The People Rise",
                triggerStat: StatType.People,
                boundary: StatBoundary.Min,
                priority: 2);

            Assert.That(ending.Id, Is.EqualTo("ending_people_min"));
            Assert.That(ending.Title, Is.EqualTo("The People Rise"));
            Assert.That(ending.TriggerStat, Is.EqualTo(StatType.People));
            Assert.That(ending.Boundary, Is.EqualTo(StatBoundary.Min));
            Assert.That(ending.Priority, Is.EqualTo(2));
            Assert.That(ending.Image, Is.Null, "missing art must not be an error");
        }
    }
}
