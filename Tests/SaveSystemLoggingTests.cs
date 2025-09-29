using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HiddenHorizons;

namespace HiddenHorizons.Tests
{
    public class SaveSystemLoggingTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _logDirectory;
        private SaveSystemLogger _logger;
        private SaveSystemDiagnostics _diagnostics;

        public SaveSystemLoggingTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "SaveSystemLoggingTests", Guid.NewGuid().ToString());
            _logDirectory = Path.Combine(_testDirectory, "Logs");
            Directory.CreateDirectory(_testDirectory);
            Directory.CreateDirectory(_logDirectory);

            // Don't create logger in constructor - create per test to avoid file conflicts
        }

        private void InitializeLogger(string testName = null)
        {
            var scopeName = testName ?? Guid.NewGuid().ToString();
            _logger = new SaveSystemLogger(_logDirectory, scopeName);
            _diagnostics = new SaveSystemDiagnostics(_logger, _testDirectory);
        }

        public void Dispose()
        {
            _logger?.Dispose();
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        [Fact]
        public void Logger_InitializesCorrectly()
        {
            // Arrange & Act
            InitializeLogger("InitTest");

            // Assert
            Assert.Equal(SaveLogLevel.Info, _logger.MinimumLevel);
            Assert.False(_logger.VerboseLogging);
        }

        [Fact]
        public void Logger_LogsAtDifferentLevels()
        {
            // Arrange
            InitializeLogger("LogLevelsTest");
            _logger.MinimumLevel = SaveLogLevel.Trace;
            var context = new SaveLogContext().WithSlot(1).WithOperation("Test");

            // Act
            _logger.LogTrace("Trace message", context);
            _logger.LogDebug("Debug message", context);
            _logger.LogInfo("Info message", context);
            _logger.LogWarning("Warning message", null, context);
            _logger.LogError("Error message", null, context);
            _logger.LogCritical("Critical message", null, context);

            // Flush and dispose to ensure file is written
            _logger.FlushAsync().Wait();
            _logger.Dispose();

            // Assert - Check that log file exists and contains entries
            var logFiles = Directory.GetFiles(_logDirectory, "*LogLevelsTest*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.Contains("TRACE", logContent);
            Assert.Contains("DEBUG", logContent);
            Assert.Contains("INFO", logContent);
            Assert.Contains("WARNING", logContent);
            Assert.Contains("ERROR", logContent);
            Assert.Contains("CRITICAL", logContent);
            Assert.Contains("Slot=1", logContent);
            Assert.Contains("Op=Test", logContent);
        }

        [Fact]
        public void Logger_RespectsMinimumLevel()
        {
            // Arrange
            InitializeLogger("MinLevelTest");
            _logger.MinimumLevel = SaveLogLevel.Warning;

            // Act
            _logger.LogTrace("Should not appear");
            _logger.LogDebug("Should not appear");
            _logger.LogInfo("Should not appear");
            _logger.LogWarning("Should appear");
            _logger.LogError("Should appear");

            // Flush and dispose to ensure file is written
            _logger.FlushAsync().Wait();
            _logger.Dispose();

            // Assert
            var logFiles = Directory.GetFiles(_logDirectory, "*MinLevelTest*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.DoesNotContain("Should not appear", logContent);
            Assert.Contains("Should appear", logContent);
        }

        [Fact]
        public void Logger_LogsTimingInformation()
        {
            // Arrange
            var operation = "TestOperation";
            var duration = TimeSpan.FromMilliseconds(150);
            var context = new SaveLogContext().WithSlot(2);

            // Act
            _logger.LogTiming(operation, duration, context);

            // Assert
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.Contains("TestOperation", logContent);
            Assert.Contains("150", logContent);
            Assert.Contains("ms", logContent);
            Assert.Contains("Slot=2", logContent);
        }

        [Fact]
        public void Logger_LogsFileSizeInformation()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "test.json");
            var fileSize = 1024 * 1024; // 1MB
            var context = new SaveLogContext().WithFile(filePath);

            // Act
            _logger.LogFileSize(filePath, fileSize, context);

            // Assert
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.Contains("test.json", logContent);
            Assert.Contains("1.0 MB", logContent);
            Assert.Contains("File=test.json", logContent);
        }

        [Fact]
        public void Logger_LogsSystemState()
        {
            // Arrange
            _logger.VerboseLogging = true;
            _logger.MinimumLevel = SaveLogLevel.Debug;
            var systemName = "TestSystem";
            var state = new { Property1 = "Value1", Property2 = 42 };
            var context = new SaveLogContext().WithSystem(systemName);

            // Act
            _logger.LogSystemState(systemName, state, context);

            // Assert
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.Contains("System state", logContent);
            Assert.Contains("TestSystem", logContent);
            Assert.Contains("System=TestSystem", logContent);
        }

        [Fact]
        public void Logger_CreatesScope()
        {
            // Arrange & Act
            using var scopedLogger = _logger.CreateScope("TestScope");

            // Assert
            Assert.NotNull(scopedLogger);
            Assert.Equal(_logger.MinimumLevel, scopedLogger.MinimumLevel);
            Assert.Equal(_logger.VerboseLogging, scopedLogger.VerboseLogging);

            // Test that scoped logger works
            scopedLogger.LogInfo("Scoped message");

            var logFiles = Directory.GetFiles(_logDirectory, "*TestScope*.log");
            Assert.Single(logFiles);
        }

        [Fact]
        public async Task Logger_FlushesCorrectly()
        {
            // Arrange
            _logger.LogInfo("Test message before flush");

            // Act
            await _logger.FlushAsync();

            // Assert - Should not throw and log file should exist
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            Assert.Single(logFiles);
            Assert.True(new FileInfo(logFiles[0]).Length > 0);
        }

        [Fact]
        public void SaveLogContext_BuildsCorrectly()
        {
            // Arrange & Act
            var context = new SaveLogContext()
                .WithSlot(5)
                .WithOperation("TestOp")
                .WithSystem("TestSystem")
                .WithFile("/path/to/file.json")
                .WithProperty("CustomProp", "CustomValue");

            // Assert
            Assert.Equal(5, context.SlotId);
            Assert.Equal("TestOp", context.Operation);
            Assert.Equal("TestSystem", context.SystemName);
            Assert.Equal("/path/to/file.json", context.FilePath);
            Assert.Equal("CustomValue", context.Properties["CustomProp"]);
        }

        [Fact]
        public async Task Diagnostics_PerformsHealthCheck()
        {
            // Arrange
            InitializeLogger("HealthCheckTest");
            
            // Create some test save structure
            var saveDir = Path.Combine(_testDirectory, "Saves");
            var slotDir = Path.Combine(saveDir, "slot_1");
            Directory.CreateDirectory(slotDir);
            await File.WriteAllTextAsync(Path.Combine(slotDir, "save.json"), "{\"Version\":1}");

            // Act
            var healthReport = await _diagnostics.PerformHealthCheckAsync();

            // Assert
            Assert.NotNull(healthReport);
            Assert.True(healthReport.ComponentResults.Count > 0);
            Assert.True(healthReport.TotalCheckDuration.TotalMilliseconds > 0);
            
            // Check that we have expected components
            var componentNames = healthReport.ComponentResults.ConvertAll(r => r.ComponentName);
            Assert.Contains("DiskSpace", componentNames);
            Assert.Contains("DirectoryStructure", componentNames);
            Assert.Contains("FileIntegrity", componentNames);
        }

        [Fact]
        public void Diagnostics_RecordsSaveOperation()
        {
            // Arrange
            InitializeLogger("SaveOpTest");
            var duration = TimeSpan.FromMilliseconds(250);
            var slotId = 3;
            var fileSize = 2048L;

            // Act
            _diagnostics.RecordSaveOperation(duration, true, slotId, fileSize);

            // Assert
            var metrics = _diagnostics.GetCurrentMetrics();
            Assert.Equal(1, metrics.SaveOperationsCount);
            Assert.True(metrics.LastSuccessfulSave > DateTime.MinValue);
            Assert.True(metrics.AverageSaveTime.TotalMilliseconds > 0);
        }

        [Fact]
        public void Diagnostics_RecordsLoadOperation()
        {
            // Arrange
            InitializeLogger("LoadOpTest");
            var duration = TimeSpan.FromMilliseconds(180);
            var slotId = 2;
            var fileSize = 1536L;

            // Act
            _diagnostics.RecordLoadOperation(duration, true, slotId, fileSize);

            // Assert
            var metrics = _diagnostics.GetCurrentMetrics();
            Assert.Equal(1, metrics.LoadOperationsCount);
            Assert.True(metrics.LastSuccessfulLoad > DateTime.MinValue);
            Assert.True(metrics.AverageLoadTime.TotalMilliseconds > 0);
        }

        [Fact]
        public void Diagnostics_RecordsFailedOperations()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            _diagnostics.RecordSaveOperation(duration, false, 1);
            _diagnostics.RecordLoadOperation(duration, false, 2);

            // Assert
            var metrics = _diagnostics.GetCurrentMetrics();
            Assert.Equal(2, metrics.ErrorCount);
            Assert.Equal(1, metrics.SaveOperationsCount);
            Assert.Equal(1, metrics.LoadOperationsCount);
        }

        [Fact]
        public void Diagnostics_CalculatesAverageTimesCorrectly()
        {
            // Arrange
            var durations = new[] { 100, 200, 300 }; // milliseconds

            // Act
            foreach (var duration in durations)
            {
                _diagnostics.RecordSaveOperation(TimeSpan.FromMilliseconds(duration), true, 1);
            }

            // Assert
            var metrics = _diagnostics.GetCurrentMetrics();
            Assert.Equal(3, metrics.SaveOperationsCount);
            
            // Average should be 200ms (100+200+300)/3
            Assert.Equal(200.0, metrics.AverageSaveTime.TotalMilliseconds, precision: 1);
        }

        [Fact]
        public async Task Diagnostics_ExportsDiagnosticData()
        {
            // Arrange
            var exportPath = Path.Combine(_testDirectory, "diagnostics.json");
            
            // Record some operations first
            _diagnostics.RecordSaveOperation(TimeSpan.FromMilliseconds(150), true, 1, 1024);
            _diagnostics.RecordLoadOperation(TimeSpan.FromMilliseconds(120), true, 1, 1024);

            // Act
            await _diagnostics.ExportDiagnosticsAsync(exportPath);

            // Assert
            Assert.True(File.Exists(exportPath));
            var content = await File.ReadAllTextAsync(exportPath);
            Assert.Contains("exportTime", content);
            Assert.Contains("healthReport", content);
            Assert.Contains("systemInfo", content);
        }

        [Fact]
        public void Diagnostics_TracksRecentHealthChecks()
        {
            // Arrange & Act
            var healthCheck1 = new SaveHealthCheckResult
            {
                ComponentName = "Test1",
                Status = SaveHealthStatus.Healthy,
                Message = "Test message 1"
            };
            
            var healthCheck2 = new SaveHealthCheckResult
            {
                ComponentName = "Test2",
                Status = SaveHealthStatus.Warning,
                Message = "Test message 2"
            };

            // Simulate adding health checks (normally done internally)
            // We'll test this through the health check method
            // Act & Assert will be done through PerformHealthCheckAsync

            // This test verifies the structure exists and can be called
            var recentChecks = _diagnostics.GetRecentHealthChecks(5);
            Assert.NotNull(recentChecks);
        }

        [Theory]
        [InlineData(SaveLogLevel.Trace)]
        [InlineData(SaveLogLevel.Debug)]
        [InlineData(SaveLogLevel.Info)]
        [InlineData(SaveLogLevel.Warning)]
        [InlineData(SaveLogLevel.Error)]
        [InlineData(SaveLogLevel.Critical)]
        public void Logger_HandlesAllLogLevels(SaveLogLevel level)
        {
            // Arrange
            InitializeLogger($"LogLevel_{level}");
            _logger.MinimumLevel = SaveLogLevel.Trace;
            var message = $"Test message for {level}";

            // Act
            _logger.Log(level, message);

            // Flush and dispose to ensure file is written
            _logger.FlushAsync().Wait();
            _logger.Dispose();

            // Assert
            var logFiles = Directory.GetFiles(_logDirectory, $"*LogLevel_{level}*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.Contains(message, logContent);
            Assert.Contains(level.ToString().ToUpper(), logContent);
        }

        [Fact]
        public void Logger_HandlesExceptionsInLogging()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            _logger.LogError("Error with exception", exception);

            // Assert
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.Contains("Error with exception", logContent);
            Assert.Contains("InvalidOperationException", logContent);
            Assert.Contains("Test exception", logContent);
        }

        [Fact]
        public void Logger_HandlesVerboseLogging()
        {
            // Arrange
            _logger.VerboseLogging = true;
            _logger.MinimumLevel = SaveLogLevel.Trace;
            var exception = new Exception("Test exception with stack trace");

            // Act
            _logger.LogError("Verbose error", exception);

            // Assert
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            Assert.Single(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);
            Assert.Contains("Verbose error", logContent);
            Assert.Contains("StackTrace:", logContent);
        }

        [Fact]
        public async Task Diagnostics_ChecksDiskHealth()
        {
            // Act
            var healthReport = await _diagnostics.PerformHealthCheckAsync();

            // Assert
            var diskHealthCheck = healthReport.ComponentResults.Find(r => r.ComponentName == "DiskSpace");
            Assert.NotNull(diskHealthCheck);
            Assert.True(diskHealthCheck.CheckDuration.TotalMilliseconds >= 0);
            Assert.Contains(diskHealthCheck.Status, new[] { SaveHealthStatus.Healthy, SaveHealthStatus.Warning, SaveHealthStatus.Critical });
        }

        [Fact]
        public async Task Diagnostics_ChecksDirectoryStructure()
        {
            // Arrange - Create test save structure
            var saveDir = Path.Combine(_testDirectory, "Saves");
            var slotDir = Path.Combine(saveDir, "slot_1");
            Directory.CreateDirectory(slotDir);
            await File.WriteAllTextAsync(Path.Combine(slotDir, "save.json"), "{}");

            // Act
            var healthReport = await _diagnostics.PerformHealthCheckAsync();

            // Assert
            var directoryCheck = healthReport.ComponentResults.Find(r => r.ComponentName == "DirectoryStructure");
            Assert.NotNull(directoryCheck);
            Assert.True(directoryCheck.Details.ContainsKey("TotalSlots"));
            Assert.True(directoryCheck.Details.ContainsKey("UsedSlots"));
        }

        [Fact]
        public async Task Diagnostics_ChecksFileIntegrity()
        {
            // Arrange
            InitializeLogger("IntegrityTest");
            
            // Create valid and invalid save files
            var saveDir = Path.Combine(_testDirectory, "Saves");
            var slot1Dir = Path.Combine(saveDir, "slot_1");
            var slot2Dir = Path.Combine(saveDir, "slot_2");
            Directory.CreateDirectory(slot1Dir);
            Directory.CreateDirectory(slot2Dir);
            
            await File.WriteAllTextAsync(Path.Combine(slot1Dir, "save.json"), "{\"Version\":1}"); // Valid JSON
            await File.WriteAllTextAsync(Path.Combine(slot2Dir, "save.json"), "invalid json"); // Invalid JSON

            // Act
            var healthReport = await _diagnostics.PerformHealthCheckAsync();

            // Assert
            var integrityCheck = healthReport.ComponentResults.Find(r => r.ComponentName == "FileIntegrity");
            Assert.NotNull(integrityCheck);
            Assert.True(integrityCheck.Details.ContainsKey("TotalFiles"));
            Assert.True(integrityCheck.Details.ContainsKey("CorruptFiles"));
            
            var totalFiles = (int)integrityCheck.Details["TotalFiles"];
            var corruptFiles = (int)integrityCheck.Details["CorruptFiles"];
            Assert.Equal(2, totalFiles);
            Assert.Equal(1, corruptFiles);
        }

        [Fact]
        public void Diagnostics_GetCurrentMetrics_ReturnsValidData()
        {
            // Arrange
            _diagnostics.RecordSaveOperation(TimeSpan.FromMilliseconds(100), true, 1, 1024);
            _diagnostics.RecordLoadOperation(TimeSpan.FromMilliseconds(80), true, 1, 1024);

            // Act
            var metrics = _diagnostics.GetCurrentMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(1, metrics.SaveOperationsCount);
            Assert.Equal(1, metrics.LoadOperationsCount);
            Assert.Equal(0, metrics.ErrorCount);
            Assert.True(metrics.LastSuccessfulSave > DateTime.MinValue);
            Assert.True(metrics.LastSuccessfulLoad > DateTime.MinValue);
            Assert.NotNull(metrics.CustomMetrics);
        }
    }
}