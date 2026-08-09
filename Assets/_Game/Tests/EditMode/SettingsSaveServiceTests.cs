using NUnit.Framework;
using RoyalDecisions.Domain;
using RoyalDecisions.Infrastructure;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class SettingsSaveServiceTests
    {
        private const string Root = "saves";

        private FakeFileSystem fileSystem;
        private SavePaths paths;
        private SettingsSaveService service;

        [SetUp]
        public void SetUp()
        {
            fileSystem = new FakeFileSystem();
            paths = new SavePaths(Root);
            service = new SettingsSaveService(fileSystem, paths);
        }

        private static GameSettings CustomSettings()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetMusicVolume(0.25f);
            settings.SetSfxVolume(0.5f);
            settings.SetHapticsEnabled(false);
            return settings;
        }

        private static GameSettings CustomSettingsWithGraphicsAndControls()
        {
            GameSettings settings = CustomSettings();
            settings.SetUseHighFrameRateCap(false);
            settings.SetBatterySaverEnabled(true);
            settings.SetTapButtonsEnabled(false);
            settings.SetInvertSwipeRotation(true);
            return settings;
        }

        [Test]
        public void SaveThenLoad_PreservesEverySetting()
        {
            Assert.That(service.Save(CustomSettings()).Succeeded, Is.True);

            GameSettings loaded = service.Load();

            Assert.That(loaded.MusicVolume, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(loaded.SfxVolume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(loaded.HapticsEnabled, Is.False);
        }

        [Test]
        public void MissingFile_ReturnsDefaults()
        {
            GameSettings loaded = service.Load();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.MusicVolume, Is.EqualTo(GameSettings.DefaultVolume));
            Assert.That(loaded.HapticsEnabled, Is.True);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json")]
        [TestCase("{\"saveVersion\":")]
        [TestCase("{\"saveVersion\":1}")]
        [TestCase("{\"saveVersion\":0,\"settings\":{}}")]
        [TestCase("{\"saveVersion\":999,\"settings\":{}}")]
        public void UnusableFile_FallsBackToDefaultsWithoutThrowing(string json)
        {
            fileSystem.Seed(paths.SettingsSavePath, json);

            GameSettings loaded = null;
            Assert.That(() => loaded = service.Load(), Throws.Nothing);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.MusicVolume, Is.EqualTo(GameSettings.DefaultVolume));
        }

        [Test]
        public void OutOfRangeVolumes_AreClamped()
        {
            fileSystem.Seed(paths.SettingsSavePath,
                "{\"saveVersion\":1,\"settings\":{\"musicVolume\":9.5,\"sfxVolume\":-4.0}}");

            GameSettings loaded = service.Load();

            Assert.That(loaded.MusicVolume, Is.EqualTo(GameSettings.MaxVolume));
            Assert.That(loaded.SfxVolume, Is.EqualTo(GameSettings.MinVolume));
        }

        [Test]
        public void NotANumberVolume_FallsBackToTheDefault()
        {
            // Every comparison against NaN is false, so it would slide straight through a clamp and
            // reach the audio mixer.
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetMusicVolume(float.NaN);

            Assert.That(float.IsNaN(settings.MusicVolume), Is.False);
            Assert.That(settings.MusicVolume, Is.EqualTo(GameSettings.DefaultVolume));
        }

        [Test]
        public void SanitizeAfterLoad_ReportsWhetherAnythingChanged()
        {
            GameSettings clean = GameSettings.CreateDefault();
            Assert.That(clean.SanitizeAfterLoad(), Is.False);

            GameSettings damaged = JsonUtility.FromJson<GameSettings>("{\"musicVolume\":42.0}");
            Assert.That(damaged.SanitizeAfterLoad(), Is.True);
            Assert.That(damaged.MusicVolume, Is.EqualTo(GameSettings.MaxVolume));
        }

        [Test]
        public void Save_RejectsNullSettings()
        {
            SaveResult result = service.Save(null);

            Assert.That(result.Status, Is.EqualTo(SaveStatus.InvalidData));
            Assert.That(fileSystem.FileCount, Is.Zero);
        }

        [Test]
        public void OlderVersionOneJsonLoadsAdditiveSettingsDefaults()
        {
            fileSystem.Seed(paths.SettingsSavePath,
                "{\"saveVersion\":1,\"settings\":{\"musicVolume\":0.25,"
                + "\"sfxVolume\":0.75,\"hapticsEnabled\":true}}");

            GameSettings loaded = service.Load();

            Assert.That(loaded.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(loaded.SfxVolume, Is.EqualTo(0.75f));
            Assert.That(loaded.MasterMuted, Is.False);
            Assert.That(loaded.ReducedMotion, Is.False);
            Assert.That(loaded.LargerText, Is.False);
            Assert.That(loaded.HighContrast, Is.False);
            Assert.That(loaded.TutorialCompleted, Is.False);
            Assert.That(SettingsSaveService.CurrentSettingsVersion, Is.EqualTo(1));
        }

        [Test]
        public void OlderJsonMissingGraphicsAndControls_LoadsSafeDefaults()
        {
            fileSystem.Seed(paths.SettingsSavePath,
                "{\"saveVersion\":1,\"settings\":{\"musicVolume\":0.25,"
                + "\"sfxVolume\":0.75,\"hapticsEnabled\":true}}");

            GameSettings loaded = service.Load();

            Assert.That(loaded.UseHighFrameRateCap, Is.True);
            Assert.That(loaded.BatterySaverEnabled, Is.False);
            Assert.That(loaded.TapButtonsEnabled, Is.True);
            Assert.That(loaded.InvertSwipeRotation, Is.False);
            Assert.That(SettingsSaveService.CurrentSettingsVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveThenLoad_PreservesGraphicsAndControlsSettings()
        {
            Assert.That(service.Save(CustomSettingsWithGraphicsAndControls()).Succeeded, Is.True);

            GameSettings loaded = service.Load();

            Assert.That(loaded.UseHighFrameRateCap, Is.False);
            Assert.That(loaded.BatterySaverEnabled, Is.True);
            Assert.That(loaded.TapButtonsEnabled, Is.False);
            Assert.That(loaded.InvertSwipeRotation, Is.True);
        }

        [Test]
        public void CorruptSettings_RecoverFromTheBackup()
        {
            service.Save(CustomSettings());
            service.Save(CustomSettings());
            Assert.That(fileSystem.FileExists(paths.SettingsBackupPath), Is.True, "precondition");

            fileSystem.Seed(paths.SettingsSavePath, "corrupt");

            GameSettings loaded = service.Load();

            Assert.That(loaded.MusicVolume, Is.EqualTo(0.25f).Within(0.0001f));
        }

        // --- Independence from run saves ----------------------------------------------

        [Test]
        public void SettingsAndRunSavesUseDifferentFiles()
        {
            SaveService runService = new SaveService(fileSystem, paths);

            runService.Save(RunState.CreateNew(7));
            service.Save(CustomSettings());

            Assert.That(fileSystem.FileExists(paths.RunSavePath), Is.True);
            Assert.That(fileSystem.FileExists(paths.SettingsSavePath), Is.True);
            Assert.That(paths.RunSavePath, Is.Not.EqualTo(paths.SettingsSavePath));
        }

        [Test]
        public void DeletingTheRunLeavesSettingsUntouched()
        {
            SaveService runService = new SaveService(fileSystem, paths);
            runService.Save(RunState.CreateNew(7));
            service.Save(CustomSettings());

            runService.Delete();

            Assert.That(service.HasSettings(), Is.True);
            Assert.That(service.Load().MusicVolume, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void CorruptSettingsDoNotAffectTheRunSave()
        {
            SaveService runService = new SaveService(fileSystem, paths);
            runService.Save(RunState.CreateNew(7));

            fileSystem.Seed(paths.SettingsSavePath, "corrupt");

            Assert.That(service.Load(), Is.Not.Null);
            Assert.That(runService.Load().Succeeded, Is.True,
                "a broken preferences file must never cost the player their run");
        }

        [Test]
        public void ConstructorRejectsNullDependencies()
        {
            Assert.That(() => new SettingsSaveService(null, paths), Throws.ArgumentNullException);
            Assert.That(() => new SettingsSaveService(fileSystem, null), Throws.ArgumentNullException);
        }
    }
}
