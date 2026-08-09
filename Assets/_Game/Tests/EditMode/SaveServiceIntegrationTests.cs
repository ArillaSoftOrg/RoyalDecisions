using System.IO;
using NUnit.Framework;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Runs the save system against a real disk, in a throwaway directory.
    /// </summary>
    /// <remarks>
    /// The in-memory double proves the logic; this proves the logic survives contact with
    /// <c>System.IO</c> — File.Replace in particular has requirements a fake cannot express.
    /// Nothing here touches <c>Application.persistentDataPath</c>.
    /// </remarks>
    [TestFixture]
    public class SaveServiceIntegrationTests
    {
        private TempDirectory directory;
        private SavePaths paths;
        private SaveService service;

        [SetUp]
        public void SetUp()
        {
            directory = new TempDirectory();
            paths = new SavePaths(directory.Path);
            service = new SaveService(new SystemFileSystem(), paths);
        }

        [TearDown]
        public void TearDown()
        {
            directory?.Dispose();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Safety net: a test that throws before TearDown must still not leave a directory.
            directory?.Dispose();
        }

        [Test]
        public void SaveThenLoad_SurvivesARealRoundTrip()
        {
            RunState original = RunState.CreateNew(4242);
            original.AddFlag("taxes_raised");
            original.MarkCardShown("card_01_coronation");
            original.SetCooldown("card_11_spy_master", 6);
            original.AdvanceTurn();

            Assert.That(service.Save(original).Succeeded, Is.True);

            RunLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Success), result.ToString());
            Assert.That(result.RunState.Seed, Is.EqualTo(4242));
            Assert.That(result.RunState.Turn, Is.EqualTo(1));
            Assert.That(result.RunState.HasFlag("taxes_raised"), Is.True);
            Assert.That(result.RunState.IsOnCooldown("card_11_spy_master"), Is.True);
        }

        [Test]
        public void SaveCreatesTheDirectoryWhenItIsMissing()
        {
            string nested = Path.Combine(directory.Path, "nested", "deeper");
            SaveService nestedService = new SaveService(
                new SystemFileSystem(), new SavePaths(nested));

            Assert.That(nestedService.Save(RunState.CreateNew(1)).Succeeded, Is.True);
            Assert.That(Directory.Exists(nested), Is.True);
        }

        [Test]
        public void ASuccessfulSaveLeavesNoTemporaryFileOnDisk()
        {
            service.Save(RunState.CreateNew(1));

            Assert.That(File.Exists(paths.RunSavePath), Is.True);
            Assert.That(File.Exists(paths.RunTempPath), Is.False);
        }

        [Test]
        public void ResavingProducesABackupHoldingThePreviousRun()
        {
            service.Save(RunState.CreateNew(111));
            service.Save(RunState.CreateNew(222));

            Assert.That(File.Exists(paths.RunBackupPath), Is.True);
            Assert.That(File.Exists(paths.RunTempPath), Is.False);
            Assert.That(service.Load().RunState.Seed, Is.EqualTo(222));

            // Corrupt the live save and the backup should carry the earlier run.
            File.WriteAllText(paths.RunSavePath, "corrupt");

            RunLoadResult recovered = service.Load();
            Assert.That(recovered.Status, Is.EqualTo(LoadStatus.RecoveredFromBackup));
            Assert.That(recovered.RunState.Seed, Is.EqualTo(111));
        }

        [Test]
        public void TheSaveFileIsHumanReadableJson()
        {
            service.Save(RunState.CreateNew(7));

            string text = File.ReadAllText(paths.RunSavePath);

            Assert.That(text.TrimStart(), Does.StartWith("{"),
                "no byte order mark should precede the opening brace");
            Assert.That(text, Does.Contain("\"seed\": 7"));
        }

        [Test]
        public void DeleteRemovesEverySaveArtefact()
        {
            service.Save(RunState.CreateNew(1));
            service.Save(RunState.CreateNew(2));

            service.Delete();

            Assert.That(File.Exists(paths.RunSavePath), Is.False);
            Assert.That(File.Exists(paths.RunBackupPath), Is.False);
            Assert.That(File.Exists(paths.RunTempPath), Is.False);
            Assert.That(Directory.Exists(directory.Path), Is.True, "the folder itself stays");
        }

        [Test]
        public void SettingsAndRunSavesCoexistOnDisk()
        {
            SettingsSaveService settingsService =
                new SettingsSaveService(new SystemFileSystem(), paths);

            service.Save(RunState.CreateNew(9));

            GameSettings settings = GameSettings.CreateDefault();
            settings.SetMusicVolume(0.1f);
            settingsService.Save(settings);

            service.Delete();

            Assert.That(File.Exists(paths.SettingsSavePath), Is.True);
            Assert.That(settingsService.Load().MusicVolume, Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void NothingIsWrittenOutsideTheTemporaryRoot()
        {
            service.Save(RunState.CreateNew(1));
            service.Save(RunState.CreateNew(2));
            new SettingsSaveService(new SystemFileSystem(), paths)
                .Save(GameSettings.CreateDefault());

            string[] written = Directory.GetFiles(
                directory.Path, "*", SearchOption.AllDirectories);

            foreach (string file in written)
            {
                Assert.That(file.Replace('\\', '/'), Does.StartWith(directory.Path.Replace('\\', '/')));
            }

            Assert.That(written.Length, Is.EqualTo(3), "run, its backup, and settings");
        }
    }
}
