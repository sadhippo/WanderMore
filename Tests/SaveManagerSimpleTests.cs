using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace HiddenHorizons.Tests
{
    public class SaveManagerSimpleTests : IDisposable
    {
        private readonly string _testSaveDirectory;
        private SaveManagerSimple _saveManager;
        private Adventurer _adventurer;
        private TimeManager _timeManager;

        public SaveManagerSimpleTests()
        {
            _testSaveDirectory = Path.Combine(Path.GetTempPath(), "SaveManagerSimpleTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSaveDirectory);
            InitializeTestSystems();
        }

        private void InitializeTestSystems()
        {
            _timeManager = new TimeManager();
            _adventurer = new Adventurer(new Vector2(100, 100));
            _saveManager = new SaveManagerSimple(_testSaveDirectory);
            
            _saveManager.RegisterSaveable(_adventurer);
            _saveManager.RegisterSaveable(_timeManager);
        }

        [Fact]
        public async Task SaveAndLoad_BasicOperation_WorksCorrectly()
        {
            // Arrange
            var originalPosition = new Vector2(123.45f, 678.90f);
            _adventurer.Position = originalPosition;
            
            // Act - Save
            await _saveManager.SaveGameAsync(1);
            
            // Verify save file exists and is readable JSON
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            Assert.True(File.Exists(saveFilePath));
            
            var jsonContent = await File.ReadAllTextAsync(saveFilePath);
            System.Console.WriteLine($"Save file content: {jsonContent}");
            Assert.Contains("\"version\":", jsonContent.ToLower());
            Assert.Contains("\"adventurer\":", jsonContent.ToLower());
            
            // Reset position
            _adventurer.Position = new Vector2(0, 0);
            
            // Act - Load
            await _saveManager.LoadGameAsync(1);
            
            // Assert
            Assert.Equal(originalPosition.X, _adventurer.Position.X, 0.01f);
            Assert.Equal(originalPosition.Y, _adventurer.Position.Y, 0.01f);
        }

        [Fact]
        public async Task SaveExists_WithExistingSave_ReturnsTrue()
        {
            // Arrange
            await _saveManager.SaveGameAsync(1);
            
            // Act & Assert
            Assert.True(_saveManager.SaveExists(1));
            Assert.False(_saveManager.SaveExists(2));
        }

        [Fact]
        public async Task GetSaveTimestamp_WithExistingSave_ReturnsTimestamp()
        {
            // Arrange
            var beforeSave = DateTime.UtcNow;
            await _saveManager.SaveGameAsync(1);
            var afterSave = DateTime.UtcNow;
            
            // Act
            var timestamp = await _saveManager.GetSaveTimestamp(1);
            
            // Assert
            Assert.NotNull(timestamp);
            Assert.True(timestamp >= beforeSave && timestamp <= afterSave);
        }

        [Fact]
        public async Task LoadGame_NonExistentSave_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _saveManager.LoadGameAsync(999));
        }

        public void Dispose()
        {
            if (Directory.Exists(_testSaveDirectory))
            {
                try
                {
                    Directory.Delete(_testSaveDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}