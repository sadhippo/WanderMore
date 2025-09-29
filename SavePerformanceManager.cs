using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Manages performance optimizations for save/load operations including delta saves,
    /// compression, and performance monitoring
    /// </summary>
    public class SavePerformanceManager
    {
        private readonly Dictionary<string, object> _lastSaveData;
        private readonly Dictionary<string, SavePerformanceMetrics> _performanceHistory;
        private readonly JsonSerializerOptions _optimizedJsonOptions;
        private readonly bool _compressionEnabled;
        private readonly bool _deltaSaveEnabled;

        /// <summary>
        /// Event fired when performance metrics are updated
        /// </summary>
        public event EventHandler<PerformanceMetricsEventArgs> MetricsUpdated;

        /// <summary>
        /// Initializes a new instance of the SavePerformanceManager
        /// </summary>
        /// <param name="compressionEnabled">Whether to enable save data compression</param>
        /// <param name="deltaSaveEnabled">Whether to enable delta save functionality</param>
        public SavePerformanceManager(bool compressionEnabled = true, bool deltaSaveEnabled = true)
        {
            _lastSaveData = new Dictionary<string, object>();
            _performanceHistory = new Dictionary<string, SavePerformanceMetrics>();
            _compressionEnabled = compressionEnabled;
            _deltaSaveEnabled = deltaSaveEnabled;

            // Configure optimized JSON serialization options for performance
            _optimizedJsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false, // Compact JSON for smaller file size
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = false,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Add custom converters for MonoGame types
            _optimizedJsonOptions.Converters.Add(new Vector2JsonConverter());
        }

        /// <summary>
        /// Determines which systems have changed data since the last save (delta save functionality)
        /// </summary>
        /// <param name="currentSaveData">Current save data from all systems</param>
        /// <returns>Dictionary containing only changed system data</returns>
        public Dictionary<string, object> GetDeltaSaveData(Dictionary<string, object> currentSaveData)
        {
            if (!_deltaSaveEnabled)
                return currentSaveData;

            var deltaData = new Dictionary<string, object>();

            foreach (var kvp in currentSaveData)
            {
                string systemKey = kvp.Key;
                object currentData = kvp.Value;

                // If this is the first save or we don't have previous data, include everything
                if (!_lastSaveData.ContainsKey(systemKey))
                {
                    deltaData[systemKey] = currentData;
                    continue;
                }

                // Compare current data with last saved data
                if (HasDataChanged(systemKey, currentData, _lastSaveData[systemKey]))
                {
                    deltaData[systemKey] = currentData;
                }
            }

            // Update last save data for next comparison
            foreach (var kvp in currentSaveData)
            {
                _lastSaveData[kvp.Key] = DeepClone(kvp.Value);
            }

            return deltaData;
        }

        /// <summary>
        /// Compresses save data using GZip compression
        /// </summary>
        /// <param name="jsonData">JSON string to compress</param>
        /// <returns>Compressed byte array</returns>
        public async Task<byte[]> CompressSaveDataAsync(string jsonData)
        {
            if (!_compressionEnabled)
                return Encoding.UTF8.GetBytes(jsonData);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                await gzipStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Decompresses save data from GZip compression
        /// </summary>
        /// <param name="compressedData">Compressed byte array</param>
        /// <returns>Decompressed JSON string</returns>
        public async Task<string> DecompressSaveDataAsync(byte[] compressedData)
        {
            if (!_compressionEnabled)
                return Encoding.UTF8.GetString(compressedData);

            using var compressedStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();

            await gzipStream.CopyToAsync(decompressedStream);
            return Encoding.UTF8.GetString(decompressedStream.ToArray());
        }

        /// <summary>
        /// Serializes data using optimized JSON settings
        /// </summary>
        /// <param name="data">Data to serialize</param>
        /// <returns>Optimized JSON string</returns>
        public string SerializeOptimized(object data)
        {
            return JsonSerializer.Serialize(data, _optimizedJsonOptions);
        }

        /// <summary>
        /// Deserializes data using optimized JSON settings
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="json">JSON string to deserialize</param>
        /// <returns>Deserialized object</returns>
        public T DeserializeOptimized<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _optimizedJsonOptions);
        }

        /// <summary>
        /// Starts performance profiling for a save/load operation
        /// </summary>
        /// <param name="operationType">Type of operation (save/load)</param>
        /// <param name="slotId">Save slot identifier</param>
        /// <returns>Performance profiler instance</returns>
        public SaveOperationProfiler StartProfiling(string operationType, int slotId)
        {
            return new SaveOperationProfiler(operationType, slotId, this);
        }

        /// <summary>
        /// Records performance metrics for a completed operation
        /// </summary>
        /// <param name="metrics">Performance metrics to record</param>
        internal void RecordMetrics(SavePerformanceMetrics metrics)
        {
            string key = $"{metrics.OperationType}_Slot{metrics.SlotId}";
            _performanceHistory[key] = metrics;

            // Fire metrics updated event
            OnMetricsUpdated(new PerformanceMetricsEventArgs { Metrics = metrics });
        }

        /// <summary>
        /// Gets performance metrics for a specific operation and slot
        /// </summary>
        /// <param name="operationType">Type of operation (save/load)</param>
        /// <param name="slotId">Save slot identifier</param>
        /// <returns>Performance metrics or null if not found</returns>
        public SavePerformanceMetrics GetMetrics(string operationType, int slotId)
        {
            string key = $"{operationType}_Slot{slotId}";
            return _performanceHistory.TryGetValue(key, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Gets all recorded performance metrics
        /// </summary>
        /// <returns>Dictionary of all performance metrics</returns>
        public Dictionary<string, SavePerformanceMetrics> GetAllMetrics()
        {
            return new Dictionary<string, SavePerformanceMetrics>(_performanceHistory);
        }

        /// <summary>
        /// Clears all recorded performance metrics
        /// </summary>
        public void ClearMetrics()
        {
            _performanceHistory.Clear();
        }

        /// <summary>
        /// Gets the optimized JSON serialization options
        /// </summary>
        public JsonSerializerOptions GetOptimizedJsonOptions()
        {
            return _optimizedJsonOptions;
        }

        /// <summary>
        /// Checks if compression is enabled
        /// </summary>
        public bool IsCompressionEnabled => _compressionEnabled;

        /// <summary>
        /// Checks if delta save is enabled
        /// </summary>
        public bool IsDeltaSaveEnabled => _deltaSaveEnabled;

        /// <summary>
        /// Determines if data has changed between current and previous versions
        /// </summary>
        /// <param name="systemKey">System identifier</param>
        /// <param name="currentData">Current data</param>
        /// <param name="previousData">Previous data</param>
        /// <returns>True if data has changed, false otherwise</returns>
        private bool HasDataChanged(string systemKey, object currentData, object previousData)
        {
            try
            {
                // Serialize both objects and compare JSON strings
                // This is a simple but effective way to detect changes
                string currentJson = JsonSerializer.Serialize(currentData, _optimizedJsonOptions);
                string previousJson = JsonSerializer.Serialize(previousData, _optimizedJsonOptions);

                return currentJson != previousJson;
            }
            catch (Exception)
            {
                // If serialization fails, assume data has changed to be safe
                return true;
            }
        }

        /// <summary>
        /// Creates a deep clone of an object using JSON serialization
        /// </summary>
        /// <param name="obj">Object to clone</param>
        /// <returns>Deep cloned object</returns>
        private object DeepClone(object obj)
        {
            try
            {
                string json = JsonSerializer.Serialize(obj, _optimizedJsonOptions);
                return JsonSerializer.Deserialize<object>(json, _optimizedJsonOptions);
            }
            catch (Exception)
            {
                // If cloning fails, return the original object
                return obj;
            }
        }

        /// <summary>
        /// Raises the MetricsUpdated event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnMetricsUpdated(PerformanceMetricsEventArgs args)
        {
            MetricsUpdated?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Profiler for tracking performance of individual save/load operations
    /// </summary>
    public class SaveOperationProfiler : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly SavePerformanceManager _performanceManager;
        private readonly SavePerformanceMetrics _metrics;
        private bool _disposed;

        /// <summary>
        /// Initializes a new profiler instance
        /// </summary>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="slotId">Save slot identifier</param>
        /// <param name="performanceManager">Performance manager instance</param>
        internal SaveOperationProfiler(string operationType, int slotId, SavePerformanceManager performanceManager)
        {
            _performanceManager = performanceManager;
            _stopwatch = Stopwatch.StartNew();
            _metrics = new SavePerformanceMetrics
            {
                OperationType = operationType,
                SlotId = slotId,
                StartTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Records the file size for the operation
        /// </summary>
        /// <param name="sizeBytes">File size in bytes</param>
        public void RecordFileSize(long sizeBytes)
        {
            _metrics.FileSizeBytes = sizeBytes;
        }

        /// <summary>
        /// Records the compressed file size for the operation
        /// </summary>
        /// <param name="compressedSizeBytes">Compressed file size in bytes</param>
        public void RecordCompressedSize(long compressedSizeBytes)
        {
            _metrics.CompressedSizeBytes = compressedSizeBytes;
        }

        /// <summary>
        /// Records the number of systems processed
        /// </summary>
        /// <param name="systemCount">Number of systems</param>
        public void RecordSystemCount(int systemCount)
        {
            _metrics.SystemCount = systemCount;
        }

        /// <summary>
        /// Records whether delta save was used
        /// </summary>
        /// <param name="deltaSaveUsed">True if delta save was used</param>
        public void RecordDeltaSaveUsage(bool deltaSaveUsed)
        {
            _metrics.DeltaSaveUsed = deltaSaveUsed;
        }

        /// <summary>
        /// Completes the profiling and records the metrics
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _stopwatch.Stop();
            _metrics.EndTime = DateTime.UtcNow;
            _metrics.DurationMs = _stopwatch.ElapsedMilliseconds;

            _performanceManager.RecordMetrics(_metrics);
            _disposed = true;
        }
    }

    /// <summary>
    /// Performance metrics for save/load operations
    /// </summary>
    public class SavePerformanceMetrics
    {
        /// <summary>
        /// Type of operation (save/load)
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// Save slot identifier
        /// </summary>
        public int SlotId { get; set; }

        /// <summary>
        /// Start time of the operation
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the operation
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the operation in milliseconds
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Original file size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Compressed file size in bytes (if compression was used)
        /// </summary>
        public long CompressedSizeBytes { get; set; }

        /// <summary>
        /// Number of systems processed
        /// </summary>
        public int SystemCount { get; set; }

        /// <summary>
        /// Whether delta save was used
        /// </summary>
        public bool DeltaSaveUsed { get; set; }

        /// <summary>
        /// Compression ratio (compressed size / original size)
        /// </summary>
        public float CompressionRatio => FileSizeBytes > 0 ? (float)CompressedSizeBytes / FileSizeBytes : 1.0f;

        /// <summary>
        /// Throughput in bytes per second
        /// </summary>
        public double ThroughputBytesPerSecond => DurationMs > 0 ? (double)FileSizeBytes / (DurationMs / 1000.0) : 0.0;

        /// <summary>
        /// Whether the operation met performance targets
        /// </summary>
        public bool MeetsPerformanceTargets
        {
            get
            {
                // Performance targets from requirements:
                // Save: < 500ms, Load: < 1000ms
                long targetMs = OperationType.ToLowerInvariant() == "save" ? 500 : 1000;
                return DurationMs <= targetMs;
            }
        }
    }

    /// <summary>
    /// Event arguments for performance metrics updates
    /// </summary>
    public class PerformanceMetricsEventArgs : EventArgs
    {
        /// <summary>
        /// The updated performance metrics
        /// </summary>
        public SavePerformanceMetrics Metrics { get; set; }
    }
}