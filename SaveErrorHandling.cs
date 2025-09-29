using System;

namespace HiddenHorizons
{
    /// <summary>
    /// Defines the types of errors that can occur during save/load operations
    /// </summary>
    public enum SaveErrorType
    {
        /// <summary>
        /// Insufficient disk space to complete the save operation
        /// </summary>
        DiskSpaceInsufficient,
        
        /// <summary>
        /// Permission denied when accessing save files or directories
        /// </summary>
        PermissionDenied,
        
        /// <summary>
        /// Save file is corrupted or invalid
        /// </summary>
        FileCorrupted,
        
        /// <summary>
        /// Save file version is incompatible with current game version
        /// </summary>
        VersionIncompatible,
        
        /// <summary>
        /// Failed to serialize or deserialize save data
        /// </summary>
        SerializationFailed,
        
        /// <summary>
        /// Network unavailable (reserved for future cloud save functionality)
        /// </summary>
        NetworkUnavailable,
        
        /// <summary>
        /// An unknown or unexpected error occurred
        /// </summary>
        UnknownError
    }

    /// <summary>
    /// Event arguments for save/load error events
    /// </summary>
    public class SaveErrorEventArgs : EventArgs
    {
        /// <summary>
        /// The type of error that occurred
        /// </summary>
        public SaveErrorType ErrorType { get; set; }
        
        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// The underlying exception that caused the error, if any
        /// </summary>
        public Exception Exception { get; set; }
        
        /// <summary>
        /// Whether the operation can be retried
        /// </summary>
        public bool CanRetry { get; set; }
        
        /// <summary>
        /// Whether a backup is available for recovery
        /// </summary>
        public bool HasBackup { get; set; }
        
        /// <summary>
        /// The save slot ID associated with the error, if applicable
        /// </summary>
        public int? SlotId { get; set; }
        
        /// <summary>
        /// Additional context information about the error
        /// </summary>
        public string Context { get; set; }

        public SaveErrorEventArgs(SaveErrorType errorType, string errorMessage, Exception exception = null)
        {
            ErrorType = errorType;
            ErrorMessage = errorMessage;
            Exception = exception;
            CanRetry = false;
            HasBackup = false;
        }
    }
}