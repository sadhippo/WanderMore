using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace HiddenHorizons.Tests
{
    /// <summary>
    /// Integration tests for full game save/load cycles with Game1 and SaveManager
    /// </summary>
    public class SaveLoadIntegrationTests : IDisposable
    {
        private readonly string _testSaveDirectory;
        private SaveManager _saveManager = null!;
        private Adventurer _adventurer = null!;
        private JournalManager _journalManager = null!;
        private PoIManager _poiManager = null!;
        private TimeManager _timeManager = null!;
        private WeatherManager _weatherManager = null!;
        private ZoneManager _zoneManager = null!;

        public SaveLoadIntegrationTests()
        {
            _testSaveDirectory = Path.Combine(Path.GetTempPath(), "HiddenHorizonsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSaveDirectory);
            
            // Initialize test systems
            InitializeTestSystems();
        }

        private void InitializeTestSystems()
        {
            // Initialize core systems (similar to Game1.Initialize)
            _timeManager = new TimeManager();
            _timeManager.SetDayNightCycle(1f, 0.5f);
            
            _journalManager = new JournalManager(_timeManager);
            _adventurer = new Adventurer(new Vector2(1000, 1000));
            _poiManager = new PoIManager(null, _journalManager, 12345); // AssetManager is null for tests
            _weatherManager = new WeatherManager(_timeManager, 12345);
            _zoneManager = new ZoneManager(12345);
            
            // Initialize SaveManager with test directory
            _saveManager = new SaveManager(_testSaveDirectory);
            
            // Register all ISaveable systems
            _saveManager.RegisterSaveable(_adventurer);
            _saveManager.RegisterSaveable(_journalManager);
            _saveManager.RegisterSaveable(_poiManager);
            _saveManager.RegisterSaveable(_timeManager);
            _saveManager.RegisterSaveable(_weatherManager);
            _saveManager.RegisterSaveable(_zoneManager);
        }

        [Fact]
        public async Task SaveLoadCycle_PreservesAllSystemStates()
        {
            // Arrange - Modify system states
            var originalAdventurerPos = new Vector2(1500, 2000);
            _adventurer.Position = originalAdventurerPos;
            _adventurer.Speed = 120f;
            
            // Advance time and trigger journal entries
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            for (int i = 0; i < 100; i++)
            {
                _timeManager.Update(gameTime);
            }
            
            // Add some journal entries by triggering events
            var testZone = new Zone 
            { 
                Id = "test1", 
                Name = "Test Zone", 
                Description = "A test zone", 
                BiomeType = BiomeType.Forest, 
                Width = 100, 
                Height = 100, 
                WorldX = 0, 
                WorldY = 0 
            };
            _journalManager.OnZoneEntered(testZone);
            _journalManager.OnSpecialEvent("Test Event", "A test special event");
            
            // Change weather
            _weatherManager.Update(gameTime);
            
            // Store original states for comparison
            var originalTime = _timeManager.CurrentTime;
            var originalDay = _timeManager.CurrentDay;
            var originalWeather = _weatherManager.CurrentWeather;
            var originalJournalCount = _journalManager.GetRecentEntries(100).Count;

            // Act - Save and then load
            await _saveManager.SaveGameAsync(1);
            
            // Modify states after save to ensure load actually restores
            _adventurer.Position = new Vector2(5000, 5000);
            _adventurer.Speed = 200f;
            _timeManager.SetDayNightCycle(2f, 1f); // Different timing
            
            await _saveManager.LoadGameAsync(1);

            // Assert - Verify all states were restored
            Assert.Equal(originalAdventurerPos.X, _adventurer.Position.X, 1f);
            Assert.Equal(originalAdventurerPos.Y, _adventurer.Position.Y, 1f);
            Assert.Equal(120f, _adventurer.Speed, 1f);
            Assert.Equal(originalTime, _timeManager.CurrentTime, 0.1f);
            Assert.Equal(originalDay, _timeManager.CurrentDay);
            Assert.Equal(originalWeather, _weatherManager.CurrentWeather);
            Assert.Equal(originalJournalCount, _journalManager.GetRecentEntries(100).Count);
        }

        [Fact]
        public async Task SaveLoadCycle_HandlesMultipleSlots()
        {
            // Arrange - Create different states for different slots
            var pos1 = new Vector2(1000, 1000);
            var pos2 = new Vector2(2000, 2000);
            var pos3 = new Vector2(3000, 3000);

            // Save to slot 1
            _adventurer.Position = pos1;
            _journalManager.OnSpecialEvent("Slot 1 Event", "Entry for slot 1");
            await _saveManager.SaveGameAsync(1);

            // Save to slot 2
            _adventurer.Position = pos2;
            _journalManager.OnSpecialEvent("Slot 2 Event", "Entry for slot 2");
            await _saveManager.SaveGameAsync(2);

            // Save to slot 3
            _adventurer.Position = pos3;
            _journalManager.OnSpecialEvent("Slot 3 Event", "Entry for slot 3");
            await _saveManager.SaveGameAsync(3);

            // Act & Assert - Load each slot and verify correct state
            await _saveManager.LoadGameAsync(1);
            Assert.Equal(pos1.X, _adventurer.Position.X, 1f);
            Assert.Equal(pos1.Y, _adventurer.Position.Y, 1f);

            await _saveManager.LoadGameAsync(2);
            Assert.Equal(pos2.X, _adventurer.Position.X, 1f);
            Assert.Equal(pos2.Y, _adventurer.Position.Y, 1f);

            await _saveManager.LoadGameAsync(3);
            Assert.Equal(pos3.X, _adventurer.Position.X, 1f);
            Assert.Equal(pos3.Y, _adventurer.Position.Y, 1f);
        }

        [Fact]
        public async Task SaveLoadCycle_PreservesJournalStatistics()
        {
            // Arrange - Build up journal statistics
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Advance multiple days
            for (int day = 0; day < 5; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    _timeManager.Update(gameTime);
                }
            }
            
            // Add various journal entries by triggering events
            _journalManager.OnSpecialEvent("Discovery 1", "First discovery");
            _journalManager.OnSpecialEvent("Discovery 2", "Second discovery");
            _journalManager.OnSpecialEvent("Interaction 1", "First interaction");
            
            // Simulate zone visits
            var testZone1 = new Zone 
            { 
                Id = "test1", 
                Name = "Test Zone 1", 
                Description = "A test zone", 
                BiomeType = BiomeType.Forest, 
                Width = 100, 
                Height = 100, 
                WorldX = 0, 
                WorldY = 0 
            };
            var testZone2 = new Zone 
            { 
                Id = "test2", 
                Name = "Test Zone 2", 
                Description = "Another test zone", 
                BiomeType = BiomeType.Lake, 
                Width = 100, 
                Height = 100, 
                WorldX = 1, 
                WorldY = 0 
            };
            _journalManager.OnZoneEntered(testZone1);
            _journalManager.OnZoneEntered(testZone2);
            
            var originalEntryCount = _journalManager.GetRecentEntries(100).Count;
            var originalStats = _journalManager.GetStatistics();

            // Act - Save and load
            await _saveManager.SaveGameAsync(1);
            
            // Clear journal to ensure load restores data
            _journalManager = new JournalManager(_timeManager);
            _saveManager.UnregisterSaveable("JournalManager");
            _saveManager.RegisterSaveable(_journalManager);
            
            await _saveManager.LoadGameAsync(1);

            // Assert - Verify statistics are preserved
            var loadedStats = _journalManager.GetStatistics();
            Assert.Equal(originalEntryCount, _journalManager.GetRecentEntries(100).Count);
            Assert.Equal(originalStats.ZonesVisited, loadedStats.ZonesVisited);
            Assert.Equal(originalStats.DaysExplored, loadedStats.DaysExplored);
        }

        [Fact]
        public async Task SaveLoadCycle_PreservesPoIStates()
        {
            // Arrange - Create and discover some PoIs
            var poi1 = new PointOfInterest 
            { 
                Id = Guid.NewGuid(), 
                Type = PoIType.Inn, 
                Position = new Vector2(1000, 1000), 
                Name = "Cozy Inn", 
                Description = "A welcoming inn",
                IsDiscovered = true,
                IsInteractable = true
            };
            var poi2 = new PointOfInterest 
            { 
                Id = Guid.NewGuid(), 
                Type = PoIType.Cottage, 
                Position = new Vector2(2000, 2000), 
                Name = "Small Cottage", 
                Description = "A peaceful cottage",
                IsDiscovered = true,
                IsInteractable = true
            };
            
            // Add PoIs to manager (simulating generation)
            var allPoIsField = _poiManager.GetType().GetField("_allPoIs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allPoIsField?.SetValue(_poiManager, new System.Collections.Generic.List<PointOfInterest> { poi1, poi2 });

            var originalPoICount = 2;

            // Act - Save and load
            await _saveManager.SaveGameAsync(1);
            
            // Clear PoI manager to ensure load restores data
            _poiManager = new PoIManager(null, _journalManager, 12345);
            _saveManager.UnregisterSaveable("PoIManager");
            _saveManager.RegisterSaveable(_poiManager);
            
            await _saveManager.LoadGameAsync(1);

            // Assert - Verify PoI states are preserved
            var loadedPoIs = _poiManager.GetType().GetField("_allPoIs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_poiManager) as System.Collections.Generic.List<PointOfInterest>;
            
            Assert.NotNull(loadedPoIs);
            Assert.Equal(originalPoICount, loadedPoIs.Count);
            
            // Verify PoI discovery states
            foreach (var poi in loadedPoIs)
            {
                Assert.True(poi.IsDiscovered);
            }
        }

        [Fact]
        public async Task SaveLoadCycle_PreservesTimeAndWeatherStates()
        {
            // Arrange - Advance time and change weather
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Advance time significantly
            for (int i = 0; i < 500; i++)
            {
                _timeManager.Update(gameTime);
                _weatherManager.Update(gameTime);
            }
            
            var originalTime = _timeManager.CurrentTime;
            var originalDay = _timeManager.CurrentDay;
            var originalTimeOfDay = _timeManager.CurrentTimeOfDay;
            var originalWeather = _weatherManager.CurrentWeather;
            var originalWeatherIntensity = _weatherManager.WeatherIntensity;

            // Act - Save and load
            await _saveManager.SaveGameAsync(1);
            
            // Reset time and weather to ensure load restores
            _timeManager = new TimeManager();
            _weatherManager = new WeatherManager(_timeManager, 54321); // Different seed
            _saveManager.UnregisterSaveable("TimeManager");
            _saveManager.UnregisterSaveable("WeatherManager");
            _saveManager.RegisterSaveable(_timeManager);
            _saveManager.RegisterSaveable(_weatherManager);
            
            await _saveManager.LoadGameAsync(1);

            // Assert - Verify time and weather states are preserved
            Assert.Equal(originalTime, _timeManager.CurrentTime, 0.1f);
            Assert.Equal(originalDay, _timeManager.CurrentDay);
            Assert.Equal(originalTimeOfDay, _timeManager.CurrentTimeOfDay);
            Assert.Equal(originalWeather, _weatherManager.CurrentWeather);
            Assert.Equal(originalWeatherIntensity, _weatherManager.WeatherIntensity, 0.1f);
        }

        [Fact]
        public async Task SaveLoadCycle_HandlesLargeDataSets()
        {
            // Arrange - Create large amounts of data
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Add many journal entries by triggering events
            for (int i = 0; i < 50; i++)
            {
                _journalManager.OnSpecialEvent($"Discovery {i}", $"Description for discovery {i}");
            }
            
            // Simulate many zone visits
            for (int i = 0; i < 25; i++)
            {
                var zone = new Zone 
                { 
                    Id = $"zone{i}", 
                    Name = $"Zone {i}", 
                    Description = $"Description for zone {i}", 
                    BiomeType = (BiomeType)(i % 6), // 6 biome types available
                    Width = 100, 
                    Height = 100, 
                    WorldX = i, 
                    WorldY = 0 
                };
                _journalManager.OnZoneEntered(zone);
            }
            
            // Advance time significantly
            for (int i = 0; i < 1000; i++)
            {
                _timeManager.Update(gameTime);
                if (i % 100 == 0)
                {
                    _weatherManager.Update(gameTime);
                }
            }
            
            var originalEntryCount = _journalManager.GetRecentEntries(200).Count;
            var originalTime = _timeManager.CurrentTime;

            // Act - Save and load (should handle large data efficiently)
            var saveStartTime = DateTime.Now;
            await _saveManager.SaveGameAsync(1);
            var saveTime = DateTime.Now - saveStartTime;
            
            var loadStartTime = DateTime.Now;
            await _saveManager.LoadGameAsync(1);
            var loadTime = DateTime.Now - loadStartTime;

            // Assert - Verify data integrity and performance
            Assert.Equal(originalEntryCount, _journalManager.GetRecentEntries(200).Count);
            Assert.Equal(originalTime, _timeManager.CurrentTime, 0.1f);
            
            // Performance assertions (should complete within reasonable time)
            Assert.True(saveTime.TotalSeconds < 5, $"Save took too long: {saveTime.TotalSeconds} seconds");
            Assert.True(loadTime.TotalSeconds < 5, $"Load took too long: {loadTime.TotalSeconds} seconds");
        }

        [Fact]
        public async Task SaveLoadCycle_HandlesCorruptedSaveRecovery()
        {
            // Arrange - Create valid save first
            _adventurer.Position = new Vector2(1234, 5678);
            await _saveManager.SaveGameAsync(1);
            
            // Corrupt the save file
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            if (File.Exists(saveFilePath))
            {
                await File.WriteAllTextAsync(saveFilePath, "corrupted data");
            }

            // Act & Assert - Should handle corruption gracefully
            var exception = await Record.ExceptionAsync(async () => await _saveManager.LoadGameAsync(1));
            
            // The save system should either recover from backup or throw a meaningful exception
            if (exception != null)
            {
                // Accept either InvalidOperationException or NotSupportedException for corrupted data
                Assert.True(exception is InvalidOperationException or NotSupportedException, 
                    $"Expected InvalidOperationException or NotSupportedException, but got {exception.GetType().Name}");
                // Check that the error message indicates a problem with the save file
                Assert.True(exception.Message.ToLower().Contains("corrupted") || 
                           exception.Message.ToLower().Contains("not compatible") ||
                           exception.Message.ToLower().Contains("version"),
                           $"Error message should indicate save file corruption or version issues, but was: {exception.Message}");
            }
        }

        [Fact]
        public void SaveManager_RegistersAllRequiredSystems()
        {
            // Arrange & Act - Systems should already be registered in constructor
            
            // Assert - Verify all required systems are registered
            Assert.True(_saveManager.IsSystemRegistered("Adventurer"));
            Assert.True(_saveManager.IsSystemRegistered("JournalManager"));
            Assert.True(_saveManager.IsSystemRegistered("PoIManager"));
            Assert.True(_saveManager.IsSystemRegistered("TimeManager"));
            Assert.True(_saveManager.IsSystemRegistered("WeatherManager"));
            Assert.True(_saveManager.IsSystemRegistered("ZoneManager"));
            Assert.Equal(6, _saveManager.RegisteredSystemCount);
        }

        [Fact]
        public async Task SaveLoadCycle_PreservesSystemRelationships()
        {
            // Arrange - Create interdependent system states
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Advance time to trigger weather changes
            for (int i = 0; i < 200; i++)
            {
                _timeManager.Update(gameTime);
                _weatherManager.Update(gameTime);
            }
            
            // Create zone and add journal entry
            var testZone = new Zone 
            { 
                Id = "relationship_test", 
                Name = "Relationship Test Zone", 
                Description = "Testing relationships", 
                BiomeType = BiomeType.Mountain, 
                Width = 100, 
                Height = 100, 
                WorldX = 0, 
                WorldY = 0 
            };
            _journalManager.OnZoneEntered(testZone);
            
            // Record weather change in journal
            _journalManager.OnWeatherChanged(_weatherManager.CurrentWeather, _timeManager.GetSeasonName());
            
            var originalWeather = _weatherManager.CurrentWeather;
            var originalSeason = _timeManager.GetSeasonName();
            var originalEntryCount = _journalManager.GetRecentEntries(100).Count;

            // Act - Save and load
            await _saveManager.SaveGameAsync(1);
            await _saveManager.LoadGameAsync(1);

            // Assert - Verify relationships are maintained
            Assert.Equal(originalWeather, _weatherManager.CurrentWeather);
            Assert.Equal(originalSeason, _timeManager.GetSeasonName());
            Assert.Equal(originalEntryCount, _journalManager.GetRecentEntries(100).Count);
            
            // Verify journal entries reference correct weather and season
            var entries = _journalManager.GetRecentEntries(100);
            var weatherEntries = entries.FindAll(e => e.Type == JournalEntryType.WeatherEvent);
            Assert.True(weatherEntries.Count > 0, "Should have weather entries");
        }

        public void Dispose()
        {
            // Cleanup test directory
            if (Directory.Exists(_testSaveDirectory))
            {
                try
                {
                    Directory.Delete(_testSaveDirectory, true);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Failed to cleanup test directory: {ex.Message}");
                }
            }
        }
    }
}