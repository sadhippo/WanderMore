using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Manages save format versioning and migration between different save formats
    /// </summary>
    public class VersionManager
    {
        private readonly Dictionary<int, IMigrationScript> _migrationScripts;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Current save format version supported by this game version
        /// </summary>
        public const int CurrentSaveVersion = 1;

        /// <summary>
        /// Minimum save format version that can be loaded without migration
        /// </summary>
        public const int MinimumCompatibleVersion = 0;

        /// <summary>
        /// Event fired when a migration operation starts
        /// </summary>
        public event EventHandler<MigrationStartedEventArgs> MigrationStarted;

        /// <summary>
        /// Event fired when a migration operation completes successfully
        /// </summary>
        public event EventHandler<MigrationCompletedEventArgs> MigrationCompleted;

        /// <summary>
        /// Event fired when a migration operation fails
        /// </summary>
        public event EventHandler<MigrationFailedEventArgs> MigrationFailed;

        /// <summary>
        /// Initializes a new instance of the VersionManager
        /// </summary>
        public VersionManager()
        {
            _migrationScripts = new Dictionary<int, IMigrationScript>();
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Register built-in migration scripts
            RegisterMigrationScripts();
        }

        /// <summary>
        /// Detects the version of a save file
        /// </summary>
        /// <param name="saveFilePath">Path to the save file</param>
        /// <returns>The detected version, or -1 if version cannot be determined</returns>
        public async Task<int> DetectVersionAsync(string saveFilePath)
        {
            if (!File.Exists(saveFilePath))
                throw new FileNotFoundException($"Save file not found: {saveFilePath}");

            try
            {
                // First try to read as plain JSON
                string jsonContent = await TryReadAsJsonAsync(saveFilePath);
                
                using var document = JsonDocument.Parse(jsonContent);
                
                // Try both camelCase and PascalCase property names
                if (document.RootElement.TryGetProperty("version", out var versionElement) ||
                    document.RootElement.TryGetProperty("Version", out versionElement))
                {
                    if (versionElement.TryGetInt32(out int version))
                    {
                        return version;
                    }
                }

                // If no version property found, assume version 1 (initial format)
                return 1;
            }
            catch (JsonException)
            {
                // If JSON parsing fails, version cannot be determined
                return -1;
            }
            catch (Exception)
            {
                // For other exceptions, version cannot be determined
                return -1;
            }
        }

        /// <summary>
        /// Attempts to read a save file as JSON, handling both compressed and uncompressed formats
        /// </summary>
        /// <param name="saveFilePath">Path to the save file</param>
        /// <returns>JSON content as string</returns>
        private async Task<string> TryReadAsJsonAsync(string saveFilePath)
        {
            byte[] fileData = await File.ReadAllBytesAsync(saveFilePath);
            
            // Check if the file starts with GZIP magic number (0x1F, 0x8B)
            if (fileData.Length >= 2 && fileData[0] == 0x1F && fileData[1] == 0x8B)
            {
                // File is compressed, decompress it
                return await DecompressDataAsync(fileData);
            }
            else
            {
                // File is uncompressed, read as UTF-8
                return Encoding.UTF8.GetString(fileData);
            }
        }

        /// <summary>
        /// Decompresses GZIP compressed data
        /// </summary>
        /// <param name="compressedData">Compressed byte array</param>
        /// <returns>Decompressed JSON string</returns>
        private async Task<string> DecompressDataAsync(byte[] compressedData)
        {
            using var compressedStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();

            await gzipStream.CopyToAsync(decompressedStream);
            return Encoding.UTF8.GetString(decompressedStream.ToArray());
        }

        /// <summary>
        /// Checks if a save version is compatible with the current game version
        /// </summary>
        /// <param name="saveVersion">The save format version to check</param>
        /// <returns>True if compatible, false otherwise</returns>
        public bool IsCompatible(int saveVersion)
        {
            // Version -1 means version could not be determined
            if (saveVersion == -1)
                return false;

            // Check if version is within supported range
            return saveVersion >= MinimumCompatibleVersion && saveVersion <= CurrentSaveVersion;
        }

        /// <summary>
        /// Determines if migration is needed for a save version
        /// </summary>
        /// <param name="saveVersion">The save format version to check</param>
        /// <returns>True if migration is needed, false otherwise</returns>
        public bool NeedsMigration(int saveVersion)
        {
            return saveVersion != -1 && saveVersion < CurrentSaveVersion && IsCompatible(saveVersion);
        }

        /// <summary>
        /// Migrates save data from an older version to the current version
        /// </summary>
        /// <param name="saveFilePath">Path to the save file to migrate</param>
        /// <returns>Task representing the async migration operation</returns>
        public async Task<bool> MigrateDataAsync(string saveFilePath)
        {
            if (!File.Exists(saveFilePath))
                throw new FileNotFoundException($"Save file not found: {saveFilePath}");

            int currentVersion = await DetectVersionAsync(saveFilePath);
            
            if (currentVersion == -1)
            {
                OnMigrationFailed(new MigrationFailedEventArgs
                {
                    SaveFilePath = saveFilePath,
                    FromVersion = currentVersion,
                    ToVersion = CurrentSaveVersion,
                    ErrorMessage = "Cannot determine save file version",
                    Exception = new InvalidOperationException("Save file version could not be detected")
                });
                return false;
            }

            if (!IsCompatible(currentVersion))
            {
                OnMigrationFailed(new MigrationFailedEventArgs
                {
                    SaveFilePath = saveFilePath,
                    FromVersion = currentVersion,
                    ToVersion = CurrentSaveVersion,
                    ErrorMessage = $"Save version {currentVersion} is not compatible with current game version",
                    Exception = new NotSupportedException($"Save version {currentVersion} is not supported")
                });
                return false;
            }

            if (!NeedsMigration(currentVersion))
            {
                // No migration needed
                return true;
            }

            // Create backup before migration
            string backupPath = saveFilePath + $".backup.v{currentVersion}";
            try
            {
                File.Copy(saveFilePath, backupPath, true);
            }
            catch (Exception ex)
            {
                OnMigrationFailed(new MigrationFailedEventArgs
                {
                    SaveFilePath = saveFilePath,
                    FromVersion = currentVersion,
                    ToVersion = CurrentSaveVersion,
                    ErrorMessage = "Failed to create backup before migration",
                    Exception = ex
                });
                return false;
            }

            OnMigrationStarted(new MigrationStartedEventArgs
            {
                SaveFilePath = saveFilePath,
                FromVersion = currentVersion,
                ToVersion = CurrentSaveVersion,
                BackupPath = backupPath
            });

            try
            {
                // Perform step-by-step migration
                int workingVersion = currentVersion;
                string workingFilePath = saveFilePath;

                while (workingVersion < CurrentSaveVersion)
                {
                    int nextVersion = workingVersion + 1;
                    
                    if (!_migrationScripts.TryGetValue(nextVersion, out var migrationScript))
                    {
                        throw new NotSupportedException($"No migration script found for version {workingVersion} to {nextVersion}");
                    }

                    // Load current data
                    GameSaveData saveData;
                    await using (var fileStream = new FileStream(workingFilePath, FileMode.Open, FileAccess.Read))
                    {
                        saveData = await JsonSerializer.DeserializeAsync<GameSaveData>(fileStream, _jsonOptions);
                    }

                    // Apply migration
                    saveData = await migrationScript.MigrateAsync(saveData);
                    saveData.Version = nextVersion;

                    // Write migrated data
                    string tempPath = workingFilePath + ".migrating";
                    await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        await JsonSerializer.SerializeAsync(fileStream, saveData, _jsonOptions);
                    }

                    // Atomic replace
                    File.Replace(tempPath, workingFilePath, null);

                    workingVersion = nextVersion;
                }

                OnMigrationCompleted(new MigrationCompletedEventArgs
                {
                    SaveFilePath = saveFilePath,
                    FromVersion = currentVersion,
                    ToVersion = CurrentSaveVersion,
                    BackupPath = backupPath
                });

                return true;
            }
            catch (Exception ex)
            {
                // Attempt rollback
                try
                {
                    await RollbackMigrationAsync(saveFilePath, backupPath);
                }
                catch (Exception rollbackEx)
                {
                    // Log rollback failure but don't mask original exception
                    OnMigrationFailed(new MigrationFailedEventArgs
                    {
                        SaveFilePath = saveFilePath,
                        FromVersion = currentVersion,
                        ToVersion = CurrentSaveVersion,
                        ErrorMessage = $"Migration failed and rollback also failed: {ex.Message}. Rollback error: {rollbackEx.Message}",
                        Exception = ex
                    });
                    return false;
                }

                OnMigrationFailed(new MigrationFailedEventArgs
                {
                    SaveFilePath = saveFilePath,
                    FromVersion = currentVersion,
                    ToVersion = CurrentSaveVersion,
                    ErrorMessage = $"Migration failed: {ex.Message}. Save file has been restored from backup.",
                    Exception = ex
                });
                return false;
            }
        }

        /// <summary>
        /// Rolls back a failed migration by restoring from backup
        /// </summary>
        /// <param name="saveFilePath">Path to the save file</param>
        /// <param name="backupPath">Path to the backup file</param>
        /// <returns>Task representing the async rollback operation</returns>
        public async Task RollbackMigrationAsync(string saveFilePath, string backupPath)
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"Backup file not found: {backupPath}");

            // Verify backup integrity before rollback
            int backupVersion = await DetectVersionAsync(backupPath);
            if (backupVersion == -1)
                throw new InvalidOperationException("Backup file is corrupted or invalid");

            // Restore from backup
            File.Copy(backupPath, saveFilePath, true);
        }

        /// <summary>
        /// Registers migration scripts for version transitions
        /// </summary>
        private void RegisterMigrationScripts()
        {
            // Register migration from version 0 to version 1 (for testing purposes)
            _migrationScripts[1] = new MigrationV0ToV1();
            
            // Future migration scripts will be registered here
            // Example: _migrationScripts[2] = new MigrationV1ToV2();
        }

        /// <summary>
        /// Registers a custom migration script
        /// </summary>
        /// <param name="targetVersion">The version this script migrates to</param>
        /// <param name="migrationScript">The migration script implementation</param>
        public void RegisterMigrationScript(int targetVersion, IMigrationScript migrationScript)
        {
            if (migrationScript == null)
                throw new ArgumentNullException(nameof(migrationScript));

            _migrationScripts[targetVersion] = migrationScript;
        }

        /// <summary>
        /// Raises the MigrationStarted event
        /// </summary>
        protected virtual void OnMigrationStarted(MigrationStartedEventArgs args)
        {
            MigrationStarted?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the MigrationCompleted event
        /// </summary>
        protected virtual void OnMigrationCompleted(MigrationCompletedEventArgs args)
        {
            MigrationCompleted?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the MigrationFailed event
        /// </summary>
        protected virtual void OnMigrationFailed(MigrationFailedEventArgs args)
        {
            MigrationFailed?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Interface for migration scripts that handle version transitions
    /// </summary>
    public interface IMigrationScript
    {
        /// <summary>
        /// Migrates save data from the previous version to the target version
        /// </summary>
        /// <param name="saveData">The save data to migrate</param>
        /// <returns>The migrated save data</returns>
        Task<GameSaveData> MigrateAsync(GameSaveData saveData);
    }

    /// <summary>
    /// Event arguments for migration started events
    /// </summary>
    public class MigrationStartedEventArgs : EventArgs
    {
        public string SaveFilePath { get; set; }
        public int FromVersion { get; set; }
        public int ToVersion { get; set; }
        public string BackupPath { get; set; }
    }

    /// <summary>
    /// Event arguments for migration completed events
    /// </summary>
    public class MigrationCompletedEventArgs : EventArgs
    {
        public string SaveFilePath { get; set; }
        public int FromVersion { get; set; }
        public int ToVersion { get; set; }
        public string BackupPath { get; set; }
    }

    /// <summary>
    /// Event arguments for migration failed events
    /// </summary>
    public class MigrationFailedEventArgs : EventArgs
    {
        public string SaveFilePath { get; set; }
        public int FromVersion { get; set; }
        public int ToVersion { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
    }
}