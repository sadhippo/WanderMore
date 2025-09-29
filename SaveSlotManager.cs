using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace HiddenHorizons
{
    /// <summary>
    /// Manages multiple save slots with metadata tracking and persistence
    /// </summary>
    public class SaveSlotManager
    {
        private readonly string _saveDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<int, SaveSlotMetadata> _slotMetadataCache;

        /// <summary>
        /// Event fired when a save slot is created
        /// </summary>
        public event EventHandler<SlotCreatedEventArgs> SlotCreated;

        /// <summary>
        /// Event fired when a save slot is deleted
        /// </summary>
        public event EventHandler<SlotDeletedEventArgs> SlotDeleted;

        /// <summary>
        /// Event fired when slot metadata is updated
        /// </summary>
        public event EventHandler<SlotMetadataUpdatedEventArgs> SlotMetadataUpdated;

        /// <summary>
        /// Initializes a new instance of the SaveSlotManager
        /// </summary>
        /// <param name="saveDirectory">Directory where save slots will be stored</param>
        public SaveSlotManager(string saveDirectory = "Saves")
        {
            _saveDirectory = saveDirectory;
            _slotMetadataCache = new Dictionary<int, SaveSlotMetadata>();
            
            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Ensure save directory exists
            EnsureDirectoryExists(_saveDirectory);
            
            // Load existing slot metadata
            LoadAllSlotMetadata();
        }

        /// <summary>
        /// Creates a new save slot with the specified ID
        /// </summary>
        /// <param name="slotId">The slot ID to create (1-based)</param>
        /// <param name="initialMetadata">Optional initial metadata for the slot</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentException">Thrown when slotId is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when slot already exists</exception>
        public async Task CreateSlotAsync(int slotId, SaveSlotMetadata initialMetadata = null)
        {
            if (slotId < 1)
                throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

            string slotDirectory = GetSlotDirectory(slotId);
            
            if (Directory.Exists(slotDirectory))
                throw new InvalidOperationException($"Save slot {slotId} already exists");

            // Create slot directory
            Directory.CreateDirectory(slotDirectory);

            // Create initial metadata
            var metadata = initialMetadata ?? new SaveSlotMetadata
            {
                SlotId = slotId,
                LastSaveTime = DateTime.UtcNow,
                PlayTime = TimeSpan.Zero,
                CurrentDay = 1,
                CurrentZoneName = "Starting Zone",
                CurrentBiome = BiomeType.Plains,
                ZonesVisited = 0,
                JournalEntries = 0,
                GameVersion = "1.0.0",
                FileSizeBytes = 0
            };

            // Save metadata to file
            await SaveSlotMetadataAsync(slotId, metadata);

            // Update cache
            _slotMetadataCache[slotId] = metadata;

            // Fire event
            OnSlotCreated(new SlotCreatedEventArgs { SlotId = slotId, Metadata = metadata });
        }

        /// <summary>
        /// Deletes the specified save slot and all associated data
        /// </summary>
        /// <param name="slotId">The slot ID to delete</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentException">Thrown when slotId is invalid</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when slot doesn't exist</exception>
        public async Task DeleteSlotAsync(int slotId)
        {
            if (slotId < 1)
                throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

            string slotDirectory = GetSlotDirectory(slotId);
            
            if (!Directory.Exists(slotDirectory))
                throw new DirectoryNotFoundException($"Save slot {slotId} does not exist");

            // Get metadata before deletion for event
            var metadata = await GetSlotInfoAsync(slotId);

            // Delete the entire slot directory and all contents
            Directory.Delete(slotDirectory, recursive: true);

            // Remove from cache
            _slotMetadataCache.Remove(slotId);

            // Fire event
            OnSlotDeleted(new SlotDeletedEventArgs { SlotId = slotId, DeletedMetadata = metadata });
        }

        /// <summary>
        /// Gets metadata information for the specified save slot
        /// </summary>
        /// <param name="slotId">The slot ID to get info for</param>
        /// <returns>SaveSlotMetadata for the slot, or null if slot doesn't exist</returns>
        public async Task<SaveSlotMetadata> GetSlotInfoAsync(int slotId)
        {
            if (slotId < 1)
                return null;

            // Check cache first
            if (_slotMetadataCache.TryGetValue(slotId, out var cachedMetadata))
            {
                return cachedMetadata;
            }

            // Try to load from file
            string metadataPath = GetSlotMetadataPath(slotId);
            if (!File.Exists(metadataPath))
                return null;

            try
            {
                await using var fileStream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read);
                var metadata = await JsonSerializer.DeserializeAsync<SaveSlotMetadata>(fileStream, _jsonOptions);
                
                // Update cache
                if (metadata != null)
                {
                    _slotMetadataCache[slotId] = metadata;
                }
                
                return metadata;
            }
            catch (Exception)
            {
                // If metadata file is corrupted, try to recreate it if save file exists
                string saveFilePath = GetSlotSaveFilePath(slotId);
                if (File.Exists(saveFilePath))
                {
                    var fileInfo = new FileInfo(saveFilePath);
                    var recreatedMetadata = new SaveSlotMetadata
                    {
                        SlotId = slotId,
                        LastSaveTime = fileInfo.LastWriteTime,
                        PlayTime = TimeSpan.Zero,
                        CurrentDay = 1,
                        CurrentZoneName = "Unknown",
                        CurrentBiome = BiomeType.Plains,
                        ZonesVisited = 0,
                        JournalEntries = 0,
                        GameVersion = "Unknown",
                        FileSizeBytes = fileInfo.Length
                    };

                    try
                    {
                        await SaveSlotMetadataAsync(slotId, recreatedMetadata);
                        _slotMetadataCache[slotId] = recreatedMetadata;
                        return recreatedMetadata;
                    }
                    catch (Exception)
                    {
                        // If we can't save the recreated metadata, return null
                        return null;
                    }
                }
                
                // If no save file exists, return null
                return null;
            }
        }

        /// <summary>
        /// Gets metadata for all existing save slots
        /// </summary>
        /// <returns>Dictionary of slot ID to metadata for all existing slots</returns>
        public async Task<Dictionary<int, SaveSlotMetadata>> GetAllSlotInfoAsync()
        {
            var result = new Dictionary<int, SaveSlotMetadata>();

            // Get all slot directories
            if (!Directory.Exists(_saveDirectory))
                return result;

            var slotDirectories = Directory.GetDirectories(_saveDirectory, "slot_*");
            
            foreach (var slotDir in slotDirectories)
            {
                var dirName = Path.GetFileName(slotDir);
                if (dirName.StartsWith("slot_") && int.TryParse(dirName.Substring(5), out int slotId))
                {
                    var metadata = await GetSlotInfoAsync(slotId);
                    if (metadata != null)
                    {
                        result[slotId] = metadata;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a save slot exists
        /// </summary>
        /// <param name="slotId">The slot ID to check</param>
        /// <returns>True if the slot exists, false otherwise</returns>
        public bool SlotExists(int slotId)
        {
            if (slotId < 1)
                return false;

            return Directory.Exists(GetSlotDirectory(slotId));
        }

        /// <summary>
        /// Updates metadata for the specified save slot
        /// </summary>
        /// <param name="slotId">The slot ID to update</param>
        /// <param name="metadata">The updated metadata</param>
        /// <returns>Task representing the async operation</returns>
        public async Task UpdateSlotMetadataAsync(int slotId, SaveSlotMetadata metadata)
        {
            if (slotId < 1)
                throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            if (!SlotExists(slotId))
                throw new DirectoryNotFoundException($"Save slot {slotId} does not exist");

            // Ensure slot ID matches
            metadata.SlotId = slotId;

            // Update file size if save file exists
            string saveFilePath = GetSlotSaveFilePath(slotId);
            if (File.Exists(saveFilePath))
            {
                var fileInfo = new FileInfo(saveFilePath);
                metadata.FileSizeBytes = fileInfo.Length;
            }

            // Save to file
            await SaveSlotMetadataAsync(slotId, metadata);

            // Update cache
            _slotMetadataCache[slotId] = metadata;

            // Fire event
            OnSlotMetadataUpdated(new SlotMetadataUpdatedEventArgs { SlotId = slotId, Metadata = metadata });
        }

        /// <summary>
        /// Validates and cleans up save slot data
        /// </summary>
        /// <returns>Task representing the async cleanup operation</returns>
        public async Task ValidateAndCleanupSlotsAsync()
        {
            if (!Directory.Exists(_saveDirectory))
                return;

            var slotDirectories = Directory.GetDirectories(_saveDirectory, "slot_*");
            
            foreach (var slotDir in slotDirectories)
            {
                var dirName = Path.GetFileName(slotDir);
                if (dirName.StartsWith("slot_") && int.TryParse(dirName.Substring(5), out int slotId))
                {
                    await ValidateSlotAsync(slotId);
                }
                else
                {
                    // Invalid slot directory name - could be cleaned up
                    // For now, just log or ignore
                }
            }
        }

        /// <summary>
        /// Gets the maximum slot ID currently in use
        /// </summary>
        /// <returns>The highest slot ID in use, or 0 if no slots exist</returns>
        public async Task<int> GetMaxSlotIdAsync()
        {
            var allSlots = await GetAllSlotInfoAsync();
            return allSlots.Keys.DefaultIfEmpty(0).Max();
        }

        /// <summary>
        /// Gets the next available slot ID
        /// </summary>
        /// <returns>The next available slot ID</returns>
        public async Task<int> GetNextAvailableSlotIdAsync()
        {
            var maxSlotId = await GetMaxSlotIdAsync();
            
            // Check for gaps in slot IDs
            for (int i = 1; i <= maxSlotId; i++)
            {
                if (!SlotExists(i))
                {
                    return i;
                }
            }
            
            // No gaps found, return next sequential ID
            return maxSlotId + 1;
        }

        /// <summary>
        /// Gets the directory path for the specified slot
        /// </summary>
        /// <param name="slotId">The slot ID</param>
        /// <returns>Full path to the slot directory</returns>
        private string GetSlotDirectory(int slotId)
        {
            return Path.Combine(_saveDirectory, $"slot_{slotId}");
        }

        /// <summary>
        /// Gets the metadata file path for the specified slot
        /// </summary>
        /// <param name="slotId">The slot ID</param>
        /// <returns>Full path to the metadata file</returns>
        private string GetSlotMetadataPath(int slotId)
        {
            return Path.Combine(GetSlotDirectory(slotId), "metadata.json");
        }

        /// <summary>
        /// Gets the save file path for the specified slot
        /// </summary>
        /// <param name="slotId">The slot ID</param>
        /// <returns>Full path to the save file</returns>
        private string GetSlotSaveFilePath(int slotId)
        {
            return Path.Combine(GetSlotDirectory(slotId), "save.json");
        }

        /// <summary>
        /// Saves slot metadata to file
        /// </summary>
        /// <param name="slotId">The slot ID</param>
        /// <param name="metadata">The metadata to save</param>
        /// <returns>Task representing the async operation</returns>
        private async Task SaveSlotMetadataAsync(int slotId, SaveSlotMetadata metadata)
        {
            string metadataPath = GetSlotMetadataPath(slotId);
            string tempPath = metadataPath + ".tmp";

            // Ensure directory exists
            EnsureDirectoryExists(Path.GetDirectoryName(metadataPath));

            // Write to temporary file first
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, metadata, _jsonOptions);
            }

            // Atomic move to final location
            if (File.Exists(metadataPath))
            {
                File.Replace(tempPath, metadataPath, null);
            }
            else
            {
                File.Move(tempPath, metadataPath);
            }
        }

        /// <summary>
        /// Validates a specific save slot
        /// </summary>
        /// <param name="slotId">The slot ID to validate</param>
        /// <returns>Task representing the async validation</returns>
        private async Task ValidateSlotAsync(int slotId)
        {
            string slotDirectory = GetSlotDirectory(slotId);
            string metadataPath = GetSlotMetadataPath(slotId);
            string saveFilePath = GetSlotSaveFilePath(slotId);

            // Check if metadata file exists and is valid
            if (!File.Exists(metadataPath))
            {
                // Try to recreate metadata from save file if it exists
                if (File.Exists(saveFilePath))
                {
                    var fileInfo = new FileInfo(saveFilePath);
                    var metadata = new SaveSlotMetadata
                    {
                        SlotId = slotId,
                        LastSaveTime = fileInfo.LastWriteTime,
                        PlayTime = TimeSpan.Zero,
                        CurrentDay = 1,
                        CurrentZoneName = "Unknown",
                        CurrentBiome = BiomeType.Plains,
                        ZonesVisited = 0,
                        JournalEntries = 0,
                        GameVersion = "Unknown",
                        FileSizeBytes = fileInfo.Length
                    };

                    await SaveSlotMetadataAsync(slotId, metadata);
                    _slotMetadataCache[slotId] = metadata;
                }
            }
            else
            {
                // Validate existing metadata
                try
                {
                    var metadata = await GetSlotInfoAsync(slotId);
                    if (metadata != null)
                    {
                        // Update file size if save file exists
                        if (File.Exists(saveFilePath))
                        {
                            var fileInfo = new FileInfo(saveFilePath);
                            if (metadata.FileSizeBytes != fileInfo.Length)
                            {
                                metadata.FileSizeBytes = fileInfo.Length;
                                await SaveSlotMetadataAsync(slotId, metadata);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Metadata file is corrupted, try to recreate
                    if (File.Exists(saveFilePath))
                    {
                        var fileInfo = new FileInfo(saveFilePath);
                        var metadata = new SaveSlotMetadata
                        {
                            SlotId = slotId,
                            LastSaveTime = fileInfo.LastWriteTime,
                            PlayTime = TimeSpan.Zero,
                            CurrentDay = 1,
                            CurrentZoneName = "Unknown",
                            CurrentBiome = BiomeType.Plains,
                            ZonesVisited = 0,
                            JournalEntries = 0,
                            GameVersion = "Unknown",
                            FileSizeBytes = fileInfo.Length
                        };

                        await SaveSlotMetadataAsync(slotId, metadata);
                        _slotMetadataCache[slotId] = metadata;
                    }
                }
            }
        }

        /// <summary>
        /// Loads all existing slot metadata into cache
        /// </summary>
        private void LoadAllSlotMetadata()
        {
            if (!Directory.Exists(_saveDirectory))
                return;

            var slotDirectories = Directory.GetDirectories(_saveDirectory, "slot_*");
            
            foreach (var slotDir in slotDirectories)
            {
                var dirName = Path.GetFileName(slotDir);
                if (dirName.StartsWith("slot_") && int.TryParse(dirName.Substring(5), out int slotId))
                {
                    // Load metadata asynchronously in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await GetSlotInfoAsync(slotId);
                        }
                        catch (Exception)
                        {
                            // Ignore errors during initial load
                        }
                    });
                }
            }
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
        /// Raises the SlotCreated event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnSlotCreated(SlotCreatedEventArgs args)
        {
            SlotCreated?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the SlotDeleted event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnSlotDeleted(SlotDeletedEventArgs args)
        {
            SlotDeleted?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the SlotMetadataUpdated event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnSlotMetadataUpdated(SlotMetadataUpdatedEventArgs args)
        {
            SlotMetadataUpdated?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Event arguments for slot creation events
    /// </summary>
    public class SlotCreatedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public SaveSlotMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Event arguments for slot deletion events
    /// </summary>
    public class SlotDeletedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public SaveSlotMetadata DeletedMetadata { get; set; }
    }

    /// <summary>
    /// Event arguments for slot metadata update events
    /// </summary>
    public class SlotMetadataUpdatedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public SaveSlotMetadata Metadata { get; set; }
    }
}