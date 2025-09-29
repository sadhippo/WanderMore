using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace HiddenHorizons.Tests
{
    /// <summary>
    /// Unit tests for save system data structures serialization and deserialization
    /// </summary>
    public class SaveSystemDataStructuresTests
    {
        [Fact]
        public void GameSaveData_SerializationRoundTrip_PreservesAllData()
        {
            // Arrange
            var originalData = new GameSaveData
            {
                Version = 2,
                SaveTimestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc),
                GameVersion = "1.2.3",
                Checksum = "abc123def456"
            };
            
            originalData.SystemData["adventurer"] = new { Position = new { X = 100.5f, Y = 200.3f } };
            originalData.SystemData["journal"] = new { EntryCount = 42 };

            // Act
            string json = JsonSerializer.Serialize(originalData);
            var deserializedData = JsonSerializer.Deserialize<GameSaveData>(json);

            // Assert
            Assert.NotNull(deserializedData);
            Assert.Equal(originalData.Version, deserializedData.Version);
            Assert.Equal(originalData.SaveTimestamp, deserializedData.SaveTimestamp);
            Assert.Equal(originalData.GameVersion, deserializedData.GameVersion);
            Assert.Equal(originalData.Checksum, deserializedData.Checksum);
            Assert.Equal(originalData.SystemData.Count, deserializedData.SystemData.Count);
            Assert.True(deserializedData.SystemData.ContainsKey("adventurer"));
            Assert.True(deserializedData.SystemData.ContainsKey("journal"));
        }

        [Fact]
        public void GameSaveData_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var saveData = new GameSaveData();

            // Assert
            Assert.Equal(1, saveData.Version);
            Assert.NotNull(saveData.SystemData);
            Assert.Empty(saveData.SystemData);
            Assert.Equal("1.0.0", saveData.GameVersion);
            Assert.True(saveData.SaveTimestamp > DateTime.UtcNow.AddMinutes(-1));
            Assert.True(saveData.SaveTimestamp <= DateTime.UtcNow);
        }

        [Fact]
        public void SaveSlotMetadata_SerializationRoundTrip_PreservesAllData()
        {
            // Arrange
            var originalMetadata = new SaveSlotMetadata
            {
                SlotId = 3,
                LastSaveTime = new DateTime(2024, 2, 20, 14, 25, 30, DateTimeKind.Utc),
                PlayTime = new TimeSpan(5, 30, 45), // 5 hours, 30 minutes, 45 seconds
                CurrentDay = 15,
                CurrentZoneName = "Mystic Forest",
                CurrentBiome = BiomeType.Forest,
                ZonesVisited = 8,
                JournalEntries = 23,
                GameVersion = "1.5.2",
                FileSizeBytes = 1024768
            };

            // Act
            string json = JsonSerializer.Serialize(originalMetadata);
            var deserializedMetadata = JsonSerializer.Deserialize<SaveSlotMetadata>(json);

            // Assert
            Assert.NotNull(deserializedMetadata);
            Assert.Equal(originalMetadata.SlotId, deserializedMetadata.SlotId);
            Assert.Equal(originalMetadata.LastSaveTime, deserializedMetadata.LastSaveTime);
            Assert.Equal(originalMetadata.PlayTime, deserializedMetadata.PlayTime);
            Assert.Equal(originalMetadata.CurrentDay, deserializedMetadata.CurrentDay);
            Assert.Equal(originalMetadata.CurrentZoneName, deserializedMetadata.CurrentZoneName);
            Assert.Equal(originalMetadata.CurrentBiome, deserializedMetadata.CurrentBiome);
            Assert.Equal(originalMetadata.ZonesVisited, deserializedMetadata.ZonesVisited);
            Assert.Equal(originalMetadata.JournalEntries, deserializedMetadata.JournalEntries);
            Assert.Equal(originalMetadata.GameVersion, deserializedMetadata.GameVersion);
            Assert.Equal(originalMetadata.FileSizeBytes, deserializedMetadata.FileSizeBytes);
        }

        [Fact]
        public void SaveSlotMetadata_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var metadata = new SaveSlotMetadata();

            // Assert
            Assert.Equal(0, metadata.SlotId);
            Assert.Equal(default(DateTime), metadata.LastSaveTime);
            Assert.Equal(TimeSpan.Zero, metadata.PlayTime);
            Assert.Equal(0, metadata.CurrentDay);
            Assert.Equal("Unknown", metadata.CurrentZoneName);
            Assert.Equal(BiomeType.Plains, metadata.CurrentBiome);
            Assert.Equal(0, metadata.ZonesVisited);
            Assert.Equal(0, metadata.JournalEntries);
            Assert.Equal("1.0.0", metadata.GameVersion);
            Assert.Equal(0, metadata.FileSizeBytes);
        }

        [Fact]
        public void GameSaveData_WithComplexSystemData_SerializesCorrectly()
        {
            // Arrange
            var saveData = new GameSaveData();
            saveData.SystemData["complex"] = new Dictionary<string, object>
            {
                ["nested"] = new { Value = 42, Name = "Test" },
                ["array"] = new[] { 1, 2, 3, 4, 5 },
                ["boolean"] = true,
                ["null_value"] = (object?)null
            };

            // Act
            string json = JsonSerializer.Serialize(saveData);
            var deserialized = JsonSerializer.Deserialize<GameSaveData>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.True(deserialized.SystemData.ContainsKey("complex"));
            Assert.NotNull(deserialized.SystemData["complex"]);
        }

        [Fact]
        public void SaveSlotMetadata_WithMaxValues_SerializesCorrectly()
        {
            // Arrange
            var metadata = new SaveSlotMetadata
            {
                SlotId = int.MaxValue,
                LastSaveTime = DateTime.MaxValue,
                PlayTime = TimeSpan.MaxValue,
                CurrentDay = int.MaxValue,
                CurrentZoneName = new string('A', 1000), // Very long zone name
                ZonesVisited = int.MaxValue,
                JournalEntries = int.MaxValue,
                FileSizeBytes = long.MaxValue
            };

            // Act & Assert - Should not throw
            string json = JsonSerializer.Serialize(metadata);
            var deserialized = JsonSerializer.Deserialize<SaveSlotMetadata>(json);
            
            Assert.NotNull(deserialized);
            Assert.Equal(metadata.SlotId, deserialized.SlotId);
            Assert.Equal(metadata.CurrentZoneName, deserialized.CurrentZoneName);
        }
    }

    /// <summary>
    /// Test implementation of ISaveable for testing purposes
    /// </summary>
    public class TestSaveableSystem : ISaveable
    {
        public string SaveKey => "test_system";
        public int SaveVersion => 1;
        
        public string TestData { get; set; } = "default";
        public int TestNumber { get; set; } = 42;

        public object GetSaveData()
        {
            return new { TestData, TestNumber };
        }

        public void LoadSaveData(object data)
        {
            if (data is JsonElement element)
            {
                if (element.TryGetProperty("TestData", out var testDataProp))
                    TestData = testDataProp.GetString() ?? "default";
                
                if (element.TryGetProperty("TestNumber", out var testNumberProp))
                    TestNumber = testNumberProp.GetInt32();
            }
        }
    }

    /// <summary>
    /// Tests for ISaveable interface implementation
    /// </summary>
    public class ISaveableTests
    {
        [Fact]
        public void ISaveable_GetSaveData_ReturnsCorrectData()
        {
            // Arrange
            var system = new TestSaveableSystem
            {
                TestData = "modified_data",
                TestNumber = 123
            };

            // Act
            var saveData = system.GetSaveData();

            // Assert
            Assert.NotNull(saveData);
            
            // Serialize and check the data
            string json = JsonSerializer.Serialize(saveData);
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            
            Assert.Equal("modified_data", element.GetProperty("TestData").GetString());
            Assert.Equal(123, element.GetProperty("TestNumber").GetInt32());
        }

        [Fact]
        public void ISaveable_LoadSaveData_RestoresCorrectState()
        {
            // Arrange
            var system = new TestSaveableSystem();
            var saveData = new { TestData = "loaded_data", TestNumber = 999 };
            var jsonElement = JsonSerializer.SerializeToElement(saveData);

            // Act
            system.LoadSaveData(jsonElement);

            // Assert
            Assert.Equal("loaded_data", system.TestData);
            Assert.Equal(999, system.TestNumber);
        }

        [Fact]
        public void ISaveable_Properties_ReturnExpectedValues()
        {
            // Arrange & Act
            var system = new TestSaveableSystem();

            // Assert
            Assert.Equal("test_system", system.SaveKey);
            Assert.Equal(1, system.SaveVersion);
        }
    }
}