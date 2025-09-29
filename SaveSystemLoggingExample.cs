using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace HiddenHorizons
{
    /// <summary>
    /// Example demonstrating how to use the comprehensive logging and debugging system
    /// with the SaveManager for detailed operation tracking and diagnostics.
    /// </summary>
    public class SaveSystemLoggingExample
    {
        /// <summary>
        /// Demonstrates basic logging setup and usage with SaveManager
        /// </summary>
        public static async Task BasicLoggingExample()
        {
            // Create a logger with custom settings
            using var logger = new SaveSystemLogger("Logs", "GameSession");
            logger.MinimumLevel = SaveLogLevel.Debug;
            logger.VerboseLogging = true;

            // Create SaveManager with logging enabled
            var saveManager = new SaveManager(
                saveDirectory: "GameSaves",
                logger: logger
            );

            // Set up some example systems
            var adventurer = new Adventurer(Vector2.Zero); // Pass Vector2.Zero for position in example
            var journalManager = new JournalManager(new TimeManager());
            
            // Register systems with logging
            saveManager.RegisterSaveable(adventurer);
            saveManager.RegisterSaveable(journalManager);

            // Perform save operation with full logging
            logger.LogInfo("Starting game save operation");
            
            try
            {
                await saveManager.SaveGameAsync(1);
                logger.LogInfo("Save operation completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError("Save operation failed", ex);
            }

            // Perform health check
            var healthReport = await saveManager.PerformHealthCheckAsync();
            logger.LogInfo($"System health check completed. Status: {healthReport.OverallStatus}");

            // Export diagnostics for troubleshooting
            await saveManager.ExportDiagnosticsAsync("diagnostics-export.json");
            logger.LogInfo("Diagnostic data exported for analysis");
        }

        /// <summary>
        /// Demonstrates advanced logging features including scoped logging and metrics
        /// </summary>
        public static async Task AdvancedLoggingExample()
        {
            using var logger = new SaveSystemLogger("Logs", "AdvancedExample");
            logger.MinimumLevel = SaveLogLevel.Trace;
            logger.VerboseLogging = true;

            var saveManager = new SaveManager(logger: logger);
            var diagnostics = saveManager.GetDiagnostics();

            // Create scoped logger for specific operations
            using var saveOpLogger = logger.CreateScope("SaveOperation");
            
            // Log with context information
            var context = new SaveLogContext()
                .WithSlot(1)
                .WithOperation("ManualSave")
                .WithProperty("Reason", "PlayerRequested")
                .WithProperty("GameTime", "Day 15, 14:30");

            saveOpLogger.LogInfo("Starting manual save operation", context);

            // Simulate save operation timing
            var startTime = DateTime.Now;
            
            // ... save operation would happen here ...
            await Task.Delay(100); // Simulate work
            
            var duration = DateTime.Now - startTime;
            saveOpLogger.LogTiming("Save operation", duration, context);

            // Log file size information
            saveOpLogger.LogFileSize("slot_1/save.json", 1024 * 50, context); // 50KB

            // Log system state information
            var systemState = new { 
                RegisteredSystems = saveManager.RegisteredSystemCount,
                AutoSaveEnabled = true,
                LastSave = DateTime.Now
            };
            saveOpLogger.LogSystemState("SaveManager", systemState, context);

            // Record metrics for diagnostics
            diagnostics.RecordSaveOperation(duration, true, 1, 1024 * 50);

            // Get current metrics
            var metrics = diagnostics.GetCurrentMetrics();
            logger.LogInfo($"Current metrics: {metrics.SaveOperationsCount} saves, avg time: {metrics.AverageSaveTime.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Demonstrates error logging and diagnostic capabilities
        /// </summary>
        public static async Task ErrorHandlingAndDiagnosticsExample()
        {
            using var logger = new SaveSystemLogger("Logs", "ErrorHandling");
            logger.MinimumLevel = SaveLogLevel.Debug;

            var saveManager = new SaveManager(logger: logger);
            var diagnostics = saveManager.GetDiagnostics();

            // Simulate various error scenarios with proper logging
            try
            {
                // This would normally fail due to invalid slot
                await saveManager.LoadGameAsync(-1);
            }
            catch (ArgumentException ex)
            {
                var context = new SaveLogContext()
                    .WithOperation("Load")
                    .WithProperty("ErrorType", "InvalidSlot");
                    
                logger.LogError("Invalid slot ID provided", ex, context);
                diagnostics.RecordLoadOperation(TimeSpan.FromMilliseconds(10), false, -1);
            }

            // Perform comprehensive health check
            logger.LogInfo("Performing system health check");
            var healthReport = await diagnostics.PerformHealthCheckAsync();

            // Log health check results
            foreach (var result in healthReport.ComponentResults)
            {
                var logLevel = result.Status switch
                {
                    SaveHealthStatus.Healthy => SaveLogLevel.Info,
                    SaveHealthStatus.Warning => SaveLogLevel.Warning,
                    SaveHealthStatus.Degraded => SaveLogLevel.Warning,
                    SaveHealthStatus.Critical => SaveLogLevel.Error,
                    _ => SaveLogLevel.Info
                };

                logger.Log(logLevel, $"Health check - {result.ComponentName}: {result.Message}");
            }

            // Set different logging levels based on health status
            if (healthReport.OverallStatus >= SaveHealthStatus.Warning)
            {
                logger.MinimumLevel = SaveLogLevel.Debug;
                logger.VerboseLogging = true;
                logger.LogWarning("Increased logging verbosity due to system health issues");
            }

            // Export detailed diagnostics if there are issues
            if (healthReport.OverallStatus >= SaveHealthStatus.Degraded)
            {
                var exportPath = $"diagnostics-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json";
                await diagnostics.ExportDiagnosticsAsync(exportPath);
                logger.LogInfo($"Detailed diagnostics exported to: {exportPath}");
            }
        }

        /// <summary>
        /// Demonstrates integration with game systems for automatic logging
        /// </summary>
        public static void GameIntegrationExample()
        {
            using var logger = new SaveSystemLogger("Logs", "GameIntegration");
            
            // Configure logging levels based on build type
            #if DEBUG
            logger.MinimumLevel = SaveLogLevel.Trace;
            logger.VerboseLogging = true;
            #else
            logger.MinimumLevel = SaveLogLevel.Info;
            logger.VerboseLogging = false;
            #endif

            var saveManager = new SaveManager(logger: logger);

            // Set up automatic save triggers with logging
            var timeManager = new TimeManager();
            var journalManager = new JournalManager(timeManager);
            
            saveManager.SubscribeToAutoSaveTriggers(
                timeManager: timeManager,
                journalManager: journalManager
            );

            // Configure logging level changes based on game events
            saveManager.SetLoggingLevel(SaveLogLevel.Debug, verboseLogging: true);

            logger.LogInfo("Save system initialized with comprehensive logging");
            logger.LogDebug($"Logging configuration: Level={logger.MinimumLevel}, Verbose={logger.VerboseLogging}");

            // The logging system will now automatically track:
            // - All save/load operations with timing and file sizes
            // - System registrations and unregistrations
            // - Health checks and diagnostics
            // - Error conditions and recovery attempts
            // - Performance metrics and optimization opportunities
        }
    }
}