using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HiddenHorizons.Tests
{
    public class PoISaveLoadTests
    {
        private PoIManager CreateTestPoIManager()
        {
            // For save/load tests, we don't need actual asset loading functionality
            // We'll pass null and the PoIManager should handle it gracefully for testing
            AssetManager assetManager = null;
            var timeManager = new TimeManager();
            var journalManager = new JournalManager(timeManager);
            return new PoIManager(assetManager, journalManager, 12345); // Fixed seed for reproducible tests
        }

        private PointOfInterest CreateTestPoI(Guid? id = null, bool discovered = true, bool interacted = false)
        {
            return new PointOfInterest
            {
                Id = id ?? Guid.NewGuid(),
                Type = PoIType.Cottage,
                Position = new Vector2(100f, 200f),
                Name = "Test Cottage",
                Description = "A cozy test cottage",
                IsDiscovered = discovered,
                IsInteractable = true,
                InteractionRange = 48f,
                ZoneId = "test-zone-1",
                HasBeenInteracted = interacted,
                LastInteractionTime = interacted ? DateTime.Now.AddHours(-1) : default,
                InteractionCount = interacted ? 3 : 0,
                AssociatedQuests = new List<string> { "quest1", "quest2" },
                Properties = new Dictionary<string, object> { { "testKey", "testValue" } }
            };
        }

        [Fact]
        public void SaveKey_ReturnsCorrectValue()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();

            // Act & Assert
            Assert.Equal("PoIManager", poiManager.SaveKey);
        }

        [Fact]
        public void SaveVersion_ReturnsCorrectValue()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();

            // Act & Assert
            Assert.Equal(1, poiManager.SaveVersion);
        }

        [Fact]
        public void GetSaveData_WithEmptyPoIList_ReturnsEmptyData()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();

            // Act
            var saveData = poiManager.GetSaveData() as PoISaveData;

            // Assert
            Assert.NotNull(saveData);
            Assert.Empty(saveData.AllPoIs);
            Assert.Empty(saveData.ChunkPoIMapping);
        }

        [Fact]
        public void GetSaveData_WithSinglePoI_ReturnsSinglePoIData()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            var testId = Guid.NewGuid();
            var testPoI = CreateTestPoI(testId, discovered: true, interacted: true);
            
            // Use reflection to add PoI to internal list for testing
            var allPoIsField = typeof(PoIManager).GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var allPoIs = (List<PointOfInterest>)allPoIsField.GetValue(poiManager);
            allPoIs.Add(testPoI);

            // Act
            var saveData = poiManager.GetSaveData() as PoISaveData;

            // Assert
            Assert.NotNull(saveData);
            Assert.Single(saveData.AllPoIs);
            
            var savedPoI = saveData.AllPoIs[0];
            Assert.Equal(testId, savedPoI.Id);
            Assert.Equal(PoIType.Cottage, savedPoI.Type);
            Assert.Equal(new Vector2(100f, 200f), savedPoI.Position);
            Assert.Equal("Test Cottage", savedPoI.Name);
            Assert.Equal("A cozy test cottage", savedPoI.Description);
            Assert.True(savedPoI.IsDiscovered);
            Assert.True(savedPoI.IsInteractable);
            Assert.Equal(48f, savedPoI.InteractionRange, precision: 1);
            Assert.Equal("test-zone-1", savedPoI.ZoneId);
            Assert.True(savedPoI.HasBeenInteracted);
            Assert.NotNull(savedPoI.LastInteractionTime);
            Assert.Equal(3, savedPoI.InteractionCount);
            Assert.Equal(2, savedPoI.AssociatedQuests.Count);
            Assert.Contains("quest1", savedPoI.AssociatedQuests);
            Assert.Contains("quest2", savedPoI.AssociatedQuests);
            Assert.Single(savedPoI.Properties);
            Assert.Equal("testValue", savedPoI.Properties["testKey"]);
        }

        [Fact]
        public void GetSaveData_WithChunkMapping_ReturnsChunkMappingData()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            var testId1 = Guid.NewGuid();
            var testId2 = Guid.NewGuid();
            var testPoI1 = CreateTestPoI(testId1);
            var testPoI2 = CreateTestPoI(testId2);
            
            // Use reflection to set up internal data structures
            var allPoIsField = typeof(PoIManager).GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chunkPoIsField = typeof(PoIManager).GetField("_chunkPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var allPoIs = (List<PointOfInterest>)allPoIsField.GetValue(poiManager);
            var chunkPoIs = (Dictionary<Point, List<PointOfInterest>>)chunkPoIsField.GetValue(poiManager);
            
            allPoIs.Add(testPoI1);
            allPoIs.Add(testPoI2);
            
            var chunkPoint = new Point(1, 2);
            chunkPoIs[chunkPoint] = new List<PointOfInterest> { testPoI1, testPoI2 };

            // Act
            var saveData = poiManager.GetSaveData() as PoISaveData;

            // Assert
            Assert.NotNull(saveData);
            Assert.Equal(2, saveData.AllPoIs.Count);
            Assert.Single(saveData.ChunkPoIMapping);
            Assert.True(saveData.ChunkPoIMapping.ContainsKey(chunkPoint));
            Assert.Equal(2, saveData.ChunkPoIMapping[chunkPoint].Count);
            Assert.Contains(testId1, saveData.ChunkPoIMapping[chunkPoint]);
            Assert.Contains(testId2, saveData.ChunkPoIMapping[chunkPoint]);
        }

        [Fact]
        public void LoadSaveData_WithValidData_RestoresPoIsCorrectly()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            var testId = Guid.NewGuid();
            var testTime = DateTime.Now.AddHours(-2);
            
            var saveData = new PoISaveData
            {
                AllPoIs = new List<PointOfInterestSaveData>
                {
                    new PointOfInterestSaveData
                    {
                        Id = testId,
                        Type = PoIType.Inn,
                        Position = new Vector2(300f, 400f),
                        Name = "Test Inn",
                        Description = "A welcoming test inn",
                        IsDiscovered = true,
                        IsInteractable = true,
                        InteractionRange = 64f,
                        ZoneId = "test-zone-2",
                        HasBeenInteracted = true,
                        LastInteractionTime = testTime,
                        InteractionCount = 5,
                        AssociatedQuests = new List<string> { "innQuest" },
                        Properties = new Dictionary<string, object> { { "innKey", "innValue" } }
                    }
                },
                ChunkPoIMapping = new Dictionary<Point, List<Guid>>
                {
                    { new Point(3, 4), new List<Guid> { testId } }
                }
            };

            // Act
            poiManager.LoadSaveData(saveData);

            // Assert
            var loadedPoI = poiManager.GetPoIById(testId);
            Assert.NotNull(loadedPoI);
            Assert.Equal(testId, loadedPoI.Id);
            Assert.Equal(PoIType.Inn, loadedPoI.Type);
            Assert.Equal(new Vector2(300f, 400f), loadedPoI.Position);
            Assert.Equal("Test Inn", loadedPoI.Name);
            Assert.Equal("A welcoming test inn", loadedPoI.Description);
            Assert.True(loadedPoI.IsDiscovered);
            Assert.True(loadedPoI.IsInteractable);
            Assert.Equal(64f, loadedPoI.InteractionRange, precision: 1);
            Assert.Equal("test-zone-2", loadedPoI.ZoneId);
            Assert.True(loadedPoI.HasBeenInteracted);
            Assert.Equal(testTime, loadedPoI.LastInteractionTime, precision: TimeSpan.FromSeconds(1));
            Assert.Equal(5, loadedPoI.InteractionCount);
            Assert.Single(loadedPoI.AssociatedQuests);
            Assert.Contains("innQuest", loadedPoI.AssociatedQuests);
            Assert.Single(loadedPoI.Properties);
            Assert.Equal("innValue", loadedPoI.Properties["innKey"]);
        }

        [Fact]
        public void LoadSaveData_WithChunkMapping_RestoresChunkMappingCorrectly()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            var testId1 = Guid.NewGuid();
            var testId2 = Guid.NewGuid();
            var chunkPoint = new Point(5, 6);
            
            var saveData = new PoISaveData
            {
                AllPoIs = new List<PointOfInterestSaveData>
                {
                    new PointOfInterestSaveData { Id = testId1, Type = PoIType.Cottage, Position = Vector2.Zero, Name = "Cottage1", Description = "Test1" },
                    new PointOfInterestSaveData { Id = testId2, Type = PoIType.Castle, Position = Vector2.One, Name = "Castle1", Description = "Test2" }
                },
                ChunkPoIMapping = new Dictionary<Point, List<Guid>>
                {
                    { chunkPoint, new List<Guid> { testId1, testId2 } }
                }
            };

            // Act
            poiManager.LoadSaveData(saveData);

            // Assert
            // Use reflection to verify chunk mapping was restored
            var chunkPoIsField = typeof(PoIManager).GetField("_chunkPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chunkPoIs = (Dictionary<Point, List<PointOfInterest>>)chunkPoIsField.GetValue(poiManager);
            
            Assert.True(chunkPoIs.ContainsKey(chunkPoint));
            Assert.Equal(2, chunkPoIs[chunkPoint].Count);
            
            var poiIds = chunkPoIs[chunkPoint].Select(p => p.Id).ToList();
            Assert.Contains(testId1, poiIds);
            Assert.Contains(testId2, poiIds);
        }

        [Fact]
        public void LoadSaveData_WithNullLastInteractionTime_HandlesCorrectly()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            var testId = Guid.NewGuid();
            
            var saveData = new PoISaveData
            {
                AllPoIs = new List<PointOfInterestSaveData>
                {
                    new PointOfInterestSaveData
                    {
                        Id = testId,
                        Type = PoIType.Hermit,
                        Position = Vector2.Zero,
                        Name = "Test Hermit",
                        Description = "A wise hermit",
                        LastInteractionTime = null // Explicitly null
                    }
                }
            };

            // Act
            poiManager.LoadSaveData(saveData);

            // Assert
            var loadedPoI = poiManager.GetPoIById(testId);
            Assert.NotNull(loadedPoI);
            Assert.Equal(default(DateTime), loadedPoI.LastInteractionTime);
        }

        [Fact]
        public void LoadSaveData_WithInvalidData_HandlesGracefully()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            var invalidData = "not a PoISaveData object";

            // Act & Assert (should not throw)
            poiManager.LoadSaveData(invalidData);
            
            // Verify no PoIs were loaded
            var allPoIsField = typeof(PoIManager).GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var allPoIs = (List<PointOfInterest>)allPoIsField.GetValue(poiManager);
            Assert.Empty(allPoIs);
        }

        [Fact]
        public void LoadSaveData_ClearsExistingData_BeforeLoadingNew()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            
            // Add some initial data
            var initialPoI = CreateTestPoI();
            var allPoIsField = typeof(PoIManager).GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var allPoIs = (List<PointOfInterest>)allPoIsField.GetValue(poiManager);
            allPoIs.Add(initialPoI);
            
            var newTestId = Guid.NewGuid();
            var saveData = new PoISaveData
            {
                AllPoIs = new List<PointOfInterestSaveData>
                {
                    new PointOfInterestSaveData
                    {
                        Id = newTestId,
                        Type = PoIType.Mine,
                        Position = Vector2.Zero,
                        Name = "Test Mine",
                        Description = "A deep mine"
                    }
                }
            };

            // Act
            poiManager.LoadSaveData(saveData);

            // Assert
            Assert.Single(allPoIs);
            Assert.Equal(newTestId, allPoIs[0].Id);
            Assert.Null(poiManager.GetPoIById(initialPoI.Id)); // Initial PoI should be gone
        }

        [Fact]
        public void SaveLoad_RoundTrip_PreservesAllData()
        {
            // Arrange
            var poiManager = CreateTestPoIManager();
            var testId1 = Guid.NewGuid();
            var testId2 = Guid.NewGuid();
            var testTime = DateTime.Now.AddMinutes(-30);
            
            // Set up test data using reflection
            var allPoIsField = typeof(PoIManager).GetField("_allPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chunkPoIsField = typeof(PoIManager).GetField("_chunkPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var allPoIs = (List<PointOfInterest>)allPoIsField.GetValue(poiManager);
            var chunkPoIs = (Dictionary<Point, List<PointOfInterest>>)chunkPoIsField.GetValue(poiManager);
            
            var poi1 = new PointOfInterest
            {
                Id = testId1,
                Type = PoIType.Dragon,
                Position = new Vector2(500f, 600f),
                Name = "Ancient Dragon",
                Description = "A fearsome ancient dragon",
                IsDiscovered = true,
                IsInteractable = false,
                InteractionRange = 96f,
                ZoneId = "dragon-lair",
                HasBeenInteracted = true,
                LastInteractionTime = testTime,
                InteractionCount = 1,
                AssociatedQuests = new List<string> { "dragonQuest", "treasureQuest" },
                Properties = new Dictionary<string, object> { { "difficulty", "legendary" }, { "treasure", 1000 } }
            };
            
            var poi2 = CreateTestPoI(testId2, discovered: false, interacted: false);
            
            allPoIs.Add(poi1);
            allPoIs.Add(poi2);
            
            var chunkPoint1 = new Point(7, 8);
            var chunkPoint2 = new Point(9, 10);
            chunkPoIs[chunkPoint1] = new List<PointOfInterest> { poi1 };
            chunkPoIs[chunkPoint2] = new List<PointOfInterest> { poi2 };

            // Act - Save and then load
            var saveData = poiManager.GetSaveData();
            var newPoiManager = CreateTestPoIManager();
            newPoiManager.LoadSaveData(saveData);

            // Assert - Verify all data was preserved
            var loadedPoi1 = newPoiManager.GetPoIById(testId1);
            var loadedPoi2 = newPoiManager.GetPoIById(testId2);
            
            Assert.NotNull(loadedPoi1);
            Assert.NotNull(loadedPoi2);
            
            // Verify poi1 details
            Assert.Equal(PoIType.Dragon, loadedPoi1.Type);
            Assert.Equal(new Vector2(500f, 600f), loadedPoi1.Position);
            Assert.Equal("Ancient Dragon", loadedPoi1.Name);
            Assert.Equal("A fearsome ancient dragon", loadedPoi1.Description);
            Assert.True(loadedPoi1.IsDiscovered);
            Assert.False(loadedPoi1.IsInteractable);
            Assert.Equal(96f, loadedPoi1.InteractionRange, precision: 1);
            Assert.Equal("dragon-lair", loadedPoi1.ZoneId);
            Assert.True(loadedPoi1.HasBeenInteracted);
            Assert.Equal(testTime, loadedPoi1.LastInteractionTime, precision: TimeSpan.FromSeconds(1));
            Assert.Equal(1, loadedPoi1.InteractionCount);
            Assert.Equal(2, loadedPoi1.AssociatedQuests.Count);
            Assert.Contains("dragonQuest", loadedPoi1.AssociatedQuests);
            Assert.Contains("treasureQuest", loadedPoi1.AssociatedQuests);
            Assert.Equal(2, loadedPoi1.Properties.Count);
            Assert.Equal("legendary", loadedPoi1.Properties["difficulty"]);
            Assert.Equal(1000, loadedPoi1.Properties["treasure"]);
            
            // Verify poi2 details
            Assert.Equal(PoIType.Cottage, loadedPoi2.Type);
            Assert.False(loadedPoi2.IsDiscovered);
            Assert.False(loadedPoi2.HasBeenInteracted);
            Assert.Equal(0, loadedPoi2.InteractionCount);
            
            // Verify chunk mapping was preserved
            var newChunkPoIsField = typeof(PoIManager).GetField("_chunkPoIs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var newChunkPoIs = (Dictionary<Point, List<PointOfInterest>>)newChunkPoIsField.GetValue(newPoiManager);
            
            Assert.Equal(2, newChunkPoIs.Count);
            Assert.True(newChunkPoIs.ContainsKey(chunkPoint1));
            Assert.True(newChunkPoIs.ContainsKey(chunkPoint2));
            Assert.Single(newChunkPoIs[chunkPoint1]);
            Assert.Single(newChunkPoIs[chunkPoint2]);
            Assert.Equal(testId1, newChunkPoIs[chunkPoint1][0].Id);
            Assert.Equal(testId2, newChunkPoIs[chunkPoint2][0].Id);
        }
    }
}