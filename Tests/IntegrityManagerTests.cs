using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using HiddenHorizons;

namespace HiddenHorizons.Tests
{
    public class IntegrityManagerTests : IDisposable
    {
        private readonly IntegrityManager _integrityManager;
        private readonly string _testDirectory;

        public IntegrityManagerTests()
        {
            _integrityManager = new IntegrityManager();
            _testDirectory = Path.Combine(Path.GetTempPath(), "IntegrityManagerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void GenerateChecksum_WithValidData_ReturnsConsistentChecksum()
        {
            // Arrange
            string testData = "test data for checksum";

            // Act
            string checksum1 = _integrityManager.GenerateChecksum(testData);
            string checksum2 = _integrityManager.GenerateChecksum(testData);

            // Assert
            Assert.NotNull(checksum1);
            Assert.NotEmpty(checksum1);
            Assert.Equal(checksum1, checksum2);
            Assert.Equal(64, checksum1.Length); // SHA-256 produces 64 character hex string
        }

        [Fact]
        public void GenerateChecksum_WithEmptyString_ReturnsEmptyString()
        {
            // Act
            string checksum = _integrityManager.GenerateChecksum("");

            // Assert
            Assert.Equal(string.Empty, checksum);
        }

        [Fact]
        public void GenerateChecksum_WithNull_ReturnsEmptyString()
        {
            // Act
            string checksum = _integrityManager.GenerateChecksum(null);

            // Assert
            Assert.Equal(string.Empty, checksum);
        }

        [Fact]
        public void GenerateChecksum_WithDifferentData_ReturnsDifferentChecksums()
        {
            // Arrange
            string data1 = "test data 1";
            string data2 = "test data 2";

            // Act
            string checksum1 = _integrityManager.GenerateChecksum(data1);
            string checksum2 = _integrityManager.GenerateChecksum(data2);

            // Assert
            Assert.NotEqual(checksum1, checksum2);
        }

        [Fact]
        public void ValidateChecksum_WithMatchingChecksum_ReturnsTrue()
        {
            // Arrange
            string testData = "test data for validation";
            string expectedChecksum = _integrityManager.GenerateChecksum(testData);

            // Act
            bool isValid = _integrityManager.ValidateChecksum(testData, expectedChecksum);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateChecksum_WithNonMatchingChecksum_ReturnsFalse()
        {
            // Arrange
            string testData = "test data for validation";
            string wrongChecksum = "wrong_checksum";

            // Act
            bool isValid = _integrityManager.ValidateChecksum(testData, wrongChecksum);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateChecksum_WithEmptyChecksum_ReturnsFalse()
        {
            // Arrange
            string testData = "test data for validation";

            // Act
            bool isValid = _integrityManager.ValidateChecksum(testData, "");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateChecksum_WithNullChecksum_ReturnsFalse()
        {
            // Arrange
            string testData = "test data for validation";

            // Act
            bool isValid = _integrityManager.ValidateChecksum(testData, null);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateChecksum_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            string testData = "test data for validation";
            string checksum = _integrityManager.GenerateChecksum(testData);
            string uppercaseChecksum = checksum.ToUpperInvariant();

            // Act
            bool isValid = _integrityManager.ValidateChecksum(testData, uppercaseChecksum);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void WriteFileAtomically_WithValidData_CreatesFile()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "test_save.json");
            var saveData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>
                {
                    { "test_key", "test_value" }
                }
            };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(saveData);

            // Act
            _integrityManager.WriteFileAtomically(filePath, jsonData);

            // Assert
            Assert.True(File.Exists(filePath));
            string fileContent = File.ReadAllText(filePath);
            Assert.NotEmpty(fileContent);
            
            // Verify checksum was added
            var loadedData = System.Text.Json.JsonSerializer.Deserialize<GameSaveData>(fileContent);
            Assert.NotNull(loadedData);
            Assert.NotNull(loadedData.Checksum);
            Assert.NotEmpty(loadedData.Checksum);
        }

        [Fact]
        public void WriteFileAtomically_WithoutChecksum_CreatesFileWithoutChecksum()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "test_save_no_checksum.json");
            string testData = "simple test data";

            // Act
            _integrityManager.WriteFileAtomically(filePath, testData, generateChecksum: false);

            // Assert
            Assert.True(File.Exists(filePath));
            string fileContent = File.ReadAllText(filePath);
            Assert.Equal(testData, fileContent);
        }

        [Fact]
        public void WriteFileAtomically_CreatesDirectoryIfNotExists()
        {
            // Arrange
            string subDirectory = Path.Combine(_testDirectory, "subdir", "nested");
            string filePath = Path.Combine(subDirectory, "test_save.json");
            var saveData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>()
            };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(saveData);

            // Act
            _integrityManager.WriteFileAtomically(filePath, jsonData);

            // Assert
            Assert.True(Directory.Exists(subDirectory));
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void WriteFileAtomically_WithNullFilePath_ThrowsArgumentException()
        {
            // Arrange
            string testData = "test data";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _integrityManager.WriteFileAtomically(null, testData));
        }

        [Fact]
        public void WriteFileAtomically_WithEmptyFilePath_ThrowsArgumentException()
        {
            // Arrange
            string testData = "test data";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _integrityManager.WriteFileAtomically("", testData));
        }

        [Fact]
        public void WriteFileAtomically_WithNullData_ThrowsArgumentNullException()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "test_save.json");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _integrityManager.WriteFileAtomically(filePath, null));
        }

        [Fact]
        public void VerifyIntegrity_WithValidFile_ReturnsTrue()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "valid_save.json");
            var saveData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>
                {
                    { "adventurer", "test_data" }
                }
            };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(saveData);
            _integrityManager.WriteFileAtomically(filePath, jsonData);

            // Act
            bool isValid = _integrityManager.VerifyIntegrity(filePath);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifyIntegrity_WithCorruptedFile_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "corrupted_save.json");
            var saveData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>
                {
                    { "adventurer", "test_data" }
                },
                Checksum = "invalid_checksum"
            };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(saveData);
            File.WriteAllText(filePath, jsonData);

            // Act
            bool isValid = _integrityManager.VerifyIntegrity(filePath);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyIntegrity_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "nonexistent_save.json");

            // Act
            bool isValid = _integrityManager.VerifyIntegrity(filePath);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyIntegrity_WithFileWithoutChecksum_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "no_checksum_save.json");
            var saveData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object>()
            };
            string jsonData = System.Text.Json.JsonSerializer.Serialize(saveData);
            File.WriteAllText(filePath, jsonData);

            // Act
            bool isValid = _integrityManager.VerifyIntegrity(filePath);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyIntegrity_WithInvalidJsonFile_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "invalid_json_save.json");
            File.WriteAllText(filePath, "invalid json content {");

            // Act
            bool isValid = _integrityManager.VerifyIntegrity(filePath);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ReadFileWithLock_WithExistingFile_ReturnsContent()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "test_read.txt");
            string expectedContent = "test file content";
            File.WriteAllText(filePath, expectedContent);

            // Act
            string actualContent = _integrityManager.ReadFileWithLock(filePath);

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [Fact]
        public void ReadFileWithLock_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "nonexistent_file.txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _integrityManager.ReadFileWithLock(filePath));
        }

        [Fact]
        public void IsFileLocked_WithUnlockedFile_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "unlocked_file.txt");
            File.WriteAllText(filePath, "test content");

            // Act
            bool isLocked = _integrityManager.IsFileLocked(filePath);

            // Assert
            Assert.False(isLocked);
        }

        [Fact]
        public void IsFileLocked_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "nonexistent_file.txt");

            // Act
            bool isLocked = _integrityManager.IsFileLocked(filePath);

            // Assert
            Assert.False(isLocked);
        }

        [Fact]
        public void WaitForFileAvailable_WithAvailableFile_ReturnsTrue()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "available_file.txt");
            File.WriteAllText(filePath, "test content");

            // Act
            bool isAvailable = _integrityManager.WaitForFileAvailable(filePath, 1000);

            // Assert
            Assert.True(isAvailable);
        }

        [Fact]
        public void WaitForFileAvailable_WithNonExistentFile_ReturnsTrue()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "nonexistent_file.txt");

            // Act
            bool isAvailable = _integrityManager.WaitForFileAvailable(filePath, 1000);

            // Assert
            Assert.True(isAvailable);
        }

        [Fact]
        public void ChecksumGeneration_WithComplexGameData_ProducesValidChecksum()
        {
            // Arrange
            var complexSaveData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = new DateTime(2024, 1, 15, 10, 30, 45),
                GameVersion = "1.2.3",
                SystemData = new Dictionary<string, object>
                {
                    { "adventurer", new { Position = new { X = 123.456f, Y = 789.012f }, Health = 100 } },
                    { "journal", new { Entries = new[] { "Entry 1", "Entry 2" }, TotalDays = 5 } },
                    { "weather", new { Type = "Sunny", Intensity = 0.75f } }
                }
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(complexSaveData);

            // Act
            string checksum1 = _integrityManager.GenerateChecksum(jsonData);
            string checksum2 = _integrityManager.GenerateChecksum(jsonData);

            // Assert
            Assert.NotNull(checksum1);
            Assert.NotEmpty(checksum1);
            Assert.Equal(checksum1, checksum2);
            Assert.Equal(64, checksum1.Length);
        }

        [Fact]
        public void AtomicWrite_WithExistingFile_OverwritesCorrectly()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "overwrite_test.json");
            var originalData = new GameSaveData
            {
                Version = 1,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "1.0.0",
                SystemData = new Dictionary<string, object> { { "original", "data" } }
            };
            var newData = new GameSaveData
            {
                Version = 2,
                SaveTimestamp = DateTime.UtcNow,
                GameVersion = "2.0.0",
                SystemData = new Dictionary<string, object> { { "new", "data" } }
            };

            string originalJson = System.Text.Json.JsonSerializer.Serialize(originalData);
            string newJson = System.Text.Json.JsonSerializer.Serialize(newData);

            // Act
            _integrityManager.WriteFileAtomically(filePath, originalJson);
            _integrityManager.WriteFileAtomically(filePath, newJson);

            // Assert
            Assert.True(File.Exists(filePath));
            string fileContent = File.ReadAllText(filePath);
            var loadedData = System.Text.Json.JsonSerializer.Deserialize<GameSaveData>(fileContent);
            Assert.NotNull(loadedData);
            Assert.Equal(2, loadedData.Version);
            Assert.Equal("2.0.0", loadedData.GameVersion);
        }
    }
}