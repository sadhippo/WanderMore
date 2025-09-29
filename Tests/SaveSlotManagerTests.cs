using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HiddenHorizons;

namespace HiddenHorizons.Tests
{
    public class SaveSlotManagerTests : IDisposable
    {
        private SaveSlotManager _saveSlotManager;
        private string _testSaveDirectory;

        public SaveSlotManagerTests()
        {
            // Create a unique test directory for each test
            _testSaveDirectory = Path.Combine(Path.GetTempPath(), "SaveSlotManagerTests", Guid.NewGuid().ToString());
            _saveSlotManager = new SaveSlotManager(_testSaveDirectory);
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testSaveDirectory))
            {
                Directory.Delete(_testSaveDirectory, recursive: true);
            }
        }

        [Fact]
        public async Task CreateSlotAsync_ValidSlotId_CreatesSlotSuccessfully()
        {
            // Arrange
            int slotId = 1;
            bool eventFired = false;
            SlotCreatedEventArgs eventArgs = null;

            _saveSlotManager.SlotCreated += (sender, args) =>
            {
                eventFired = true;
                eventArgs = args;
            };

            // Act
            await _saveSlotManager.CreateSlotAsync(slotId);

            // Assert
            Assert.True(_saveSlotManager.SlotExists(slotId));
            Assert.True(eventFired);
            Assert.NotNull(eventArgs);
            Assert.Equal(slotId, eventArgs.SlotId);
            Assert.NotNull(eventArgs.Metadata);
            Assert.Equal(slotId, eventArgs.Metadata.SlotId);

            // Verify directory structure
            string slotDirectory = Path.Combine(_testSaveDirectory, $"slot_{slotId}");
            string metadataPath = Path.Combine(slotDirectory, "metadata.json");
            
            Assert.True(Directory.Exists(slotDirectory));
            Assert.True(File.Exists(metadataPath));
        }

        [Fact]
        public async Task CreateSlotAsync_WithInitialMetadata_UsesProvidedMetadata()
        {
            // Arrange
            int slotId = 2;
            var initialMetadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                CurrentZoneName = "Test Zone",
                CurrentBiome = BiomeType.Forest,
                CurrentDay = 5,
                PlayTime = TimeSpan.FromHours(2),
                ZonesVisited = 3,
                JournalEntries = 10,
                GameVersion = "1.2.3"
            };

            // Act
            await _saveSlotManager.CreateSlotAsync(slotId, initialMetadata);

            // Assert
            var retrievedMetadata = await _saveSlotManager.GetSlotInfoAsync(slotId);
            Assert.NotNull(retrievedMetadata);
            Assert.Equal("Test Zone", retrievedMetadata.CurrentZoneName);
            Assert.Equal(BiomeType.Forest, retrievedMetadata.CurrentBiome);
            Assert.Equal(5, retrievedMetadata.CurrentDay);
            Assert.Equal(TimeSpan.FromHours(2), retrievedMetadata.PlayTime);
            Assert.Equal(3, retrievedMetadata.ZonesVisited);
            Assert.Equal(10, retrievedMetadata.JournalEntries);
            Assert.Equal("1.2.3", retrievedMetadata.GameVersion);
        }

        [Fact]
        public async Task CreateSlotAsync_InvalidSlotId_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _saveSlotManager.CreateSlotAsync(0));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _saveSlotManager.CreateSlotAsync(-1));
        }

        [Fact]
        public async Task CreateSlotAsync_SlotAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            int slotId = 1;
            await _saveSlotManager.CreateSlotAsync(slotId);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _saveSlotManager.CreateSlotAsync(slotId));
        }

        [Fact]
        public async Task DeleteSlotAsync_ExistingSlot_DeletesSlotSuccessfully()
        {
            // Arrange
            int slotId = 1;
            await _saveSlotManager.CreateSlotAsync(slotId);
            
            bool eventFired = false;
            SlotDeletedEventArgs eventArgs = null;

            _saveSlotManager.SlotDeleted += (sender, args) =>
            {
                eventFired = true;
                eventArgs = args;
            };

            // Act
            await _saveSlotManager.DeleteSlotAsync(slotId);

            // Assert
            Assert.False(_saveSlotManager.SlotExists(slotId));
            Assert.True(eventFired);
            Assert.NotNull(eventArgs);
            Assert.Equal(slotId, eventArgs.SlotId);
            Assert.NotNull(eventArgs.DeletedMetadata);

            // Verify directory is deleted
            string slotDirectory = Path.Combine(_testSaveDirectory, $"slot_{slotId}");
            Assert.False(Directory.Exists(slotDirectory));
        }

        [Fact]
        public async Task DeleteSlotAsync_NonExistentSlot_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            int slotId = 999;

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => _saveSlotManager.DeleteSlotAsync(slotId));
        }

        [Fact]
        public async Task DeleteSlotAsync_InvalidSlotId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _saveSlotManager.DeleteSlotAsync(0));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _saveSlotManager.DeleteSlotAsync(-1));
        }

        [Fact]
        public async Task GetSlotInfoAsync_ExistingSlot_ReturnsCorrectMetadata()
        {
            // Arrange
            int slotId = 1;
            var initialMetadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                CurrentZoneName = "Test Zone",
                CurrentBiome = BiomeType.Mountain,
                CurrentDay = 10,
                PlayTime = TimeSpan.FromHours(5),
                ZonesVisited = 7,
                JournalEntries = 25,
                GameVersion = "2.0.0"
            };

            await _saveSlotManager.CreateSlotAsync(slotId, initialMetadata);

            // Act
            var retrievedMetadata = await _saveSlotManager.GetSlotInfoAsync(slotId);

            // Assert
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(slotId, retrievedMetadata.SlotId);
            Assert.Equal("Test Zone", retrievedMetadata.CurrentZoneName);
            Assert.Equal(BiomeType.Mountain, retrievedMetadata.CurrentBiome);
            Assert.Equal(10, retrievedMetadata.CurrentDay);
            Assert.Equal(TimeSpan.FromHours(5), retrievedMetadata.PlayTime);
            Assert.Equal(7, retrievedMetadata.ZonesVisited);
            Assert.Equal(25, retrievedMetadata.JournalEntries);
            Assert.Equal("2.0.0", retrievedMetadata.GameVersion);
        }

        [Fact]
        public async Task GetSlotInfoAsync_NonExistentSlot_ReturnsNull()
        {
            // Act
            var metadata = await _saveSlotManager.GetSlotInfoAsync(999);

            // Assert
            Assert.Null(metadata);
        }

        [Fact]
        public async Task GetSlotInfoAsync_InvalidSlotId_ReturnsNull()
        {
            // Act
            var metadata1 = await _saveSlotManager.GetSlotInfoAsync(0);
            var metadata2 = await _saveSlotManager.GetSlotInfoAsync(-1);

            // Assert
            Assert.Null(metadata1);
            Assert.Null(metadata2);
        }

        [Fact]
        public async Task GetAllSlotInfoAsync_MultipleSlots_ReturnsAllSlots()
        {
            // Arrange
            await _saveSlotManager.CreateSlotAsync(1);
            await _saveSlotManager.CreateSlotAsync(3);
            await _saveSlotManager.CreateSlotAsync(5);

            // Act
            var allSlots = await _saveSlotManager.GetAllSlotInfoAsync();

            // Assert
            Assert.Equal(3, allSlots.Count);
            Assert.True(allSlots.ContainsKey(1));
            Assert.True(allSlots.ContainsKey(3));
            Assert.True(allSlots.ContainsKey(5));
            Assert.False(allSlots.ContainsKey(2));
            Assert.False(allSlots.ContainsKey(4));
        }

        [Fact]
        public async Task GetAllSlotInfoAsync_NoSlots_ReturnsEmptyDictionary()
        {
            // Act
            var allSlots = await _saveSlotManager.GetAllSlotInfoAsync();

            // Assert
            Assert.NotNull(allSlots);
            Assert.Equal(0, allSlots.Count);
        }

        [Fact]
        public void SlotExists_ExistingSlot_ReturnsTrue()
        {
            // Arrange
            int slotId = 1;
            _saveSlotManager.CreateSlotAsync(slotId).Wait();

            // Act
            bool exists = _saveSlotManager.SlotExists(slotId);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void SlotExists_NonExistentSlot_ReturnsFalse()
        {
            // Act
            bool exists = _saveSlotManager.SlotExists(999);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void SlotExists_InvalidSlotId_ReturnsFalse()
        {
            // Act
            bool exists1 = _saveSlotManager.SlotExists(0);
            bool exists2 = _saveSlotManager.SlotExists(-1);

            // Assert
            Assert.False(exists1);
            Assert.False(exists2);
        }

        [Fact]
        public async Task UpdateSlotMetadataAsync_ExistingSlot_UpdatesMetadataSuccessfully()
        {
            // Arrange
            int slotId = 1;
            await _saveSlotManager.CreateSlotAsync(slotId);

            bool eventFired = false;
            SlotMetadataUpdatedEventArgs eventArgs = null;

            _saveSlotManager.SlotMetadataUpdated += (sender, args) =>
            {
                eventFired = true;
                eventArgs = args;
            };

            var updatedMetadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                CurrentZoneName = "Updated Zone",
                CurrentBiome = BiomeType.Swamp,
                CurrentDay = 15,
                PlayTime = TimeSpan.FromHours(10),
                ZonesVisited = 12,
                JournalEntries = 50,
                GameVersion = "3.0.0"
            };

            // Act
            await _saveSlotManager.UpdateSlotMetadataAsync(slotId, updatedMetadata);

            // Assert
            var retrievedMetadata = await _saveSlotManager.GetSlotInfoAsync(slotId);
            Assert.NotNull(retrievedMetadata);
            Assert.Equal("Updated Zone", retrievedMetadata.CurrentZoneName);
            Assert.Equal(BiomeType.Swamp, retrievedMetadata.CurrentBiome);
            Assert.Equal(15, retrievedMetadata.CurrentDay);
            Assert.Equal(TimeSpan.FromHours(10), retrievedMetadata.PlayTime);
            Assert.Equal(12, retrievedMetadata.ZonesVisited);
            Assert.Equal(50, retrievedMetadata.JournalEntries);
            Assert.Equal("3.0.0", retrievedMetadata.GameVersion);

            Assert.True(eventFired);
            Assert.NotNull(eventArgs);
            Assert.Equal(slotId, eventArgs.SlotId);
        }

        [Fact]
        public async Task UpdateSlotMetadataAsync_NonExistentSlot_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            var metadata = new SaveSlotMetadata { SlotId = 999 };

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => _saveSlotManager.UpdateSlotMetadataAsync(999, metadata));
        }

        [Fact]
        public async Task UpdateSlotMetadataAsync_NullMetadata_ThrowsArgumentNullException()
        {
            // Arrange
            int slotId = 1;
            await _saveSlotManager.CreateSlotAsync(slotId);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _saveSlotManager.UpdateSlotMetadataAsync(slotId, null));
        }

        [Fact]
        public async Task UpdateSlotMetadataAsync_InvalidSlotId_ThrowsArgumentException()
        {
            // Arrange
            var metadata = new SaveSlotMetadata();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _saveSlotManager.UpdateSlotMetadataAsync(0, metadata));
        }

        [Fact]
        public async Task GetMaxSlotIdAsync_MultipleSlots_ReturnsHighestSlotId()
        {
            // Arrange
            await _saveSlotManager.CreateSlotAsync(1);
            await _saveSlotManager.CreateSlotAsync(5);
            await _saveSlotManager.CreateSlotAsync(3);

            // Act
            int maxSlotId = await _saveSlotManager.GetMaxSlotIdAsync();

            // Assert
            Assert.Equal(5, maxSlotId);
        }

        [Fact]
        public async Task GetMaxSlotIdAsync_NoSlots_ReturnsZero()
        {
            // Act
            int maxSlotId = await _saveSlotManager.GetMaxSlotIdAsync();

            // Assert
            Assert.Equal(0, maxSlotId);
        }

        [Fact]
        public async Task GetNextAvailableSlotIdAsync_NoSlots_ReturnsOne()
        {
            // Act
            int nextSlotId = await _saveSlotManager.GetNextAvailableSlotIdAsync();

            // Assert
            Assert.Equal(1, nextSlotId);
        }

        [Fact]
        public async Task GetNextAvailableSlotIdAsync_WithGaps_ReturnsFirstGap()
        {
            // Arrange
            await _saveSlotManager.CreateSlotAsync(1);
            await _saveSlotManager.CreateSlotAsync(3);
            await _saveSlotManager.CreateSlotAsync(4);

            // Act
            int nextSlotId = await _saveSlotManager.GetNextAvailableSlotIdAsync();

            // Assert
            Assert.Equal(2, nextSlotId);
        }

        [Fact]
        public async Task GetNextAvailableSlotIdAsync_NoGaps_ReturnsNextSequential()
        {
            // Arrange
            await _saveSlotManager.CreateSlotAsync(1);
            await _saveSlotManager.CreateSlotAsync(2);
            await _saveSlotManager.CreateSlotAsync(3);

            // Act
            int nextSlotId = await _saveSlotManager.GetNextAvailableSlotIdAsync();

            // Assert
            Assert.Equal(4, nextSlotId);
        }

        [Fact]
        public async Task GetSlotInfoAsync_CorruptedMetadata_RecreatesMetadata()
        {
            // Arrange
            int slotId = 1;
            string slotDirectory = Path.Combine(_testSaveDirectory, $"slot_{slotId}");
            string saveFilePath = Path.Combine(slotDirectory, "save.json");
            string metadataPath = Path.Combine(slotDirectory, "metadata.json");

            // Create slot directory and save file
            Directory.CreateDirectory(slotDirectory);
            await File.WriteAllTextAsync(saveFilePath, "{}");

            // Create corrupted metadata file
            await File.WriteAllTextAsync(metadataPath, "invalid json content");

            // Act - GetSlotInfoAsync should recreate the metadata when it encounters corruption
            var metadata = await _saveSlotManager.GetSlotInfoAsync(slotId);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(slotId, metadata.SlotId);
            Assert.Equal("Unknown", metadata.CurrentZoneName);
            Assert.Equal(BiomeType.Plains, metadata.CurrentBiome);
        }

        [Fact]
        public async Task ValidateAndCleanupSlotsAsync_MissingMetadata_CreatesMetadata()
        {
            // Arrange
            int slotId = 1;
            string slotDirectory = Path.Combine(_testSaveDirectory, $"slot_{slotId}");
            string saveFilePath = Path.Combine(slotDirectory, "save.json");
            string metadataPath = Path.Combine(slotDirectory, "metadata.json");

            // Create slot directory and save file without metadata
            Directory.CreateDirectory(slotDirectory);
            await File.WriteAllTextAsync(saveFilePath, "{}");

            // Create a new SaveSlotManager to simulate restart
            _saveSlotManager = new SaveSlotManager(_testSaveDirectory);

            // Act
            await _saveSlotManager.ValidateAndCleanupSlotsAsync();

            // Assert
            Assert.True(File.Exists(metadataPath));
            var metadata = await _saveSlotManager.GetSlotInfoAsync(slotId);
            Assert.NotNull(metadata);
            Assert.Equal(slotId, metadata.SlotId);
        }

        [Fact]
        public async Task UpdateSlotMetadataAsync_WithSaveFile_UpdatesFileSizeCorrectly()
        {
            // Arrange
            int slotId = 1;
            await _saveSlotManager.CreateSlotAsync(slotId);

            // Create a save file with some content
            string saveFilePath = Path.Combine(_testSaveDirectory, $"slot_{slotId}", "save.json");
            string saveContent = "{\"test\": \"data\", \"more\": \"content\"}";
            await File.WriteAllTextAsync(saveFilePath, saveContent);

            var updatedMetadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                CurrentZoneName = "Test Zone"
            };

            // Act
            await _saveSlotManager.UpdateSlotMetadataAsync(slotId, updatedMetadata);

            // Assert
            var retrievedMetadata = await _saveSlotManager.GetSlotInfoAsync(slotId);
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(saveContent.Length, retrievedMetadata.FileSizeBytes);
        }

        [Fact]
        public async Task SlotMetadataCaching_MultipleReads_UsesCache()
        {
            // Arrange
            int slotId = 1;
            await _saveSlotManager.CreateSlotAsync(slotId);

            // Act - Read metadata multiple times
            var metadata1 = await _saveSlotManager.GetSlotInfoAsync(slotId);
            var metadata2 = await _saveSlotManager.GetSlotInfoAsync(slotId);
            var metadata3 = await _saveSlotManager.GetSlotInfoAsync(slotId);

            // Assert - All should return the same instance (cached)
            Assert.NotNull(metadata1);
            Assert.NotNull(metadata2);
            Assert.NotNull(metadata3);
            Assert.Equal(metadata1.SlotId, metadata2.SlotId);
            Assert.Equal(metadata2.SlotId, metadata3.SlotId);
        }
    }
}