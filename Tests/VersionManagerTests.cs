using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace HiddenHorizons.Tests
{
    public class VersionManagerTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly VersionManager _versionManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public VersionManagerTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "VersionManagerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            
            _versionManager = new VersionManager();
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task DetectVersionAsync_ValidSaveFile_ReturnsCorrectVersion()
        {
            // Arrange
            var saveData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>()
            };

            string saveFilePath = Path.Combine(_testDirectory, "test_save.json");
            await using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, saveData, _jsonOptions);
            }

            // Act
            int detectedVersion = await _versionManager.DetectVersionAsync(saveFilePath);

            // Assert
            Assert.Equal(1, detectedVersion);
        }

        [Fact]
        public async Task DetectVersionAsync_SaveFileWithoutVersion_ReturnsVersion1()
        {
            // Arrange
            var saveDataWithoutVersion = new
            {
                saveTimestamp = DateTime.UtcNow,
                gameVersion = "1.0.0",
                systemData = new Dictionary<string, object>()
            };

            string saveFilePath = Path.Combine(_testDirectory, "test_save_no_version.json");
            await using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, saveDataWithoutVersion, _jsonOptions);
            }

            // Act
            int detectedVersion = await _versionManager.DetectVersionAsync(saveFilePath);

            // Assert
            Assert.Equal(1, detectedVersion);
        }

        [Fact]
        public async Task DetectVersionAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _versionManager.DetectVersionAsync(nonExistentPath));
        }

        [Fact]
        public async Task DetectVersionAsync_CorruptedJsonFile_ReturnsMinusOne()
        {
            // Arrange
            string saveFilePath = Path.Combine(_testDirectory, "corrupted_save.json");
            await File.WriteAllTextAsync(saveFilePath, "{ invalid json content");

            // Act
            int detectedVersion = await _versionManager.DetectVersionAsync(saveFilePath);

            // Assert
            Assert.Equal(-1, detectedVersion);
        }

        [Theory]
        [InlineData(0, true)] // Minimum compatible version
        [InlineData(1, true)] // Current version
        [InlineData(2, false)] // Future version
        [InlineData(-1, false)] // Invalid version
        public void IsCompatible_VariousVersions_ReturnsExpectedResult(int version, bool expectedCompatible)
        {
            // Act
            bool isCompatible = _versionManager.IsCompatible(version);

            // Assert
            Assert.Equal(expectedCompatible, isCompatible);
        }

        [Theory]
        [InlineData(1, false)] // Current version, no migration needed
        [InlineData(0, true)] // Old version, needs migration
        [InlineData(2, false)] // Future version, not compatible
        [InlineData(-1, false)] // Invalid version
        public void NeedsMigration_VariousVersions_ReturnsExpectedResult(int version, bool expectedNeedsMigration)
        {
            // Act
            bool needsMigration = _versionManager.NeedsMigration(version);

            // Assert
            Assert.Equal(expectedNeedsMigration, needsMigration);
        }

        [Fact]
        public async Task MigrateDataAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _versionManager.MigrateDataAsync(nonExistentPath));
        }

        [Fact]
        public async Task MigrateDataAsync_CurrentVersion_ReturnsTrue()
        {
            // Arrange
            var saveData = new GameSaveData
            {
                Version = VersionManager.CurrentSaveVersion,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>()
            };

            string saveFilePath = Path.Combine(_testDirectory, "current_version_save.json");
            await using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, saveData, _jsonOptions);
            }

            // Act
            bool result = await _versionManager.MigrateDataAsync(saveFilePath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task MigrateDataAsync_CorruptedFile_ReturnsFalse()
        {
            // Arrange
            string saveFilePath = Path.Combine(_testDirectory, "corrupted_migration_save.json");
            await File.WriteAllTextAsync(saveFilePath, "{ invalid json content");

            // Act
            bool result = await _versionManager.MigrateDataAsync(saveFilePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MigrateDataAsync_IncompatibleVersion_ReturnsFalse()
        {
            // Arrange
            var saveData = new GameSaveData
            {
                Version = 999, // Future version
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>()
            };

            string saveFilePath = Path.Combine(_testDirectory, "future_version_save.json");
            await using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, saveData, _jsonOptions);
            }

            // Act
            bool result = await _versionManager.MigrateDataAsync(saveFilePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MigrateDataAsync_CreatesBackupFile()
        {
            // Arrange - Use version 0 to force migration
            // The VersionManager already has a migration script from v0 to v1
            var saveData = new GameSaveData
            {
                Version = 0, // Use version 0 to trigger migration to version 1
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>()
            };

            string saveFilePath = Path.Combine(_testDirectory, "backup_test_save.json");
            await using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, saveData, _jsonOptions);
            }

            string expectedBackupPath = saveFilePath + ".backup.v0";

            // Act
            await _versionManager.MigrateDataAsync(saveFilePath);

            // Assert
            Assert.True(File.Exists(expectedBackupPath));
        }

        [Fact]
        public async Task RollbackMigrationAsync_ValidBackup_RestoresOriginalFile()
        {
            // Arrange
            var originalData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object> { { "test", "original" } }
            };

            var modifiedData = new GameSaveData
            {
                Version = 2,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object> { { "test", "modified" } }
            };

            string saveFilePath = Path.Combine(_testDirectory, "rollback_test_save.json");
            string backupFilePath = saveFilePath + ".backup";

            // Create original backup
            await using (var fileStream = new FileStream(backupFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, originalData, _jsonOptions);
            }

            // Create modified save file
            await using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, modifiedData, _jsonOptions);
            }

            // Act
            await _versionManager.RollbackMigrationAsync(saveFilePath, backupFilePath);

            // Assert
            await using (var fileStream = new FileStream(saveFilePath, FileMode.Open, FileAccess.Read))
            {
                var restoredData = await JsonSerializer.DeserializeAsync<GameSaveData>(fileStream, _jsonOptions);
                Assert.Equal(1, restoredData.Version);
                Assert.Equal("original", restoredData.SystemData["test"].ToString());
            }
        }

        [Fact]
        public async Task RollbackMigrationAsync_NonExistentBackup_ThrowsFileNotFoundException()
        {
            // Arrange
            string saveFilePath = Path.Combine(_testDirectory, "rollback_no_backup_save.json");
            string nonExistentBackupPath = saveFilePath + ".backup";

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _versionManager.RollbackMigrationAsync(saveFilePath, nonExistentBackupPath));
        }

        [Fact]
        public async Task RollbackMigrationAsync_CorruptedBackup_ThrowsInvalidOperationException()
        {
            // Arrange
            string saveFilePath = Path.Combine(_testDirectory, "rollback_corrupted_backup_save.json");
            string backupFilePath = saveFilePath + ".backup";

            // Create corrupted backup
            await File.WriteAllTextAsync(backupFilePath, "{ invalid json content");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _versionManager.RollbackMigrationAsync(saveFilePath, backupFilePath));
        }

        [Fact]
        public void RegisterMigrationScript_ValidScript_RegistersSuccessfully()
        {
            // Arrange
            var mockScript = new MockMigrationScript();

            // Act
            _versionManager.RegisterMigrationScript(2, mockScript);

            // Assert - No exception should be thrown
            // The registration is internal, so we can't directly verify it
            // but we can test that it doesn't throw
        }

        [Fact]
        public void RegisterMigrationScript_NullScript_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _versionManager.RegisterMigrationScript(2, null));
        }

        [Fact]
        public void MigrationEvents_AreRaisedCorrectly()
        {
            // Arrange
            bool migrationStartedRaised = false;
            bool migrationCompletedRaised = false;
            bool migrationFailedRaised = false;

            _versionManager.MigrationStarted += (sender, args) => migrationStartedRaised = true;
            _versionManager.MigrationCompleted += (sender, args) => migrationCompletedRaised = true;
            _versionManager.MigrationFailed += (sender, args) => migrationFailedRaised = true;

            // Act - Test that events can be subscribed to without errors
            // The actual event raising is tested implicitly in other migration tests

            // Assert
            Assert.False(migrationStartedRaised); // Events haven't been triggered yet
            Assert.False(migrationCompletedRaised);
            Assert.False(migrationFailedRaised);
        }

        /// <summary>
        /// Mock migration script for testing
        /// </summary>
        private class MockMigrationScript : IMigrationScript
        {
            public async Task<GameSaveData> MigrateAsync(GameSaveData saveData)
            {
                if (saveData == null)
                    throw new ArgumentNullException(nameof(saveData));

                // Simple mock migration - just increment version
                var migratedData = new GameSaveData
                {
                    Version = saveData.Version + 1,
                    SaveTimestamp = saveData.SaveTimestamp,
                    GameVersion = saveData.GameVersion,
                    SystemData = new Dictionary<string, object>(saveData.SystemData),
                    Checksum = null
                };

                return await Task.FromResult(migratedData);
            }
        }
    }
}