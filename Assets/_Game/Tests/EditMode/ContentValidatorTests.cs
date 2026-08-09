using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class ContentValidatorTests
    {
        private ContentValidator validator;
        private List<EndingDefinition> allEndings;

        [SetUp]
        public void SetUp()
        {
            validator = new ContentValidator();
            allEndings = CardTestFactory.AllBoundaryEndings();
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        /// <summary>Cards sorted ordinally, so the ordering rule never fires by accident.</summary>
        private static List<CardDefinition> Sorted(params CardDefinition[] cards)
        {
            List<CardDefinition> list = new List<CardDefinition>(cards);
            list.Sort((left, right) => StringComparer.Ordinal.Compare(
                left == null ? string.Empty : left.Id,
                right == null ? string.Empty : right.Id));
            return list;
        }

        private ContentValidationReport Validate(
            List<CardDefinition> cards,
            string openingCardId = null)
        {
            return validator.Validate(cards, allEndings, openingCardId);
        }

        // --- Valid content ---------------------------------------------------

        [Test]
        public void ValidContent_ProducesNoIssues()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a"),
                CardTestFactory.Card(id: "card_b")));

            Assert.That(report.HasErrors, Is.False, report.ToString());
            Assert.That(report.HasWarnings, Is.False, report.ToString());
            Assert.That(report.IsValid, Is.True);
        }

        // --- Structural errors -----------------------------------------------

        [Test]
        public void NullCardEntry_IsAnError()
        {
            List<CardDefinition> cards = new List<CardDefinition>
            {
                CardTestFactory.Card(id: "card_a"),
                null
            };

            Assert.That(Validate(cards).Contains(ContentIssueCode.NullCardEntry), Is.True);
        }

        [Test]
        public void NullEndingEntry_IsAnError()
        {
            allEndings.Add(null);

            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(id: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.NullEndingEntry), Is.True);
        }

        [Test]
        public void EmptyCardId_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: string.Empty)));

            Assert.That(report.Contains(ContentIssueCode.EmptyCardId), Is.True);
        }

        [Test]
        public void WhitespaceCardId_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(id: "   ")));

            Assert.That(report.Contains(ContentIssueCode.EmptyCardId), Is.True);
        }

        [Test]
        public void EmptyEndingId_IsAnError()
        {
            allEndings.Add(CardTestFactory.Ending(
                id: string.Empty, triggerStat: StatType.People, boundary: StatBoundary.Min));

            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(id: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.EmptyEndingId), Is.True);
        }

        [Test]
        public void DuplicateCardId_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_same"),
                CardTestFactory.Card(id: "card_same")));

            Assert.That(report.Contains(ContentIssueCode.DuplicateCardId), Is.True);
        }

        [Test]
        public void DuplicateEndingId_IsAnError()
        {
            allEndings.Add(CardTestFactory.Ending(
                id: CardTestFactory.EndingId(StatType.People, StatBoundary.Min),
                triggerStat: StatType.People,
                boundary: StatBoundary.Min));

            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(id: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.DuplicateEndingId), Is.True);
        }

        [Test]
        public void MissingChoice_IsAnError()
        {
            // SetAuthoringData substitutes empty choices for nulls, so a null choice can only
            // reach the game from a malformed asset — a hand-edited file or a bad merge. The
            // factory reproduces that state directly.
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.CardWithNullChoices("card_broken")));

            Assert.That(report.Contains(ContentIssueCode.MissingChoice), Is.True);
        }

        [Test]
        public void MissingChoice_IsReportedForEachAbsentSide()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.CardWithNullChoices("card_broken")));

            Assert.That(report.CountOf(ContentIssueCode.MissingChoice), Is.EqualTo(2));
        }

        // --- Stat ranges --------------------------------------------------------

        [Test]
        public void InvertedStatRange_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                conditions: CardTestFactory.Conditions(
                    statRanges: new[] { new StatRange(StatType.People, 60, 20) }))));

            Assert.That(report.Contains(ContentIssueCode.InvalidStatRange), Is.True);
        }

        [Test]
        public void StatRangeBelowTheLegalMinimum_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                conditions: CardTestFactory.Conditions(
                    statRanges: new[] { new StatRange(StatType.People, -5, 20) }))));

            Assert.That(report.Contains(ContentIssueCode.InvalidStatRange), Is.True);
        }

        [Test]
        public void StatRangeAboveTheLegalMaximum_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                conditions: CardTestFactory.Conditions(
                    statRanges: new[] { new StatRange(StatType.People, 20, 500) }))));

            Assert.That(report.Contains(ContentIssueCode.InvalidStatRange), Is.True);
        }

        [Test]
        public void StatRangeSpanningTheFullLegalBand_IsAccepted()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                conditions: CardTestFactory.Conditions(
                    statRanges: new[] {
                        new StatRange(StatType.People, StatBounds.Min, StatBounds.Max) }))));

            Assert.That(report.Contains(ContentIssueCode.InvalidStatRange), Is.False);
        }

        // --- Forced chains --------------------------------------------------------

        [Test]
        public void CardLevelForcedTargetThatDoesNotExist_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a", forcedNextCardId: "card_missing")));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardTargetMissing), Is.True);
        }

        [Test]
        public void ChoiceLevelForcedTargetThatDoesNotExist_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                left: CardTestFactory.Choice("Left", forcedNextCardId: "card_missing"))));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardTargetMissing), Is.True);
        }

        [Test]
        public void ResolvableForcedChain_IsAccepted()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a", forcedNextCardId: "card_b"),
                CardTestFactory.Card(id: "card_b")));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardTargetMissing), Is.False);
            Assert.That(report.Contains(ContentIssueCode.ForcedCardCycle), Is.False);
        }

        [Test]
        public void CardForcingItself_IsACycle()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a", forcedNextCardId: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardCycle), Is.True);
        }

        [Test]
        public void TwoCardCycle_IsDetected()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a", forcedNextCardId: "card_b"),
                CardTestFactory.Card(id: "card_b", forcedNextCardId: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardCycle), Is.True);
        }

        [Test]
        public void ThreeCardCycle_IsDetected()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a", forcedNextCardId: "card_b"),
                CardTestFactory.Card(id: "card_b", forcedNextCardId: "card_c"),
                CardTestFactory.Card(id: "card_c", forcedNextCardId: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardCycle), Is.True);
        }

        [Test]
        public void CycleThroughAChoiceLevelForcedCard_IsDetected()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(
                    id: "card_a",
                    right: CardTestFactory.Choice("Right", forcedNextCardId: "card_b")),
                CardTestFactory.Card(id: "card_b", forcedNextCardId: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardCycle), Is.True);
        }

        [Test]
        public void AcyclicDiamond_IsNotReportedAsACycle()
        {
            // a -> b and a -> c by side, both converging on d. Two paths reach d, but nothing loops.
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(
                    id: "card_a",
                    left: CardTestFactory.Choice("Left", forcedNextCardId: "card_b"),
                    right: CardTestFactory.Choice("Right", forcedNextCardId: "card_c")),
                CardTestFactory.Card(id: "card_b", forcedNextCardId: "card_d"),
                CardTestFactory.Card(id: "card_c", forcedNextCardId: "card_d"),
                CardTestFactory.Card(id: "card_d")));

            Assert.That(report.Contains(ContentIssueCode.ForcedCardCycle), Is.False,
                "converging paths are not a cycle");
            Assert.That(report.HasErrors, Is.False, report.ToString());
        }

        // --- Endings ----------------------------------------------------------------

        [Test]
        public void MissingEndingBoundary_IsAnError()
        {
            allEndings.RemoveAll(ending =>
                ending != null
                && ending.TriggerStat == StatType.Wealth
                && ending.Boundary == StatBoundary.Max);

            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(id: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.MissingEndingBoundary), Is.True);
        }

        [Test]
        public void EveryMissingBoundary_IsReportedSeparately()
        {
            ContentValidationReport report = validator.Validate(
                Sorted(CardTestFactory.Card(id: "card_a")),
                new List<EndingDefinition>());

            Assert.That(report.CountOf(ContentIssueCode.MissingEndingBoundary), Is.EqualTo(8));
        }

        [Test]
        public void DuplicateEndingForOneBoundary_IsAWarningNotAnError()
        {
            allEndings.Add(CardTestFactory.Ending(
                id: "ending_people_min_alternate",
                triggerStat: StatType.People,
                boundary: StatBoundary.Min));

            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(id: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.DuplicateEndingBoundary), Is.True);
            Assert.That(report.HasErrors, Is.False, report.ToString());
        }

        // --- Opening card ---------------------------------------------------------------

        [Test]
        public void OpeningCardThatDoesNotExist_IsAnError()
        {
            ContentValidationReport report = Validate(
                Sorted(CardTestFactory.Card(id: "card_a")),
                "card_missing");

            Assert.That(report.Contains(ContentIssueCode.OpeningCardMissing), Is.True);
        }

        [Test]
        public void OpeningCardThatResolves_IsAccepted()
        {
            ContentValidationReport report = Validate(
                Sorted(CardTestFactory.Card(id: "card_a")),
                "card_a");

            Assert.That(report.Contains(ContentIssueCode.OpeningCardMissing), Is.False);
        }

        [Test]
        public void NoOpeningCard_IsAccepted()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(id: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.OpeningCardMissing), Is.False);
        }

        // --- Ordering ---------------------------------------------------------------------

        [Test]
        public void CardsOutOfOrdinalOrder_IsAnError()
        {
            List<CardDefinition> unsorted = new List<CardDefinition>
            {
                CardTestFactory.Card(id: "card_b"),
                CardTestFactory.Card(id: "card_a")
            };

            Assert.That(Validate(unsorted).Contains(ContentIssueCode.CardsNotOrdinallySorted),
                Is.True);
        }

        [Test]
        public void OrderingIsJudgedOrdinally_NotByCulture()
        {
            // Ordinal puts uppercase 'C' (0x43) before lowercase 'c' (0x63); a culture-aware
            // comparison generally does the opposite, and would flag this list as unsorted.
            List<CardDefinition> ordinallySorted = new List<CardDefinition>
            {
                CardTestFactory.Card(id: "Card_B"),
                CardTestFactory.Card(id: "card_a")
            };

            Assert.That(StringComparer.Ordinal.Compare("Card_B", "card_a"), Is.LessThan(0),
                "precondition");
            Assert.That(
                Validate(ordinallySorted).Contains(ContentIssueCode.CardsNotOrdinallySorted),
                Is.False);
        }

        // --- Warnings -------------------------------------------------------------------------

        [Test]
        public void RequiredFlagNoChoiceEverAdds_IsAWarning()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                conditions: CardTestFactory.Conditions(requiredFlags: new[] { "never_added" }))));

            Assert.That(report.Contains(ContentIssueCode.UnreachableRequiredFlag), Is.True);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void RequiredFlagSomeChoiceAdds_IsNotWarned()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(
                    id: "card_a",
                    left: CardTestFactory.Choice("Left", flagsToAdd: new[] { "granted" })),
                CardTestFactory.Card(
                    id: "card_b",
                    conditions: CardTestFactory.Conditions(requiredFlags: new[] { "granted" }))));

            Assert.That(report.Contains(ContentIssueCode.UnreachableRequiredFlag), Is.False);
        }

        [Test]
        public void CooldownOnAOncePerRunCard_IsAWarning()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a", oncePerRun: true, cooldownTurns: 4)));

            Assert.That(report.Contains(ContentIssueCode.RedundantCooldown), Is.True);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void EmptyBodyText_IsAWarning()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a", bodyText: string.Empty)));

            Assert.That(report.Contains(ContentIssueCode.EmptyText), Is.True);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void EmptyChoicePreviewText_IsAWarning()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                left: CardTestFactory.Choice(string.Empty))));

            Assert.That(report.Contains(ContentIssueCode.EmptyText), Is.True);
        }

        [Test]
        public void SameFlagRequiredAndForbidden_IsAnError()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                conditions: CardTestFactory.Conditions(
                    requiredFlags: new[] { "gate" },
                    forbiddenFlags: new[] { "gate" }))));

            Assert.That(report.Contains(ContentIssueCode.ConflictingFlags), Is.True);
            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void DisjointRangesForOneStat_AreAnError()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                conditions: CardTestFactory.Conditions(statRanges: new[]
                {
                    new StatRange(StatType.People, 0, 20),
                    new StatRange(StatType.People, 30, 50)
                }))));

            Assert.That(report.Contains(ContentIssueCode.EmptyStatRangeIntersection), Is.True);
        }

        [Test]
        public void DuplicateFlagCondition_IsAWarning()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                left: CardTestFactory.Choice(flagsToAdd: new[] { "gate" }),
                conditions: CardTestFactory.Conditions(
                    requiredFlags: new[] { "gate", "gate" }))));

            Assert.That(report.Contains(ContentIssueCode.DuplicateConditionEntry), Is.True);
        }

        [Test]
        public void ExcessiveDelta_IsAWarning()
        {
            ContentValidationReport report = Validate(Sorted(CardTestFactory.Card(
                id: "card_a",
                left: CardTestFactory.Choice(authority: 26))));

            Assert.That(report.Contains(ContentIssueCode.ExcessiveStatDelta), Is.True);
        }

        [Test]
        public void MissingOptionalArt_IsInformationOnly()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_a")));

            Assert.That(report.Contains(ContentIssueCode.OptionalPortraitMissing), Is.True);
            Assert.That(report.InformationCount, Is.GreaterThan(0));
            Assert.That(report.HasErrors, Is.False);
        }

        // --- Culture independence ---------------------------------------------------------------

        [Test]
        public void IdsDifferingOnlyByCase_AreDistinct()
        {
            ContentValidationReport report = Validate(Sorted(
                CardTestFactory.Card(id: "card_i"),
                CardTestFactory.Card(id: "card_I")));

            Assert.That(report.Contains(ContentIssueCode.DuplicateCardId), Is.False,
                "ordinal comparison keeps these distinct");
        }

        [Test]
        public void ValidationIsIdenticalUnderATurkishCulture()
        {
            // The dotted/dotless I is the classic trap: any culture-aware casing or collation on
            // IDs would change what counts as a duplicate on a Turkish machine.
            List<CardDefinition> cards = Sorted(
                CardTestFactory.Card(id: "card_i"),
                CardTestFactory.Card(id: "card_I"),
                CardTestFactory.Card(id: "card_istanbul"));

            ContentValidationReport invariant = Validate(cards);

            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            ContentValidationReport turkish;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                turkish = Validate(cards);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }

            Assert.That(turkish.ErrorCount, Is.EqualTo(invariant.ErrorCount));
            Assert.That(turkish.WarningCount, Is.EqualTo(invariant.WarningCount));
            Assert.That(turkish.HasErrors, Is.False, turkish.ToString());
        }

        // --- Null tolerance --------------------------------------------------------------------

        [Test]
        public void NullCardList_DoesNotThrow()
        {
            Assert.That(() => validator.Validate(null, allEndings), Throws.Nothing);
        }

        [Test]
        public void NullEndingList_ReportsEveryBoundaryAsMissing()
        {
            ContentValidationReport report = validator.Validate(
                Sorted(CardTestFactory.Card(id: "card_a")), null);

            Assert.That(report.CountOf(ContentIssueCode.MissingEndingBoundary), Is.EqualTo(8));
        }
    }
}
