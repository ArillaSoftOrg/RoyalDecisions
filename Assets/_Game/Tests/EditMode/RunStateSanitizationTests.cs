using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers the post-load repair that stands between a damaged save file and gameplay.
    /// </summary>
    [TestFixture]
    public class RunStateSanitizationTests
    {
        private static RunState FromJson(string json)
        {
            return JsonUtility.FromJson<RunState>(json);
        }

        [Test]
        public void ACleanStateReportsNoRepairs()
        {
            RunState state = RunState.CreateNew(7);
            state.AddFlag("a");
            state.MarkCardShown("card_a");
            state.SetCooldown("card_a", 5);

            Assert.That(state.SanitizeAfterLoad(), Is.False,
                "a healthy run must not be reported as repaired");
        }

        [Test]
        public void SanitizingTwiceIsStable()
        {
            RunState state = FromJson("{\"turn\":-5,\"flags\":[\"a\",\"a\",\"\"]}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);
            Assert.That(state.SanitizeAfterLoad(), Is.False,
                "the second pass has nothing left to fix");
        }

        // --- Turn --------------------------------------------------------------

        [TestCase(-1)]
        [TestCase(-9999)]
        [TestCase(int.MinValue)]
        public void NegativeTurnIsClamped(int turn)
        {
            // A negative turn would invert every IsOnCooldown comparison, quietly unblocking every
            // cooling card for the rest of the run.
            RunState state = FromJson("{\"turn\":" + turn + "}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);
            Assert.That(state.Turn, Is.EqualTo(GameConstants.FirstTurn));
        }

        [Test]
        public void APositiveTurnIsLeftAlone()
        {
            RunState state = FromJson("{\"turn\":37}");

            state.SanitizeAfterLoad();

            Assert.That(state.Turn, Is.EqualTo(37));
        }

        [Test]
        public void ClampingTheTurnRestoresCooldownBehaviour()
        {
            RunState state = FromJson(
                "{\"turn\":-5,\"cooldowns\":[{\"cardId\":\"card_a\",\"availableOnTurn\":3}]}");

            state.SanitizeAfterLoad();

            Assert.That(state.Turn, Is.EqualTo(0));
            Assert.That(state.IsOnCooldown("card_a"), Is.True);
        }

        // --- Flags and history ---------------------------------------------------

        [Test]
        public void BlankAndDuplicateFlagsAreStripped()
        {
            RunState state = FromJson("{\"flags\":[\"a\",\"\",\"a\",\"   \",\"b\"]}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);
            Assert.That(state.Flags, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void BlankAndDuplicateShownCardIdsAreStripped()
        {
            RunState state = FromJson("{\"shownCardIds\":[\"card_a\",\"card_a\",\"\",\"card_b\"]}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);
            Assert.That(state.ShownCardIds, Is.EqualTo(new[] { "card_a", "card_b" }));
        }

        [Test]
        public void TheFirstOccurrenceOfADuplicateIsKept()
        {
            RunState state = FromJson("{\"flags\":[\"first\",\"second\",\"first\"]}");

            state.SanitizeAfterLoad();

            Assert.That(state.Flags[0], Is.EqualTo("first"));
            Assert.That(state.Flags[1], Is.EqualTo("second"));
        }

        [Test]
        public void FlagsRemainUsableAfterCompaction()
        {
            RunState state = FromJson("{\"flags\":[\"a\",\"a\"]}");
            state.SanitizeAfterLoad();

            Assert.That(state.HasFlag("a"), Is.True);
            Assert.That(state.RemoveFlag("a"), Is.True);
            Assert.That(state.Flags, Is.Empty, "no shadow duplicate should survive");
        }

        [Test]
        public void CaseDifferingIdsAreNotTreatedAsDuplicates()
        {
            RunState state = FromJson("{\"flags\":[\"Flag\",\"flag\"]}");

            state.SanitizeAfterLoad();

            Assert.That(state.Flags.Count, Is.EqualTo(2),
                "flag comparison is ordinal, so case matters");
        }

        // --- Cooldowns ---------------------------------------------------------------

        [Test]
        public void CooldownEntriesWithoutACardIdAreDropped()
        {
            RunState state = FromJson(
                "{\"cooldowns\":[{\"cardId\":\"\",\"availableOnTurn\":5}," +
                "{\"cardId\":\"card_a\",\"availableOnTurn\":5}]}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);
            Assert.That(state.Cooldowns.Count, Is.EqualTo(1));
            Assert.That(state.Cooldowns[0].CardId, Is.EqualTo("card_a"));
        }

        [Test]
        public void DuplicateCooldownsCollapseOntoTheLaterTurn()
        {
            // Keeping the shorter one would let a card come back early.
            RunState state = FromJson(
                "{\"cooldowns\":[{\"cardId\":\"card_a\",\"availableOnTurn\":3}," +
                "{\"cardId\":\"card_a\",\"availableOnTurn\":9}]}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);
            Assert.That(state.Cooldowns.Count, Is.EqualTo(1));
            Assert.That(state.TryGetCooldownTurn("card_a", out int turn), Is.True);
            Assert.That(turn, Is.EqualTo(9));
        }

        [Test]
        public void DuplicateCooldownsCollapseRegardlessOfOrder()
        {
            RunState state = FromJson(
                "{\"cooldowns\":[{\"cardId\":\"card_a\",\"availableOnTurn\":9}," +
                "{\"cardId\":\"card_a\",\"availableOnTurn\":3}]}");

            state.SanitizeAfterLoad();

            state.TryGetCooldownTurn("card_a", out int turn);
            Assert.That(turn, Is.EqualTo(9));
        }

        [Test]
        public void CooldownsRemainQueryableAfterCompaction()
        {
            RunState state = FromJson(
                "{\"turn\":0,\"cooldowns\":[{\"cardId\":\"card_a\",\"availableOnTurn\":4}," +
                "{\"cardId\":\"card_a\",\"availableOnTurn\":2}]}");

            state.SanitizeAfterLoad();

            Assert.That(state.IsOnCooldown("card_a"), Is.True);
            state.SetCooldown("card_a", 6);
            Assert.That(state.Cooldowns.Count, Is.EqualTo(1), "no hidden duplicate to find");
        }

        // --- Stats and collections -------------------------------------------------------

        [Test]
        public void OutOfRangeStatsAreClampedAndReported()
        {
            RunState state = FromJson(
                "{\"stats\":{\"authority\":999,\"people\":-5,\"security\":50,\"wealth\":50}}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);
            Assert.That(state.Stats.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(state.Stats.People, Is.EqualTo(StatBounds.Min));
            Assert.That(state.Stats.Security, Is.EqualTo(50));
        }

        [Test]
        public void InRangeStatsAreNotReportedAsRepaired()
        {
            RunState state = FromJson(
                "{\"stats\":{\"authority\":10,\"people\":20,\"security\":30,\"wealth\":40}}");

            Assert.That(state.SanitizeAfterLoad(), Is.False);
            Assert.That(state.Stats.Authority, Is.EqualTo(10));
        }

        [Test]
        public void ExplicitlyNulledCollectionsAreRestored()
        {
            RunState state = FromJson(
                "{\"flags\":null,\"shownCardIds\":null,\"cooldowns\":null}");

            state.SanitizeAfterLoad();

            Assert.That(state.Flags, Is.Not.Null);
            Assert.That(state.ShownCardIds, Is.Not.Null);
            Assert.That(state.Cooldowns, Is.Not.Null);
            Assert.That(() => state.AddFlag("a"), Throws.Nothing);
            Assert.That(() => state.MarkCardShown("card_a"), Throws.Nothing);
            Assert.That(() => state.SetCooldown("card_a", 3), Throws.Nothing);
        }

        [Test]
        public void SeveralProblemsAtOnceAreAllRepaired()
        {
            RunState state = FromJson(
                "{\"turn\":-4," +
                "\"stats\":{\"authority\":999,\"people\":50,\"security\":50,\"wealth\":-1}," +
                "\"flags\":[\"a\",\"a\"]," +
                "\"shownCardIds\":[\"\",\"card_a\"]," +
                "\"cooldowns\":[{\"cardId\":\"card_a\",\"availableOnTurn\":2}," +
                "{\"cardId\":\"card_a\",\"availableOnTurn\":7}]}");

            Assert.That(state.SanitizeAfterLoad(), Is.True);

            Assert.That(state.Turn, Is.EqualTo(0));
            Assert.That(state.Stats.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(state.Stats.Wealth, Is.EqualTo(StatBounds.Min));
            Assert.That(state.Flags.Count, Is.EqualTo(1));
            Assert.That(state.ShownCardIds, Is.EqualTo(new[] { "card_a" }));
            Assert.That(state.Cooldowns.Count, Is.EqualTo(1));
            Assert.That(state.SanitizeAfterLoad(), Is.False, "and the result is stable");
        }
    }
}
