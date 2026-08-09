using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class SaveServiceTests
    {
        private const string Root = "saves";
        private const int TestSeed = 20260730;

        private FakeFileSystem fileSystem;
        private SavePaths paths;
        private SaveService service;

        [SetUp]
        public void SetUp()
        {
            fileSystem = new FakeFileSystem();
            paths = new SavePaths(Root);
            service = new SaveService(fileSystem, paths);
        }

        /// <summary>A run with something in every field, so a round trip has something to lose.</summary>
        private static RunState PopulatedRun()
        {
            RunState state = RunState.CreateNew(TestSeed);
            state.SetStats(state.Stats.WithDelta(new StatDeltas(-10, 5, 0, 20)));
            state.AddFlag("taxes_raised");
            state.AddFlag("army_favoured");
            state.MarkCardShown("card_01_coronation");
            state.MarkCardShown("card_04_tax_reform");
            state.SetCooldown("card_11_spy_master", 11);
            state.SetForcedNextCardId("card_16_inquisitor_verdict");
            state.SetCurrentCardId("card_07_general_visit");
            state.AdvanceTurn();
            state.AdvanceTurn();
            return state;
        }

        private void SeedRun(string json)
        {
            fileSystem.Seed(paths.RunSavePath, json);
        }

        /// <summary>Wraps a run body in a fully marked, current-version envelope.</summary>
        private static string Envelope(string runBody, int version = 1)
        {
            return "{\"saveVersion\":" + version
                + ",\"format\":\"" + RunSaveFile.FormatMarker + "\""
                + ",\"payload\":{\"kind\":\"" + RunSavePayload.KindMarker + "\""
                + ",\"run\":" + runBody + "}}";
        }

        private const string FullRunBody =
            "{\"saveVersion\":1,\"turn\":4,\"seed\":99," +
            "\"stats\":{\"authority\":55,\"people\":45,\"security\":50,\"wealth\":60}," +
            "\"flags\":[\"a\"],\"shownCardIds\":[\"card_x\"],\"cooldowns\":[]," +
            "\"forcedNextCardId\":\"\",\"currentCardId\":\"card_y\",\"isRunActive\":true}";

        private static string ValidRunJson => Envelope(FullRunBody);

        // --- Round trip -----------------------------------------------------------

        [Test]
        public void SaveThenLoad_PreservesEveryField()
        {
            RunState original = PopulatedRun();

            Assert.That(service.Save(original).Succeeded, Is.True);
            RunLoadResult result = service.Load();

            Assert.That(result.Succeeded, Is.True, result.ToString());
            RunState restored = result.RunState;

            Assert.That(restored.Seed, Is.EqualTo(original.Seed));
            Assert.That(restored.Turn, Is.EqualTo(original.Turn));
            Assert.That(restored.SaveVersion, Is.EqualTo(GameConstants.CurrentSaveVersion));
            Assert.That(restored.Stats.Authority, Is.EqualTo(original.Stats.Authority));
            Assert.That(restored.Stats.People, Is.EqualTo(original.Stats.People));
            Assert.That(restored.Stats.Security, Is.EqualTo(original.Stats.Security));
            Assert.That(restored.Stats.Wealth, Is.EqualTo(original.Stats.Wealth));
            Assert.That(restored.Flags, Is.EquivalentTo(original.Flags));
            Assert.That(restored.ShownCardIds, Is.EquivalentTo(original.ShownCardIds));
            Assert.That(restored.ForcedNextCardId, Is.EqualTo(original.ForcedNextCardId));
            Assert.That(restored.CurrentCardId, Is.EqualTo(original.CurrentCardId));
            Assert.That(restored.IsRunActive, Is.EqualTo(original.IsRunActive));
            Assert.That(restored.IsOnCooldown("card_11_spy_master"), Is.True);
        }

        [Test]
        public void SaveThenLoad_ReportsACleanLoadWithNoRepairs()
        {
            service.Save(PopulatedRun());

            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.Success),
                "a save this service just wrote should need no repair");
        }

        /// <summary>
        /// Locks the wire format. The save file is keyed on RunState's private field names, so a
        /// rename would silently invalidate every existing save — this fails first instead.
        /// </summary>
        [TestCase("saveVersion")]
        [TestCase("format")]
        [TestCase("payload")]
        [TestCase("kind")]
        [TestCase("run")]
        [TestCase("turn")]
        [TestCase("seed")]
        [TestCase("stats")]
        [TestCase("authority")]
        [TestCase("people")]
        [TestCase("security")]
        [TestCase("wealth")]
        [TestCase("flags")]
        [TestCase("shownCardIds")]
        [TestCase("cooldowns")]
        [TestCase("forcedNextCardId")]
        [TestCase("currentCardId")]
        [TestCase("isRunActive")]
        public void SaveFileSchemaIsLocked(string fieldName)
        {
            service.Save(PopulatedRun());
            string json = fileSystem.Peek(paths.RunSavePath);

            Assert.That(json, Does.Contain("\"" + fieldName + "\""),
                "The save format lost the field '" + fieldName + "'. If this rename was "
                + "deliberate, existing saves can no longer be read: bump "
                + "GameConstants.CurrentSaveVersion and decide how to handle the old version.");
        }

        [Test]
        public void SaveFile_CarriesTheCurrentVersionInItsEnvelope()
        {
            service.Save(PopulatedRun());
            string json = fileSystem.Peek(paths.RunSavePath);

            Assert.That(json, Does.Contain("\"saveVersion\": " + GameConstants.CurrentSaveVersion));
        }

        // --- Presence and deletion --------------------------------------------------

        [Test]
        public void HasSave_TracksTheFile()
        {
            Assert.That(service.HasSave(), Is.False);

            service.Save(PopulatedRun());
            Assert.That(service.HasSave(), Is.True);

            service.Delete();
            Assert.That(service.HasSave(), Is.False);
        }

        [Test]
        public void Delete_RemovesTheBackupAndTemporaryFilesToo()
        {
            service.Save(PopulatedRun());
            service.Save(PopulatedRun());
            Assert.That(fileSystem.FileExists(paths.RunBackupPath), Is.True, "precondition");

            service.Delete();

            Assert.That(fileSystem.FileExists(paths.RunSavePath), Is.False);
            Assert.That(fileSystem.FileExists(paths.RunBackupPath), Is.False);
            Assert.That(fileSystem.FileExists(paths.RunTempPath), Is.False);
        }

        [Test]
        public void Save_RejectsANullRunWithoutWritingAnything()
        {
            SaveResult result = service.Save(null);

            Assert.That(result.Status, Is.EqualTo(SaveStatus.InvalidData));
            Assert.That(fileSystem.FileCount, Is.Zero);
        }

        [Test]
        public void FailedSave_LeavesAPreviousSaveIntact()
        {
            RunState first = RunState.CreateNew(1);
            service.Save(first);
            string before = fileSystem.Peek(paths.RunSavePath);

            fileSystem.FailAllWrites = true;
            SaveResult result = service.Save(RunState.CreateNew(2));

            Assert.That(result.Status, Is.EqualTo(SaveStatus.WriteFailed));
            Assert.That(fileSystem.Peek(paths.RunSavePath), Is.EqualTo(before));

            fileSystem.FailAllWrites = false;
            Assert.That(service.Load().RunState.Seed, Is.EqualTo(1));
        }

        // --- The behaviour table ------------------------------------------------------

        [Test]
        public void MissingFile_ReportsNoSaveFile()
        {
            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.NoSaveFile));
            Assert.That(result.HasRun, Is.False);
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void EmptyFile_ReportsEmpty()
        {
            SeedRun("   ");

            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.Empty));
        }

        [TestCase("{\"saveVersion\":")]
        [TestCase("not json at all")]
        [TestCase("[1,2,3]")]
        [TestCase("{{{{")]
        public void MalformedJson_ReportsCorrupt(string json)
        {
            SeedRun(json);

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            Assert.That(result.HasRun, Is.False);
        }

        // --- Required format and payload markers -------------------------------------
        //
        // JsonUtility materialises nested serializable objects even when the JSON omits them, and
        // every field of a run has a legitimate default. Inspecting the run therefore cannot reveal
        // that it was never written. The markers can: they are set only by the writing constructor,
        // so anything conjured from an absent field arrives without them.

        [Test]
        public void TruncatedEnvelope_ReportsCorrupt()
        {
            // Case 1: the exact file that must never be mistaken for a valid turn-0 run.
            SeedRun("{\"saveVersion\":1}");

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            Assert.That(result.HasRun, Is.False, "no run may be exposed from an unmarked file");
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void ValidFormatMarkerButOmittedPayload_ReportsCorrupt()
        {
            // Case 2: the envelope is convincing, the payload never arrived.
            SeedRun("{\"saveVersion\":1,\"format\":\"" + RunSaveFile.FormatMarker + "\"}");

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            Assert.That(result.HasRun, Is.False);
        }

        [Test]
        public void RunObjectPresentButPayloadMarkerOmitted_ReportsCorrupt()
        {
            // Case 3: a real-looking run, but nothing vouches for it.
            SeedRun("{\"saveVersion\":1,\"format\":\"" + RunSaveFile.FormatMarker + "\""
                + ",\"payload\":{\"run\":" + FullRunBody + "}}");

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            Assert.That(result.HasRun, Is.False);
        }

        [TestCase("\"wrong.format\"", "\"" + RunSavePayload.KindMarker + "\"")]
        [TestCase("\"" + RunSaveFile.FormatMarker + "\"", "\"wrong-kind\"")]
        [TestCase("\"\"", "\"" + RunSavePayload.KindMarker + "\"")]
        [TestCase("\"" + RunSaveFile.FormatMarker + "\"", "\"\"")]
        [TestCase("null", "null")]
        public void IncorrectMarkerValues_ReportCorrupt(string format, string kind)
        {
            // Case 4: the markers must match exactly, not merely be present.
            SeedRun("{\"saveVersion\":1,\"format\":" + format
                + ",\"payload\":{\"kind\":" + kind + ",\"run\":" + FullRunBody + "}}");

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            Assert.That(result.HasRun, Is.False);
        }

        [Test]
        public void MarkerComparisonIsCaseSensitive()
        {
            SeedRun("{\"saveVersion\":1,\"format\":\"" + RunSaveFile.FormatMarker.ToUpperInvariant()
                + "\",\"payload\":{\"kind\":\"" + RunSavePayload.KindMarker
                + "\",\"run\":" + FullRunBody + "}}");

            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.Corrupt));
        }

        [Test]
        public void ACompleteValidSaveLoadsCleanly()
        {
            // Case 5.
            SeedRun(ValidRunJson);

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Success), result.ToString());
            Assert.That(result.RunState.Seed, Is.EqualTo(99));
            Assert.That(result.RunState.Turn, Is.EqualTo(4));
            Assert.That(result.RunState.CurrentCardId, Is.EqualTo("card_y"));
        }

        [Test]
        public void ACompleteButRepairableSaveLoadsAfterRepair()
        {
            // Case 6: structurally identifiable — both markers valid — but carrying values that
            // need repair. This is the only shape SuccessAfterRepair is for.
            SeedRun(Envelope("{\"seed\":42,\"turn\":-3,\"flags\":null," +
                             "\"stats\":{\"authority\":999,\"people\":50," +
                             "\"security\":50,\"wealth\":50}}"));

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.SuccessAfterRepair));
            Assert.That(result.RunState.Seed, Is.EqualTo(42));
            Assert.That(result.RunState.Turn, Is.EqualTo(GameConstants.FirstTurn));
            Assert.That(result.RunState.Stats.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(result.RunState.Flags, Is.Not.Null.And.Empty);
        }

        [Test]
        public void APartialButMarkedRunLoadsWithConstructorDefaults()
        {
            // A genuinely written save whose run happens to be early in its life: the markers vouch
            // for it, so constructor defaults are the right answer here.
            SeedRun(Envelope("{\"seed\":42}"));

            RunLoadResult result = service.Load();

            Assert.That(result.Succeeded, Is.True, result.ToString());
            Assert.That(result.RunState.Seed, Is.EqualTo(42));
            Assert.That(result.RunState.Stats.Authority, Is.EqualTo(StatBounds.Initial));
        }

        [Test]
        public void CorruptSaveIsNotRewritten()
        {
            const string original = "{\"saveVersion\":1}";
            SeedRun(original);

            service.Load();

            Assert.That(fileSystem.Peek(paths.RunSavePath), Is.EqualTo(original),
                "loading must never repair a file in place");
            Assert.That(fileSystem.FileExists(paths.RunBackupPath), Is.False);
            Assert.That(fileSystem.FileExists(paths.RunTempPath), Is.False);
        }

        // These use fully marked envelopes on purpose: a newer build's save would carry valid
        // markers, so the version rule has to be what rejects them. With the markers absent the
        // tests would pass even if the version check were deleted.

        [Test]
        public void FutureVersion_ReportsUnsupportedVersion()
        {
            SeedRun(Envelope(FullRunBody, 999));

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.UnsupportedVersion));
            Assert.That(result.HasRun, Is.False);
        }

        [Test]
        public void FutureVersion_LeavesTheFileByteForByteUnchanged()
        {
            string json = Envelope(FullRunBody, 999);
            SeedRun(json);

            service.Load();

            Assert.That(fileSystem.Peek(paths.RunSavePath), Is.EqualTo(json),
                "downgrading then upgrading again must not destroy a newer save");
            Assert.That(fileSystem.FileExists(paths.RunBackupPath), Is.False,
                "an unsupported save must not be rewritten or backed up behind the player's back");
        }

        [Test]
        public void FutureVersion_DoesNotSilentlyFallBackToAnOlderBackup()
        {
            fileSystem.Seed(paths.RunBackupPath, ValidRunJson);
            SeedRun(Envelope(FullRunBody, 999));

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.UnsupportedVersion),
                "a newer save is not damage, so the player must not be quietly downgraded");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void VersionBelowTheFirstEverWritten_ReportsCorrupt(int version)
        {
            SeedRun(Envelope(FullRunBody, version));

            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.Corrupt));
        }

        [Test]
        public void MissingVersionField_ReportsCorrupt()
        {
            SeedRun("{\"format\":\"" + RunSaveFile.FormatMarker + "\""
                + ",\"payload\":{\"kind\":\"" + RunSavePayload.KindMarker
                + "\",\"run\":" + FullRunBody + "}}");

            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.Corrupt));
        }

        [Test]
        public void NullCollections_AreRepairedBeforeTheCallerSeesThem()
        {
            SeedRun(Envelope("{\"seed\":1,\"flags\":null," +
                             "\"shownCardIds\":null,\"cooldowns\":null}"));

            RunLoadResult result = service.Load();

            Assert.That(result.Succeeded, Is.True, result.ToString());
            Assert.That(result.RunState.Flags, Is.Not.Null);
            Assert.That(result.RunState.ShownCardIds, Is.Not.Null);
            Assert.That(result.RunState.Cooldowns, Is.Not.Null);
            Assert.That(() => result.RunState.AddFlag("still_writable"), Throws.Nothing);
        }

        [Test]
        public void OutOfRangeStats_AreClampedAndReported()
        {
            SeedRun(Envelope("{\"seed\":1,\"stats\":" +
                             "{\"authority\":999,\"people\":-5,\"security\":50,\"wealth\":50}}"));

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.SuccessAfterRepair));
            Assert.That(result.RunState.Stats.Authority, Is.EqualTo(StatBounds.Max));
            Assert.That(result.RunState.Stats.People, Is.EqualTo(StatBounds.Min));
        }

        [Test]
        public void NegativeTurn_IsClampedAndReported()
        {
            SeedRun(Envelope("{\"seed\":1,\"turn\":-42}"));

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.SuccessAfterRepair));
            Assert.That(result.RunState.Turn, Is.EqualTo(GameConstants.FirstTurn));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void AnySeedIsAccepted(int seed)
        {
            service.Save(RunState.CreateNew(seed));

            RunLoadResult result = service.Load();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RunState.Seed, Is.EqualTo(seed));
        }

        [Test]
        public void ACompletedRunLoadsWithoutBeingResurrected()
        {
            RunState finished = PopulatedRun();
            finished.EndRun();
            service.Save(finished);

            RunLoadResult result = service.Load();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RunState.IsRunActive, Is.False,
                "the save layer must not decide to restart a finished run");
        }

        [Test]
        public void ReadFailure_ReportsReadFailed()
        {
            SeedRun(ValidRunJson);
            fileSystem.FailAllReads = true;

            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.ReadFailed));
        }

        [TestCase("")]
        [TestCase("garbage")]
        [TestCase("{\"saveVersion\":1}")]
        [TestCase("{\"saveVersion\":999,\"format\":\"royaldecisions.save\",\"payload\":{}}")]
        [TestCase("{\"format\":\"wrong\",\"payload\":{\"kind\":\"wrong\"}}")]
        public void LoadNeverThrows(string json)
        {
            SeedRun(json);

            Assert.That(() => service.Load(), Throws.Nothing);
        }

        // --- Backup recovery ------------------------------------------------------------

        [Test]
        public void CorruptMainFile_RecoversFromTheBackup()
        {
            fileSystem.Seed(paths.RunBackupPath, ValidRunJson);
            SeedRun("hopelessly corrupt");

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.RecoveredFromBackup));
            Assert.That(result.RunState.Seed, Is.EqualTo(99));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void MissingMainFileWithABackup_RecoversFromTheBackup()
        {
            fileSystem.Seed(paths.RunBackupPath, ValidRunJson);

            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.RecoveredFromBackup));
        }

        [Test]
        public void CorruptMainAndCorruptBackup_ReportsTheMainFailureAndKeepsBothFiles()
        {
            fileSystem.Seed(paths.RunBackupPath, "also corrupt");
            SeedRun("hopelessly corrupt");

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            Assert.That(result.HasRun, Is.False);
            Assert.That(fileSystem.FileExists(paths.RunSavePath), Is.True,
                "an unreadable file must survive so it can still be recovered by hand");
            Assert.That(fileSystem.FileExists(paths.RunBackupPath), Is.True);
        }

        [Test]
        public void SavingTwiceLeavesExactlyTheSaveAndItsBackup()
        {
            service.Save(RunState.CreateNew(1));
            service.Save(RunState.CreateNew(2));

            Assert.That(fileSystem.FileExists(paths.RunSavePath), Is.True);
            Assert.That(fileSystem.FileExists(paths.RunBackupPath), Is.True);
            Assert.That(fileSystem.FileExists(paths.RunTempPath), Is.False);
            Assert.That(fileSystem.FileCount, Is.EqualTo(2));

            Assert.That(service.Load().RunState.Seed, Is.EqualTo(2));
        }

        // --- Construction guards ------------------------------------------------------------

        [Test]
        public void ConstructorRejectsNullDependencies()
        {
            Assert.That(() => new SaveService(null, paths), Throws.ArgumentNullException);
            Assert.That(() => new SaveService(fileSystem, null), Throws.ArgumentNullException);
        }

        [Test]
        public void ServiceHoldsNoStaticState()
        {
            // Two services over separate roots must not see each other's saves.
            SaveService other = new SaveService(fileSystem, new SavePaths("other-root"));

            service.Save(RunState.CreateNew(1));

            Assert.That(other.HasSave(), Is.False);
            Assert.That(other.Load().Status, Is.EqualTo(LoadStatus.NoSaveFile));
        }
    }
}
