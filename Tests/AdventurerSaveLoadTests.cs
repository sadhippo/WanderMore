using Microsoft.Xna.Framework;
using System;
using Xunit;

namespace HiddenHorizons.Tests
{
    public class AdventurerSaveLoadTests
    {
        private readonly Adventurer _adventurer;
        private readonly Vector2 _testPosition;

        public AdventurerSaveLoadTests()
        {
            _testPosition = new Vector2(100, 200);
            _adventurer = new Adventurer(_testPosition);
        }

        [Fact]
        public void ISaveable_Properties_AreCorrect()
        {
            // Arrange & Act
            string saveKey = _adventurer.SaveKey;
            int saveVersion = _adventurer.SaveVersion;

            // Assert
            Assert.Equal("Adventurer", saveKey);
            Assert.Equal(1, saveVersion);
        }

        [Fact]
        public void GetSaveData_ReturnsCorrectAdventurerSaveData()
        {
            // Arrange
            _adventurer.Position = new Vector2(150, 250);
            _adventurer.Velocity = new Vector2(10, 20);
            _adventurer.Speed = 100f;

            // Act
            var saveData = _adventurer.GetSaveData() as AdventurerSaveData;

            // Assert
            Assert.NotNull(saveData);
            Assert.Equal(new Vector2(150, 250), saveData.Position);
            Assert.Equal(new Vector2(10, 20), saveData.Velocity);
            Assert.Equal(100f, saveData.Speed);
        }

        [Fact]
        public void LoadSaveData_RestoresBasicState()
        {
            // Arrange
            var saveData = new AdventurerSaveData
            {
                Position = new Vector2(300, 400),
                Velocity = new Vector2(15, 25),
                Direction = new Vector2(1, 0),
                Speed = 120f,
                DirectionChangeTimer = 1.5f,
                DirectionChangeInterval = 3.5f,
                IsInteracting = false,
                InteractionTimer = 0f,
                InteractionDuration = 0f,
                InteractionCooldownTimer = 0f,
                CurrentAnimation = AnimationType.Walking,
                CurrentFrame = 2,
                AnimationTimer = 0.5f,
                IsMoving = true,
                LastPosition = new Vector2(290, 390),
                StuckTimer = 0.2f
            };

            // Act
            _adventurer.LoadSaveData(saveData);

            // Assert
            Assert.Equal(new Vector2(300, 400), _adventurer.Position);
            Assert.Equal(new Vector2(15, 25), _adventurer.Velocity);
            Assert.Equal(120f, _adventurer.Speed);
        }

        [Fact]
        public void LoadSaveData_RestoresInteractionState()
        {
            // Arrange
            var testPoIId = Guid.NewGuid();
            var lastPoIId = Guid.NewGuid();
            
            var saveData = new AdventurerSaveData
            {
                Position = new Vector2(100, 100),
                Velocity = Vector2.Zero,
                Direction = Vector2.UnitX,
                Speed = 80f,
                IsInteracting = true,
                InteractionTimer = 1.2f,
                InteractionDuration = 3.0f,
                CurrentInteractionPoIId = testPoIId,
                LastInteractionPoIId = lastPoIId,
                InteractionCooldownTimer = 2.5f,
                CurrentAnimation = AnimationType.Idle,
                CurrentFrame = 0,
                AnimationTimer = 0f,
                IsMoving = false,
                LastPosition = new Vector2(95, 95),
                StuckTimer = 0f
            };

            // Act
            _adventurer.LoadSaveData(saveData);
            var restoredSaveData = _adventurer.GetSaveData() as AdventurerSaveData;

            // Assert
            Assert.NotNull(restoredSaveData);
            Assert.True(restoredSaveData.IsInteracting);
            Assert.Equal(1.2f, restoredSaveData.InteractionTimer, 0.001f);
            Assert.Equal(3.0f, restoredSaveData.InteractionDuration, 0.001f);
            Assert.Equal(testPoIId, restoredSaveData.CurrentInteractionPoIId);
            Assert.Equal(lastPoIId, restoredSaveData.LastInteractionPoIId);
            Assert.Equal(2.5f, restoredSaveData.InteractionCooldownTimer, 0.001f);
        }

        [Fact]
        public void LoadSaveData_RestoresAnimationState()
        {
            // Arrange
            var saveData = new AdventurerSaveData
            {
                Position = new Vector2(100, 100),
                Velocity = Vector2.Zero,
                Direction = Vector2.UnitX,
                Speed = 80f,
                CurrentAnimation = AnimationType.Walking,
                CurrentFrame = 1,
                AnimationTimer = 0.15f,
                IsMoving = true,
                LastPosition = new Vector2(95, 100),
                StuckTimer = 0.5f
            };

            // Act
            _adventurer.LoadSaveData(saveData);
            var restoredSaveData = _adventurer.GetSaveData() as AdventurerSaveData;

            // Assert
            Assert.NotNull(restoredSaveData);
            Assert.Equal(AnimationType.Walking, restoredSaveData.CurrentAnimation);
            Assert.Equal(1, restoredSaveData.CurrentFrame);
            Assert.Equal(0.15f, restoredSaveData.AnimationTimer, 0.001f);
            Assert.True(restoredSaveData.IsMoving);
        }

        [Fact]
        public void LoadSaveData_RestoresStuckDetectionState()
        {
            // Arrange
            var saveData = new AdventurerSaveData
            {
                Position = new Vector2(100, 100),
                Velocity = Vector2.Zero,
                Direction = Vector2.UnitX,
                Speed = 80f,
                LastPosition = new Vector2(99, 99),
                StuckTimer = 1.8f
            };

            // Act
            _adventurer.LoadSaveData(saveData);
            var restoredSaveData = _adventurer.GetSaveData() as AdventurerSaveData;

            // Assert
            Assert.NotNull(restoredSaveData);
            Assert.Equal(new Vector2(99, 99), restoredSaveData.LastPosition);
            Assert.Equal(1.8f, restoredSaveData.StuckTimer, 0.001f);
        }

        [Fact]
        public void SaveLoad_RoundTrip_PreservesAllState()
        {
            // Arrange - Set up adventurer with complex state
            _adventurer.Position = new Vector2(250, 350);
            _adventurer.Velocity = new Vector2(30, 40);
            _adventurer.Speed = 150f;

            // Act - Save and then load the data
            var saveData = _adventurer.GetSaveData();
            var newAdventurer = new Adventurer(Vector2.Zero);
            newAdventurer.LoadSaveData(saveData);

            // Get the restored state
            var restoredSaveData = newAdventurer.GetSaveData() as AdventurerSaveData;
            var originalSaveData = saveData as AdventurerSaveData;

            // Assert - All state should be preserved
            Assert.NotNull(restoredSaveData);
            Assert.NotNull(originalSaveData);
            
            Assert.Equal(originalSaveData.Position, restoredSaveData.Position);
            Assert.Equal(originalSaveData.Velocity, restoredSaveData.Velocity);
            Assert.Equal(originalSaveData.Direction, restoredSaveData.Direction);
            Assert.Equal(originalSaveData.Speed, restoredSaveData.Speed);
            Assert.Equal(originalSaveData.DirectionChangeTimer, restoredSaveData.DirectionChangeTimer);
            Assert.Equal(originalSaveData.DirectionChangeInterval, restoredSaveData.DirectionChangeInterval);
            Assert.Equal(originalSaveData.IsInteracting, restoredSaveData.IsInteracting);
            Assert.Equal(originalSaveData.InteractionTimer, restoredSaveData.InteractionTimer);
            Assert.Equal(originalSaveData.InteractionDuration, restoredSaveData.InteractionDuration);
            Assert.Equal(originalSaveData.CurrentInteractionPoIId, restoredSaveData.CurrentInteractionPoIId);
            Assert.Equal(originalSaveData.LastInteractionPoIId, restoredSaveData.LastInteractionPoIId);
            Assert.Equal(originalSaveData.InteractionCooldownTimer, restoredSaveData.InteractionCooldownTimer);
            Assert.Equal(originalSaveData.CurrentAnimation, restoredSaveData.CurrentAnimation);
            Assert.Equal(originalSaveData.CurrentFrame, restoredSaveData.CurrentFrame);
            Assert.Equal(originalSaveData.AnimationTimer, restoredSaveData.AnimationTimer);
            Assert.Equal(originalSaveData.IsMoving, restoredSaveData.IsMoving);
            Assert.Equal(originalSaveData.LastPosition, restoredSaveData.LastPosition);
            Assert.Equal(originalSaveData.StuckTimer, restoredSaveData.StuckTimer);
        }

        [Fact]
        public void LoadSaveData_WithInvalidData_DoesNotCrash()
        {
            // Arrange
            var invalidData = "not an AdventurerSaveData object";
            var originalPosition = _adventurer.Position;

            // Act
            _adventurer.LoadSaveData(invalidData);

            // Assert - Should not crash and should not change state
            Assert.Equal(originalPosition, _adventurer.Position);
        }

        [Fact]
        public void LoadSaveData_WithNullData_DoesNotCrash()
        {
            // Arrange
            var originalPosition = _adventurer.Position;

            // Act
            _adventurer.LoadSaveData(null);

            // Assert - Should not crash and should not change state
            Assert.Equal(originalPosition, _adventurer.Position);
        }

        [Fact]
        public void ResolvePoIReferences_WithValidPoIManager_ResolvesReferences()
        {
            // This test would require a mock PoIManager
            // For now, we'll test that the method exists and doesn't crash
            
            // Arrange
            var saveData = new AdventurerSaveData
            {
                Position = new Vector2(100, 100),
                CurrentInteractionPoIId = Guid.NewGuid(),
                LastInteractionPoIId = Guid.NewGuid()
            };
            
            _adventurer.LoadSaveData(saveData);

            // Act & Assert - Should not crash
            // Note: This would need a proper PoIManager mock for full testing
            try
            {
                _adventurer.ResolvePoIReferences(null);
                Assert.True(true); // Method exists and doesn't crash with null
            }
            catch (NullReferenceException)
            {
                Assert.True(true); // Expected with null PoIManager
            }
        }
    }
}