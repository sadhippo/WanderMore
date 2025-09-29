using System;
using System.IO;
using System.Threading.Tasks;
using System.Security;
using System.Text.Json;
using System.Threading;

namespace HiddenHorizons
{
    /// <summary>
    /// Manages error handling and recovery for save/load operations
    /// </summary>
    public class SaveErrorManager
    {
        private const long MinimumDiskSpaceBytes = 50 * 1024 * 1024; // 50 MB minimum
        private const int MaxRetryAttempts = 3;
        private const int BaseDelayMs = 1000; // 1 second base delay

        /// <summary>
        /// Event raised when a save/load error occurs
        /// </summary>
        public event EventHandler<SaveErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Checks if there is sufficient disk space for save operations
        /// </summary>
        /// <param name="saveDirectory">The directory where saves will be written</param>
        /// <param name="estimatedSizeBytes">Estimated size of the save data in bytes</param>
        /// <returns>True if there is sufficient space, false otherwise</returns>
        public bool CheckDiskSpace(string saveDirectory, long estimatedSizeBytes = 0)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(saveDirectory);
                var drive = new DriveInfo(directoryInfo.Root.FullName);
                
                if (!drive.IsReady)
                {
                    RaiseError(SaveErrorType.UnknownError, "Drive is not ready", null, false, false);
                    return false;
                }

                long requiredSpace = Math.Max(MinimumDiskSpaceBytes, estimatedSizeBytes * 2); // 2x for safety
                
                if (drive.AvailableFreeSpace < requiredSpace)
                {
                    string message = $"Insufficient disk space. Required: {FormatBytes(requiredSpace)}, Available: {FormatBytes(drive.AvailableFreeSpace)}";
                    RaiseError(SaveErrorType.DiskSpaceInsufficient, message, null, false, false);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseError(SaveErrorType.UnknownError, "Failed to check disk space", ex, false, false);
                return false;
            }
        }

        /// <summary>
        /// Checks if the application has permission to access the save directory
        /// </summary>
        /// <param name="saveDirectory">The directory to check permissions for</param>
        /// <returns>True if permissions are sufficient, false otherwise</returns>
        public bool CheckPermissions(string saveDirectory)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                // Test write permission by creating a temporary file
                string testFile = Path.Combine(saveDirectory, $"test_{Guid.NewGuid()}.tmp");
                
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    string message = $"Permission denied. Cannot write to save directory: {saveDirectory}. " +
                                   "Please check that the application has write permissions to this location.";
                    RaiseError(SaveErrorType.PermissionDenied, message, null, false, false);
                    return false;
                }
                catch (SecurityException ex)
                {
                    string message = $"Security restriction prevents access to save directory: {saveDirectory}";
                    RaiseError(SaveErrorType.PermissionDenied, message, ex, false, false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                RaiseError(SaveErrorType.UnknownError, "Failed to check permissions", ex, false, false);
                return false;
            }
        }

        /// <summary>
        /// Executes an operation with retry logic and exponential backoff
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="context">Context information for error reporting</param>
        /// <param name="slotId">Optional save slot ID</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string context, int? slotId = null)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    // Determine if this is a retryable error
                    bool canRetry = IsRetryableError(ex) && attempt < MaxRetryAttempts - 1;
                    
                    if (!canRetry)
                    {
                        // For non-retryable errors, throw immediately without wrapping
                        if (!IsRetryableError(ex))
                        {
                            SaveErrorType immediateErrorType = DetermineErrorType(ex);
                            RaiseError(immediateErrorType, ex.Message, ex, false, false, slotId, context);
                            throw;
                        }
                        break;
                    }

                    // Calculate delay with exponential backoff
                    int delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay);
                }
            }

            // All retries failed, raise error
            SaveErrorType finalErrorType = DetermineErrorType(lastException);
            string message = $"Operation failed after {MaxRetryAttempts} attempts. Context: {context}";
            RaiseError(finalErrorType, message, lastException, false, false, slotId, context);
            
            throw new SaveLoadException(message, lastException);
        }

        /// <summary>
        /// Executes an operation with retry logic (synchronous version)
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="context">Context information for error reporting</param>
        /// <param name="slotId">Optional save slot ID</param>
        /// <returns>The result of the operation</returns>
        public T ExecuteWithRetry<T>(Func<T> operation, string context, int? slotId = null)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    // Determine if this is a retryable error
                    bool canRetry = IsRetryableError(ex) && attempt < MaxRetryAttempts - 1;
                    
                    if (!canRetry)
                    {
                        // For non-retryable errors, throw immediately without wrapping
                        if (!IsRetryableError(ex))
                        {
                            SaveErrorType immediateErrorType2 = DetermineErrorType(ex);
                            RaiseError(immediateErrorType2, ex.Message, ex, false, false, slotId, context);
                            throw;
                        }
                        break;
                    }

                    // Calculate delay with exponential backoff
                    int delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                    Thread.Sleep(delay);
                }
            }

            // All retries failed, raise error
            SaveErrorType finalErrorType2 = DetermineErrorType(lastException);
            string message = $"Operation failed after {MaxRetryAttempts} attempts. Context: {context}";
            RaiseError(finalErrorType2, message, lastException, false, false, slotId, context);
            
            throw new SaveLoadException(message, lastException);
        }

        /// <summary>
        /// Handles graceful degradation for partial save/load failures
        /// </summary>
        /// <param name="partialData">The partially loaded/saved data</param>
        /// <param name="failedSystems">Names of systems that failed</param>
        /// <param name="context">Context information</param>
        /// <param name="slotId">Optional save slot ID</param>
        public void HandlePartialFailure(object partialData, string[] failedSystems, string context, int? slotId = null)
        {
            string systemsList = string.Join(", ", failedSystems);
            string message = $"Partial {context} failure. Failed systems: {systemsList}. " +
                           "Some data may be missing or incomplete.";
            
            RaiseError(SaveErrorType.SerializationFailed, message, null, true, true, slotId, context);
        }

        /// <summary>
        /// Validates save data integrity and handles corruption
        /// </summary>
        /// <param name="saveData">The save data to validate</param>
        /// <param name="expectedChecksum">Expected checksum for validation</param>
        /// <param name="slotId">Save slot ID</param>
        /// <returns>True if data is valid, false if corrupted</returns>
        public bool ValidateSaveData(string saveData, string expectedChecksum, int slotId)
        {
            try
            {
                // Basic JSON validation
                JsonDocument.Parse(saveData);
                
                // Checksum validation (if IntegrityManager is available)
                // This would typically be handled by IntegrityManager, but we include basic validation here
                if (!string.IsNullOrEmpty(expectedChecksum))
                {
                    // For now, we'll assume checksum validation is handled elsewhere
                    // In a full implementation, we'd calculate and compare checksums here
                }
                
                return true;
            }
            catch (JsonException ex)
            {
                string message = $"Save data is corrupted or invalid JSON. Slot: {slotId}";
                RaiseError(SaveErrorType.FileCorrupted, message, ex, false, true, slotId);
                return false;
            }
            catch (Exception ex)
            {
                string message = $"Failed to validate save data. Slot: {slotId}";
                RaiseError(SaveErrorType.UnknownError, message, ex, false, true, slotId);
                return false;
            }
        }

        private bool IsRetryableError(Exception ex)
        {
            // FileNotFoundException should not be retried - the file either exists or it doesn't
            if (ex is FileNotFoundException)
                return false;
                
            return ex is IOException ||
                   ex is UnauthorizedAccessException ||
                   ex is TimeoutException ||
                   (ex is JsonException && ex.Message.Contains("timeout"));
        }

        private SaveErrorType DetermineErrorType(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => SaveErrorType.PermissionDenied,
                SecurityException => SaveErrorType.PermissionDenied,
                DirectoryNotFoundException => SaveErrorType.PermissionDenied,
                JsonException => SaveErrorType.SerializationFailed,
                InvalidDataException => SaveErrorType.FileCorrupted,
                IOException when ex.Message.Contains("space") => SaveErrorType.DiskSpaceInsufficient,
                IOException => SaveErrorType.UnknownError,
                _ => SaveErrorType.UnknownError
            };
        }

        private void RaiseError(SaveErrorType errorType, string message, Exception exception, 
                               bool canRetry, bool hasBackup, int? slotId = null, string context = null)
        {
            var args = new SaveErrorEventArgs(errorType, message, exception)
            {
                CanRetry = canRetry,
                HasBackup = hasBackup,
                SlotId = slotId,
                Context = context
            };

            ErrorOccurred?.Invoke(this, args);
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

    /// <summary>
    /// Custom exception for save/load operations
    /// </summary>
    public class SaveLoadException : Exception
    {
        public SaveLoadException(string message) : base(message) { }
        public SaveLoadException(string message, Exception innerException) : base(message, innerException) { }
    }
}