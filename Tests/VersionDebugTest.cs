using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace HiddenHorizons.Tests
{
    public class VersionDebugTest : IDisposable
    {
        private readonly string _testDirectory;
        private readonly VersionManager _versionManager;

        public VersionDebugTest()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "VersionDebugTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _versionManager = new VersionManager();
        }

        [Fact]
        public async Task DebugVersionDetection()
        {
            // Create a GameSaveData and serialize it the same way SaveManager does
            var saveData = new GameSaveData();
            
            // Use the same JSON options as SavePerformanceManager
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = false,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            string jsonData = JsonSerializer.Serialize(saveData, jsonOptions);
            
            // Write to file
            string testFilePath = Path.Combine(_testDirectory, "test_save.json");
            await File.WriteAllTextAsync(testFilePath, jsonData);
            
            // Debug: Print the JSON content
            Console.WriteLine($"JSON Content: {jsonData}");
            
            // Test version detection
            int detectedVersion = await _versionManager.DetectVersionAsync(testFilePath);
            
            Console.WriteLine($"Detected Version: {detectedVersion}");
            Console.WriteLine($"Is Compatible: {_versionManager.IsCompatible(detectedVersion)}");
            
            // This should not be -1
            Assert.NotEqual(-1, detectedVersion);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to cleanup test directory: {ex.Message}");
                }
            }
        }
    }
}