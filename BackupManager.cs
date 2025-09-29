using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Manages automatic backup creation, restoration, and cleanup for save files
    /// </summary>
    public class BackupManager
    {
        private readonly string _saveDirectory;
        private readonly int _maxBackupsToKeep;

        /// <summary>
        /// Event fired when a backup operation completes successfully
        /// </summary>
        public event EventHandler<BackupCompletedEventArgs> BackupCompleted;

        /// <summary>
        /// Event fired when a backup restoration completes successfully
        /// </summary>
        public event EventHandler<BackupRestoredEventArgs> BackupRestored;

        /// <summary>
        /// Event fired when a backup operation encounters an error
        /// </summary>
        public event EventHandler<BackupErrorEventArgs> BackupError;

        /// <summary>
        /// Initializes a new instance of the BackupManager
        /// </summary>
        /// <param name="saveDirectory">Directory where save files are stored</param>
        /// <param name="maxBackupsToKeep">Maximum number of backups to retain per save slot</param>
        public BackupManager(string saveDirectory = "Saves", int maxBackupsToKeep = 5)
        {
            _saveDirectory = saveDirectory ?? throw new ArgumentNullException(nameof(saveDirectory));
            
            if (maxBackupsToKeep < 1)
                throw new ArgumentException("Must keep at least 1 backup", nameof(maxBackupsToKeep));
                
            _maxBackupsToKeep = maxBackupsToKeep;
        }

        /// <summary>
        /// Creates a timestamped backup of the save file for the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>Task representing the async backup operation, returns backup file path if successful</returns>
        public async Task<string> CreateBackupAsync(int slotId)
        {
            try
            {
                if (slotId < 1)
                    throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

                string saveFilePath = GetSaveFilePath(slotId);
                
                if (!File.Exists(saveFilePath))
                {
                    // No save file exists, nothing to backup
                    return null;
                }

                string backupDirectory = GetBackupDirectory(slotId);
                EnsureDirectoryExists(backupDirectory);

                // Create timestamped backup filename
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                string backupFileName = $"save_backup_{timestamp}.json";
                string backupFilePath = Path.Combine(backupDirectory, backupFileName);

                // Copy save file to backup location
                await CopyFileAsync(saveFilePath, backupFilePath);

                // Cleanup old backups
                await CleanupOldBackupsAsync(slotId);

                // Fire success event
                OnBackupCompleted(new BackupCompletedEventArgs
                {
                    SlotId = slotId,
                    BackupFilePath = backupFilePath,
                    OriginalFilePath = saveFilePath,
                    BackupTimestamp = DateTime.UtcNow
                });

                return backupFilePath;
            }
            catch (Exception ex)
            {
                OnBackupError(new BackupErrorEventArgs
                {
                    SlotId = slotId,
                    ErrorType = GetBackupErrorType(ex),
                    ErrorMessage = $"Failed to create backup for slot {slotId}: {ex.Message}",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Restores the most recent backup for the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>Task representing the async restore operation, returns true if successful</returns>
        public async Task<bool> RestoreFromBackupAsync(int slotId)
        {
            try
            {
                if (slotId < 1)
                    throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

                string backupDirectory = GetBackupDirectory(slotId);
                
                if (!Directory.Exists(backupDirectory))
                {
                    return false; // No backups exist
                }

                // Find the most recent backup
                var backupFiles = Directory.GetFiles(backupDirectory, "save_backup_*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                if (!backupFiles.Any())
                {
                    return false; // No backup files found
                }

                var mostRecentBackup = backupFiles.First();
                string saveFilePath = GetSaveFilePath(slotId);
                
                // Ensure save directory exists
                EnsureDirectoryExists(Path.GetDirectoryName(saveFilePath));

                // Copy backup to save location
                await CopyFileAsync(mostRecentBackup.FullName, saveFilePath);

                // Fire success event
                OnBackupRestored(new BackupRestoredEventArgs
                {
                    SlotId = slotId,
                    BackupFilePath = mostRecentBackup.FullName,
                    RestoredFilePath = saveFilePath,
                    BackupTimestamp = mostRecentBackup.CreationTimeUtc,
                    RestoreTimestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                OnBackupError(new BackupErrorEventArgs
                {
                    SlotId = slotId,
                    ErrorType = GetBackupErrorType(ex),
                    ErrorMessage = $"Failed to restore backup for slot {slotId}: {ex.Message}",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Restores from a specific backup file
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <param name="backupFilePath">Path to the specific backup file to restore</param>
        /// <returns>Task representing the async restore operation, returns true if successful</returns>
        public async Task<bool> RestoreFromSpecificBackupAsync(int slotId, string backupFilePath)
        {
            try
            {
                if (slotId < 1)
                    throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

                if (string.IsNullOrEmpty(backupFilePath))
                    throw new ArgumentException("Backup file path cannot be null or empty", nameof(backupFilePath));

                if (!File.Exists(backupFilePath))
                    throw new FileNotFoundException($"Backup file not found: {backupFilePath}");

                string saveFilePath = GetSaveFilePath(slotId);
                
                // Ensure save directory exists
                EnsureDirectoryExists(Path.GetDirectoryName(saveFilePath));

                // Copy backup to save location
                await CopyFileAsync(backupFilePath, saveFilePath);

                // Fire success event
                OnBackupRestored(new BackupRestoredEventArgs
                {
                    SlotId = slotId,
                    BackupFilePath = backupFilePath,
                    RestoredFilePath = saveFilePath,
                    BackupTimestamp = File.GetCreationTimeUtc(backupFilePath),
                    RestoreTimestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                OnBackupError(new BackupErrorEventArgs
                {
                    SlotId = slotId,
                    ErrorType = GetBackupErrorType(ex),
                    ErrorMessage = $"Failed to restore from specific backup for slot {slotId}: {ex.Message}",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Cleans up old backup files, keeping only the most recent ones based on retention policy
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>Task representing the async cleanup operation</returns>
        public async Task CleanupOldBackupsAsync(int slotId)
        {
            try
            {
                if (slotId < 1)
                    throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

                string backupDirectory = GetBackupDirectory(slotId);
                
                if (!Directory.Exists(backupDirectory))
                {
                    return; // No backup directory exists
                }

                // Get all backup files sorted by creation time (newest first)
                var backupFiles = Directory.GetFiles(backupDirectory, "save_backup_*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                // Delete files beyond the retention limit
                var filesToDelete = backupFiles.Skip(_maxBackupsToKeep).ToList();
                
                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        await Task.Run(() => fileToDelete.Delete());
                    }
                    catch (Exception ex)
                    {
                        // Log individual file deletion errors but continue cleanup
                        OnBackupError(new BackupErrorEventArgs
                        {
                            SlotId = slotId,
                            ErrorType = BackupErrorType.CleanupFailed,
                            ErrorMessage = $"Failed to delete backup file {fileToDelete.Name}: {ex.Message}",
                            Exception = ex
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                OnBackupError(new BackupErrorEventArgs
                {
                    SlotId = slotId,
                    ErrorType = BackupErrorType.CleanupFailed,
                    ErrorMessage = $"Failed to cleanup old backups for slot {slotId}: {ex.Message}",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Gets information about available backups for the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>List of backup information objects</returns>
        public List<BackupInfo> GetBackupInfo(int slotId)
        {
            if (slotId < 1)
                throw new ArgumentException("Slot ID must be 1 or greater", nameof(slotId));

            try
            {
                string backupDirectory = GetBackupDirectory(slotId);
                
                if (!Directory.Exists(backupDirectory))
                {
                    return new List<BackupInfo>();
                }

                return Directory.GetFiles(backupDirectory, "save_backup_*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Select(f => new BackupInfo
                    {
                        FilePath = f.FullName,
                        FileName = f.Name,
                        CreationTime = f.CreationTimeUtc,
                        SizeBytes = f.Length
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                OnBackupError(new BackupErrorEventArgs
                {
                    SlotId = slotId,
                    ErrorType = BackupErrorType.UnknownError,
                    ErrorMessage = $"Failed to get backup info for slot {slotId}: {ex.Message}",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Checks if any backups exist for the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>True if backups exist, false otherwise</returns>
        public bool HasBackups(int slotId)
        {
            if (slotId < 1)
                return false;

            string backupDirectory = GetBackupDirectory(slotId);
            
            if (!Directory.Exists(backupDirectory))
                return false;

            return Directory.GetFiles(backupDirectory, "save_backup_*.json").Length > 0;
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
        /// Gets the backup directory path for the specified slot
        /// </summary>
        /// <param name="slotId">The save slot identifier</param>
        /// <returns>Full path to the backup directory</returns>
        private string GetBackupDirectory(int slotId)
        {
            return Path.Combine(_saveDirectory, $"slot_{slotId}", "backups");
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
        /// Asynchronously copies a file from source to destination
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="destinationFilePath">Destination file path</param>
        /// <returns>Task representing the async copy operation</returns>
        private async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
        {
            await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
            await using var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(destinationStream);
        }

        /// <summary>
        /// Determines the appropriate backup error type based on the exception
        /// </summary>
        /// <param name="exception">The exception to categorize</param>
        /// <returns>Appropriate BackupErrorType</returns>
        private BackupErrorType GetBackupErrorType(Exception exception)
        {
            return exception switch
            {
                FileNotFoundException => BackupErrorType.FileNotFound,
                DirectoryNotFoundException => BackupErrorType.PermissionDenied,
                UnauthorizedAccessException => BackupErrorType.PermissionDenied,
                DriveNotFoundException => BackupErrorType.DiskSpaceInsufficient,
                IOException => BackupErrorType.DiskSpaceInsufficient,
                _ => BackupErrorType.UnknownError
            };
        }

        /// <summary>
        /// Raises the BackupCompleted event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnBackupCompleted(BackupCompletedEventArgs args)
        {
            BackupCompleted?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the BackupRestored event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnBackupRestored(BackupRestoredEventArgs args)
        {
            BackupRestored?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the BackupError event
        /// </summary>
        /// <param name="args">Event arguments</param>
        protected virtual void OnBackupError(BackupErrorEventArgs args)
        {
            BackupError?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Information about a backup file
    /// </summary>
    public class BackupInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime CreationTime { get; set; }
        public long SizeBytes { get; set; }
    }

    /// <summary>
    /// Types of backup errors that can occur
    /// </summary>
    public enum BackupErrorType
    {
        DiskSpaceInsufficient,
        PermissionDenied,
        FileNotFound,
        CleanupFailed,
        UnknownError
    }

    /// <summary>
    /// Event arguments for successful backup operations
    /// </summary>
    public class BackupCompletedEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public string BackupFilePath { get; set; }
        public string OriginalFilePath { get; set; }
        public DateTime BackupTimestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for successful backup restoration operations
    /// </summary>
    public class BackupRestoredEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public string BackupFilePath { get; set; }
        public string RestoredFilePath { get; set; }
        public DateTime BackupTimestamp { get; set; }
        public DateTime RestoreTimestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for backup error events
    /// </summary>
    public class BackupErrorEventArgs : EventArgs
    {
        public int SlotId { get; set; }
        public BackupErrorType ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
    }
}