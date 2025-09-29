using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Simplified SaveManager without version management or compression
    /// Perfect for a 16-bit game that doesn't need complex save features
    /// </summary>
    public class SaveManagerSimple
    {
        private readonly string _saveDirectory;
        private readonly Dictionary<string, ISaveable> _saveableSystems;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _autoSaveEnabled = true;
        private float _autoSaveTimer = 0f;
        private readonly float _autoSaveInterval = 300f; // 5 minutes
        private bool _isSaveInProgress = false;
        public const int AUTO_SAVE_SLOT = -1; // Special slot for auto-saves

        // Events for compatibility with existing UI
        public event EventHandler<SaveCompletedEventArgs> SaveCompleted;
        public event EventHandler<LoadCompletedEventArgs> LoadCompleted;
        public event EventHandler<SaveErrorEventArgs> SaveError;
        public event EventHandler<AutoSaveTriggeredEventArgs> AutoSaveTriggered;
        public event EventHandler<SaveProgressEventArgs> SaveProgressChanged;

        public SaveManagerSimple(string saveDirectory = "Saves")
        {
            _saveDirectory = saveDirectory;
            _saveableSystems = new Dictionary<string, ISaveable>();
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = true
            };
        }

        // Properties for compatibility
        public int RegisteredSystemCount => _saveableSystems.Count;
        public string SaveDirectory => _saveDirectory;
        public bool AutoSaveEnabled 
        { 
            get => _autoSaveEnabled; 
            set => _autoSaveEnabled = value; 
        }

        public void RegisterSaveable(ISaveable saveable)
        {
            _saveableSystems[saveable.SaveKey] = saveable;
        }

        public async Task SaveGameAsync(int slotNumber)
        {
            if (_isSaveInProgress) return;
            
            try
            {
                _isSaveInProgress = true;
                var progress = new SaveProgress { Phase = SavePhase.Starting, CurrentStep = "Starting save operation" };
                SaveProgressChanged?.Invoke(this, new SaveProgressEventArgs { Progress = progress });

                string slotDirectoryName = slotNumber == AUTO_SAVE_SLOT ? "autosave" : $"slot_{slotNumber}";
                var slotDirectory = Path.Combine(_saveDirectory, slotDirectoryName);
                Directory.CreateDirectory(slotDirectory);

                progress.Phase = SavePhase.CollectingData;
                progress.CurrentStep = "Collecting system data";
                progress.TotalSystems = _saveableSystems.Count;
                SaveProgressChanged?.Invoke(this, new SaveProgressEventArgs { Progress = progress });

                var saveData = new GameSaveData
                {
                    Version = 1,
                    SaveTimestamp = DateTime.UtcNow,
                    SystemData = new Dictionary<string, object>()
                };

                // Collect data from all registered systems
                int systemIndex = 0;
                foreach (var system in _saveableSystems.Values)
                {
                    progress.CurrentStep = $"Collecting data from {system.SaveKey}";
                    progress.SystemsProcessed = systemIndex++;
                    SaveProgressChanged?.Invoke(this, new SaveProgressEventArgs { Progress = progress });
                    
                    saveData.SystemData[system.SaveKey] = system.GetSaveData();
                }

                progress.Phase = SavePhase.Writing;
                progress.CurrentStep = "Writing save file";
                SaveProgressChanged?.Invoke(this, new SaveProgressEventArgs { Progress = progress });

                var jsonData = JsonSerializer.Serialize(saveData, _jsonOptions);
                var saveFilePath = Path.Combine(slotDirectory, "save.json");
                
                await File.WriteAllTextAsync(saveFilePath, jsonData);

                progress.Phase = SavePhase.Complete;
                progress.CurrentStep = "Save completed";
                SaveProgressChanged?.Invoke(this, new SaveProgressEventArgs { Progress = progress });

                // Fire success event
                SaveCompleted?.Invoke(this, new SaveCompletedEventArgs 
                { 
                    SlotId = slotNumber,
                    SaveTimestamp = saveData.SaveTimestamp,
                    SaveFilePath = saveFilePath,
                    SystemCount = _saveableSystems.Count
                });
            }
            catch (Exception ex)
            {
                var errorArgs = new SaveErrorEventArgs(SaveErrorType.UnknownError, $"Save failed: {ex.Message}", ex)
                {
                    SlotId = slotNumber,
                    CanRetry = true,
                    HasBackup = false
                };
                SaveError?.Invoke(this, errorArgs);
                throw;
            }
            finally
            {
                _isSaveInProgress = false;
            }
        }

        public async Task LoadGameAsync(int slotNumber)
        {
            try
            {
                string slotDirectoryName = slotNumber == AUTO_SAVE_SLOT ? "autosave" : $"slot_{slotNumber}";
                var saveFilePath = Path.Combine(_saveDirectory, slotDirectoryName, "save.json");
                
                if (!File.Exists(saveFilePath))
                {
                    throw new FileNotFoundException($"Save file not found: {saveFilePath}");
                }

                var jsonData = await File.ReadAllTextAsync(saveFilePath);
                var saveData = JsonSerializer.Deserialize<GameSaveData>(jsonData, _jsonOptions);

                if (saveData?.SystemData == null)
                {
                    throw new InvalidOperationException("Invalid save data");
                }

                // Load data into all registered systems
                foreach (var system in _saveableSystems.Values)
                {
                    if (saveData.SystemData.TryGetValue(system.SaveKey, out var systemData))
                    {
                        var jsonElement = (JsonElement)systemData;
                        
                        // Deserialize based on the system type
                        object deserializedData = system.SaveKey switch
                        {
                            "Adventurer" => JsonSerializer.Deserialize<AdventurerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                            "TimeManager" => JsonSerializer.Deserialize<TimeManagerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                            "JournalManager" => JsonSerializer.Deserialize<JournalSaveData>(jsonElement.GetRawText(), _jsonOptions),
                            "PoIManager" => JsonSerializer.Deserialize<PoISaveData>(jsonElement.GetRawText(), _jsonOptions),
                            "WeatherManager" => JsonSerializer.Deserialize<WeatherManagerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                            "ZoneManager" => JsonSerializer.Deserialize<ZoneManagerSaveData>(jsonElement.GetRawText(), _jsonOptions),
                            _ => jsonElement
                        };
                        
                        system.LoadSaveData(deserializedData);
                    }
                }

                // Fire success event
                LoadCompleted?.Invoke(this, new LoadCompletedEventArgs 
                { 
                    SlotId = slotNumber,
                    SaveTimestamp = saveData.SaveTimestamp,
                    SaveFilePath = saveFilePath,
                    SystemCount = _saveableSystems.Count,
                    GameVersion = "1.0.0"
                });
            }
            catch (Exception ex)
            {
                var errorArgs = new SaveErrorEventArgs(SaveErrorType.UnknownError, $"Load failed: {ex.Message}", ex)
                {
                    SlotId = slotNumber,
                    CanRetry = true,
                    HasBackup = false
                };
                SaveError?.Invoke(this, errorArgs);
                throw;
            }
        }

        public bool SaveExists(int slotNumber)
        {
            string slotDirectoryName = slotNumber == AUTO_SAVE_SLOT ? "autosave" : $"slot_{slotNumber}";
            var saveFilePath = Path.Combine(_saveDirectory, slotDirectoryName, "save.json");
            return File.Exists(saveFilePath);
        }

        public async Task<DateTime?> GetSaveTimestamp(int slotNumber)
        {
            if (!SaveExists(slotNumber))
                return null;

            try
            {
                string slotDirectoryName = slotNumber == AUTO_SAVE_SLOT ? "autosave" : $"slot_{slotNumber}";
                var saveFilePath = Path.Combine(_saveDirectory, slotDirectoryName, "save.json");
                var jsonData = await File.ReadAllTextAsync(saveFilePath);
                var saveData = JsonSerializer.Deserialize<GameSaveData>(jsonData, _jsonOptions);
                return saveData?.SaveTimestamp;
            }
            catch
            {
                return null;
            }
        }

        // Methods for compatibility with existing game integration
        public void Update(GameTime gameTime)
        {
            if (!_autoSaveEnabled || _isSaveInProgress)
                return;

            _autoSaveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_autoSaveTimer >= _autoSaveInterval)
            {
                TriggerAutoSave(AutoSaveTrigger.TimeInterval);
                _autoSaveTimer = 0f;
            }
        }

        public void TriggerAutoSave(AutoSaveTrigger trigger, string additionalInfo = null)
        {
            if (!_autoSaveEnabled || _isSaveInProgress)
                return;

            AutoSaveTriggered?.Invoke(this, new AutoSaveTriggeredEventArgs
            {
                Trigger = trigger,
                AdditionalInfo = additionalInfo,
                SlotId = AUTO_SAVE_SLOT,
                TriggerTime = DateTime.UtcNow
            });

            // Auto-save to special auto-save slot
            _ = Task.Run(async () =>
            {
                try
                {
                    await SaveGameAsync(AUTO_SAVE_SLOT);
                }
                catch (Exception ex)
                {
                    var errorArgs = new SaveErrorEventArgs(SaveErrorType.UnknownError, $"Auto-save failed: {ex.Message}", ex)
                    {
                        SlotId = AUTO_SAVE_SLOT,
                        CanRetry = true,
                        HasBackup = false
                    };
                    SaveError?.Invoke(this, errorArgs);
                }
            });
        }

        // Stub methods for compatibility (not needed for simple save system)
        public void InitializeEventHandlers() { }
        public void EnableAutoSave() => _autoSaveEnabled = true;
        public void DisableAutoSave() => _autoSaveEnabled = false;

        // Auto-save specific methods
        public bool AutoSaveExists() => SaveExists(AUTO_SAVE_SLOT);
        public async Task<DateTime?> GetAutoSaveTimestamp() => await GetSaveTimestamp(AUTO_SAVE_SLOT);
        public async Task LoadAutoSaveAsync() => await LoadGameAsync(AUTO_SAVE_SLOT);
    }
}