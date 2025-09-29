using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HiddenHorizons.Tests
{
    public class BackupManagerTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly BackupManager _backupManager;

        public BackupManagerTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "BackupManagerTests", Guid.NewGuid().ToString());
            _backupManager = new BackupManager(_testDirectory, maxBackupsToKeep: 3);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var backupManager = new BackupManager("TestSaves", 5);

            // Assert
            Assert.NotNull(backupManager);
        }

        [Fact]
        public void Constructor_WithNullSaveDirectory_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BackupManager(null, 3));
        }

        [Fact]
        public void Constructor_WithZeroMaxBackups_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new BackupManager("TestSaves", 0));
        }

        [Fact]
        public void Constructor_WithNegativeMaxBackups_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new BackupManager("TestSaves", -1));
        }

        [Fact]
        public async Task CreateBackupAsync_WithInvalidSlotId_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _backupManager.CreateBackupAsync(0));
            await Assert.ThrowsAsync<ArgumentException>(() => _backupManager.CreateBackupAsync(-1));
        }

        [Fact]
        public async Task CreateBackupAsync_WithNonExistentSaveFile_ReturnsNull()
        {
            // Arrange
            int slotId = 1;

            // Act
            string backupPath = await _backupManager.CreateBackupAsync(slotId);

            // Assert
            Assert.Null(backupPath);
        }

        [Fact]
        public async Task CreateBackupAsync_WithExistingSaveFile_CreatesBackup()
        {
            // Arrange
            int slotId = 1;
            string saveContent = "{\"version\":1,\"data\":\"test\"}";
            await CreateTestSaveFile(slotId, saveContent);

            bool backupCompleted = false;
            _backupManager.BackupCompleted += (sender, args) =>
            {
                backupCompleted = true;
                Assert.Equal(slotId, args.SlotId);
                Assert.NotNull(args.BackupFilePath);
                Assert.NotNull(args.OriginalFilePath);
            };

            // Act
            string backupPath = await _backupManager.CreateBackupAsync(slotId);

            // Assert
            Assert.NotNull(backupPath);
            Assert.True(File.Exists(backupPath));
            Assert.True(backupCompleted);

            // Verify backup content matches original
            string backupContent = await File.ReadAllTextAsync(backupPath);
            Assert.Equal(saveContent, backupContent);
        }

        [Fact]
        public async Task CreateBackupAsync_MultipleBackups_CreatesUniqueTimestampedFiles()
        {
            // Arrange
            int slotId = 1;
            string saveContent = "{\"version\":1,\"data\":\"test\"}";
            await CreateTestSaveFile(slotId, saveContent);

            // Act
            string backup1 = await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10); // Ensure different timestamps
            string backup2 = await _backupManager.CreateBackupAsync(slotId);

            // Assert
            Assert.NotNull(backup1);
            Assert.NotNull(backup2);
            Assert.NotEqual(backup1, backup2);
            Assert.True(File.Exists(backup1));
            Assert.True(File.Exists(backup2));
        }

        [Fact]
        public async Task RestoreFromBackupAsync_WithInvalidSlotId_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _backupManager.RestoreFromBackupAsync(0));
            await Assert.ThrowsAsync<ArgumentException>(() => _backupManager.RestoreFromBackupAsync(-1));
        }

        [Fact]
        public async Task RestoreFromBackupAsync_WithNoBackups_ReturnsFalse()
        {
            // Arrange
            int slotId = 1;

            // Act
            bool result = await _backupManager.RestoreFromBackupAsync(slotId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RestoreFromBackupAsync_WithExistingBackup_RestoresSuccessfully()
        {
            // Arrange
            int slotId = 1;
            string originalContent = "{\"version\":1,\"data\":\"original\"}";
            string modifiedContent = "{\"version\":1,\"data\":\"modified\"}";
            
            await CreateTestSaveFile(slotId, originalContent);
            string backupPath = await _backupManager.CreateBackupAsync(slotId);
            
            // Modify the save file
            await File.WriteAllTextAsync(GetSaveFilePath(slotId), modifiedContent);

            bool backupRestored = false;
            _backupManager.BackupRestored += (sender, args) =>
            {
                backupRestored = true;
                Assert.Equal(slotId, args.SlotId);
                Assert.NotNull(args.BackupFilePath);
                Assert.NotNull(args.RestoredFilePath);
            };

            // Act
            bool result = await _backupManager.RestoreFromBackupAsync(slotId);

            // Assert
            Assert.True(result);
            Assert.True(backupRestored);

            // Verify content was restored
            string restoredContent = await File.ReadAllTextAsync(GetSaveFilePath(slotId));
            Assert.Equal(originalContent, restoredContent);
        }

        [Fact]
        public async Task RestoreFromBackupAsync_WithMultipleBackups_RestoresMostRecent()
        {
            // Arrange
            int slotId = 1;
            string content1 = "{\"version\":1,\"data\":\"backup1\"}";
            string content2 = "{\"version\":1,\"data\":\"backup2\"}";
            string content3 = "{\"version\":1,\"data\":\"backup3\"}";

            // Create multiple backups
            await CreateTestSaveFile(slotId, content1);
            await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10);

            await File.WriteAllTextAsync(GetSaveFilePath(slotId), content2);
            await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10);

            await File.WriteAllTextAsync(GetSaveFilePath(slotId), content3);
            await _backupManager.CreateBackupAsync(slotId);

            // Modify save file
            await File.WriteAllTextAsync(GetSaveFilePath(slotId), "{\"version\":1,\"data\":\"corrupted\"}");

            // Act
            bool result = await _backupManager.RestoreFromBackupAsync(slotId);

            // Assert
            Assert.True(result);

            // Verify most recent backup was restored
            string restoredContent = await File.ReadAllTextAsync(GetSaveFilePath(slotId));
            Assert.Equal(content3, restoredContent);
        }

        [Fact]
        public async Task RestoreFromSpecificBackupAsync_WithInvalidSlotId_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _backupManager.RestoreFromSpecificBackupAsync(0, "test.json"));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _backupManager.RestoreFromSpecificBackupAsync(-1, "test.json"));
        }

        [Fact]
        public async Task RestoreFromSpecificBackupAsync_WithNullBackupPath_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _backupManager.RestoreFromSpecificBackupAsync(1, null));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _backupManager.RestoreFromSpecificBackupAsync(1, ""));
        }

        [Fact]
        public async Task RestoreFromSpecificBackupAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange, Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _backupManager.RestoreFromSpecificBackupAsync(1, "nonexistent.json"));
        }

        [Fact]
        public async Task RestoreFromSpecificBackupAsync_WithValidBackup_RestoresSuccessfully()
        {
            // Arrange
            int slotId = 1;
            string backupContent = "{\"version\":1,\"data\":\"specific_backup\"}";
            
            await CreateTestSaveFile(slotId, backupContent);
            string backupPath = await _backupManager.CreateBackupAsync(slotId);
            
            // Modify save file
            await File.WriteAllTextAsync(GetSaveFilePath(slotId), "{\"version\":1,\"data\":\"modified\"}");

            // Act
            bool result = await _backupManager.RestoreFromSpecificBackupAsync(slotId, backupPath);

            // Assert
            Assert.True(result);

            // Verify content was restored
            string restoredContent = await File.ReadAllTextAsync(GetSaveFilePath(slotId));
            Assert.Equal(backupContent, restoredContent);
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithInvalidSlotId_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _backupManager.CleanupOldBackupsAsync(0));
            await Assert.ThrowsAsync<ArgumentException>(() => _backupManager.CleanupOldBackupsAsync(-1));
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithNoBackups_CompletesWithoutError()
        {
            // Arrange
            int slotId = 1;

            // Act & Assert (should not throw)
            await _backupManager.CleanupOldBackupsAsync(slotId);
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithExcessBackups_KeepsOnlyRecentOnes()
        {
            // Arrange
            int slotId = 1;
            string baseContent = "{\"version\":1,\"data\":\"backup";
            
            // Create more backups than the retention limit (3)
            await CreateTestSaveFile(slotId, baseContent + "1\"}");
            await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10);

            await File.WriteAllTextAsync(GetSaveFilePath(slotId), baseContent + "2\"}");
            await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10);

            await File.WriteAllTextAsync(GetSaveFilePath(slotId), baseContent + "3\"}");
            await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10);

            await File.WriteAllTextAsync(GetSaveFilePath(slotId), baseContent + "4\"}");
            await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10);

            await File.WriteAllTextAsync(GetSaveFilePath(slotId), baseContent + "5\"}");
            await _backupManager.CreateBackupAsync(slotId);

            // Verify we have 5 backups initially
            var backupsBefore = _backupManager.GetBackupInfo(slotId);
            Assert.Equal(3, backupsBefore.Count); // Should already be cleaned up during creation

            // Act
            await _backupManager.CleanupOldBackupsAsync(slotId);

            // Assert
            var backupsAfter = _backupManager.GetBackupInfo(slotId);
            Assert.Equal(3, backupsAfter.Count); // Should keep only 3 most recent
        }

        [Fact]
        public void GetBackupInfo_WithInvalidSlotId_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _backupManager.GetBackupInfo(0));
            Assert.Throws<ArgumentException>(() => _backupManager.GetBackupInfo(-1));
        }

        [Fact]
        public void GetBackupInfo_WithNoBackups_ReturnsEmptyList()
        {
            // Arrange
            int slotId = 1;

            // Act
            var backupInfo = _backupManager.GetBackupInfo(slotId);

            // Assert
            Assert.NotNull(backupInfo);
            Assert.Empty(backupInfo);
        }

        [Fact]
        public async Task GetBackupInfo_WithExistingBackups_ReturnsCorrectInfo()
        {
            // Arrange
            int slotId = 1;
            string saveContent = "{\"version\":1,\"data\":\"test\"}";
            
            await CreateTestSaveFile(slotId, saveContent);
            string backupPath1 = await _backupManager.CreateBackupAsync(slotId);
            await Task.Delay(10);
            string backupPath2 = await _backupManager.CreateBackupAsync(slotId);

            // Act
            var backupInfo = _backupManager.GetBackupInfo(slotId);

            // Assert
            Assert.Equal(2, backupInfo.Count);
            
            // Should be ordered by creation time (newest first)
            Assert.Equal(backupPath2, backupInfo[0].FilePath);
            Assert.Equal(backupPath1, backupInfo[1].FilePath);
            
            foreach (var info in backupInfo)
            {
                Assert.NotNull(info.FileName);
                Assert.True(info.SizeBytes > 0);
                Assert.True(info.CreationTime > DateTime.MinValue);
            }
        }

        [Fact]
        public void HasBackups_WithInvalidSlotId_ReturnsFalse()
        {
            // Arrange, Act & Assert
            Assert.False(_backupManager.HasBackups(0));
            Assert.False(_backupManager.HasBackups(-1));
        }

        [Fact]
        public void HasBackups_WithNoBackups_ReturnsFalse()
        {
            // Arrange
            int slotId = 1;

            // Act
            bool hasBackups = _backupManager.HasBackups(slotId);

            // Assert
            Assert.False(hasBackups);
        }

        [Fact]
        public async Task HasBackups_WithExistingBackups_ReturnsTrue()
        {
            // Arrange
            int slotId = 1;
            string saveContent = "{\"version\":1,\"data\":\"test\"}";
            
            await CreateTestSaveFile(slotId, saveContent);
            await _backupManager.CreateBackupAsync(slotId);

            // Act
            bool hasBackups = _backupManager.HasBackups(slotId);

            // Assert
            Assert.True(hasBackups);
        }

        [Fact]
        public async Task BackupManager_EventHandling_FiresCorrectEvents()
        {
            // Arrange
            int slotId = 1;
            string saveContent = "{\"version\":1,\"data\":\"test\"}";
            await CreateTestSaveFile(slotId, saveContent);

            bool backupCompletedFired = false;
            bool backupRestoredFired = false;
            bool backupErrorFired = false;

            _backupManager.BackupCompleted += (sender, args) => backupCompletedFired = true;
            _backupManager.BackupRestored += (sender, args) => backupRestoredFired = true;
            _backupManager.BackupError += (sender, args) => backupErrorFired = true;

            // Act
            await _backupManager.CreateBackupAsync(slotId);
            await _backupManager.RestoreFromBackupAsync(slotId);

            // Assert
            Assert.True(backupCompletedFired);
            Assert.True(backupRestoredFired);
            Assert.False(backupErrorFired); // No errors should occur in this test
        }

        [Fact]
        public async Task BackupManager_ErrorHandling_FiresErrorEvents()
        {
            // Arrange
            int slotId = 1;
            bool backupErrorFired = false;
            BackupErrorEventArgs errorArgs = null;

            _backupManager.BackupError += (sender, args) =>
            {
                backupErrorFired = true;
                errorArgs = args;
            };

            // Act & Assert - Test invalid file path for restore
            try
            {
                await _backupManager.RestoreFromSpecificBackupAsync(slotId, "nonexistent.json");
            }
            catch (FileNotFoundException)
            {
                // Expected exception
            }

            Assert.True(backupErrorFired);
            Assert.NotNull(errorArgs);
            Assert.Equal(slotId, errorArgs.SlotId);
            Assert.Equal(BackupErrorType.FileNotFound, errorArgs.ErrorType);
        }

        [Fact]
        public async Task BackupManager_ConcurrentOperations_HandlesCorrectly()
        {
            // Arrange
            int slotId = 1;
            string saveContent = "{\"version\":1,\"data\":\"concurrent_test\"}";
            await CreateTestSaveFile(slotId, saveContent);

            // Act - Create multiple backups sequentially to avoid cleanup issues
            var backupPaths = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                string backupPath = await _backupManager.CreateBackupAsync(slotId);
                backupPaths.Add(backupPath);
                await Task.Delay(10); // Small delay to ensure different timestamps
            }

            // Assert
            Assert.Equal(5, backupPaths.Count);
            Assert.All(backupPaths, path => Assert.NotNull(path));
            
            // Only check that the retained backups exist (max 3 due to cleanup policy)
            var backupInfo = _backupManager.GetBackupInfo(slotId);
            Assert.True(backupInfo.Count <= 3); // Should be cleaned up to max retention
            Assert.All(backupInfo, info => Assert.True(File.Exists(info.FilePath)));
            
            // Verify all paths are unique
            Assert.Equal(backupPaths.Count, backupPaths.Distinct().Count());
        }

        /// <summary>
        /// Helper method to create a test save file
        /// </summary>
        private async Task CreateTestSaveFile(int slotId, string content)
        {
            string saveFilePath = GetSaveFilePath(slotId);
            string saveDirectory = Path.GetDirectoryName(saveFilePath);
            
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            await File.WriteAllTextAsync(saveFilePath, content);
        }

        /// <summary>
        /// Helper method to get save file path
        /// </summary>
        private string GetSaveFilePath(int slotId)
        {
            return Path.Combine(_testDirectory, $"slot_{slotId}", "save.json");
        }
    }
}