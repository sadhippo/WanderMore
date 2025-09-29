using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Xunit;
using HiddenHorizons;

namespace HiddenHorizons.Tests
{
    public class WorldSaveLoadTests
    {
        [Fact]
        public void TimeManager_SaveLoad_PreservesAllState()
        {
            // Arrange
            var timeManager = new TimeManager();
            timeManager.SetDayDuration(240f);
            timeManager.SetNightDuration(120f);
            
            // Simulate some time progression
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(100));
            timeManager.Update(gameTime);
            
            var originalCurrentTime = timeManager.CurrentTime;
            var originalTimeOfDay = timeManager.CurrentTimeOfDay;
            var originalDayProgress = timeManager.DayProgress;
            var originalCurrentDay = timeManager.CurrentDay;
            var originalDayDuration = timeManager.DayDuration;
            var originalNightDuration = timeManager.NightDuration;

            // Act - Save
            var saveData = timeManager.GetSaveData();
            
            // Create new instance and load
            var newTimeManager = new TimeManager();
            newTimeManager.LoadSaveData(saveData);

            // Assert
            Assert.Equal(originalCurrentTime, newTimeManager.CurrentTime, precision: 3);
            Assert.Equal(originalTimeOfDay, newTimeManager.CurrentTimeOfDay);
            Assert.Equal(originalDayProgress, newTimeManager.DayProgress, precision: 3);
            Assert.Equal(originalCurrentDay, newTimeManager.CurrentDay);
            Assert.Equal(originalDayDuration, newTimeManager.DayDuration, precision: 3);
            Assert.Equal(originalNightDuration, newTimeManager.NightDuration, precision: 3);
        }

        [Fact]
        public void TimeManager_SaveLoad_MaintainsTemporalConsistency()
        {
            // Arrange
            var timeManager = new TimeManager();
            
            // Progress through multiple day cycles
            for (int i = 0; i < 5; i++)
            {
                var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(100));
                timeManager.Update(gameTime);
            }
            
            var originalGameHour = timeManager.GetCurrentGameHour();
            var originalSeason = timeManager.GetSeason();
            var originalSeasonName = timeManager.GetSeasonName();

            // Act
            var saveData = timeManager.GetSaveData();
            var newTimeManager = new TimeManager();
            newTimeManager.LoadSaveData(saveData);

            // Assert
            Assert.Equal(originalGameHour, newTimeManager.GetCurrentGameHour(), precision: 3);
            Assert.Equal(originalSeason, newTimeManager.GetSeason());
            Assert.Equal(originalSeasonName, newTimeManager.GetSeasonName());
        }

        [Fact]
        public void WeatherManager_SaveLoad_PreservesAllState()
        {
            // Arrange
            var timeManager = new TimeManager();
            var weatherManager = new WeatherManager(timeManager, 12345);
            
            // Simulate some weather changes
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(50));
            weatherManager.Update(gameTime);
            
            var originalWeather = weatherManager.CurrentWeather;
            var originalIntensity = weatherManager.WeatherIntensity;

            // Act - Save
            var saveData = weatherManager.GetSaveData();
            
            // Create new instance and load
            var newWeatherManager = new WeatherManager(new TimeManager(), 0);
            newWeatherManager.LoadSaveData(saveData);

            // Assert
            Assert.Equal(originalWeather, newWeatherManager.CurrentWeather);
            Assert.Equal(originalIntensity, newWeatherManager.WeatherIntensity, precision: 3);
        }

        [Fact]
        public void WeatherManager_SaveLoad_MaintainsDeterministicBehavior()
        {
            // Arrange
            var timeManager = new TimeManager();
            var weatherManager1 = new WeatherManager(timeManager, 12345);
            
            // Progress the manager
            for (int i = 0; i < 3; i++)
            {
                var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(30));
                weatherManager1.Update(gameTime);
            }

            var originalWeather = weatherManager1.CurrentWeather;
            var originalIntensity = weatherManager1.WeatherIntensity;

            // Save and reload
            var saveData = weatherManager1.GetSaveData();
            var weatherManager2 = new WeatherManager(new TimeManager(), 0);
            weatherManager2.LoadSaveData(saveData);

            // Assert - Weather state should be preserved after load
            Assert.Equal(originalWeather, weatherManager2.CurrentWeather);
            Assert.Equal(originalIntensity, weatherManager2.WeatherIntensity, precision: 3);
            
            // Assert - Random seed should be preserved for future deterministic behavior
            var saveData1 = (WeatherManagerSaveData)weatherManager1.GetSaveData();
            var saveData2 = (WeatherManagerSaveData)weatherManager2.GetSaveData();
            Assert.Equal(saveData1.RandomSeed, saveData2.RandomSeed);
        }

        [Fact]
        public void ZoneManager_SaveLoad_PreservesZoneStructure()
        {
            // Arrange
            var zoneManager = new ZoneManager(12345);
            zoneManager.LoadContent(null); // Pass null for AssetManager as per instructions
            
            var originalCurrentZoneId = zoneManager.CurrentZone.Id;
            var originalZoneName = zoneManager.CurrentZone.Name;
            var originalBiomeType = zoneManager.CurrentZone.BiomeType;
            var originalWidth = zoneManager.CurrentZone.Width;
            var originalHeight = zoneManager.CurrentZone.Height;

            // Mark some tiles as explored
            zoneManager.MarkTileExplored(new Vector2(32, 32));
            zoneManager.MarkTileExplored(new Vector2(64, 64));

            // Act - Save
            var saveData = zoneManager.GetSaveData();
            
            // Create new instance and load (use same seed to ensure consistent zone generation)
            var newZoneManager = new ZoneManager(12345);
            // Load content first to initialize AssetManager reference
            newZoneManager.LoadContent(null); // Pass null for AssetManager
            // Then load save data which should preserve explored tiles
            newZoneManager.LoadSaveData(saveData);

            // Assert
            Assert.Equal(originalCurrentZoneId, newZoneManager.CurrentZone.Id);
            Assert.Equal(originalZoneName, newZoneManager.CurrentZone.Name);
            Assert.Equal(originalBiomeType, newZoneManager.CurrentZone.BiomeType);
            Assert.Equal(originalWidth, newZoneManager.CurrentZone.Width);
            Assert.Equal(originalHeight, newZoneManager.CurrentZone.Height);
            
            // Check explored tiles are preserved (32,32 = tile 1,1 and 64,64 = tile 2,2)
            Assert.True(newZoneManager.CurrentZone.ExploredTiles[1][1], "Tile [1,1] should be explored after save/load");
            Assert.True(newZoneManager.CurrentZone.ExploredTiles[2][2], "Tile [2,2] should be explored after save/load");
        }

        [Fact]
        public void ZoneManager_SaveLoad_PreservesZoneConnections()
        {
            // Arrange
            var zoneManager = new ZoneManager(12345);
            zoneManager.LoadContent(null);
            
            // Force zone transitions to create connections
            var currentZone = zoneManager.CurrentZone;
            var edgePosition = new Vector2(currentZone.Width * 32 + 10, currentZone.Height / 2 * 32);
            
            if (zoneManager.TryTransitionZone(edgePosition, out Vector2 newPosition))
            {
                var originalConnections = new Dictionary<Direction, string>(currentZone.Connections);
                var originalGeneratedConnections = new Dictionary<Direction, bool>(currentZone.GeneratedConnections);

                // Act - Save
                var saveData = zoneManager.GetSaveData();
                
                // Create new instance and load
                var newZoneManager = new ZoneManager(0);
                newZoneManager.LoadSaveData(saveData);
                newZoneManager.LoadContent(null);

                // Find the corresponding zone in the new manager
                var zoneManagerSaveData = (ZoneManagerSaveData)saveData;
                var savedZoneData = zoneManagerSaveData.Zones[currentZone.Id];

                // Assert
                Assert.Equal(originalConnections.Count, savedZoneData.Connections.Count);
                Assert.Equal(originalGeneratedConnections.Count, savedZoneData.GeneratedConnections.Count);
                
                foreach (var kvp in originalConnections)
                {
                    Assert.True(savedZoneData.Connections.ContainsKey(kvp.Key));
                    Assert.Equal(kvp.Value, savedZoneData.Connections[kvp.Key]);
                }
            }
        }

        [Fact]
        public void ZoneManager_SaveLoad_MaintainsDeterministicGeneration()
        {
            // Arrange
            var zoneManager1 = new ZoneManager(12345);
            var zoneManager2 = new ZoneManager(12345);
            
            zoneManager1.LoadContent(null);
            zoneManager2.LoadContent(null);
            
            var zone1 = zoneManager1.CurrentZone;
            var zone2 = zoneManager2.CurrentZone;

            // Assert initial zones are identical
            Assert.Equal(zone1.Id, zone2.Id);
            Assert.Equal(zone1.BiomeType, zone2.BiomeType);
            Assert.Equal(zone1.Width, zone2.Width);
            Assert.Equal(zone1.Height, zone2.Height);

            // Save and reload first manager
            var saveData = zoneManager1.GetSaveData();
            var zoneManager3 = new ZoneManager(0);
            zoneManager3.LoadSaveData(saveData);
            zoneManager3.LoadContent(null);

            var zone3 = zoneManager3.CurrentZone;

            // Assert reloaded zone matches original
            Assert.Equal(zone1.Id, zone3.Id);
            Assert.Equal(zone1.BiomeType, zone3.BiomeType);
            Assert.Equal(zone1.Width, zone3.Width);
            Assert.Equal(zone1.Height, zone3.Height);
        }

        [Fact]
        public void WorldSaveData_Integration_PreservesAllWorldState()
        {
            // Arrange
            var timeManager = new TimeManager();
            var weatherManager = new WeatherManager(timeManager, 12345);
            var zoneManager = new ZoneManager(12345);
            
            zoneManager.LoadContent(null);
            
            // Progress all systems
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(100));
            timeManager.Update(gameTime);
            weatherManager.Update(gameTime);
            
            // Mark some exploration (96,96 = tile 3,3)
            zoneManager.MarkTileExplored(new Vector2(96, 96));

            // Act - Create composite world save data
            var worldSaveData = new WorldSaveData
            {
                TimeData = (TimeManagerSaveData)timeManager.GetSaveData(),
                WeatherData = (WeatherManagerSaveData)weatherManager.GetSaveData(),
                ZoneData = (ZoneManagerSaveData)zoneManager.GetSaveData()
            };

            // Create new systems and load
            var newTimeManager = new TimeManager();
            var newWeatherManager = new WeatherManager(new TimeManager(), 0);
            var newZoneManager = new ZoneManager(12345);
            
            newTimeManager.LoadSaveData(worldSaveData.TimeData);
            newWeatherManager.LoadSaveData(worldSaveData.WeatherData);
            newZoneManager.LoadContent(null);
            newZoneManager.LoadSaveData(worldSaveData.ZoneData);

            // Assert all systems preserved their state
            Assert.Equal(timeManager.CurrentTime, newTimeManager.CurrentTime, precision: 3);
            Assert.Equal(timeManager.CurrentDay, newTimeManager.CurrentDay);
            Assert.Equal(weatherManager.CurrentWeather, newWeatherManager.CurrentWeather);
            Assert.Equal(weatherManager.WeatherIntensity, newWeatherManager.WeatherIntensity, precision: 3);
            Assert.Equal(zoneManager.CurrentZone.Id, newZoneManager.CurrentZone.Id);
            Assert.True(newZoneManager.CurrentZone.ExploredTiles[3][3], "Tile [3,3] should be explored after save/load");
        }

        [Fact]
        public void ISaveable_Implementation_HasCorrectMetadata()
        {
            // Arrange
            var timeManager = new TimeManager();
            var weatherManager = new WeatherManager(timeManager, 12345);
            var zoneManager = new ZoneManager(12345);

            // Assert
            Assert.Equal("TimeManager", timeManager.SaveKey);
            Assert.Equal(1, timeManager.SaveVersion);
            Assert.Equal("WeatherManager", weatherManager.SaveKey);
            Assert.Equal(1, weatherManager.SaveVersion);
            Assert.Equal("ZoneManager", zoneManager.SaveKey);
            Assert.Equal(1, zoneManager.SaveVersion);
        }

        [Fact]
        public void SaveData_Serialization_HandlesNullValues()
        {
            // Arrange
            var timeManager = new TimeManager();
            var weatherManager = new WeatherManager(timeManager, 12345);
            var zoneManager = new ZoneManager(12345);

            // Act - Get save data
            var timeSaveData = timeManager.GetSaveData();
            var weatherSaveData = weatherManager.GetSaveData();
            var zoneSaveData = zoneManager.GetSaveData();

            // Assert - All save data objects are not null
            Assert.NotNull(timeSaveData);
            Assert.NotNull(weatherSaveData);
            Assert.NotNull(zoneSaveData);
            
            Assert.IsType<TimeManagerSaveData>(timeSaveData);
            Assert.IsType<WeatherManagerSaveData>(weatherSaveData);
            Assert.IsType<ZoneManagerSaveData>(zoneSaveData);
        }

        [Fact]
        public void LoadSaveData_HandlesInvalidData()
        {
            // Arrange
            var timeManager = new TimeManager();
            var weatherManager = new WeatherManager(timeManager, 12345);
            var zoneManager = new ZoneManager(12345);

            var originalTimeDay = timeManager.CurrentDay;
            var originalWeather = weatherManager.CurrentWeather;

            // Act - Try to load invalid data
            timeManager.LoadSaveData(null);
            timeManager.LoadSaveData("invalid data");
            weatherManager.LoadSaveData(null);
            weatherManager.LoadSaveData(42);
            zoneManager.LoadSaveData(null);
            zoneManager.LoadSaveData(new object());

            // Assert - Systems should remain unchanged
            Assert.Equal(originalTimeDay, timeManager.CurrentDay);
            Assert.Equal(originalWeather, weatherManager.CurrentWeather);
        }
    }
}