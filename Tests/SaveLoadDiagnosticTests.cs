using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace HiddenHorizons.Tests
{
    /// <summary>
    /// Diagnostic tests to identify why save/load operations are failing
    /// Focuses on version detection, file format, and basic save/load mechanics
    /// </summary>
    public class SaveLoadDiagnosticTests : IDisposable
    {
        private readonly string _testSaveDirectory;
        private SaveManager _saveManager = null!;
        private Adventurer _adventurer = null!;
        private TimeManager _timeManager = null!;

        public SaveLoadDiagnosticTests()
        {
            _testSaveDirectory = Path.Combine(Path.GetTempPath(), "SaveLoadDiagnosticTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSaveDirectory);
            InitializeTestSystems();
        }

        private void InitializeTestSystems()
        {
            _timeManager = new TimeManager();
            _adventurer = new Adventurer(new Vector2(100, 100));
            
            var logDirectory = Path.Combine(_testSaveDirectory, "Logs");
            var logger = new SaveSystemLogger(logDirectory, $"DiagnosticTest_{Guid.NewGuid():N}");
            
            _saveManager = new SaveManager(_testSaveDirectory, logger: logger);
            _saveManager.RegisterSaveable(_adventurer);
            _saveManager.RegisterSaveable(_timeManager);
        }

        [Fact]
        public async Task Diagnostic_BasicSaveOperation_CreatesValidFile()
        {
            // Arrange
            _adventurer.Position = new Vector2(123.45f, 678.90f);
            
            // Act
            await _saveManager.SaveGameAsync(1);
            
            // Assert - Check if save file exists
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            Assert.True(File.Exists(saveFilePath), "Save file should exist");
            
            // Check file size
            var fileInfo = new FileInfo(saveFilePath);
            Assert.True(fileInfo.Length > 0, "Save file should not be empty");
            
            // Read and examine the raw file content
            var rawContent = await File.ReadAllTextAsync(saveFilePath);
            Assert.False(string.IsNullOrWhiteSpace(rawContent), "Save file should contain data");
            
            // Output for debugging
            System.Console.WriteLine($"Save file size: {fileInfo.Length} bytes");
            System.Console.WriteLine($"Save file content preview: {rawContent.Substring(0, Math.Min(200, rawContent.Length))}...");
        }

        [Fact]
        public async Task Diagnostic_SaveFileStructure_ContainsExpectedFields()
        {
            // Arrange
            _adventurer.Position = new Vector2(123.45f, 678.90f);
            
            // Act
            await _saveManager.SaveGameAsync(1);
            
            // Assert - Parse and examine the JSON structure
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            var jsonContent = await File.ReadAllTextAsync(saveFilePath);
            
            // Try to parse as JSON
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(jsonContent);
            }
            catch (JsonException ex)
            {
                Assert.True(false, $"Save file is not valid JSON: {ex.Message}");
                return;
            }

            var root = document.RootElement;
            
            // Check for version field
            Assert.True(root.TryGetProperty("Version", out var versionElement), "Save file should contain Version field");
            System.Console.WriteLine($"Version in save file: {versionElement}");
            
            // Check for timestamp
            Assert.True(root.TryGetProperty("SaveTimestamp", out var timestampElement) || 
                       root.TryGetProperty("saveTimestamp", out timestampElement), "Save file should contain SaveTimestamp field");
            System.Console.WriteLine($"Timestamp in save file: {timestampElement}");
            
            // Check for systems data
            Assert.True(root.TryGetProperty("SystemData", out var systemsElement) || 
                       root.TryGetProperty("systemData", out systemsElement), "Save file should contain SystemData field");
            Assert.True(systemsElement.ValueKind == JsonValueKind.Object, "SystemData should be an object");
            
            // List all system keys
            System.Console.WriteLine("Systems in save file:");
            foreach (var property in systemsElement.EnumerateObject())
            {
                System.Console.WriteLine($"  - {property.Name}");
            }
        }

        [Fact]
        public async Task Diagnostic_VersionDetection_ReturnsCorrectVersion()
        {
            // Arrange
            _adventurer.Position = new Vector2(123.45f, 678.90f);
            await _saveManager.SaveGameAsync(1);
            
            // Act - Test version detection directly
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            var versionManager = new VersionManager();
            
            int detectedVersion;
            try
            {
                detectedVersion = await versionManager.DetectVersionAsync(saveFilePath);
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Version detection failed: {ex.Message}");
                return;
            }
            
            // Assert
            System.Console.WriteLine($"Detected version: {detectedVersion}");
            Assert.True(detectedVersion >= 0, $"Version should be non-negative, got {detectedVersion}");
            Assert.NotEqual(-1, detectedVersion); // -1 indicates detection failure
        }

        [Fact]
        public async Task Diagnostic_LoadOperation_StepByStep()
        {
            // Arrange - Save first
            var originalPosition = new Vector2(123.45f, 678.90f);
            _adventurer.Position = originalPosition;
            await _saveManager.SaveGameAsync(1);
            
            // Reset adventurer position to verify load works
            _adventurer.Position = new Vector2(0, 0);
            
            // Act - Try to load step by step
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            
            // Step 1: Check if file exists
            Assert.True(File.Exists(saveFilePath), "Save file should exist before loading");
            
            // Step 2: Check version detection
            var versionManager = new VersionManager();
            var version = await versionManager.DetectVersionAsync(saveFilePath);
            System.Console.WriteLine($"Version detected during load: {version}");
            
            // Step 3: Check version compatibility
            var isCompatible = versionManager.IsCompatible(version);
            System.Console.WriteLine($"Version {version} is compatible: {isCompatible}");
            Assert.True(isCompatible, $"Version {version} should be compatible");
            
            // Step 4: Attempt the actual load
            Exception loadException = null;
            try
            {
                await _saveManager.LoadGameAsync(1);
            }
            catch (Exception ex)
            {
                loadException = ex;
                System.Console.WriteLine($"Load failed with exception: {ex.GetType().Name}: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            // Assert - Load should succeed
            Assert.Null(loadException);
            Assert.Equal(originalPosition.X, _adventurer.Position.X, 0.01f);
            Assert.Equal(originalPosition.Y, _adventurer.Position.Y, 0.01f);
        }

        [Fact]
        public async Task Diagnostic_SaveFileContent_ManualInspection()
        {
            // Arrange
            _adventurer.Position = new Vector2(999.99f, 111.11f);
            _adventurer.Speed = 150.5f;
            
            // Act
            await _saveManager.SaveGameAsync(1);
            
            // Read and output the entire save file for manual inspection
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            var content = await File.ReadAllTextAsync(saveFilePath);
            
            System.Console.WriteLine("=== COMPLETE SAVE FILE CONTENT ===");
            System.Console.WriteLine(content);
            System.Console.WriteLine("=== END SAVE FILE CONTENT ===");
            
            // Also check if the file is properly formatted JSON
            try
            {
                var formatted = JsonSerializer.Serialize(
                    JsonSerializer.Deserialize<object>(content), 
                    new JsonSerializerOptions { WriteIndented = true }
                );
                System.Console.WriteLine("=== FORMATTED JSON ===");
                System.Console.WriteLine(formatted);
                System.Console.WriteLine("=== END FORMATTED JSON ===");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to format JSON: {ex.Message}");
            }
        }

        [Fact]
        public async Task Diagnostic_CompareWorkingVsFailingLoad()
        {
            // This test will help us understand the difference between what works and what doesn't
            
            // Arrange - Create a minimal save
            _adventurer.Position = new Vector2(50, 50);
            
            // Act - Save
            await _saveManager.SaveGameAsync(1);
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            
            // Read the save file content
            var saveContent = await File.ReadAllTextAsync(saveFilePath);
            System.Console.WriteLine("Save file created successfully");
            System.Console.WriteLine($"File size: {new FileInfo(saveFilePath).Length} bytes");
            
            // Try to manually parse what the version manager would see
            try
            {
                var doc = JsonDocument.Parse(saveContent);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("Version", out var versionProp))
                {
                    System.Console.WriteLine($"Version property found: {versionProp}");
                    System.Console.WriteLine($"Version property type: {versionProp.ValueKind}");
                    
                    if (versionProp.ValueKind == JsonValueKind.Number)
                    {
                        var versionValue = versionProp.GetInt32();
                        System.Console.WriteLine($"Version value: {versionValue}");
                    }
                }
                else
                {
                    System.Console.WriteLine("No Version property found in save file!");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to parse save file: {ex.Message}");
            }
            
            // Now try the actual load and see what happens
            _adventurer.Position = new Vector2(0, 0); // Reset position
            
            try
            {
                await _saveManager.LoadGameAsync(1);
                System.Console.WriteLine("Load succeeded!");
                System.Console.WriteLine($"Loaded position: {_adventurer.Position}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Load failed: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
            }
        }

        [Fact]
        public async Task Diagnostic_VersionManagerBehavior_Isolated()
        {
            // Test the VersionManager in isolation to see if it's the source of the problem
            
            // Create a simple test JSON file with version
            var testJsonContent = @"{
                ""Version"": 1,
                ""Timestamp"": ""2023-01-01T00:00:00Z"",
                ""Systems"": {
                    ""TestSystem"": {
                        ""TestData"": ""TestValue""
                    }
                }
            }";
            
            var testFilePath = Path.Combine(_testSaveDirectory, "test_version.json");
            await File.WriteAllTextAsync(testFilePath, testJsonContent);
            
            // Test version detection
            var versionManager = new VersionManager();
            
            try
            {
                var detectedVersion = await versionManager.DetectVersionAsync(testFilePath);
                System.Console.WriteLine($"Version detected from test file: {detectedVersion}");
                Assert.Equal(1, detectedVersion);
                
                var isCompatible = versionManager.IsCompatible(detectedVersion);
                System.Console.WriteLine($"Version 1 compatibility: {isCompatible}");
                
                var needsMigration = versionManager.NeedsMigration(detectedVersion);
                System.Console.WriteLine($"Version 1 needs migration: {needsMigration}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"VersionManager test failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task Diagnostic_SaveManagerInternals_CheckSaveData()
        {
            // This test examines what the SaveManager is actually trying to save
            
            // Arrange
            _adventurer.Position = new Vector2(42.0f, 84.0f);
            
            // Get the save data that would be written
            var gameSaveData = new GameSaveData
            {
                Version = 1, // Explicitly set version
                SaveTimestamp = DateTime.UtcNow,
                SystemData = new Dictionary<string, object>()
            };
            
            // Manually add system data like SaveManager would
            gameSaveData.SystemData[_adventurer.SaveKey] = _adventurer.GetSaveData();
            gameSaveData.SystemData[_timeManager.SaveKey] = _timeManager.GetSaveData();
            
            // Serialize this manually to see what it looks like
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var serializedData = JsonSerializer.Serialize(gameSaveData, jsonOptions);
            System.Console.WriteLine("=== MANUAL SERIALIZATION ===");
            System.Console.WriteLine(serializedData);
            System.Console.WriteLine("=== END MANUAL SERIALIZATION ===");
            
            // Now do the actual save and compare
            await _saveManager.SaveGameAsync(1);
            
            var saveFilePath = Path.Combine(_testSaveDirectory, "slot_1", "save.json");
            var actualSaveContent = await File.ReadAllTextAsync(saveFilePath);
            
            System.Console.WriteLine("=== ACTUAL SAVE CONTENT ===");
            System.Console.WriteLine(actualSaveContent);
            System.Console.WriteLine("=== END ACTUAL SAVE CONTENT ===");
            
            // Compare the two
            Assert.Contains("\"version\":", actualSaveContent.ToLower());
            Assert.Contains("\"savetimestamp\":", actualSaveContent.ToLower());
            Assert.Contains("\"systemdata\":", actualSaveContent.ToLower());
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