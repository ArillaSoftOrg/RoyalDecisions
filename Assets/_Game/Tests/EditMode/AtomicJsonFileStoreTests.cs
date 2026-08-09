using NUnit.Framework;
using RoyalDecisions.Infrastructure;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class AtomicJsonFileStoreTests
    {
        private const string Target = "root/file.json";
        private const string Temp = "root/file.json.tmp";
        private const string Backup = "root/file.json.bak";

        private FakeFileSystem fileSystem;
        private AtomicJsonFileStore store;

        [SetUp]
        public void SetUp()
        {
            fileSystem = new FakeFileSystem();
            store = new AtomicJsonFileStore(fileSystem);
        }

        // --- Writing ---------------------------------------------------------

        [Test]
        public void Write_CreatesTheTargetFile()
        {
            SaveResult result = store.Write(Target, Temp, Backup, "payload");

            Assert.That(result.Succeeded, Is.True, result.ToString());
            Assert.That(fileSystem.Peek(Target), Is.EqualTo("payload"));
        }

        [Test]
        public void Write_LeavesNoTemporaryFileBehind()
        {
            store.Write(Target, Temp, Backup, "payload");

            Assert.That(fileSystem.FileExists(Temp), Is.False,
                "the temp file must be consumed by the swap");
        }

        [Test]
        public void Write_OverExistingFile_KeepsThePreviousContentAsBackup()
        {
            store.Write(Target, Temp, Backup, "first");
            store.Write(Target, Temp, Backup, "second");

            Assert.That(fileSystem.Peek(Target), Is.EqualTo("second"));
            Assert.That(fileSystem.Peek(Backup), Is.EqualTo("first"));
            Assert.That(fileSystem.FileExists(Temp), Is.False);
        }

        [Test]
        public void Write_CreatesTheDirectoryWhenMissing()
        {
            store.Write(Target, Temp, Backup, "payload");

            Assert.That(fileSystem.DirectoryExists("root"), Is.True);
        }

        [Test]
        public void FailedWrite_LeavesTheExistingFileByteIdentical()
        {
            store.Write(Target, Temp, Backup, "original");

            fileSystem.FailAllWrites = true;
            SaveResult result = store.Write(Target, Temp, Backup, "replacement");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Status, Is.EqualTo(SaveStatus.WriteFailed));
            Assert.That(fileSystem.Peek(Target), Is.EqualTo("original"),
                "a failed write must never damage the save already on disk");
        }

        [Test]
        public void FailedWrite_RemovesItsTemporaryFile()
        {
            fileSystem.Seed(Temp, "leftover from an earlier crash");
            fileSystem.FailAllWrites = true;

            store.Write(Target, Temp, Backup, "payload");

            Assert.That(fileSystem.FileExists(Temp), Is.False);
        }

        [Test]
        public void PartialWrite_LeavesTheExistingFileIntactAndClearsTheTemp()
        {
            fileSystem.Seed(Target, "original");
            fileSystem.FailWritesAfterStoring = true;

            SaveResult result = store.Write(Target, Temp, Backup, "replacement");

            Assert.That(result.Status, Is.EqualTo(SaveStatus.WriteFailed));
            Assert.That(fileSystem.Peek(Target), Is.EqualTo("original"),
                "a half-written temp file must never reach the target");
            Assert.That(fileSystem.FileExists(Temp), Is.False);
        }

        [Test]
        public void FailedWrite_ReportsACleanupFailureWithoutHidingTheOriginalError()
        {
            fileSystem.Seed(Target, "original");
            fileSystem.FailWritesAfterStoring = true;
            fileSystem.FailAllDeletes = true;

            SaveResult result = store.Write(Target, Temp, Backup, "replacement");

            Assert.That(result.Status, Is.EqualTo(SaveStatus.WriteFailed));
            Assert.That(result.Message, Does.Contain("fail writes"),
                "the write failure must still be the headline");
            Assert.That(result.Message, Does.Contain("could also not be removed"));
        }

        [Test]
        public void Write_ClearsAnOrphanedTemporaryFileFromAPreviousRun()
        {
            fileSystem.Seed(Temp, "orphan");

            SaveResult result = store.Write(Target, Temp, Backup, "payload");

            Assert.That(result.Succeeded, Is.True, result.ToString());
            Assert.That(fileSystem.Peek(Target), Is.EqualTo("payload"));
        }

        // --- Reading -----------------------------------------------------------

        [Test]
        public void Read_MissingFile_ReportsNoSaveFile()
        {
            TextReadResult result = store.Read(Target);

            Assert.That(result.HasText, Is.False);
            Assert.That(result.Status, Is.EqualTo(LoadStatus.NoSaveFile));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\n\t ")]
        public void Read_EmptyFile_ReportsEmpty(string contents)
        {
            fileSystem.Seed(Target, contents);

            TextReadResult result = store.Read(Target);

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Empty));
            Assert.That(result.HasText, Is.False);
        }

        [Test]
        public void Read_ReturnsTheStoredText()
        {
            fileSystem.Seed(Target, "payload");

            TextReadResult result = store.Read(Target);

            Assert.That(result.HasText, Is.True);
            Assert.That(result.Text, Is.EqualTo("payload"));
        }

        [Test]
        public void Read_WhenTheFileSystemThrows_ReportsReadFailed()
        {
            fileSystem.Seed(Target, "payload");
            fileSystem.FailAllReads = true;

            TextReadResult result = store.Read(Target);

            Assert.That(result.Status, Is.EqualTo(LoadStatus.ReadFailed));
            Assert.That(result.Message, Is.Not.Empty);
        }

        [Test]
        public void Read_NeverThrows()
        {
            fileSystem.FailAllReads = true;

            Assert.That(() => store.Read(Target), Throws.Nothing);
        }

        // --- Deleting ------------------------------------------------------------

        [Test]
        public void DeleteIfPresent_ReportsWhetherAnythingWasRemoved()
        {
            Assert.That(store.DeleteIfPresent(Target), Is.False);

            fileSystem.Seed(Target, "payload");

            Assert.That(store.DeleteIfPresent(Target), Is.True);
            Assert.That(fileSystem.FileExists(Target), Is.False);
        }
    }
}
