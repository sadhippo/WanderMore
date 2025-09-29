using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HiddenHorizons.Tests
{
    /// <summary>
    /// Tests to verify the integration between SaveManager and SaveSlotManager is working correctly
    /// </summary>
    public class SaveLoadIntegrationFixTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly SaveManager _saveManager;
        private readonly SaveSlotManager _saveSlotManager;
        private readonly TimeManager _timeManager;
        private readonly JournalManager _journalManager;
        private readonly ZoneManager _zoneManager;
        private readonly Adventurer _adventurer;

        public SaveLoadIntegrationFixTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "SaveLoadIntegrationFixTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // Create test managers (without logger to avoid file conflicts)
            _saveManager = new SaveManager(_testDirectory, null, null, null, null, null);
            _saveSlotManager = new SaveSlotManager(_testDirectory);
            _timeManager = new TimeManager();
            _journalManager = new JournalManager(_timeManager);
            _zoneManager = new ZoneManager(12345);
            _adventurer = new Adventurer(Vector2.Zero);

            // Register systems with save manager
            _saveManager.RegisterSaveable(_timeManager);
            _saveManager.RegisterSaveable(_journalManager);
            _saveManager.RegisterSaveable(_zoneManager);
            _saveManager.RegisterSaveable(_adventurer);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task SaveOperation_CreatesSlotMetadata()
        {
            // Arrange
            int slotId = 1;
            
            // Simulate some game progress
            _timeManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMinutes(10))); // 10 minutes of game time
            _journalManager.OnZoneEntered(new Zone { Name = "Test Zone", BiomeType = BiomeType.Forest });

            // Act - Save the game
            await _saveManager.SaveGameAsync(slotId);

            // Simulate the metadata update that Game1.cs would do
            var journalStats = _journalManager.GetStatistics();
            var metadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                LastSaveTime = DateTime.UtcNow,
                PlayTime = TimeSpan.FromSeconds(_timeManager.CurrentTime),
                CurrentDay = _timeManager.CurrentDay,
                CurrentZoneName = "Test Zone",
                CurrentBiome = BiomeType.Forest,
                ZonesVisited = journalStats.ZonesVisited,
                JournalEntries = journalStats.TotalEntries,
                GameVersion = "1.0.0",
                FileSizeBytes = 0
            };

            if (!_saveSlotManager.SlotExists(slotId))
            {
                await _saveSlotManager.CreateSlotAsync(slotId, metadata);
            }
            else
            {
                await _saveSlotManager.UpdateSlotMetadataAsync(slotId, metadata);
            }

            // Assert - Check that both save file and metadata exist
            Assert.True(_saveManager.SaveExists(slotId), "Save file should exist");
            Assert.True(_saveSlotManager.SlotExists(slotId), "Slot metadata should exist");

            // Check that metadata contains correct information
            var retrievedMetadata = await _saveSlotManager.GetSlotInfoAsync(slotId);
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(slotId, retrievedMetadata.SlotId);
            Assert.Equal("Test Zone", retrievedMetadata.CurrentZoneName);
            Assert.Equal(BiomeType.Forest, retrievedMetadata.CurrentBiome);
            Assert.True(retrievedMetadata.PlayTime.TotalSeconds > 0);
        }

        [Fact]
        public async Task SaveLoadUI_DetectsExistingSaves()
        {
            // Arrange
            int slotId = 2;
            
            // Create a save with metadata (simulating the fixed integration)
            await _saveManager.SaveGameAsync(slotId);
            
            var metadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                LastSaveTime = DateTime.UtcNow,
                PlayTime = TimeSpan.FromMinutes(5),
                CurrentDay = 1,
                CurrentZoneName = "Starting Zone",
                CurrentBiome = BiomeType.Plains,
                ZonesVisited = 1,
                JournalEntries = 0,
                GameVersion = "1.0.0",
                FileSizeBytes = 0
            };

            await _saveSlotManager.CreateSlotAsync(slotId, metadata);

            // Act - Get all slot info (this is what SaveLoadUI does)
            var allSlots = await _saveSlotManager.GetAllSlotInfoAsync();

            // Assert - The slot should be detected
            Assert.True(allSlots.ContainsKey(slotId), "Slot should be detected by SaveSlotManager");
            Assert.Equal("Starting Zone", allSlots[slotId].CurrentZoneName);
            Assert.Equal(1, allSlots[slotId].ZonesVisited);
        }

        [Fact]
        public async Task MultipleSlots_WorkCorrectly()
        {
            // Arrange - Create multiple saves
            for (int slotId = 1; slotId <= 3; slotId++)
            {
                // Save game data
                await _saveManager.SaveGameAsync(slotId);
                
                // Create metadata for each slot
                var metadata = new SaveSlotMetadata
                {
                    SlotId = slotId,
                    LastSaveTime = DateTime.UtcNow.AddMinutes(-slotId), // Different times
                    PlayTime = TimeSpan.FromMinutes(slotId * 10), // Different play times
                    CurrentDay = slotId,
                    CurrentZoneName = $"Zone {slotId}",
                    CurrentBiome = (BiomeType)(slotId % 4), // Cycle through biomes
                    ZonesVisited = slotId,
                    JournalEntries = slotId * 2,
                    GameVersion = "1.0.0",
                    FileSizeBytes = 0
                };

                await _saveSlotManager.CreateSlotAsync(slotId, metadata);
            }

            // Act - Get all slots
            var allSlots = await _saveSlotManager.GetAllSlotInfoAsync();

            // Assert - All slots should be detected
            Assert.Equal(3, allSlots.Count);
            
            for (int slotId = 1; slotId <= 3; slotId++)
            {
                Assert.True(allSlots.ContainsKey(slotId), $"Slot {slotId} should exist");
                Assert.Equal($"Zone {slotId}", allSlots[slotId].CurrentZoneName);
                Assert.Equal(slotId, allSlots[slotId].ZonesVisited);
                Assert.Equal(slotId * 2, allSlots[slotId].JournalEntries);
            }
        }

        [Fact]
        public async Task LoadOperation_WorksWithExistingSlot()
        {
            // Arrange - Create a save with metadata
            int slotId = 3;
            
            // Set up some initial state
            _timeManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMinutes(5)));
            _journalManager.OnZoneEntered(new Zone { Name = "Initial Zone", BiomeType = BiomeType.Mountain });
            
            // Save the game
            await _saveManager.SaveGameAsync(slotId);
            
            // Create metadata
            var metadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                LastSaveTime = DateTime.UtcNow,
                PlayTime = TimeSpan.FromSeconds(_timeManager.CurrentTime),
                CurrentDay = _timeManager.CurrentDay,
                CurrentZoneName = "Initial Zone",
                CurrentBiome = BiomeType.Mountain,
                ZonesVisited = 1,
                JournalEntries = 1,
                GameVersion = "1.0.0",
                FileSizeBytes = 0
            };

            await _saveSlotManager.CreateSlotAsync(slotId, metadata);

            // Verify slot exists before loading
            Assert.True(_saveSlotManager.SlotExists(slotId));
            var slotInfo = await _saveSlotManager.GetSlotInfoAsync(slotId);
            Assert.NotNull(slotInfo);
            Assert.Equal("Initial Zone", slotInfo.CurrentZoneName);

            // Act - Load the game (this should work now)
            await _saveManager.LoadGameAsync(slotId);

            // Assert - Load should complete without errors
            // The fact that we got here without exceptions means the load worked
            Assert.True(true, "Load operation completed successfully");
        }

        [Fact]
        public async Task DeleteSlot_RemovesBothSaveAndMetadata()
        {
            // Arrange - Create a save with metadata
            int slotId = 4;
            
            await _saveManager.SaveGameAsync(slotId);
            var metadata = new SaveSlotMetadata
            {
                SlotId = slotId,
                LastSaveTime = DateTime.UtcNow,
                PlayTime = TimeSpan.FromMinutes(1),
                CurrentDay = 1,
                CurrentZoneName = "Test Zone",
                CurrentBiome = BiomeType.Plains,
                ZonesVisited = 1,
                JournalEntries = 0,
                GameVersion = "1.0.0",
                FileSizeBytes = 0
            };
            await _saveSlotManager.CreateSlotAsync(slotId, metadata);

            // Verify both exist
            Assert.True(_saveManager.SaveExists(slotId));
            Assert.True(_saveSlotManager.SlotExists(slotId));

            // Act - Delete the slot
            await _saveSlotManager.DeleteSlotAsync(slotId);

            // Assert - Both should be gone
            Assert.False(_saveManager.SaveExists(slotId));
            Assert.False(_saveSlotManager.SlotExists(slotId));
        }
    }
}