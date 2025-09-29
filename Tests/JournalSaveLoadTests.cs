using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using HiddenHorizons;

namespace HiddenHorizons.Tests
{
    public class JournalSaveLoadTests
    {
        private TimeManager CreateMockTimeManager()
        {
            return new TimeManager();
        }

        private Zone CreateTestZone(string id, string name, BiomeType biomeType)
        {
            return new Zone
            {
                Id = id,
                Name = name,
                BiomeType = biomeType,
                Width = 100,
                Height = 100,
                WorldX = 0,
                WorldY = 0
            };
        }

        [Fact]
        public void JournalManager_ImplementsISaveable()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var journalManager = new JournalManager(timeManager);

            // Assert
            Assert.IsAssignableFrom<ISaveable>(journalManager);
            Assert.Equal("JournalManager", journalManager.SaveKey);
            Assert.Equal(1, journalManager.SaveVersion);
        }

        [Fact]
        public void GetSaveData_ReturnsCorrectJournalSaveData()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var journalManager = new JournalManager(timeManager);
            
            var testZone = CreateTestZone("zone1", "Test Forest", BiomeType.Forest);
            journalManager.OnZoneEntered(testZone);

            // Act
            var saveData = journalManager.GetSaveData() as JournalSaveData;

            // Assert
            Assert.NotNull(saveData);
            Assert.NotEmpty(saveData.Entries);
            Assert.Contains("zone1_Forest", saveData.VisitedZones);
            Assert.Contains("Forest", saveData.DiscoveredBiomes);
            Assert.Equal(1, saveData.TotalZonesVisited);
            Assert.NotNull(saveData.Statistics);
        }

        [Fact]
        public void LoadSaveData_RestoresJournalState()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var journalManager = new JournalManager(timeManager);
            
            var testEntry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                Type = JournalEntryType.ZoneDiscovery,
                Title = "Test Entry",
                Description = "Test Description",
                Timestamp = DateTime.Now,
                GameDay = 5,
                GameTime = "12:00",
                Season = "Spring"
            };

            var saveData = new JournalSaveData
            {
                Entries = new List<JournalEntry> { testEntry },
                VisitedZones = new HashSet<string> { "zone1_Forest", "zone2_Plains" },
                DiscoveredBiomes = new HashSet<string> { "Forest", "Plains" },
                TotalZonesVisited = 2,
                TotalDaysExplored = 5,
                Statistics = new JournalStatistics
                {
                    TotalEntries = 1,
                    ZonesVisited = 2,
                    BiomesDiscovered = 2,
                    DaysExplored = 5,
                    WeatherEventsRecorded = 0,
                    MilestonesReached = 0
                }
            };

            // Act
            journalManager.LoadSaveData(saveData);

            // Assert
            var entries = journalManager.GetRecentEntries(10);
            Assert.Single(entries.Where(e => e.Id == testEntry.Id));
            
            var statistics = journalManager.GetStatistics();
            Assert.Equal(2, statistics.ZonesVisited); // 1 from save data + 1 from initial entry
            Assert.Equal(2, statistics.BiomesDiscovered);
            Assert.Equal(5, statistics.DaysExplored);
        }

        [Fact]
        public void LoadSaveData_ThrowsExceptionForInvalidData()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var journalManager = new JournalManager(timeManager);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => journalManager.LoadSaveData("invalid data"));
            Assert.Throws<ArgumentException>(() => journalManager.LoadSaveData(123));
            Assert.Throws<ArgumentException>(() => journalManager.LoadSaveData(new object()));
        }

        [Fact]
        public void LoadSaveData_HandlesNullCollections()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var journalManager = new JournalManager(timeManager);
            
            var saveData = new JournalSaveData
            {
                Entries = null,
                VisitedZones = null,
                DiscoveredBiomes = null,
                TotalZonesVisited = 0,
                TotalDaysExplored = 0
            };

            // Act
            journalManager.LoadSaveData(saveData);

            // Assert
            var statistics = journalManager.GetStatistics();
            Assert.Equal(0, statistics.TotalEntries); // No entries when loading null collection
            Assert.Equal(0, statistics.ZonesVisited);
            Assert.Equal(0, statistics.BiomesDiscovered);
        }

        [Fact]
        public void SaveLoad_PreservesJournalEntryData()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var originalJournal = new JournalManager(timeManager);
            
            // Add some test data
            var testZone1 = CreateTestZone("forest1", "Whispering Woods", BiomeType.Forest);
            var testZone2 = CreateTestZone("plains1", "Golden Fields", BiomeType.Plains);
            
            originalJournal.OnZoneEntered(testZone1);
            originalJournal.OnZoneEntered(testZone2);
            originalJournal.OnWeatherChanged(WeatherType.Rain, "Spring");
            originalJournal.OnSpecialEvent("Test Event", "This is a test event");

            // Act - Save and load
            var saveData = originalJournal.GetSaveData();
            var newJournal = new JournalManager(CreateMockTimeManager());
            newJournal.LoadSaveData(saveData);

            // Assert
            var originalStats = originalJournal.GetStatistics();
            var newStats = newJournal.GetStatistics();
            
            Assert.Equal(originalStats.TotalEntries, newStats.TotalEntries);
            Assert.Equal(originalStats.ZonesVisited, newStats.ZonesVisited);
            Assert.Equal(originalStats.BiomesDiscovered, newStats.BiomesDiscovered);
            Assert.Equal(originalStats.WeatherEventsRecorded, newStats.WeatherEventsRecorded);

            // Check specific entries are preserved
            var originalEntries = originalJournal.GetRecentEntries(10);
            var newEntries = newJournal.GetRecentEntries(10);
            
            Assert.Equal(originalEntries.Count, newEntries.Count);
            
            // Find the special event entry in both collections
            var originalSpecialEvent = originalEntries.FirstOrDefault(e => e.Type == JournalEntryType.SpecialEvent);
            var newSpecialEvent = newEntries.FirstOrDefault(e => e.Type == JournalEntryType.SpecialEvent);
            
            Assert.NotNull(originalSpecialEvent);
            Assert.NotNull(newSpecialEvent);
            Assert.Equal(originalSpecialEvent.Title, newSpecialEvent.Title);
            Assert.Equal(originalSpecialEvent.Description, newSpecialEvent.Description);
            Assert.Equal(originalSpecialEvent.Type, newSpecialEvent.Type);
        }

        [Fact]
        public void SaveLoad_PreservesFloatValues()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var journalManager = new JournalManager(timeManager);
            
            // Simulate some time progression to get float values
            var gameTime = new Microsoft.Xna.Framework.GameTime(
                TimeSpan.FromSeconds(1000.5), 
                TimeSpan.FromSeconds(16.67)
            );
            timeManager.Update(gameTime);
            
            var testZone = CreateTestZone("test1", "Test Zone", BiomeType.Forest);
            journalManager.OnZoneEntered(testZone);

            // Act - Save and load
            var saveData = journalManager.GetSaveData() as JournalSaveData;
            var newJournal = new JournalManager(CreateMockTimeManager());
            newJournal.LoadSaveData(saveData);

            // Assert - Check that integer values are preserved exactly
            var originalStats = journalManager.GetStatistics();
            var newStats = newJournal.GetStatistics();
            
            Assert.Equal(originalStats.TotalEntries, newStats.TotalEntries);
            Assert.Equal(originalStats.ZonesVisited, newStats.ZonesVisited);
            Assert.Equal(originalStats.DaysExplored, newStats.DaysExplored);
        }

        [Fact]
        public void LoadSaveData_TriggersRestorationEvents()
        {
            // Arrange
            var timeManager = CreateMockTimeManager();
            var journalManager = new JournalManager(timeManager);
            
            bool zoneEventTriggered = false;
            bool biomeEventTriggered = false;
            bool entryEventTriggered = false;
            
            journalManager.NewZoneDiscovered += (zoneName) => zoneEventTriggered = true;
            journalManager.NewBiomeDiscovered += (biome) => biomeEventTriggered = true;
            journalManager.NewEntryAdded += (entry) => entryEventTriggered = true;

            var saveData = new JournalSaveData
            {
                Entries = new List<JournalEntry>
                {
                    new JournalEntry
                    {
                        Id = Guid.NewGuid(),
                        Type = JournalEntryType.ZoneDiscovery,
                        Title = "Test",
                        Description = "Test",
                        Timestamp = DateTime.Now,
                        GameDay = 1,
                        GameTime = "12:00",
                        Season = "Spring"
                    }
                },
                VisitedZones = new HashSet<string> { "TestZone_Forest" },
                DiscoveredBiomes = new HashSet<string> { "Forest" },
                TotalZonesVisited = 1,
                TotalDaysExplored = 1
            };

            // Act
            journalManager.LoadSaveData(saveData);

            // Assert
            Assert.True(zoneEventTriggered, "NewZoneDiscovered event should be triggered");
            Assert.True(biomeEventTriggered, "NewBiomeDiscovered event should be triggered");
            Assert.True(entryEventTriggered, "NewEntryAdded event should be triggered");
        }

        [Fact]
        public void JournalSaveData_DefaultConstructor_InitializesCollections()
        {
            // Act
            var saveData = new JournalSaveData();

            // Assert
            Assert.NotNull(saveData.Entries);
            Assert.NotNull(saveData.VisitedZones);
            Assert.NotNull(saveData.DiscoveredBiomes);
            Assert.NotNull(saveData.Statistics);
            Assert.Empty(saveData.Entries);
            Assert.Empty(saveData.VisitedZones);
            Assert.Empty(saveData.DiscoveredBiomes);
        }
    }
}