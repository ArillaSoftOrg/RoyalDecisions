using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class RunStateTests
    {
        private const int TestSeed = 20260729;

        [Test]
        public void CreateNew_StartsEveryStatAtTheInitialValue()
        {
            RunState state = RunState.CreateNew(TestSeed);

            Assert.That(state.Stats.Authority, Is.EqualTo(StatBounds.Initial));
            Assert.That(state.Stats.People, Is.EqualTo(StatBounds.Initial));
            Assert.That(state.Stats.Security, Is.EqualTo(StatBounds.Initial));
            Assert.That(state.Stats.Wealth, Is.EqualTo(StatBounds.Initial));
        }

        [Test]
        public void CreateNew_StampsTheCurrentSaveVersion()
        {
            Assert.That(RunState.CreateNew(TestSeed).SaveVersion,
                Is.EqualTo(GameConstants.CurrentSaveVersion));
        }

        [Test]
        public void CreateNew_RetainsTheSeedSoSelectionStaysDeterministic()
        {
            Assert.That(RunState.CreateNew(TestSeed).Seed, Is.EqualTo(TestSeed));
        }

        [Test]
        public void CreateNew_StartsActiveAtTheFirstTurnWithNoHistory()
        {
            RunState state = RunState.CreateNew(TestSeed);

            Assert.That(state.IsRunActive, Is.True);
            Assert.That(state.Turn, Is.EqualTo(GameConstants.FirstTurn));
            Assert.That(state.Flags, Is.Empty);
            Assert.That(state.ShownCardIds, Is.Empty);
            Assert.That(state.Cooldowns, Is.Empty);
            Assert.That(state.HasForcedNextCard, Is.False);
            Assert.That(state.CurrentCardId, Is.Empty);
        }

        [Test]
        public void AddFlag_StoresTheFlagOnce()
        {
            RunState state = RunState.CreateNew(TestSeed);

            Assert.That(state.AddFlag("war_declared"), Is.True);
            Assert.That(state.AddFlag("war_declared"), Is.False, "adding twice must not duplicate");
            Assert.That(state.Flags.Count, Is.EqualTo(1));
            Assert.That(state.HasFlag("war_declared"), Is.True);
        }

        [Test]
        public void AddFlag_IgnoresEmptyIds()
        {
            RunState state = RunState.CreateNew(TestSeed);

            Assert.That(state.AddFlag(null), Is.False);
            Assert.That(state.AddFlag(string.Empty), Is.False);
            Assert.That(state.Flags, Is.Empty);
        }

        [Test]
        public void RemoveFlag_RemovesOnlyFlagsThatArePresent()
        {
            RunState state = RunState.CreateNew(TestSeed);
            state.AddFlag("war_declared");

            Assert.That(state.RemoveFlag("never_added"), Is.False);
            Assert.That(state.RemoveFlag("war_declared"), Is.True);
            Assert.That(state.HasFlag("war_declared"), Is.False);
            Assert.That(state.Flags, Is.Empty);
        }

        [Test]
        public void AdvanceTurn_IncrementsTheTurnCounter()
        {
            RunState state = RunState.CreateNew(TestSeed);

            state.AdvanceTurn();
            state.AdvanceTurn();

            Assert.That(state.Turn, Is.EqualTo(GameConstants.FirstTurn + 2));
        }

        [Test]
        public void MarkCardShown_RecordsEachCardOnce()
        {
            RunState state = RunState.CreateNew(TestSeed);

            state.MarkCardShown("card_a");
            state.MarkCardShown("card_a");
            state.MarkCardShown("card_b");

            Assert.That(state.ShownCardIds.Count, Is.EqualTo(2));
            Assert.That(state.HasShownCard("card_a"), Is.True);
            Assert.That(state.HasShownCard("card_c"), Is.False);
        }

        [Test]
        public void SetCooldown_BlocksTheCardUntilTheTargetTurn()
        {
            RunState state = RunState.CreateNew(TestSeed);
            state.SetCooldown("card_a", 3);

            Assert.That(state.IsOnCooldown("card_a"), Is.True, "turn 0 is before turn 3");

            state.AdvanceTurn();
            state.AdvanceTurn();
            Assert.That(state.IsOnCooldown("card_a"), Is.True, "turn 2 is still before turn 3");

            state.AdvanceTurn();
            Assert.That(state.IsOnCooldown("card_a"), Is.False, "turn 3 releases the cooldown");
        }

        [Test]
        public void IsOnCooldown_IsFalseForCardsNeverPutOnCooldown()
        {
            RunState state = RunState.CreateNew(TestSeed);

            Assert.That(state.IsOnCooldown("card_never_seen"), Is.False);
            Assert.That(state.TryGetCooldownTurn("card_never_seen", out int turn), Is.False);
            Assert.That(turn, Is.Zero);
        }

        [Test]
        public void SetCooldown_ExtendsButNeverShortensAnExistingCooldown()
        {
            RunState state = RunState.CreateNew(TestSeed);

            state.SetCooldown("card_a", 10);
            state.SetCooldown("card_a", 4);

            Assert.That(state.Cooldowns.Count, Is.EqualTo(1), "the same card must not gain a second entry");
            Assert.That(state.TryGetCooldownTurn("card_a", out int turn), Is.True);
            Assert.That(turn, Is.EqualTo(10));
        }

        [Test]
        public void SetForcedNextCardId_DrivesTheForcedCardFlag()
        {
            RunState state = RunState.CreateNew(TestSeed);

            state.SetForcedNextCardId("card_chain_2");
            Assert.That(state.HasForcedNextCard, Is.True);
            Assert.That(state.ForcedNextCardId, Is.EqualTo("card_chain_2"));

            state.ClearForcedNextCardId();
            Assert.That(state.HasForcedNextCard, Is.False);
            Assert.That(state.ForcedNextCardId, Is.Empty);
        }

        [Test]
        public void EndRun_DeactivatesTheRunAndClearsPendingCards()
        {
            RunState state = RunState.CreateNew(TestSeed);
            state.SetCurrentCardId("card_a");
            state.SetForcedNextCardId("card_b");

            state.EndRun();

            Assert.That(state.IsRunActive, Is.False);
            Assert.That(state.CurrentCardId, Is.Empty);
            Assert.That(state.HasForcedNextCard, Is.False);
        }

        [Test]
        public void SetStats_ReplacesTheStoredStats()
        {
            RunState state = RunState.CreateNew(TestSeed);

            state.SetStats(state.Stats.WithDelta(new StatDeltas(-10, 10, 0, 0)));

            Assert.That(state.Stats.Authority, Is.EqualTo(StatBounds.Initial - 10));
            Assert.That(state.Stats.People, Is.EqualTo(StatBounds.Initial + 10));
        }

        [Test]
        public void RunState_SurvivesAJsonRoundTrip()
        {
            RunState original = RunState.CreateNew(TestSeed);
            original.AddFlag("war_declared");
            original.MarkCardShown("card_a");
            original.SetCooldown("card_a", 5);
            original.SetCurrentCardId("card_b");
            original.AdvanceTurn();
            original.SetStats(original.Stats.WithDelta(new StatDeltas(-10, 0, 0, 0)));

            RunState restored = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(original));

            Assert.That(restored.Seed, Is.EqualTo(TestSeed));
            Assert.That(restored.Turn, Is.EqualTo(original.Turn));
            Assert.That(restored.SaveVersion, Is.EqualTo(GameConstants.CurrentSaveVersion));
            Assert.That(restored.Stats.Authority, Is.EqualTo(StatBounds.Initial - 10));
            Assert.That(restored.HasFlag("war_declared"), Is.True);
            Assert.That(restored.HasShownCard("card_a"), Is.True);
            Assert.That(restored.IsOnCooldown("card_a"), Is.True);
            Assert.That(restored.CurrentCardId, Is.EqualTo("card_b"));
            Assert.That(restored.IsRunActive, Is.True);
        }

        [Test]
        public void EmptyJson_StillProducesAUsableStateBecauseTheConstructorRuns()
        {
            // Verified against Unity 6000.3.20f1: JsonUtility invokes the parameterless
            // constructor and only then overwrites the fields the JSON actually contains. A
            // truncated save therefore cannot hand gameplay null collections, and SanitizeAfterLoad
            // must leave a state like this alone rather than resetting it.
            RunState restored = JsonUtility.FromJson<RunState>("{}");
            restored.SanitizeAfterLoad();

            Assert.That(restored.Flags, Is.Not.Null.And.Empty);
            Assert.That(restored.ShownCardIds, Is.Not.Null.And.Empty);
            Assert.That(restored.Cooldowns, Is.Not.Null.And.Empty);
            Assert.That(restored.ForcedNextCardId, Is.Empty);
            Assert.That(restored.CurrentCardId, Is.Empty);
            Assert.That(restored.Stats.Authority, Is.EqualTo(StatBounds.Initial));
            Assert.That(restored.SaveVersion, Is.EqualTo(GameConstants.CurrentSaveVersion));
        }

        [Test]
        public void SanitizeAfterLoad_LeavesTheStateWritableAfterNulledCollections()
        {
            RunState restored = JsonUtility.FromJson<RunState>(
                "{\"flags\":null,\"shownCardIds\":null,\"cooldowns\":null}");

            restored.SanitizeAfterLoad();

            Assert.That(restored.Flags, Is.Not.Null);
            Assert.That(restored.ShownCardIds, Is.Not.Null);
            Assert.That(restored.Cooldowns, Is.Not.Null);
            Assert.That(restored.AddFlag("still_writable"), Is.True);
            Assert.That(() => restored.MarkCardShown("card_a"), Throws.Nothing);
            Assert.That(() => restored.SetCooldown("card_a", 3), Throws.Nothing);
        }

        [Test]
        public void SanitizeAfterLoad_ClampsOutOfRangeStatsFromAnEditedSave()
        {
            RunState restored = JsonUtility.FromJson<RunState>(
                "{\"stats\":{\"authority\":9999,\"people\":-5,\"security\":50,\"wealth\":50}}");

            restored.SanitizeAfterLoad();

            Assert.That(restored.Stats.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(restored.Stats.People, Is.EqualTo(StatBounds.Min));
            Assert.That(restored.Stats.Security, Is.EqualTo(50));
        }
    }
}
