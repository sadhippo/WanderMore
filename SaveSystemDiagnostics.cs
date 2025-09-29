using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Health status levels for save system components
    /// </summary>
    public enum SaveHealthStatus
    {
        /// <summary>
        /// Component is functioning normally
        /// </summary>
        Healthy = 0,
        
        /// <summary>
        /// Component has minor issues but is functional
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// Component has significant issues affecting functionality
        /// </summary>
        Degraded = 2,
        
        /// <summary>
        /// Component is not functioning
        /// </summary>
        Critical = 3,
        
        /// <summary>
        /// Component status cannot be determined
        /// </summary>
        Unknown = 4
    }

    /// <summary>
    /// Health check result for a save system component
    /// </summary>
    public class SaveHealthCheckResult
    {
        public string ComponentName { get; set; }
        public SaveHealthStatus Status { get; set; }
        public string Message { get; set; }
        public TimeSpan CheckDuration { get; set; }
        public DateTime CheckTime { get; set; } = DateTime.Now;
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Overall save system health report
    /// </summary>
    public class SaveSystemHealthReport
    {
        public SaveHealthStatus OverallStatus { get; set; }
        public DateTime ReportTime { get; set; } = DateTime.Now;
        public TimeSpan TotalCheckDuration { get; set; }
        public List<SaveHealthCheckResult> ComponentResults { get; set; } = new List<SaveHealthCheckResult>();
        public SaveSystemMetrics Metrics { get; set; } = new SaveSystemMetrics();
        
        /// <summary>
        /// Gets the worst status among all components
        /// </summary>
        public SaveHealthStatus GetWorstStatus()
        {
            if (!ComponentResults.Any())
                return SaveHealthStatus.Unknown;
                
            return ComponentResults.Max(r => r.Status);
        }
    }

    /// <summary>
    /// Metrics and statistics for the save system
    /// </summary>
    public class SaveSystemMetrics
    {
        public int TotalSaveSlots { get; set; }
        public int UsedSaveSlots { get; set; }
        public long TotalSaveDataSize { get; set; }
        public long AvailableDiskSpace { get; set; }
        public TimeSpan AverageSaveTime { get; set; }
        public TimeSpan AverageLoadTime { get; set; }
        public int SaveOperationsCount { get; set; }
        public int LoadOperationsCount { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastSuccessfulSave { get; set; }
        public DateTime LastSuccessfulLoad { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Diagnostic and health checking functionality for the save system
    /// </summary>
    public class SaveSystemDiagnostics
    {
        private readonly ISaveSystemLogger _logger;
        private readonly string _saveDirectory;
        private readonly SaveSystemMetrics _metrics;
        private readonly List<SaveHealthCheckResult> _recentHealthChecks;
        private readonly object _metricsLock = new object();

        public SaveSystemDiagnostics(ISaveSystemLogger logger, string saveDirectory = "Saves")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _saveDirectory = saveDirectory;
            _metrics = new SaveSystemMetrics();
            _recentHealthChecks = new List<SaveHealthCheckResult>();
        }

        /// <summary>
        /// Performs a comprehensive health check of the save system
        /// </summary>
        /// <returns>Complete health report</returns>
        public async Task<SaveSystemHealthReport> PerformHealthCheckAsync()
        {
            var startTime = DateTime.Now;
            var report = new SaveSystemHealthReport();
            
            _logger.LogInfo("Starting save system health check");

            try
            {
                // Check disk space and permissions
                var diskCheck = await CheckDiskHealthAsync();
                report.ComponentResults.Add(diskCheck);

                // Check save directory structure
                var directoryCheck = await CheckDirectoryStructureAsync();
                report.ComponentResults.Add(directoryCheck);

                // Check save file integrity
                var integrityCheck = await CheckSaveFileIntegrityAsync();
                report.ComponentResults.Add(integrityCheck);

                // Check backup system
                var backupCheck = await CheckBackupSystemAsync();
                report.ComponentResults.Add(backupCheck);

                // Check performance metrics
                var performanceCheck = await CheckPerformanceMetricsAsync();
                report.ComponentResults.Add(performanceCheck);

                // Update overall metrics
                await UpdateSystemMetricsAsync();
                report.Metrics = GetCurrentMetrics();

                // Determine overall status
                report.OverallStatus = report.GetWorstStatus();
                report.TotalCheckDuration = DateTime.Now - startTime;

                // Store recent health check results
                lock (_metricsLock)
                {
                    _recentHealthChecks.AddRange(report.ComponentResults);
                    
                    // Keep only recent results (last 100)
                    if (_recentHealthChecks.Count > 100)
                    {
                        _recentHealthChecks.RemoveRange(0, _recentHealthChecks.Count - 100);
                    }
                }

                _logger.LogInfo($"Health check completed. Overall status: {report.OverallStatus}, Duration: {report.TotalCheckDuration.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError("Health check failed", ex);
                report.OverallStatus = SaveHealthStatus.Critical;
                report.ComponentResults.Add(new SaveHealthCheckResult
                {
                    ComponentName = "HealthCheck",
                    Status = SaveHealthStatus.Critical,
                    Message = "Health check process failed",
                    Exception = ex,
                    CheckDuration = DateTime.Now - startTime
                });
            }

            return report;
        }

        /// <summary>
        /// Records metrics for a save operation
        /// </summary>
        public void RecordSaveOperation(TimeSpan duration, bool success, int slotId, long fileSize = 0)
        {
            lock (_metricsLock)
            {
                _metrics.SaveOperationsCount++;
                
                if (success)
                {
                    _metrics.LastSuccessfulSave = DateTime.Now;
                    
                    // Update average save time
                    var totalTime = _metrics.AverageSaveTime.TotalMilliseconds * (_metrics.SaveOperationsCount - 1) + duration.TotalMilliseconds;
                    _metrics.AverageSaveTime = TimeSpan.FromMilliseconds(totalTime / _metrics.SaveOperationsCount);
                }
                else
                {
                    _metrics.ErrorCount++;
                }
            }

            var context = new SaveLogContext()
                .WithSlot(slotId)
                .WithOperation("Save")
                .WithProperty("Duration", $"{duration.TotalMilliseconds:F2}ms")
                .WithProperty("Success", success)
                .WithProperty("FileSize", fileSize);

            if (success)
            {
                _logger.LogTiming($"Save operation (slot {slotId})", duration, context);
                if (fileSize > 0)
                {
                    _logger.LogFileSize($"slot_{slotId}/save.json", fileSize, context);
                }
            }
            else
            {
                _logger.LogError($"Save operation failed (slot {slotId})", null, context);
            }
        }

        /// <summary>
        /// Records metrics for a load operation
        /// </summary>
        public void RecordLoadOperation(TimeSpan duration, bool success, int slotId, long fileSize = 0)
        {
            lock (_metricsLock)
            {
                _metrics.LoadOperationsCount++;
                
                if (success)
                {
                    _metrics.LastSuccessfulLoad = DateTime.Now;
                    
                    // Update average load time
                    var totalTime = _metrics.AverageLoadTime.TotalMilliseconds * (_metrics.LoadOperationsCount - 1) + duration.TotalMilliseconds;
                    _metrics.AverageLoadTime = TimeSpan.FromMilliseconds(totalTime / _metrics.LoadOperationsCount);
                }
                else
                {
                    _metrics.ErrorCount++;
                }
            }

            var context = new SaveLogContext()
                .WithSlot(slotId)
                .WithOperation("Load")
                .WithProperty("Duration", $"{duration.TotalMilliseconds:F2}ms")
                .WithProperty("Success", success)
                .WithProperty("FileSize", fileSize);

            if (success)
            {
                _logger.LogTiming($"Load operation (slot {slotId})", duration, context);
                if (fileSize > 0)
                {
                    _logger.LogFileSize($"slot_{slotId}/save.json", fileSize, context);
                }
            }
            else
            {
                _logger.LogError($"Load operation failed (slot {slotId})", null, context);
            }
        }

        /// <summary>
        /// Gets current system metrics
        /// </summary>
        public SaveSystemMetrics GetCurrentMetrics()
        {
            lock (_metricsLock)
            {
                // Create a copy to avoid concurrent modification
                return new SaveSystemMetrics
                {
                    TotalSaveSlots = _metrics.TotalSaveSlots,
                    UsedSaveSlots = _metrics.UsedSaveSlots,
                    TotalSaveDataSize = _metrics.TotalSaveDataSize,
                    AvailableDiskSpace = _metrics.AvailableDiskSpace,
                    AverageSaveTime = _metrics.AverageSaveTime,
                    AverageLoadTime = _metrics.AverageLoadTime,
                    SaveOperationsCount = _metrics.SaveOperationsCount,
                    LoadOperationsCount = _metrics.LoadOperationsCount,
                    ErrorCount = _metrics.ErrorCount,
                    LastSuccessfulSave = _metrics.LastSuccessfulSave,
                    LastSuccessfulLoad = _metrics.LastSuccessfulLoad,
                    CustomMetrics = new Dictionary<string, object>(_metrics.CustomMetrics)
                };
            }
        }

        /// <summary>
        /// Gets recent health check results
        /// </summary>
        public List<SaveHealthCheckResult> GetRecentHealthChecks(int count = 10)
        {
            lock (_metricsLock)
            {
                return _recentHealthChecks.TakeLast(count).ToList();
            }
        }

        /// <summary>
        /// Exports diagnostic information to a file
        /// </summary>
        public async Task ExportDiagnosticsAsync(string filePath)
        {
            try
            {
                var healthReport = await PerformHealthCheckAsync();
                var diagnosticData = new
                {
                    ExportTime = DateTime.Now,
                    HealthReport = healthReport,
                    RecentHealthChecks = GetRecentHealthChecks(50),
                    SystemInfo = new
                    {
                        SaveDirectory = _saveDirectory,
                        LogLevel = _logger.MinimumLevel,
                        VerboseLogging = _logger.VerboseLogging
                    }
                };

                var json = JsonSerializer.Serialize(diagnosticData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInfo($"Diagnostic data exported to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to export diagnostics to {filePath}", ex);
                throw;
            }
        }

        private async Task<SaveHealthCheckResult> CheckDiskHealthAsync()
        {
            var startTime = DateTime.Now;
            var result = new SaveHealthCheckResult { ComponentName = "DiskSpace" };

            try
            {
                var directoryInfo = new DirectoryInfo(_saveDirectory);
                var drive = new DriveInfo(directoryInfo.Root.FullName);

                if (!drive.IsReady)
                {
                    result.Status = SaveHealthStatus.Critical;
                    result.Message = "Drive is not ready";
                    return result;
                }

                long availableSpace = drive.AvailableFreeSpace;
                long totalSpace = drive.TotalSize;
                double usagePercentage = (double)(totalSpace - availableSpace) / totalSpace * 100;

                result.Details["AvailableSpace"] = availableSpace;
                result.Details["TotalSpace"] = totalSpace;
                result.Details["UsagePercentage"] = usagePercentage;

                // Update metrics
                lock (_metricsLock)
                {
                    _metrics.AvailableDiskSpace = availableSpace;
                }

                if (availableSpace < 100 * 1024 * 1024) // Less than 100MB
                {
                    result.Status = SaveHealthStatus.Critical;
                    result.Message = $"Very low disk space: {FormatBytes(availableSpace)} available";
                }
                else if (availableSpace < 500 * 1024 * 1024) // Less than 500MB
                {
                    result.Status = SaveHealthStatus.Warning;
                    result.Message = $"Low disk space: {FormatBytes(availableSpace)} available";
                }
                else
                {
                    result.Status = SaveHealthStatus.Healthy;
                    result.Message = $"Sufficient disk space: {FormatBytes(availableSpace)} available";
                }
            }
            catch (Exception ex)
            {
                result.Status = SaveHealthStatus.Critical;
                result.Message = "Failed to check disk space";
                result.Exception = ex;
            }
            finally
            {
                result.CheckDuration = DateTime.Now - startTime;
            }

            return result;
        }

        private async Task<SaveHealthCheckResult> CheckDirectoryStructureAsync()
        {
            var startTime = DateTime.Now;
            var result = new SaveHealthCheckResult { ComponentName = "DirectoryStructure" };

            try
            {
                if (!Directory.Exists(_saveDirectory))
                {
                    result.Status = SaveHealthStatus.Warning;
                    result.Message = "Save directory does not exist (will be created on first save)";
                    return result;
                }

                // Count save slots and calculate total size
                var slotDirectories = Directory.GetDirectories(_saveDirectory, "slot_*");
                int usedSlots = 0;
                long totalSize = 0;

                foreach (var slotDir in slotDirectories)
                {
                    var saveFile = Path.Combine(slotDir, "save.json");
                    if (File.Exists(saveFile))
                    {
                        usedSlots++;
                        totalSize += new FileInfo(saveFile).Length;
                    }
                }

                // Update metrics
                lock (_metricsLock)
                {
                    _metrics.TotalSaveSlots = slotDirectories.Length;
                    _metrics.UsedSaveSlots = usedSlots;
                    _metrics.TotalSaveDataSize = totalSize;
                }

                result.Details["TotalSlots"] = slotDirectories.Length;
                result.Details["UsedSlots"] = usedSlots;
                result.Details["TotalSize"] = totalSize;

                result.Status = SaveHealthStatus.Healthy;
                result.Message = $"Directory structure OK: {usedSlots} used slots, {FormatBytes(totalSize)} total";
            }
            catch (Exception ex)
            {
                result.Status = SaveHealthStatus.Critical;
                result.Message = "Failed to check directory structure";
                result.Exception = ex;
            }
            finally
            {
                result.CheckDuration = DateTime.Now - startTime;
            }

            return result;
        }

        private async Task<SaveHealthCheckResult> CheckSaveFileIntegrityAsync()
        {
            var startTime = DateTime.Now;
            var result = new SaveHealthCheckResult { ComponentName = "FileIntegrity" };

            try
            {
                if (!Directory.Exists(_saveDirectory))
                {
                    result.Status = SaveHealthStatus.Healthy;
                    result.Message = "No save files to check";
                    return result;
                }

                var slotDirectories = Directory.GetDirectories(_saveDirectory, "slot_*");
                int totalFiles = 0;
                int corruptFiles = 0;
                var corruptSlots = new List<string>();

                foreach (var slotDir in slotDirectories)
                {
                    var saveFile = Path.Combine(slotDir, "save.json");
                    if (File.Exists(saveFile))
                    {
                        totalFiles++;
                        
                        try
                        {
                            // Basic JSON validation
                            var content = await File.ReadAllTextAsync(saveFile);
                            JsonDocument.Parse(content);
                        }
                        catch
                        {
                            corruptFiles++;
                            corruptSlots.Add(Path.GetFileName(slotDir));
                        }
                    }
                }

                result.Details["TotalFiles"] = totalFiles;
                result.Details["CorruptFiles"] = corruptFiles;
                result.Details["CorruptSlots"] = corruptSlots;

                if (corruptFiles == 0)
                {
                    result.Status = SaveHealthStatus.Healthy;
                    result.Message = $"All {totalFiles} save files are valid";
                }
                else if (corruptFiles < totalFiles)
                {
                    result.Status = SaveHealthStatus.Warning;
                    result.Message = $"{corruptFiles}/{totalFiles} save files are corrupt: {string.Join(", ", corruptSlots)}";
                }
                else
                {
                    result.Status = SaveHealthStatus.Critical;
                    result.Message = "All save files are corrupt";
                }
            }
            catch (Exception ex)
            {
                result.Status = SaveHealthStatus.Critical;
                result.Message = "Failed to check file integrity";
                result.Exception = ex;
            }
            finally
            {
                result.CheckDuration = DateTime.Now - startTime;
            }

            return result;
        }

        private async Task<SaveHealthCheckResult> CheckBackupSystemAsync()
        {
            var startTime = DateTime.Now;
            var result = new SaveHealthCheckResult { ComponentName = "BackupSystem" };

            try
            {
                if (!Directory.Exists(_saveDirectory))
                {
                    result.Status = SaveHealthStatus.Healthy;
                    result.Message = "No backups to check";
                    return result;
                }

                var slotDirectories = Directory.GetDirectories(_saveDirectory, "slot_*");
                int slotsWithBackups = 0;
                int totalBackups = 0;

                foreach (var slotDir in slotDirectories)
                {
                    var backupDir = Path.Combine(slotDir, "backups");
                    if (Directory.Exists(backupDir))
                    {
                        var backupFiles = Directory.GetFiles(backupDir, "*.backup");
                        if (backupFiles.Length > 0)
                        {
                            slotsWithBackups++;
                            totalBackups += backupFiles.Length;
                        }
                    }
                }

                result.Details["SlotsWithBackups"] = slotsWithBackups;
                result.Details["TotalBackups"] = totalBackups;

                result.Status = SaveHealthStatus.Healthy;
                result.Message = $"Backup system OK: {totalBackups} backups across {slotsWithBackups} slots";
            }
            catch (Exception ex)
            {
                result.Status = SaveHealthStatus.Warning;
                result.Message = "Failed to check backup system";
                result.Exception = ex;
            }
            finally
            {
                result.CheckDuration = DateTime.Now - startTime;
            }

            return result;
        }

        private async Task<SaveHealthCheckResult> CheckPerformanceMetricsAsync()
        {
            var startTime = DateTime.Now;
            var result = new SaveHealthCheckResult { ComponentName = "Performance" };

            try
            {
                var metrics = GetCurrentMetrics();
                
                result.Details["AverageSaveTime"] = metrics.AverageSaveTime.TotalMilliseconds;
                result.Details["AverageLoadTime"] = metrics.AverageLoadTime.TotalMilliseconds;
                result.Details["SaveOperations"] = metrics.SaveOperationsCount;
                result.Details["LoadOperations"] = metrics.LoadOperationsCount;
                result.Details["ErrorCount"] = metrics.ErrorCount;

                // Determine status based on performance
                if (metrics.AverageSaveTime.TotalSeconds > 10 || metrics.AverageLoadTime.TotalSeconds > 10)
                {
                    result.Status = SaveHealthStatus.Warning;
                    result.Message = "Performance is degraded (operations taking too long)";
                }
                else if (metrics.ErrorCount > metrics.SaveOperationsCount * 0.1) // More than 10% error rate
                {
                    result.Status = SaveHealthStatus.Warning;
                    result.Message = "High error rate detected";
                }
                else
                {
                    result.Status = SaveHealthStatus.Healthy;
                    result.Message = "Performance metrics are within acceptable ranges";
                }
            }
            catch (Exception ex)
            {
                result.Status = SaveHealthStatus.Warning;
                result.Message = "Failed to check performance metrics";
                result.Exception = ex;
            }
            finally
            {
                result.CheckDuration = DateTime.Now - startTime;
            }

            return result;
        }

        private async Task UpdateSystemMetricsAsync()
        {
            // This method can be extended to update additional metrics
            // For now, most metrics are updated in real-time by other methods
            await Task.CompletedTask;
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}