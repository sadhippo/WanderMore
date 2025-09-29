using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Xunit;
using HiddenHorizons;

namespace HiddenHorizons.Tests
{
    public class SaveManagerTests : IDisposable
    {
        private readonly SaveManager _saveManager;
        private readonly string _testSaveDirectory;
        private readonly SaveManagerTestSystem _testSystem1;
        private readonly SaveManagerTestSystem _testSystem2;

        public SaveManagerTests()
        {
            // Create a unique test directory for each test
            _testSaveDirectory = Path.Combine(Path.GetTempPath(), "SaveManagerTests", Guid.NewGuid().ToString());
            _saveManager = new SaveManager(_testSaveDirectory);
            
            _testSystem1 = new SaveManagerTestSystem("TestSystem1", "Test data 1", 1);
            _testSystem2 = new SaveManagerTestSystem("TestSystem2", "Test data 2", 1);
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testSaveDirectory))
            {
                Directory.Delete(_testSaveDirectory, true);
            }
        }

        [Fact]
        public void Constructor_CreatesDirectoryIfNotExists()
        {
            // Arrange & Act
            var saveManager = new SaveManager(_testSaveDirectory);

            // Assert
            Assert.True(Directory.Exists(_testSaveDirectory));
        }

        [Fact]
        public void RegisterSaveable_ValidSystem_RegistersSuccessfully()
        {
            // Act
            _saveManager.RegisterSaveable(_testSystem1);

            // Assert
            Assert.Equal(1, _saveManager.RegisteredSystemCount);
            Assert.True(_saveManager.IsSystemRegistered("TestSystem1"));
        }

        [Fact]
        public void RegisterSaveable_MultipleValidSystems_RegistersAll()
        {
            // Act
            _saveManager.RegisterSaveable(_testSystem1);
            _saveManager.RegisterSaveable(_testSystem2);

            // Assert
            Assert.Equal(2, _saveManager.RegisteredSystemCount);
            Assert.True(_saveManager.IsSystemRegistered("TestSystem1"));
            Assert.True(_saveManager.IsSystemRegistered("TestSystem2"));
        }

        [Fact]
        public void RegisterSaveable_NullSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _saveManager.RegisterSaveable(null));
        }

        [Fact]
        public void RegisterSaveable_EmptySaveKey_ThrowsArgumentException()
        {
            // Arrange
            var invalidSystem = new SaveManagerTestSystem("", "data", 1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _saveManager.RegisterSaveable(invalidSystem));
        }

        [Fact]
        public void RegisterSaveable_DuplicateSaveKey_ThrowsArgumentException()
        {
            // Arrange
            var system1 = new SaveManagerTestSystem("DuplicateKey", "data1", 1);
            var system2 = new SaveManagerTestSystem("DuplicateKey", "data2", 1);

            // Act
            _saveManager.RegisterSaveable(system1);

            // Assert
            Assert.Throws<ArgumentException>(() => _saveManager.RegisterSaveable(system2));
        }

        [Fact]
        public void UnregisterSaveable_ExistingSystem_RemovesSuccessfully()
        {
            // Arrange
            _saveManager.RegisterSaveable(_testSystem1);
            _saveManager.RegisterSaveable(_testSystem2);

            // Act
            bool result = _saveManager.UnregisterSaveable("TestSystem1");

            // Assert
            Assert.True(result);
            Assert.Equal(1, _saveManager.RegisteredSystemCount);
            Assert.False(_saveManager.IsSystemRegistered("TestSystem1"));
            Assert.True(_saveManager.IsSystemRegistered("TestSystem2"));
        }

        [Fact]
        public void UnregisterSaveable_NonExistentSystem_ReturnsFalse()
        {
            // Act
            bool result = _saveManager.UnregisterSaveable("NonExistent");

            // Assert
            Assert.False(result);
            Assert.Equal(0, _saveManager.RegisteredSystemCount);
        }

        [Fact]
        public async Task SaveGameAsync_ValidSlot_SavesSuccessfully()
        {
            // Arrange
            _saveManager.RegisterSaveable(_testSystem1);
            _saveManager.RegisterSaveable(_testSystem2);
            bool saveCompletedFired = false;
            SaveCompletedEventArgs saveArgs = null;

            _saveManager.SaveCompleted += (sender, args) =>
            {
                saveCompletedFired = true;
                saveArgs = args;
            };

            // Act
            await _saveManager.SaveGameAsync(1);

            // Assert
            Assert.True(_saveManager.SaveExists(1));
            Assert.True(saveCompletedFired);
            Assert.NotNull(saveArgs);
            Assert.Equal(1, saveArgs.SlotId);
            Assert.Equal(2, saveArgs.SystemCount);
        }

        [Fact]
        public async Task SaveGameAsync_InvalidSlot_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _saveManager.SaveGameAsync(0));
        }

        [Fact]
        public async Task LoadGameAsync_ExistingSave_LoadsSuccessfully()
        {
            // Arrange
            _saveManager.RegisterSaveable(_testSystem1);
            _saveManager.RegisterSaveable(_testSystem2);
            
            // Save first
            await _saveManager.SaveGameAsync(1);
            
            // Modify test systems to verify loading
            _testSystem1.TestData = "Modified data 1";
            _testSystem2.TestData = "Modified data 2";
            
            bool loadCompletedFired = false;
            LoadCompletedEventArgs loadArgs = null;

            _saveManager.LoadCompleted += (sender, args) =>
            {
                loadCompletedFired = true;
                loadArgs = args;
            };

            // Act
            await _saveManager.LoadGameAsync(1);

            // Assert
            Assert.True(loadCompletedFired);
            Assert.NotNull(loadArgs);
            Assert.Equal(1, loadArgs.SlotId);
            Assert.Equal(2, loadArgs.SystemCount);
            
            // Verify data was restored
            Assert.Equal("Test data 1", _testSystem1.TestData);
            Assert.Equal("Test data 2", _testSystem2.TestData);
        }

        [Fact]
        public async Task LoadGameAsync_InvalidSlot_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _saveManager.LoadGameAsync(0));
        }

        [Fact]
        public async Task LoadGameAsync_NonExistentSave_ThrowsFileNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _saveManager.LoadGameAsync(999));
        }

        [Fact]
        public void SaveExists_ExistingSave_ReturnsTrue()
        {
            // Arrange
            _saveManager.RegisterSaveable(_testSystem1);

            // Act & Assert (before save)
            Assert.False(_saveManager.SaveExists(1));

            // Save and check again
            _saveManager.SaveGameAsync(1).Wait();
            Assert.True(_saveManager.SaveExists(1));
        }

        [Fact]
        public void SaveExists_NonExistentSave_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(_saveManager.SaveExists(999));
        }

        [Fact]
        public async Task SaveGameAsync_SystemThrowsException_ContinuesWithOtherSystems()
        {
            // Arrange
            var faultySystem = new FaultySaveableSystem("FaultySystem", 1);
            _saveManager.RegisterSaveable(_testSystem1);
            _saveManager.RegisterSaveable(faultySystem);
            _saveManager.RegisterSaveable(_testSystem2);

            bool errorFired = false;
            _saveManager.SaveError += (sender, args) => errorFired = true;

            // Act
            await _saveManager.SaveGameAsync(1);

            // Assert
            Assert.True(errorFired);
            Assert.True(_saveManager.SaveExists(1));
            
            // Verify the good systems were still saved
            await _saveManager.LoadGameAsync(1);
            Assert.Equal("Test data 1", _testSystem1.TestData);
            Assert.Equal("Test data 2", _testSystem2.TestData);
        }

        [Fact]
        public async Task LoadGameAsync_SystemThrowsException_ContinuesWithOtherSystems()
        {
            // Arrange
            _saveManager.RegisterSaveable(_testSystem1);
            _saveManager.RegisterSaveable(_testSystem2);
            
            // Save first
            await _saveManager.SaveGameAsync(1);
            
            // Replace one system with a faulty one
            _saveManager.UnregisterSaveable("TestSystem1");
            var faultySystem = new FaultySaveableSystem("TestSystem1", 1);
            _saveManager.RegisterSaveable(faultySystem);

            bool errorFired = false;
            _saveManager.SaveError += (sender, args) => errorFired = true;

            // Act
            await _saveManager.LoadGameAsync(1);

            // Assert
            Assert.True(errorFired);
            // System2 should still load correctly
            Assert.Equal("Test data 2", _testSystem2.TestData);
        }

        [Fact]
        public async Task SaveAndLoad_MultipleSlots_WorksIndependently()
        {
            // Arrange
            _saveManager.RegisterSaveable(_testSystem1);
            
            // Save to slot 1
            _testSystem1.TestData = "Slot 1 data";
            await _saveManager.SaveGameAsync(1);
            
            // Save to slot 2
            _testSystem1.TestData = "Slot 2 data";
            await _saveManager.SaveGameAsync(2);

            // Act & Assert
            // Load slot 1
            await _saveManager.LoadGameAsync(1);
            Assert.Equal("Slot 1 data", _testSystem1.TestData);
            
            // Load slot 2
            await _saveManager.LoadGameAsync(2);
            Assert.Equal("Slot 2 data", _testSystem1.TestData);
        }
    }

    /// <summary>
    /// Test implementation of ISaveable for SaveManager unit testing
    /// </summary>
    public class SaveManagerTestSystem : ISaveable
    {
        public string SaveKey { get; }
        public string TestData { get; set; }
        public int SaveVersion { get; }

        public SaveManagerTestSystem(string saveKey, string testData, int saveVersion)
        {
            SaveKey = saveKey;
            TestData = testData;
            SaveVersion = saveVersion;
        }

        public object GetSaveData()
        {
            return new { TestData = TestData };
        }

        public void LoadSaveData(object data)
        {
            if (data is JsonElement jsonElement)
            {
                if (jsonElement.TryGetProperty("testData", out var testDataProperty))
                {
                    TestData = testDataProperty.GetString();
                }
            }
            else if (data is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("TestData", out var testDataValue))
                {
                    TestData = testDataValue?.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Test implementation that throws exceptions for error testing
    /// </summary>
    public class FaultySaveableSystem : ISaveable
    {
        public string SaveKey { get; }
        public int SaveVersion { get; }

        public FaultySaveableSystem(string saveKey, int saveVersion)
        {
            SaveKey = saveKey;
            SaveVersion = saveVersion;
        }

        public object GetSaveData()
        {
            throw new InvalidOperationException("Simulated save error");
        }

        public void LoadSaveData(object data)
        {
            throw new InvalidOperationException("Simulated load error");
        }
    }
}