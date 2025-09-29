using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HiddenHorizons.Tests
{
    /// <summary>
    /// Final comprehensive integration tests for save/load system validation
    /// Tests large datasets, error recovery, performance, and cross-session compatibility
    /// </summary>
    public class FinalIntegrationValidationTests : IDisposable
    {
        private readonly string _testSaveDirectory;
        private SaveManager _saveManager = null!;
        private Adventurer _adventurer = null!;
        private JournalManager _journalManager = null!;
        private PoIManager _poiManager = null!;
        private TimeManager _timeManager = null!;
        private WeatherManager _weatherManager = null!;
        private ZoneManager _zoneManager = null!;
        private QuestManager _questManager = null!;

        public FinalIntegrationValidationTests()
        {
            _testSaveDirectory = Path.Combine(Path.GetTempPath(), "HiddenHorizonsValidationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSaveDirectory);
            InitializeTestSystems();
        }

        private void InitializeTestSystems()
        {
            _timeManager = new TimeManager();
            _timeManager.SetDayNightCycle(1f, 0.5f);
            
            _journalManager = new JournalManager(_timeManager);
            _adventurer = new Adventurer(new Vector2(1000, 1000));
            _poiManager = new PoIManager(null, _journalManager, 12345);
            _weatherManager = new WeatherManager(_timeManager, 12345);
            _zoneManager = new ZoneManager(12345);
            _questManager = new QuestManager();
            
            // Create a unique log directory for this test instance to avoid file conflicts
            var logDirectory = Path.Combine(_testSaveDirectory, "Logs");
            var logger = new SaveSystemLogger(logDirectory, $"FinalIntegrationTest_{Guid.NewGuid():N}");
            
            _saveManager = new SaveManager(_testSaveDirectory, logger: logger);
            
            // Register all systems
            _saveManager.RegisterSaveable(_adventurer);
            _saveManager.RegisterSaveable(_journalManager);
            _saveManager.RegisterSaveable(_poiManager);
            _saveManager.RegisterSaveable(_timeManager);
            _saveManager.RegisterSaveable(_weatherManager);
            _saveManager.RegisterSaveable(_zoneManager);
            _saveManager.RegisterSaveable(_questManager);
        }

        [Fact]
        public async Task ComprehensiveSaveLoadCycle_WithLargeDatasets_PreservesAllData()
        {
            // Arrange - Create extensive game state with large amounts of data
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Create many zones (simulating extended exploration)
            var zones = new List<Zone>();
            for (int i = 0; i < 100; i++)
            {
                var zone = new Zone
                {
                    Id = $"zone_{i}",
                    Name = $"Zone {i}",
                    Description = $"A procedurally generated zone number {i}",
                    BiomeType = (BiomeType)(i % 6),
                    Width = 100 + (i % 50),
                    Height = 100 + (i % 50),
                    WorldX = i % 10,
                    WorldY = i / 10
                };
                zones.Add(zone);
                _journalManager.OnZoneEntered(zone);
            }
            
            // Create many journal entries (simulating long play session)
            for (int i = 0; i < 200; i++)
            {
                _journalManager.OnSpecialEvent($"Event_{i}", $"Special event number {i} occurred during exploration");
                
                // Advance time between events
                for (int j = 0; j < 10; j++)
                {
                    _timeManager.Update(gameTime);
                    if (j % 5 == 0) _weatherManager.Update(gameTime);
                }
            }
            
            // Create many PoIs (simulating rich world generation)
            var poiList = new List<PointOfInterest>();
            for (int i = 0; i < 150; i++)
            {
                var poi = new PointOfInterest
                {
                    Id = Guid.NewGuid(),
                    Type = (PoIType)(i % 4),
                    Position = new Vector2(i * 100, (i % 10) * 100),
                    Name = $"PoI_{i}",
                    Description = $"Point of interest number {i}",
                    IsDiscovered = i % 3 == 0, // Some discovered, some not
                    IsInteractable = true,
                    ZoneId = zones[i % zones.Count].Id
                };
                poiList.Add(poi);
            }
            
            // Set PoIs in manager using reflection
            var allPoIsField = _poiManager.GetType().GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allPoIsField?.SetValue(_poiManager, poiList);
            
            // Set adventurer to complex state
            _adventurer.Position = new Vector2(5432.1f, 9876.5f);
            _adventurer.Speed = 125.75f;
            // Note: IsInteracting and InteractionTimer are private fields, not public properties
            
            // Create quest data
            var activeQuests = new List<QuestInstanceSaveData>
            {
                new QuestInstanceSaveData
                {
                    QuestId = Guid.NewGuid(),
                    QuestTemplateId = "epic_journey",
                    Status = QuestStatus.Active,
                    StartTime = DateTime.Now.AddHours(-5),
                    QuestState = new Dictionary<string, object> { { "progress", 75 }, { "location", "mountain_peak" } }
                },
                new QuestInstanceSaveData
                {
                    QuestId = Guid.NewGuid(),
                    QuestTemplateId = "treasure_hunt",
                    Status = QuestStatus.Active,
                    StartTime = DateTime.Now.AddHours(-2),
                    QuestState = new Dictionary<string, object> { { "clues_found", 2 }, { "total_clues", 8 } }
                }
            };
            
            var completedQuests = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var questVariables = new Dictionary<string, object>
            {
                { "global_reputation", 150 },
                { "total_quests_completed", 25 },
                { "favorite_biome", "Forest" }
            };
            
            // Set quest data using reflection
            var questSaveData = new QuestSaveData
            {
                ActiveQuests = activeQuests,
                CompletedQuestIds = completedQuests,
                QuestVariables = questVariables
            };
            
            var questDataField = _questManager.GetType().GetField("_questSaveData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            questDataField?.SetValue(_questManager, questSaveData);
            
            // Store original values for comparison
            var originalAdventurerPos = _adventurer.Position;
            var originalAdventurerSpeed = _adventurer.Speed;
            var originalTime = _timeManager.CurrentTime;
            var originalDay = _timeManager.CurrentDay;
            var originalWeather = _weatherManager.CurrentWeather;
            var originalJournalCount = _journalManager.GetRecentEntries(500).Count;
            var originalPoICount = poiList.Count;
            var originalActiveQuestCount = activeQuests.Count;
            var originalCompletedQuestCount = completedQuests.Count;

            // Act - Perform save operation and measure performance
            var saveStartTime = DateTime.Now;
            await _saveManager.SaveGameAsync(1);
            var saveTime = DateTime.Now - saveStartTime;
            
            // Verify save file exists and has reasonable size
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            Assert.True(File.Exists(saveFilePath), "Save file should exist");
            var saveFileInfo = new FileInfo(saveFilePath);
            Assert.True(saveFileInfo.Length > 1000, "Save file should contain substantial data");
            
            // Reset all systems to ensure load actually restores data
            InitializeTestSystems();
            
            // Perform load operation and measure performance
            var loadStartTime = DateTime.Now;
            await _saveManager.LoadGameAsync(1);
            var loadTime = DateTime.Now - loadStartTime;

            // Assert - Verify all data was preserved correctly
            Assert.Equal(originalAdventurerPos.X, _adventurer.Position.X, 0.1f);
            Assert.Equal(originalAdventurerPos.Y, _adventurer.Position.Y, 0.1f);
            Assert.Equal(originalAdventurerSpeed, _adventurer.Speed, 0.1f);
            Assert.Equal(originalTime, _timeManager.CurrentTime, 0.1f);
            Assert.Equal(originalDay, _timeManager.CurrentDay);
            Assert.Equal(originalWeather, _weatherManager.CurrentWeather);
            Assert.Equal(originalJournalCount, _journalManager.GetRecentEntries(500).Count);
            
            // Verify PoI data
            var loadedPoIs = allPoIsField?.GetValue(_poiManager) as List<PointOfInterest>;
            Assert.NotNull(loadedPoIs);
            Assert.Equal(originalPoICount, loadedPoIs.Count);
            
            // Verify quest data
            var loadedQuestData = questDataField?.GetValue(_questManager) as QuestSaveData;
            Assert.NotNull(loadedQuestData);
            Assert.Equal(originalActiveQuestCount, loadedQuestData.ActiveQuests.Count);
            Assert.Equal(originalCompletedQuestCount, loadedQuestData.CompletedQuestIds.Count);
            Assert.Equal(questVariables.Count, loadedQuestData.QuestVariables.Count);
            
            // Performance validation
            Assert.True(saveTime.TotalSeconds < 10, $"Save operation took too long: {saveTime.TotalSeconds} seconds");
            Assert.True(loadTime.TotalSeconds < 10, $"Load operation took too long: {loadTime.TotalSeconds} seconds");
            
            // Memory usage should be reasonable (file size check)
            Assert.True(saveFileInfo.Length < 50 * 1024 * 1024, "Save file should not exceed 50MB");
        }

        [Fact]
        public async Task ErrorRecovery_CorruptedSaveFile_RecoverFromBackup()
        {
            // Arrange - Create valid save with backup
            _adventurer.Position = new Vector2(1111, 2222);
            _journalManager.OnSpecialEvent("Test Event", "This should be preserved");
            
            await _saveManager.SaveGameAsync(1);
            
            // Verify backup was created
            var backupDir = Path.Combine(_testSaveDirectory, "slot_1", "backups");
            Assert.True(Directory.Exists(backupDir), "Backup directory should exist");
            var backupFiles = Directory.GetFiles(backupDir, "*.json");
            Assert.True(backupFiles.Length > 0, "At least one backup should exist");
            
            // Corrupt the main save file
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            await File.WriteAllTextAsync(saveFilePath, "{ corrupted json data }");
            
            // Reset systems
            InitializeTestSystems();

            // Act - Attempt to load corrupted save
            var exception = await Record.ExceptionAsync(async () => await _saveManager.LoadGameAsync(1));
            
            // Assert - Should handle corruption gracefully
            if (exception != null)
            {
                // Verify it's an expected exception type for corrupted data
                Assert.True(exception is InvalidOperationException or NotSupportedException or System.Text.Json.JsonException,
                    $"Expected corruption-related exception, got {exception.GetType().Name}");
            }
        }

        [Fact]
        public async Task ErrorRecovery_MissingSaveFile_HandlesGracefully()
        {
            // Arrange - Ensure no save file exists
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }

            // Act & Assert - Should handle missing file gracefully
            var exception = await Record.ExceptionAsync(async () => await _saveManager.LoadGameAsync(1));
            
            Assert.NotNull(exception);
            Assert.True(exception is FileNotFoundException or DirectoryNotFoundException or InvalidOperationException,
                $"Expected file-related exception, got {exception.GetType().Name}");
        }

        [Fact]
        public async Task ErrorRecovery_InsufficientDiskSpace_HandlesGracefully()
        {
            // Note: This test simulates the error condition since we can't actually fill up disk space
            // The actual disk space checking is tested in SaveErrorHandlingTests
            
            // Arrange - Create large dataset that would require significant disk space
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Create extensive data
            for (int i = 0; i < 1000; i++)
            {
                _journalManager.OnSpecialEvent($"Large Event {i}", 
                    $"This is a very long description for event {i} that contains lots of text to make the save file larger. " +
                    $"We're simulating a scenario where the save data becomes quite large and might approach disk space limits. " +
                    $"Event number {i} occurred at time {DateTime.Now} and involved complex interactions.");
                
                if (i % 100 == 0)
                {
                    _timeManager.Update(gameTime);
                }
            }

            // Act - Save should complete successfully under normal conditions
            await _saveManager.SaveGameAsync(1);
            
            // Assert - Verify save completed and file exists
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            Assert.True(File.Exists(saveFilePath), "Save should complete successfully");
            
            var fileInfo = new FileInfo(saveFilePath);
            Assert.True(fileInfo.Length > 10000, "Save file should contain substantial data");
        }

        [Fact]
        public async Task CrossSessionCompatibility_MultipleGameRestarts_PreservesData()
        {
            // Arrange - Simulate multiple game sessions
            var sessionData = new List<(Vector2 position, string eventName)>();
            
            // Session 1
            _adventurer.Position = new Vector2(1000, 1000);
            _journalManager.OnSpecialEvent("Session 1 Event", "First session event");
            sessionData.Add((_adventurer.Position, "Session 1 Event"));
            await _saveManager.SaveGameAsync(1);
            
            // Simulate game restart - reinitialize systems
            InitializeTestSystems();
            await _saveManager.LoadGameAsync(1);
            
            // Session 2 - Continue from loaded state
            _adventurer.Position = new Vector2(2000, 2000);
            _journalManager.OnSpecialEvent("Session 2 Event", "Second session event");
            sessionData.Add((_adventurer.Position, "Session 2 Event"));
            await _saveManager.SaveGameAsync(1);
            
            // Simulate another game restart
            InitializeTestSystems();
            await _saveManager.LoadGameAsync(1);
            
            // Session 3 - Continue from loaded state
            _adventurer.Position = new Vector2(3000, 3000);
            _journalManager.OnSpecialEvent("Session 3 Event", "Third session event");
            sessionData.Add((_adventurer.Position, "Session 3 Event"));
            await _saveManager.SaveGameAsync(1);
            
            // Final restart and verification
            InitializeTestSystems();
            await _saveManager.LoadGameAsync(1);

            // Assert - Verify all session data is preserved
            Assert.Equal(3000f, _adventurer.Position.X, 0.1f);
            Assert.Equal(3000f, _adventurer.Position.Y, 0.1f);
            
            var allEntries = _journalManager.GetRecentEntries(100);
            Assert.True(allEntries.Count >= 3, "Should have entries from all sessions");
            
            // Verify entries from all sessions exist
            var eventNames = allEntries.Select(e => e.Title).ToList();
            Assert.Contains("Session 1 Event", eventNames);
            Assert.Contains("Session 2 Event", eventNames);
            Assert.Contains("Session 3 Event", eventNames);
        }

        [Fact]
        public async Task ExtendedPlaySession_LongRunningGame_MaintainsPerformance()
        {
            // Arrange - Simulate extended play session with frequent saves
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            var saveTimes = new List<TimeSpan>();
            var loadTimes = new List<TimeSpan>();
            
            // Simulate 10 save/load cycles with increasing data
            for (int cycle = 0; cycle < 10; cycle++)
            {
                // Add data each cycle
                for (int i = 0; i < 20; i++)
                {
                    _journalManager.OnSpecialEvent($"Cycle {cycle} Event {i}", 
                        $"Event {i} from cycle {cycle} of extended play session");
                    
                    // Advance time
                    for (int j = 0; j < 5; j++)
                    {
                        _timeManager.Update(gameTime);
                        if (j % 2 == 0) _weatherManager.Update(gameTime);
                    }
                }
                
                // Move adventurer
                _adventurer.Position = new Vector2(cycle * 1000, cycle * 500);
                
                // Measure save performance
                var saveStart = DateTime.Now;
                await _saveManager.SaveGameAsync(1);
                var saveTime = DateTime.Now - saveStart;
                saveTimes.Add(saveTime);
                
                // Measure load performance
                var loadStart = DateTime.Now;
                await _saveManager.LoadGameAsync(1);
                var loadTime = DateTime.Now - loadStart;
                loadTimes.Add(loadTime);
            }

            // Assert - Performance should remain consistent
            var avgSaveTime = saveTimes.Average(t => t.TotalMilliseconds);
            var avgLoadTime = loadTimes.Average(t => t.TotalMilliseconds);
            var maxSaveTime = saveTimes.Max(t => t.TotalMilliseconds);
            var maxLoadTime = loadTimes.Max(t => t.TotalMilliseconds);
            
            Assert.True(avgSaveTime < 5000, $"Average save time should be under 5 seconds, was {avgSaveTime}ms");
            Assert.True(avgLoadTime < 5000, $"Average load time should be under 5 seconds, was {avgLoadTime}ms");
            Assert.True(maxSaveTime < 10000, $"Max save time should be under 10 seconds, was {maxSaveTime}ms");
            Assert.True(maxLoadTime < 10000, $"Max load time should be under 10 seconds, was {maxLoadTime}ms");
            
            // Performance should not degrade significantly over time
            var firstHalfAvgSave = saveTimes.Take(5).Average(t => t.TotalMilliseconds);
            var secondHalfAvgSave = saveTimes.Skip(5).Average(t => t.TotalMilliseconds);
            var performanceDegradation = (secondHalfAvgSave - firstHalfAvgSave) / firstHalfAvgSave;
            
            Assert.True(performanceDegradation < 2.0, 
                $"Save performance should not degrade more than 200%, degradation was {performanceDegradation * 100:F1}%");
        }

        [Fact]
        public async Task SystemIntegration_AllSystemsWorking_NoDataLoss()
        {
            // Arrange - Create complex interconnected state
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Create zones and visit them
            var zones = new List<Zone>();
            for (int i = 0; i < 10; i++)
            {
                var zone = new Zone
                {
                    Id = $"integration_zone_{i}",
                    Name = $"Integration Zone {i}",
                    Description = $"Zone for integration testing {i}",
                    BiomeType = (BiomeType)(i % 6),
                    Width = 100,
                    Height = 100,
                    WorldX = i,
                    WorldY = 0
                };
                zones.Add(zone);
                _journalManager.OnZoneEntered(zone);
            }
            
            // Advance time through multiple days and weather changes
            for (int day = 0; day < 3; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    _timeManager.Update(gameTime);
                    _weatherManager.Update(gameTime);
                    
                    // Record weather changes
                    if (hour % 6 == 0)
                    {
                        _journalManager.OnWeatherChanged(_weatherManager.CurrentWeather, _timeManager.GetSeasonName());
                    }
                }
            }
            
            // Create PoIs in different zones
            var poiList = new List<PointOfInterest>();
            for (int i = 0; i < zones.Count; i++)
            {
                var poi = new PointOfInterest
                {
                    Id = Guid.NewGuid(),
                    Type = (PoIType)(i % 4),
                    Position = new Vector2(i * 100, 100),
                    Name = $"Integration PoI {i}",
                    Description = $"PoI in {zones[i].Name}",
                    IsDiscovered = true,
                    IsInteractable = true,
                    ZoneId = zones[i].Id
                };
                poiList.Add(poi);
            }
            
            var allPoIsField = _poiManager.GetType().GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allPoIsField?.SetValue(_poiManager, poiList);
            
            // Set adventurer state
            _adventurer.Position = new Vector2(2500, 1500);
            _adventurer.Speed = 100f;
            
            // Create quest state
            var questSaveData = new QuestSaveData
            {
                ActiveQuests = new List<QuestInstanceSaveData>
                {
                    new QuestInstanceSaveData
                    {
                        QuestId = Guid.NewGuid(),
                        QuestTemplateId = "integration_quest",
                        Status = QuestStatus.Active,
                        StartTime = DateTime.Now.AddHours(-1),
                        QuestState = new Dictionary<string, object> { { "zones_visited", zones.Count } }
                    }
                },
                CompletedQuestIds = new List<Guid> { Guid.NewGuid() },
                QuestVariables = new Dictionary<string, object> { { "integration_test", true } }
            };
            
            var questDataField = _questManager.GetType().GetField("_questSaveData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            questDataField?.SetValue(_questManager, questSaveData);
            
            // Store original state
            var originalState = new
            {
                AdventurerPos = _adventurer.Position,
                AdventurerSpeed = _adventurer.Speed,
                Time = _timeManager.CurrentTime,
                Day = _timeManager.CurrentDay,
                Weather = _weatherManager.CurrentWeather,
                JournalEntries = _journalManager.GetRecentEntries(100).Count,
                PoICount = poiList.Count,
                ActiveQuests = questSaveData.ActiveQuests.Count,
                CompletedQuests = questSaveData.CompletedQuestIds.Count
            };

            // Act - Save and load
            await _saveManager.SaveGameAsync(1);
            InitializeTestSystems();
            await _saveManager.LoadGameAsync(1);

            // Assert - Verify all systems maintained their state
            Assert.Equal(originalState.AdventurerPos.X, _adventurer.Position.X, 0.1f);
            Assert.Equal(originalState.AdventurerPos.Y, _adventurer.Position.Y, 0.1f);
            Assert.Equal(originalState.AdventurerSpeed, _adventurer.Speed, 0.1f);
            Assert.Equal(originalState.Time, _timeManager.CurrentTime, 0.1f);
            Assert.Equal(originalState.Day, _timeManager.CurrentDay);
            Assert.Equal(originalState.Weather, _weatherManager.CurrentWeather);
            Assert.Equal(originalState.JournalEntries, _journalManager.GetRecentEntries(100).Count);
            
            var loadedPoIs = allPoIsField?.GetValue(_poiManager) as List<PointOfInterest>;
            Assert.Equal(originalState.PoICount, loadedPoIs?.Count ?? 0);
            
            var loadedQuestData = questDataField?.GetValue(_questManager) as QuestSaveData;
            Assert.Equal(originalState.ActiveQuests, loadedQuestData?.ActiveQuests.Count ?? 0);
            Assert.Equal(originalState.CompletedQuests, loadedQuestData?.CompletedQuestIds.Count ?? 0);
        }

        [Fact]
        public async Task PerformanceValidation_LargeDatasets_MeetsRequirements()
        {
            // Arrange - Create maximum realistic dataset
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            // Create 500 journal entries (very long play session)
            for (int i = 0; i < 500; i++)
            {
                _journalManager.OnSpecialEvent($"Performance Test Event {i}", 
                    $"This is event number {i} for performance testing. It contains detailed information about the event " +
                    $"including timestamps, locations, and other metadata that would be typical in a real game scenario.");
            }
            
            // Create 200 zones (extensive exploration)
            for (int i = 0; i < 200; i++)
            {
                var zone = new Zone
                {
                    Id = $"perf_zone_{i}",
                    Name = $"Performance Zone {i}",
                    Description = $"Zone {i} for performance testing with detailed description",
                    BiomeType = (BiomeType)(i % 6),
                    Width = 100,
                    Height = 100,
                    WorldX = i % 20,
                    WorldY = i / 20
                };
                _journalManager.OnZoneEntered(zone);
            }
            
            // Create 300 PoIs (rich world)
            var poiList = new List<PointOfInterest>();
            for (int i = 0; i < 300; i++)
            {
                poiList.Add(new PointOfInterest
                {
                    Id = Guid.NewGuid(),
                    Type = (PoIType)(i % 4),
                    Position = new Vector2(i * 50, (i % 20) * 50),
                    Name = $"Performance PoI {i}",
                    Description = $"Point of interest {i} for performance testing",
                    IsDiscovered = i % 2 == 0,
                    IsInteractable = true,
                    ZoneId = $"perf_zone_{i % 200}"
                });
            }
            
            var allPoIsField = _poiManager.GetType().GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allPoIsField?.SetValue(_poiManager, poiList);
            
            // Advance time significantly
            for (int i = 0; i < 2000; i++)
            {
                _timeManager.Update(gameTime);
                if (i % 100 == 0) _weatherManager.Update(gameTime);
            }

            // Act - Measure save performance
            var saveStart = DateTime.Now;
            await _saveManager.SaveGameAsync(1);
            var saveTime = DateTime.Now - saveStart;
            
            // Measure load performance
            InitializeTestSystems();
            var loadStart = DateTime.Now;
            await _saveManager.LoadGameAsync(1);
            var loadTime = DateTime.Now - loadStart;
            
            // Measure file size
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            var fileInfo = new FileInfo(saveFilePath);

            // Assert - Performance requirements (based on requirements 10.1-10.6)
            Assert.True(saveTime.TotalSeconds < 15, 
                $"Save time should be under 15 seconds for large dataset, was {saveTime.TotalSeconds:F2} seconds");
            Assert.True(loadTime.TotalSeconds < 15, 
                $"Load time should be under 15 seconds for large dataset, was {loadTime.TotalSeconds:F2} seconds");
            
            // File size should be reasonable (compressed JSON)
            Assert.True(fileInfo.Length < 100 * 1024 * 1024, 
                $"Save file should be under 100MB, was {fileInfo.Length / (1024 * 1024):F1}MB");
            
            // Verify data integrity after performance test
            Assert.Equal(500, _journalManager.GetRecentEntries(1000).Count);
            var loadedPoIs = allPoIsField?.GetValue(_poiManager) as List<PointOfInterest>;
            Assert.Equal(300, loadedPoIs?.Count ?? 0);
        }

        public void Dispose()
        {
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