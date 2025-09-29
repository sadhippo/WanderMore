using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Logging levels for save system operations
    /// </summary>
    public enum SaveLogLevel
    {
        /// <summary>
        /// Detailed trace information for debugging
        /// </summary>
        Trace = 0,
        
        /// <summary>
        /// Debug information for development
        /// </summary>
        Debug = 1,
        
        /// <summary>
        /// General information about operations
        /// </summary>
        Info = 2,
        
        /// <summary>
        /// Warning messages for potential issues
        /// </summary>
        Warning = 3,
        
        /// <summary>
        /// Error messages for failures
        /// </summary>
        Error = 4,
        
        /// <summary>
        /// Critical errors that may cause system failure
        /// </summary>
        Critical = 5
    }

    /// <summary>
    /// Interface for save system logging
    /// </summary>
    public interface ISaveSystemLogger : IDisposable
    {
        /// <summary>
        /// Current minimum log level
        /// </summary>
        SaveLogLevel MinimumLevel { get; set; }
        
        /// <summary>
        /// Whether verbose logging is enabled
        /// </summary>
        bool VerboseLogging { get; set; }
        
        /// <summary>
        /// Logs a message at the specified level
        /// </summary>
        void Log(SaveLogLevel level, string message, Exception exception = null, object context = null);
        
        /// <summary>
        /// Logs a trace message
        /// </summary>
        void LogTrace(string message, object context = null);
        
        /// <summary>
        /// Logs a debug message
        /// </summary>
        void LogDebug(string message, object context = null);
        
        /// <summary>
        /// Logs an info message
        /// </summary>
        void LogInfo(string message, object context = null);
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        void LogWarning(string message, Exception exception = null, object context = null);
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        void LogError(string message, Exception exception = null, object context = null);
        
        /// <summary>
        /// Logs a critical error message
        /// </summary>
        void LogCritical(string message, Exception exception = null, object context = null);
        
        /// <summary>
        /// Logs operation timing information
        /// </summary>
        void LogTiming(string operation, TimeSpan duration, object context = null);
        
        /// <summary>
        /// Logs file size information
        /// </summary>
        void LogFileSize(string filePath, long sizeBytes, object context = null);
        
        /// <summary>
        /// Logs system state information
        /// </summary>
        void LogSystemState(string systemName, object state, object context = null);
        
        /// <summary>
        /// Creates a scoped logger for tracking related operations
        /// </summary>
        ISaveSystemLogger CreateScope(string scopeName);
        
        /// <summary>
        /// Flushes any pending log entries
        /// </summary>
        Task FlushAsync();
    }

    /// <summary>
    /// Context information for log entries
    /// </summary>
    public class SaveLogContext
    {
        public int? SlotId { get; set; }
        public string Operation { get; set; }
        public string SystemName { get; set; }
        public string FilePath { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        public SaveLogContext WithSlot(int slotId)
        {
            SlotId = slotId;
            return this;
        }
        
        public SaveLogContext WithOperation(string operation)
        {
            Operation = operation;
            return this;
        }
        
        public SaveLogContext WithSystem(string systemName)
        {
            SystemName = systemName;
            return this;
        }
        
        public SaveLogContext WithFile(string filePath)
        {
            FilePath = filePath;
            return this;
        }
        
        public SaveLogContext WithProperty(string key, object value)
        {
            Properties[key] = value;
            return this;
        }
    }

    /// <summary>
    /// Default implementation of ISaveSystemLogger that writes to file and console
    /// </summary>
    public class SaveSystemLogger : ISaveSystemLogger, IDisposable
    {
        private readonly string _logFilePath;
        private readonly StreamWriter _logWriter;
        private readonly object _lockObject = new object();
        private readonly string _scopeName;
        private bool _disposed = false;

        public SaveLogLevel MinimumLevel { get; set; } = SaveLogLevel.Info;
        public bool VerboseLogging { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of SaveSystemLogger
        /// </summary>
        /// <param name="logDirectory">Directory where log files will be stored</param>
        /// <param name="scopeName">Optional scope name for this logger instance</param>
        public SaveSystemLogger(string logDirectory = "Logs", string scopeName = null)
        {
            _scopeName = scopeName;
            
            // Ensure log directory exists
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Create log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd");
            string fileName = scopeName != null ? $"save-system-{scopeName}-{timestamp}.log" : $"save-system-{timestamp}.log";
            _logFilePath = Path.Combine(logDirectory, fileName);
            
            // Initialize file writer with UTF-8 encoding and auto-flush
            _logWriter = new StreamWriter(_logFilePath, append: true, Encoding.UTF8) { AutoFlush = true };
            
            // Log initialization
            LogInfo($"Save system logger initialized. Scope: {scopeName ?? "Global"}, MinLevel: {MinimumLevel}");
        }

        public void Log(SaveLogLevel level, string message, Exception exception = null, object context = null)
        {
            if (level < MinimumLevel)
                return;

            lock (_lockObject)
            {
                if (_disposed)
                    return;

                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string levelStr = level.ToString().ToUpper().PadRight(8);
                    string scopeStr = !string.IsNullOrEmpty(_scopeName) ? $"[{_scopeName}] " : "";
                    
                    var logEntry = new StringBuilder();
                    logEntry.Append($"{timestamp} {levelStr} {scopeStr}{message}");
                    
                    // Add context information if available
                    if (context != null)
                    {
                        string contextStr = FormatContext(context);
                        if (!string.IsNullOrEmpty(contextStr))
                        {
                            logEntry.Append($" | Context: {contextStr}");
                        }
                    }
                    
                    // Add exception information if present
                    if (exception != null)
                    {
                        logEntry.AppendLine();
                        logEntry.Append($"    Exception: {exception.GetType().Name}: {exception.Message}");
                        
                        if (VerboseLogging && !string.IsNullOrEmpty(exception.StackTrace))
                        {
                            logEntry.AppendLine();
                            logEntry.Append($"    StackTrace: {exception.StackTrace}");
                        }
                    }
                    
                    string finalMessage = logEntry.ToString();
                    
                    // Write to file
                    _logWriter.WriteLine(finalMessage);
                    
                    // Also write to console for errors and warnings
                    if (level >= SaveLogLevel.Warning)
                    {
                        Console.WriteLine($"[SaveSystem] {finalMessage}");
                    }
                    else if (VerboseLogging)
                    {
                        Console.WriteLine($"[SaveSystem] {finalMessage}");
                    }
                }
                catch (Exception ex)
                {
                    // Fallback to console if file logging fails
                    Console.WriteLine($"[SaveSystem] Logging failed: {ex.Message}");
                    Console.WriteLine($"[SaveSystem] Original message: {message}");
                }
            }
        }

        public void LogTrace(string message, object context = null)
        {
            Log(SaveLogLevel.Trace, message, null, context);
        }

        public void LogDebug(string message, object context = null)
        {
            Log(SaveLogLevel.Debug, message, null, context);
        }

        public void LogInfo(string message, object context = null)
        {
            Log(SaveLogLevel.Info, message, null, context);
        }

        public void LogWarning(string message, Exception exception = null, object context = null)
        {
            Log(SaveLogLevel.Warning, message, exception, context);
        }

        public void LogError(string message, Exception exception = null, object context = null)
        {
            Log(SaveLogLevel.Error, message, exception, context);
        }

        public void LogCritical(string message, Exception exception = null, object context = null)
        {
            Log(SaveLogLevel.Critical, message, exception, context);
        }

        public void LogTiming(string operation, TimeSpan duration, object context = null)
        {
            string message = $"Operation '{operation}' completed in {duration.TotalMilliseconds:F2}ms";
            
            // Log as warning if operation took too long
            if (duration.TotalSeconds > 5.0)
            {
                LogWarning($"SLOW: {message}", null, context);
            }
            else if (duration.TotalSeconds > 1.0)
            {
                LogInfo($"MODERATE: {message}", context);
            }
            else if (VerboseLogging)
            {
                LogDebug(message, context);
            }
        }

        public void LogFileSize(string filePath, long sizeBytes, object context = null)
        {
            string sizeStr = FormatBytes(sizeBytes);
            string fileName = Path.GetFileName(filePath);
            
            string message = $"File size: {fileName} = {sizeStr} ({sizeBytes:N0} bytes)";
            
            // Log as warning if file is very large
            if (sizeBytes > 100 * 1024 * 1024) // > 100MB
            {
                LogWarning($"LARGE FILE: {message}", null, context);
            }
            else if (sizeBytes > 10 * 1024 * 1024) // > 10MB
            {
                LogInfo($"MODERATE SIZE: {message}", context);
            }
            else if (VerboseLogging)
            {
                LogDebug(message, context);
            }
        }

        public void LogSystemState(string systemName, object state, object context = null)
        {
            if (!VerboseLogging && MinimumLevel > SaveLogLevel.Debug)
                return;
                
            string stateStr = state?.ToString() ?? "null";
            if (stateStr.Length > 200)
            {
                stateStr = stateStr.Substring(0, 200) + "...";
            }
            
            LogDebug($"System state: {systemName} = {stateStr}", context);
        }

        public ISaveSystemLogger CreateScope(string scopeName)
        {
            string fullScopeName = !string.IsNullOrEmpty(_scopeName) ? $"{_scopeName}.{scopeName}" : scopeName;
            var scopedLogger = new SaveSystemLogger(Path.GetDirectoryName(_logFilePath), fullScopeName)
            {
                MinimumLevel = this.MinimumLevel,
                VerboseLogging = this.VerboseLogging
            };
            return scopedLogger;
        }

        public async Task FlushAsync()
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    if (!_disposed)
                    {
                        _logWriter?.Flush();
                    }
                }
            });
        }

        private string FormatContext(object context)
        {
            if (context == null)
                return string.Empty;
                
            if (context is SaveLogContext logContext)
            {
                var parts = new List<string>();
                
                if (logContext.SlotId.HasValue)
                    parts.Add($"Slot={logContext.SlotId}");
                    
                if (!string.IsNullOrEmpty(logContext.Operation))
                    parts.Add($"Op={logContext.Operation}");
                    
                if (!string.IsNullOrEmpty(logContext.SystemName))
                    parts.Add($"System={logContext.SystemName}");
                    
                if (!string.IsNullOrEmpty(logContext.FilePath))
                    parts.Add($"File={Path.GetFileName(logContext.FilePath)}");
                    
                foreach (var prop in logContext.Properties)
                {
                    parts.Add($"{prop.Key}={prop.Value}");
                }
                
                return string.Join(", ", parts);
            }
            
            return context.ToString();
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

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    if (!_disposed)
                    {
                        LogInfo("Save system logger shutting down");
                        _logWriter?.Dispose();
                        _disposed = true;
                    }
                }
            }
        }
    }
}