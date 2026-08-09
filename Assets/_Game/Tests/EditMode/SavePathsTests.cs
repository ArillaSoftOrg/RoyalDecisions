using NUnit.Framework;
using RoyalDecisions.Infrastructure;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class SavePathsTests
    {
        private const string Root = "C:/temp/royal";

        [Test]
        public void RunAndSettingsAreSeparateFiles()
        {
            SavePaths paths = new SavePaths(Root);

            Assert.That(paths.RunSavePath, Is.Not.EqualTo(paths.SettingsSavePath),
                "losing a run must not cost the player their preferences");
            Assert.That(paths.RunSavePath, Does.EndWith(SavePaths.RunFileName));
            Assert.That(paths.SettingsSavePath, Does.EndWith(SavePaths.SettingsFileName));
        }

        [Test]
        public void TempAndBackupDeriveFromTheirTarget()
        {
            SavePaths paths = new SavePaths(Root);

            Assert.That(paths.RunTempPath,
                Is.EqualTo(paths.RunSavePath + SavePaths.TempExtension));
            Assert.That(paths.RunBackupPath,
                Is.EqualTo(paths.RunSavePath + SavePaths.BackupExtension));
            Assert.That(paths.SettingsTempPath,
                Is.EqualTo(paths.SettingsSavePath + SavePaths.TempExtension));
            Assert.That(paths.SettingsBackupPath,
                Is.EqualTo(paths.SettingsSavePath + SavePaths.BackupExtension));
        }

        [Test]
        public void EveryPathSitsUnderTheRoot()
        {
            SavePaths paths = new SavePaths(Root);

            Assert.That(paths.RunSavePath, Does.StartWith(Root));
            Assert.That(paths.RunTempPath, Does.StartWith(Root));
            Assert.That(paths.RunBackupPath, Does.StartWith(Root));
            Assert.That(paths.SettingsSavePath, Does.StartWith(Root));
        }

        [Test]
        public void BackslashesAndTrailingSlashesAreNormalized()
        {
            SavePaths paths = new SavePaths("C:\\temp\\royal\\");

            Assert.That(paths.RootDirectory, Is.EqualTo(Root));
            Assert.That(paths.RunSavePath, Is.EqualTo(Root + "/" + SavePaths.RunFileName));
        }

        [Test]
        public void AllSixPathsAreDistinct()
        {
            SavePaths paths = new SavePaths(Root);

            string[] all =
            {
                paths.RunSavePath, paths.RunTempPath, paths.RunBackupPath,
                paths.SettingsSavePath, paths.SettingsTempPath, paths.SettingsBackupPath
            };

            Assert.That(all, Is.Unique);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void AnEmptyRootIsRejected(string root)
        {
            Assert.That(() => new SavePaths(root), Throws.ArgumentException);
        }
    }
}
