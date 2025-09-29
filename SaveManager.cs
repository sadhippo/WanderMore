using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;

namespace HiddenHorizons
{
    /// <summary>
    /// Central manager for save and load operations across all game systems
    /// </summary>
    public class SaveManager
    {
        private readonly Dictionary<string, ISaveable> _saveableSystems;
        private readonly string _saveDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly VersionManager _versionManager;
        private readonly SaveErrorManager _errorManager;
        private readonly SavePerformanceManager _performanceManager;
        private readonly ISaveSystemLogger _logger;
        private readonly SaveSystemDiagnostics _diagnostics;
        
        // Auto-save configuration
        private readonly AutoSaveConfig _autoSaveConfig;
        private float _autoSaveTimer;
        private bool _autoSaveEnabled;
        private int _currentAutoSaveSlot = 1;
        
        // Background operation tracking
        private readonly SemaphoreSlim _saveOperationSemaphore;
        private volatile bool _isSaveInProgress;
        private SaveProgress _currentSaveProgress;

        /// <summary>
        /// Event fired when a save operation completes successfully
        /// </summary>
        public event EventHandler<SaveCompletedEventArgs> SaveCompleted;

        /// <summary>
        /// Event fired when a load operation completes successfully
        /// </summary>
        public event EventHandler<LoadCompletedEventArgs> LoadCompleted;

        /// <summary>
        /// Event fired when a save or load operation encounters an error
        /// </summary>
        public event EventHandler<SaveErrorEventArgs> SaveError;

        /// <summary>
        /// Event fired when an automatic save is triggered
        /// </summary>
        public event EventHandler<AutoSaveTriggeredEventArgs> AutoSaveTriggered;

        /// <summary>
        /// Event fired when save progress changes during background operations
        /// </summary>
        public event EventHandler<SaveProgressEventArgs> SaveProgressChanged;

        /// <summary>
        /// Initializes a new instance of the SaveManager
        /// </summary>
        /// <param name="saveDirectory">Directory where save files will be stored</param>
        /// <param name="versionManager">Version manager for handling save format migrations</param>
        /// <param name="errorManager">Error manager for handling save/load errors</param>
        /// <param name="autoSaveConfig">Configuration for automatic save triggers</param>
        /// <param name="performanceManager">Performance manager for optimizations</param>
        /// <param name="logger">Logger for save system operations</param>
        public SaveManager(string saveDirectory = "Saves", VersionManager versionManager = null, SaveErrorManager errorManager = null, AutoSaveConfig autoSaveConfig = null, SavePerformanceManager performanceManager = null, ISaveSystemLogger logger = null)
        {
            _saveableSystems = new Dictionary<string, ISaveable>();
            _saveDirectory = saveDirectory;
            _versionManager = versionManager ?? new VersionManager();
            _errorManager = errorManager ?? new SaveErrorManager();
            _autoSaveConfig = autoSaveConfig ?? new AutoSaveConfig();
            _performanceManager = performanceManager ?? new SavePerformanceManager(compressionEnabled: false);
            _logger = logger ?? new SaveSystemLogger();
            _diagnostics = new SaveSystemDiagnostics(_logger, _saveDirectory);
            
            // Initialize auto-save and background operation tracking
            _autoSaveTimer = 0f;
            _autoSaveEnabled = _autoSaveConfig.EnableAutoSave;
            _saveOperationSemaphore = new SemaphoreSlim(1, 1);
            _isSaveInProgress = false;
            _currentSaveProgress = new SaveProgress();
            
            // Subscribe to error manager events
            _errorManager.ErrorOccurred += (sender, args) => OnSaveError(args);
            
            // Use optimized JSON options from performance manager
            _jsonOptions = _performanceManager.GetOptimizedJsonOptions();
            
            // Add custom converters for MonoGame types if not already present
            bool hasVector2Converter = false;
            foreach (var converter in _jsonOptions.Converters)
            {
                if (converter is Vector2JsonConverter)
                {
                    hasVector2Converter = true;
                    break;
                }
            }
            
            if (!hasVector2Converter)
            {
                _jsonOptions.Converters.Add(new Vector2JsonConverter());
            }

            // Ensure save directory exists
            EnsureDirectoryExists(_saveDirectory);
            
            // Log initialization
            _logger.LogInfo($"SaveManager initialized. Directory: {_saveDirectory}, AutoSave: {_autoSaveEnabled}");
            _logger.LogDebug($"Registered components: VersionManager, ErrorManager, PerformanceManager, Diagnostics");
        }

        /// <summary>
        /// Registers a system that implements ISaveable for inclusion in save operations
        /// </summary>
        /// <param name="saveable">The system to register</param>
        /// <exception cref="ArgumentNullException">Thrown when saveable is null</exception>
        /// <exception cref="ArgumentException">Thrown when SaveKey is null, empty, or already registered</exception>
        public void RegisterSaveable(ISaveable saveable)
        {
            if (saveable == null)
                throw new ArgumentNullException(nameof(saveable));

            if (string.IsNullOrEmpty(saveable.SaveKey))
                throw new ArgumentException("SaveKey cannot be null or empty", nameof(saveable));

            if (_saveableSystems.ContainsKey(saveable.SaveKey))
                throw new ArgumentException($"A system with SaveKey '{saveable.SaveKey}' is already registered", nameof(saveable));

            _saveableSystems[saveable.SaveKey] = saveable;
            
            var context = new SaveLogContext().WithSystem(saveable.SaveKey).WithProperty("SaveVersion", saveable.SaveVersion);
            _logger.LogInfo($"Registered saveable system: {saveable.SaveKey} (version {saveable.SaveVersion})", context);
        }

        /// <summary>
        /// Unregisters a previously registered ISaveable system
        /// </summary>
        /// <param name="saveKey">The SaveKey of the system to unregister</param>
        /// <returns>True if the system was found and removed, false otherwise</returns>
        public bool UnregisterSaveable(string saveKey)
        {
            bool removed = _saveableSystems.Remove(saveKey);
            if (removed)
            {
                var context = new SaveLogContext().WithSystem(saveKey);
                _logger.LogInfo($"Unregistered saveable system: {saveKey}", context);
            }
            else
            {
                _logger.LogWarning($"Attempted to unregister unknown system: {saveKey}");
            }
            return removed;
        }

        /// <summary>
        /// Gets the number of registered saveable systems
        /// </summary>
        public int RegisteredSystemCount => _saveableSystems.Count;

        /// <summary>
        /// Checks if a system with the given SaveKey is registered
        /// </summary>
        /// <param name="saveKey">The SaveKey to check</param>
        /// <returns>True if registered, false otherwise</returns>
        public bool IsSystemRegistered(string saveKey)
        {
            return _saveableSystems.ContainsKey(saveKey);
        }

        /// <summary>
        /// Updates the auto-save timer and triggers automatic saves when conditions are met
        /// </summary>
        /// <param name="gameTime">Current game time</param>
        public void Update(GameTime gameTime)
        {
            if (!_autoSaveEnabled || _isSaveInProgress)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _autoSaveTimer += deltaTime;

            // Check if it's time for an interval-based auto-save
            if (_autoSaveTimer >= _autoSaveConfig.AutoSaveIntervalSeconds)
            {
                TriggerAutoSave(AutoSaveTrigger.TimeInterval);
                _autoSaveTimer = 0f;
            }
        }

        /// <summary>
        /// Triggers an automatic save with the specified trigger reason
        /// </summary>
        /// <param name="trigger">The reason for the automatic save</param>
        /// <param name="additionalInfo">Additional information about the trigger</param>
        public void TriggerAutoSave(AutoSaveTrigger trigger, string additionalInfo = null)
        {
            if (!_autoSaveEnabled || _isSaveInProgress)
                return;

            // Fire event to notify listeners
            OnAutoSaveTriggered(new AutoSaveTriggeredEventArgs
            {
                Trigger = trigger,
                AdditionalInfo = additionalInfo,
                SlotId = _currentAutoSaveSlot
            });

            // Perform background save
            _ = Task.Run(async () =>
            {
                try
                {
                    await SaveGameAsync(_currentAutoSaveSlot);
                }
                catch (Exception ex)
                {
                    OnSaveError(new SaveErrorEventArgs(
                        SaveErrorType.UnknownError,
                        $"Auto-save failed: {ex.Message}",
                        ex)
                    {
                        CanRetry = true,
                        HasBackup = false
                    });
                }
            });
        }

        /// <summary>
        /// Subscribes to game system events for automatic save triggers
        /// </summary>
        /// <param name="zoneManager">Zone manager for biome transition events</param>
        /// <param name="timeManager">Time manager for season change events</param>
        /// <param name="journalManager">Journal manager for significant events</param>
        /// <param name="weatherManager">Weather manager for weather change events</param>
        /// <param name="questManager">Quest manager for quest completion and milestone events</param>
        public void SubscribeToAutoSaveTriggers(ZoneManager zoneManager = null, TimeManager timeManager = null, 
            JournalManager journalManager = null, WeatherManager weatherManager = null, QuestManager questManager = null)
        {
            if (zoneManager != null && _autoSaveConfig.SaveOnBiomeTransition)
            {
                // Note: ZoneManager doesn't have a direct biome transition event, 
                // but we can monitor zone changes and detect biome transitions
                // This would require adding an event to ZoneManager or monitoring through the game loop
            }

            if (timeManager != null)
            {
                if (_autoSaveConfig.SaveOnSeasonChange)
                {
                    // Subscribe to day changes and check for season transitions
                    timeManager.DayChanged += (day) =>
                    {
                        // Season changes every 30 days (0-29 = Spring, 30-59 = Summer, etc.)
                        if (day % 30 == 1 && day > 1) // First day of new season (except first season)
                        {
                            string seasonName = timeManager.GetSeasonName();
                            TriggerAutoSave(AutoSaveTrigger.SeasonChange, $"Entered {seasonName}");
                        }
                    };
                }

                if (_autoSaveConfig.SaveOnSignificantEvents)
                {
                    // Save on weekly milestones
                    timeManager.WeekPassed += (week) =>
                    {
                        if (week > 0) // Don't save on the first week
                        {
                            TriggerAutoSave(AutoSaveTrigger.SignificantEvent, $"Week {week} milestone");
                        }
                    };
                }
            }

            if (journalManager != null && _autoSaveConfig.SaveOnSignificantEvents)
            {
                journalManager.NewBiomeDiscovered += (biome) =>
                {
                    TriggerAutoSave(AutoSaveTrigger.BiomeTransition, $"Discovered {biome} biome");
                };

                journalManager.NewEntryAdded += (entry) =>
                {
                    if (entry.Type == JournalEntryType.Milestone)
                    {
                        TriggerAutoSave(AutoSaveTrigger.SignificantEvent, entry.Title);
                    }
                };
            }

            if (weatherManager != null && _autoSaveConfig.SaveOnWeatherChange)
            {
                weatherManager.WeatherChanged += (weather) =>
                {
                    if (weather != WeatherType.Clear) // Only save on significant weather changes
                    {
                        TriggerAutoSave(AutoSaveTrigger.WeatherChange, $"Weather changed to {weather}");
                    }
                };
            }

            if (questManager != null && _autoSaveConfig.SaveOnSignificantEvents)
            {
                // Save when quests are completed
                questManager.QuestCompleted += (sender, args) =>
                {
                    TriggerAutoSave(AutoSaveTrigger.SignificantEvent, $"Quest completed: {args.Quest.QuestTemplateId}");
                };

                // Save when quest data changes (for important quest state changes)
                questManager.QuestDataChanged += (sender, args) =>
                {
                    // Only trigger auto-save for significant quest data changes, not every minor update
                    // This could be enhanced to check for specific types of changes
                    if (_autoSaveConfig.SaveOnSignificantEvents)
                    {
                        TriggerAutoSave(AutoSaveTrigger.SignificantEvent, "Quest progress updated");
                    }
                };
            }
        }

        /// <summary>
        /// Gets the current auto-save configuration
        /// </summary>
        public AutoSaveConfig GetAutoSaveConfig()
        {
            return _autoSaveConfig;
        }

        /// <summary>
        /// Updates auto-save settings
        /// </summary>
        /// <param name="enabled">Whether auto-save is enabled</param>
        /// <param name="slotId">Slot to use for auto-saves (1-based)</param>
        public void SetAutoSaveSettings(bool enabled, int slotId = 1)
        {
            _autoSaveEnabled = enabled;
            _currentAutoSaveSlot = Math.Max(1, slotId);
            _autoSaveTimer = 0f; // Reset timer when settings change
        }

        /// <summary>
        /// Gets whether a save operation is currently in progress
        /// </summary>
        public bool IsSaveInProgress => _isSaveInProgress;

        /// <summary>
        /// Gets the current save progress information
        /// </summary>
        public SaveProgress GetSaveProgress()
        {
            return _currentSaveProgress;
        }

        /// <summary>
        /// Gets the performance manager for accessing performance metrics and optimizations
        /// </summary>
        public SavePerformanceManager GetPerformanceManager()
        {
            return _performanceManager;
        }

        /// <summary>
        /// Gets the logger for save system operations
        /// </summary>
        public ISaveSystemLogger GetLogger()
        {
            return _logger;
        }

        /// <summary>
        /// Gets the diagnostics system for health checks and metrics
        /// </summary>
        public SaveSystemDiagnostics GetDiagnostics()
        {
            return _diagnostics;
        }

        /// <summary>
        /// Performs a health check of the save system
        /// </summary>
        /// <returns>Health report with status and metrics</returns>
        public async Task<SaveSystemHealthReport> PerformHealthCheckAsync()
        {
            _logger.LogInfo("Performing save system health check");
            return await _diagnostics.PerformHealthCheckAsync();
        }

        /// <summary>
        /// Exports diagnostic information to a file
        /// </summary>
        /// <param name="filePath">Path where diagnostic data will be exported</param>
        public async Task ExportDiagnosticsAsync(string filePath)
        {
            _logger.LogInfo($"Exporting diagnostics to: {filePath}");
            await _diagnostics.ExportDiagnosticsAsync(filePath);
        }

        /// <summary>
        /// Sets the logging level for save system operations
        /// </summary>
        /// <param name="level">Minimum log level to record</param>
        /// <param name="verboseLogging">Whether to enable verbose logging</param>
        public void SetLoggingLevel(SaveLogLevel level, bool verboseLogging = false)
        {
            _logger.MinimumLevel = level;
            _logger.VerboseLogging = verboseLogging;
            _logger.LogInfo($"Logging level changed to: {level}, Verbose: {verboseLogging}");
        }

        /// <summary>
        /// Asynchronously saves the current game state to the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier (1-based)</param>
        /// <returns>Task representing the async save operation</returns>
        public async Task SaveGameAsync(int slotId)
        {
            var operationStartTime = DateTime.Now;
            var context = new SaveLogContext().WithSlot(slotId).WithOperation("Save");
            
            _logger.LogInfo($"Starting save operation for slot {slotId}", context);
            
            // Acquire semaphore to prevent concurrent saves
            await _saveOperationSemaphore.WaitAsync();
            
            // Start performance profiling
            using var profiler = _performanceManager.StartProfiling("save", slotId);
            
            try
            {
                _isSaveInProgress = true;
                _currentSaveProgress.Reset();
                _currentSaveProgress.Phase = SavePhase.Starting;
                OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });

                await _errorManager.ExecuteWithRetryAsync(async () =>
                {
                    if (slotId < 1)
                        throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

                    // Update progress: Validating
                    _currentSaveProgress.Phase = SavePhase.Validating;
                    _currentSaveProgress.CurrentStep = "Checking disk space and permissions";
                    OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });

                    // Check disk space and permissions before starting
                    string slotDirectory = Path.GetDirectoryName(GetSaveFilePath(slotId));
                    _logger.LogDebug($"Checking disk space and permissions for: {slotDirectory}", context);
                    
                    if (!_errorManager.CheckDiskSpace(slotDirectory))
                    {
                        _logger.LogError("Insufficient disk space for save operation", null, context);
                        throw new IOException("Insufficient disk space for save operation");
                    }
                    
                    if (!_errorManager.CheckPermissions(slotDirectory))
                    {
                        _logger.LogError("Insufficient permissions for save operation", null, context);
                        throw new UnauthorizedAccessException("Insufficient permissions for save operation");
                    }

                    // Update progress: Collecting data
                    _currentSaveProgress.Phase = SavePhase.CollectingData;
                    _currentSaveProgress.TotalSystems = _saveableSystems.Count;
                    OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });

                    // Create save data structure
                    var saveData = new GameSaveData();
                    var failedSystems = new List<string>();
                    
                    // Collect data from all registered systems
                    int systemIndex = 0;
                    foreach (var kvp in _saveableSystems)
                    {
                        var systemContext = context.WithSystem(kvp.Key);
                        try
                        {
                            _currentSaveProgress.CurrentStep = $"Collecting data from {kvp.Key}";
                            _currentSaveProgress.SystemsProcessed = systemIndex;
                            OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });

                            _logger.LogTrace($"Collecting save data from system: {kvp.Key}", systemContext);
                            var systemData = kvp.Value.GetSaveData();
                            if (systemData != null)
                            {
                                saveData.SystemData[kvp.Key] = systemData;
                                _logger.LogSystemState(kvp.Key, systemData, systemContext);
                            }
                            else
                            {
                                _logger.LogWarning($"System {kvp.Key} returned null save data", null, systemContext);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to collect save data from system: {kvp.Key}", ex, systemContext);
                            failedSystems.Add(kvp.Key);
                            // Continue with other systems for graceful degradation
                        }
                        systemIndex++;
                    }

                    // Record system count for profiling
                    profiler.RecordSystemCount(saveData.SystemData.Count);

                    // Apply delta save optimization if enabled
                    var deltaData = _performanceManager.GetDeltaSaveData(saveData.SystemData);
                    bool deltaSaveUsed = deltaData.Count < saveData.SystemData.Count;
                    profiler.RecordDeltaSaveUsage(deltaSaveUsed);

                    if (deltaSaveUsed)
                    {
                        _currentSaveProgress.CurrentStep = $"Delta save: {deltaData.Count}/{saveData.SystemData.Count} systems changed";
                        OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });
                        
                        // Use delta data for save
                        saveData.SystemData = deltaData;
                    }

                    // Handle partial failures
                    if (failedSystems.Count > 0)
                    {
                        _errorManager.HandlePartialFailure(saveData, failedSystems.ToArray(), "save", slotId);
                    }

                    // Update progress: Processing
                    _currentSaveProgress.Phase = SavePhase.Processing;
                    _currentSaveProgress.CurrentStep = "Generating checksum and optimizing data";
                    OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });

                    // Generate checksum
                    saveData.Checksum = GenerateChecksum(saveData);

                    // Serialize using optimized settings
                    string jsonData = _performanceManager.SerializeOptimized(saveData);
                    profiler.RecordFileSize(Encoding.UTF8.GetByteCount(jsonData));

                    // Apply compression if enabled
                    byte[] finalData;
                    if (_performanceManager.IsCompressionEnabled)
                    {
                        _currentSaveProgress.CurrentStep = "Compressing save data";
                        OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });
                        
                        finalData = await _performanceManager.CompressSaveDataAsync(jsonData);
                        profiler.RecordCompressedSize(finalData.Length);
                    }
                    else
                    {
                        finalData = Encoding.UTF8.GetBytes(jsonData);
                        profiler.RecordCompressedSize(finalData.Length);
                    }

                    // Update progress: Writing
                    _currentSaveProgress.Phase = SavePhase.Writing;
                    _currentSaveProgress.CurrentStep = "Writing to disk";
                    OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });

                    // Get save file path
                    string saveFilePath = GetSaveFilePath(slotId);
                    string tempFilePath = saveFilePath + ".tmp";

                    // Ensure slot directory exists
                    EnsureDirectoryExists(Path.GetDirectoryName(saveFilePath));

                    // Write compressed/optimized data to temporary file
                    await File.WriteAllBytesAsync(tempFilePath, finalData);

                    // Atomic move from temp to final location
                    if (File.Exists(saveFilePath))
                    {
                        File.Replace(tempFilePath, saveFilePath, null);
                    }
                    else
                    {
                        File.Move(tempFilePath, saveFilePath);
                    }

                    // Update progress: Complete
                    _currentSaveProgress.Phase = SavePhase.Complete;
                    _currentSaveProgress.CurrentStep = "Save completed successfully";
                    OnSaveProgressChanged(new SaveProgressEventArgs { Progress = _currentSaveProgress });

                    // Record operation metrics
                    var operationDuration = DateTime.Now - operationStartTime;
                    var fileInfo = new FileInfo(saveFilePath);
                    _diagnostics.RecordSaveOperation(operationDuration, true, slotId, fileInfo.Length);

                    // Fire success event
                    OnSaveCompleted(new SaveCompletedEventArgs
                    {
                        SlotId = slotId,
                        SaveFilePath = saveFilePath,
                        SystemCount = saveData.SystemData.Count,
                        SaveTimestamp = saveData.SaveTimestamp
                    });

                    _logger.LogInfo($"Save operation completed successfully for slot {slotId}. Duration: {operationDuration.TotalMilliseconds:F2}ms, Size: {fileInfo.Length:N0} bytes", context);

                    return true; // Return value for ExecuteWithRetryAsync
                }, $"SaveGame_Slot{slotId}", slotId);
            }
            catch (Exception ex)
            {
                var operationDuration = DateTime.Now - operationStartTime;
                _diagnostics.RecordSaveOperation(operationDuration, false, slotId);
                _logger.LogError($"Save operation failed for slot {slotId}", ex, context);
                throw;
            }
            finally
            {
                _isSaveInProgress = false;
                _saveOperationSemaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously loads game state from the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier (1-based)</param>
        /// <returns>Task representing the async load operation</returns>
        public async Task LoadGameAsync(int slotId)
        {
            var operationStartTime = DateTime.Now;
            var context = new SaveLogContext().WithSlot(slotId).WithOperation("Load");
            
            _logger.LogInfo($"Starting load operation for slot {slotId}", context);
            
            // Start performance profiling
            using var profiler = _performanceManager.StartProfiling("load", slotId);
            
            try
            {
                await _errorManager.ExecuteWithRetryAsync(async () =>
                {
                    if (slotId < 1)
                        throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

                string saveFilePath = GetSaveFilePath(slotId);
                context = context.WithFile(saveFilePath);

                _logger.LogDebug($"Loading from file: {saveFilePath}", context);

                if (!File.Exists(saveFilePath))
                {
                    _logger.LogError($"Save file not found for slot {slotId}: {saveFilePath}", null, context);
                    throw new FileNotFoundException($"Save file not found for slot {slotId}", saveFilePath);
                }

                // Record file size for profiling
                var fileInfo = new FileInfo(saveFilePath);
                profiler.RecordCompressedSize(fileInfo.Length);
                _logger.LogFileSize(saveFilePath, fileInfo.Length, context);

                // Check version compatibility and migrate if needed
                int saveVersion = await _versionManager.DetectVersionAsync(saveFilePath);
                _logger.LogDebug($"Detected save file version: {saveVersion}", context.WithProperty("SaveVersion", saveVersion));
                
                if (!_versionManager.IsCompatible(saveVersion))
                {
                    _logger.LogError($"Save file version {saveVersion} is not compatible with current game version", null, context);
                    throw new NotSupportedException($"Save file version {saveVersion} is not compatible with current game version");
                }

                if (_versionManager.NeedsMigration(saveVersion))
                {
                    _logger.LogInfo($"Migrating save file from version {saveVersion}", context);
                    bool migrationSuccess = await _versionManager.MigrateDataAsync(saveFilePath);
                    if (!migrationSuccess)
                    {
                        _logger.LogError($"Failed to migrate save file from version {saveVersion}", null, context);
                        throw new InvalidOperationException($"Failed to migrate save file from version {saveVersion}");
                    }
                    _logger.LogInfo($"Successfully migrated save file from version {saveVersion}", context);
                }

                GameSaveData saveData;
                string saveDataJson;

                // Read file data
                byte[] fileData = await File.ReadAllBytesAsync(saveFilePath);

                // Try to decompress if compression was used
                try
                {
                    saveDataJson = await _performanceManager.DecompressSaveDataAsync(fileData);
                }
                catch (Exception)
                {
                    // If decompression fails, assume it's uncompressed data
                    saveDataJson = Encoding.UTF8.GetString(fileData);
                }

                // Record uncompressed size for profiling
                profiler.RecordFileSize(Encoding.UTF8.GetByteCount(saveDataJson));

                // Validate save data integrity
                if (!_errorManager.ValidateSaveData(saveDataJson, null, slotId))
                {
                    throw new InvalidDataException("Save data validation failed");
                }

                // Deserialize using optimized settings
                saveData = _performanceManager.DeserializeOptimized<GameSaveData>(saveDataJson);

                if (saveData == null)
                    throw new InvalidOperationException("Failed to deserialize save data");

                // Verify checksum
                string expectedChecksum = GenerateChecksum(saveData);
                if (saveData.Checksum != expectedChecksum)
                {
                    throw new InvalidDataException("Save file checksum mismatch - file may be corrupted");
                }

                // Record system count for profiling
                profiler.RecordSystemCount(saveData.SystemData.Count);

                // Load data into registered systems
                int systemsLoaded = 0;
                var failedSystems = new List<string>();
                
                foreach (var kvp in _saveableSystems)
                {
                    var systemContext = context.WithSystem(kvp.Key);
                    try
                    {
                        if (saveData.SystemData.TryGetValue(kvp.Key, out var systemData))
                        {
                            _logger.LogTrace($"Loading data into system: {kvp.Key}", systemContext);
                            
                            // Convert JsonElement to the expected type if needed
                            if (systemData is JsonElement jsonElement)
                            {
                                // Deserialize JsonElement to the appropriate type based on the system
                                var deserializedData = DeserializeSystemData(kvp.Key, jsonElement);
                                kvp.Value.LoadSaveData(deserializedData);
                            }
                            else
                            {
                                kvp.Value.LoadSaveData(systemData);
                            }
                            systemsLoaded++;
                            _logger.LogDebug($"Successfully loaded data into system: {kvp.Key}", systemContext);
                        }
                        else
                        {
                            _logger.LogWarning($"No save data found for system: {kvp.Key}", null, systemContext);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to load data into system: {kvp.Key}", ex, systemContext);
                        failedSystems.Add(kvp.Key);
                        // Continue with other systems for graceful degradation
                    }
                }

                // Handle partial failures
                if (failedSystems.Count > 0)
                {
                    _errorManager.HandlePartialFailure(saveData, failedSystems.ToArray(), "load", slotId);
                }

                // Record operation metrics
                var operationDuration = DateTime.Now - operationStartTime;
                _diagnostics.RecordLoadOperation(operationDuration, true, slotId, fileInfo.Length);

                // Fire success event
                OnLoadCompleted(new LoadCompletedEventArgs
                {
                    SlotId = slotId,
                    SaveFilePath = saveFilePath,
                    SystemCount = systemsLoaded,
                    SaveTimestamp = saveData.SaveTimestamp,
                    GameVersion = saveData.GameVersion
                });

                _logger.LogInfo($"Load operation completed successfully for slot {slotId}. Duration: {operationDuration.TotalMilliseconds:F2}ms, Systems loaded: {systemsLoaded}", context);

                return true; // Return value for ExecuteWithRetryAsync
                }, $"LoadGame_Slot{slotId}", slotId);
            }
            catch (Exception ex)
            {
                var operationDuration = DateTime.Now - operationStartTime;
                _diagnostics.RecordLoadOperation(operationDuration, false, slotId);
                _logger.LogError($"Load operation failed for slot {slotId}", ex, context);
                throw;
            }
        }

        /// <summary>
        /// Checks if a save file exists for the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>True if save file exists, false otherwise</returns>
        public bool SaveExists(int slotId)
        {
            return File.Exists(GetSaveFilePath(slotId));
        }

        /// <summary>
        /// Gets the full path to the save file for the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>Full path to the save file</returns>
        private string GetSaveFilePath(int slotId)
        {
            return Path.Combine(_saveDirectory, $"slot_{slotId}", "save.json");
        }

        /// <summary>
        /// Ensures the specified directory exists, creating it if necessary
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Generates a SHA-256 checksum for the save data (excluding the checksum field itself)
        /// </summary>
        /// <param name="saveData">The save data to generate checksum for</param>
        /// <returns>SHA-256 checksum as hex string</returns>
        private object DeserializeSystemData(string systemKey, JsonElement jsonElement)
        {
            try
            {
                return systemKey switch
                {
                    "Adventurer" => JsonSerializer.Deserialize<AdventurerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                    "JournalManager" => JsonSerializer.Deserialize<JournalSaveData>(jsonElement.GetRawText(), _jsonOptions),
                    "PoIManager" => JsonSerializer.Deserialize<PoISaveData>(jsonElement.GetRawText(), _jsonOptions),
                    "TimeManager" => JsonSerializer.Deserialize<TimeManagerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                    "WeatherManager" => JsonSerializer.Deserialize<WeatherManagerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                    "ZoneManager" => JsonSerializer.Deserialize<ZoneManagerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                    "QuestManager" => JsonSerializer.Deserialize<QuestSaveData>(jsonElement.GetRawText(), _jsonOptions),
                    _ => jsonElement // Fallback to JsonElement for unknown systems
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to deserialize {systemKey}: {ex.Message}");
                return jsonElement; // Fallback to JsonElement on error
            }
        }

        private string GenerateChecksum(GameSaveData saveData)
        {
            // Create a copy without the checksum for hashing
            var dataForHashing = new GameSaveData
            {
                Version = saveData.Version,
                SaveTimestamp = saveData.SaveTimestamp,
                GameVersion = saveData.GameVersion,
                SystemData = saveData.SystemData,
                Checksum = null // Exclude checksum from hash calculation
            };

            string jsonString = JsonSerializer.Serialize(dataForHashing, _jsonOptions);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(jsonBytes);
                return Convert.ToHexString(hashBytes);
            }
        }



        /// <summary>
        /// Raises the SaveCompleted event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnSaveCompleted(SaveCompletedEventArgs args)
        {
            SaveCompleted?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the LoadCompleted event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnLoadCompleted(LoadCompletedEventArgs args)
        {
            LoadCompleted?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the SaveError event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnSaveError(SaveErrorEventArgs args)
        {
            SaveError?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the AutoSaveTriggered event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnAutoSaveTriggered(AutoSaveTriggeredEventArgs args)
        {
            AutoSaveTriggered?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the SaveProgressChanged event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnSaveProgressChanged(SaveProgressEventArgs args)
        {
            SaveProgressChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Event arguments for successful save operations
    /// </summary>
    public class SaveCompletedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public string SaveFilePath { get; set; }
        public int SystemCount { get; set; }
        public DateTime SaveTimestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for successful load operations
    /// </summary>
    public class LoadCompletedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public string SaveFilePath { get; set; }
        public int SystemCount { get; set; }
        public DateTime SaveTimestamp { get; set; }
        public string GameVersion { get; set; }
    }

    /// <summary>
    /// Event arguments for automatic save triggers
    /// </summary>
    public class AutoSaveTriggeredEventArgs : EventArgs
    {
        public AutoSaveTrigger Trigger { get; set; }
        public string AdditionalInfo { get; set; }
        public int SlotId { get; set; }
        public DateTime TriggerTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Event arguments for save progress updates
    /// </summary>
    public class SaveProgressEventArgs : EventArgs
    {
        public SaveProgress Progress { get; set; }
    }

    /// <summary>
    /// Enumeration of automatic save triggers
    /// </summary>
    public enum AutoSaveTrigger
    {
        TimeInterval,
        BiomeTransition,
        SeasonChange,
        WeatherChange,
        SignificantEvent,
        ManualTrigger
    }

    /// <summary>
    /// Enumeration of save operation phases
    /// </summary>
    public enum SavePhase
    {
        Starting,
        Validating,
        CollectingData,
        Processing,
        Writing,
        Complete,
        Error
    }

    /// <summary>
    /// Configuration for automatic save triggers
    /// </summary>
    public class AutoSaveConfig
    {
        /// <summary>
        /// Whether automatic saves are enabled
        /// </summary>
        public bool EnableAutoSave { get; set; } = true;

        /// <summary>
        /// Interval in seconds between automatic saves
        /// </summary>
        public float AutoSaveIntervalSeconds { get; set; } = 300f; // 5 minutes

        /// <summary>
        /// Whether to save when transitioning between biomes
        /// </summary>
        public bool SaveOnBiomeTransition { get; set; } = true;

        /// <summary>
        /// Whether to save when seasons change
        /// </summary>
        public bool SaveOnSeasonChange { get; set; } = true;

        /// <summary>
        /// Whether to save when weather changes significantly
        /// </summary>
        public bool SaveOnWeatherChange { get; set; } = false; // Disabled by default as weather changes frequently

        /// <summary>
        /// Whether to save on significant game events (milestones, discoveries, etc.)
        /// </summary>
        public bool SaveOnSignificantEvents { get; set; } = true;

        /// <summary>
        /// Maximum number of background save operations that can be queued
        /// </summary>
        public int MaxConcurrentSaves { get; set; } = 1;
    }

    /// <summary>
    /// Tracks the progress of a save operation
    /// </summary>
    public class SaveProgress
    {
        /// <summary>
        /// Current phase of the save operation
        /// </summary>
        public SavePhase Phase { get; set; } = SavePhase.Starting;

        /// <summary>
        /// Description of the current step being performed
        /// </summary>
        public string CurrentStep { get; set; } = "";

        /// <summary>
        /// Total number of systems to process
        /// </summary>
        public int TotalSystems { get; set; } = 0;

        /// <summary>
        /// Number of systems processed so far
        /// </summary>
        public int SystemsProcessed { get; set; } = 0;

        /// <summary>
        /// Overall progress percentage (0.0 to 1.0)
        /// </summary>
        public float ProgressPercentage
        {
            get
            {
                return Phase switch
                {
                    SavePhase.Starting => 0.0f,
                    SavePhase.Validating => 0.1f,
                    SavePhase.CollectingData => TotalSystems > 0 ? 0.2f + (0.5f * SystemsProcessed / TotalSystems) : 0.2f,
                    SavePhase.Processing => 0.7f,
                    SavePhase.Writing => 0.9f,
                    SavePhase.Complete => 1.0f,
                    SavePhase.Error => 0.0f,
                    _ => 0.0f
                };
            }
        }

        /// <summary>
        /// Whether the operation has completed (successfully or with error)
        /// </summary>
        public bool IsComplete => Phase == SavePhase.Complete || Phase == SavePhase.Error;

        /// <summary>
        /// Resets the progress to initial state
        /// </summary>
        public void Reset()
        {
            Phase = SavePhase.Starting;
            CurrentStep = "";
            TotalSystems = 0;
            SystemsProcessed = 0;
        }
    }

    /// <summary>
    /// Custom JSON converter for Microsoft.Xna.Framework.Vector2
    /// </summary>
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            float x = 0, y = 0;
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName?.ToLowerInvariant())
                    {
                        case "x":
                            x = reader.GetSingle();
                            break;
                        case "y":
                            y = reader.GetSingle();
                            break;
                    }
                }
            }

            return new Vector2(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteEndObject();
        }
    }

}