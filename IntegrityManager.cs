using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Manages save file integrity through checksum validation and atomic file operations
    /// </summary>
    public class IntegrityManager
    {
        private static readonly object _fileLock = new object();

        /// <summary>
        /// Generates a SHA-256 checksum for the given data
        /// </summary>
        /// <param name="data">The data to generate checksum for</param>
        /// <returns>SHA-256 checksum as hexadecimal string</returns>
        public string GenerateChecksum(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] hashBytes = sha256.ComputeHash(dataBytes);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }

        /// <summary>
        /// Validates that the provided data matches the expected checksum
        /// </summary>
        /// <param name="data">The data to validate</param>
        /// <param name="expectedChecksum">The expected checksum</param>
        /// <returns>True if checksum matches, false otherwise</returns>
        public bool ValidateChecksum(string data, string expectedChecksum)
        {
            if (string.IsNullOrEmpty(expectedChecksum))
                return false;

            string actualChecksum = GenerateChecksum(data);
            return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies the integrity of a save file by checking its checksum
        /// </summary>
        /// <param name="filePath">Path to the save file</param>
        /// <returns>True if file integrity is valid, false otherwise</returns>
        public bool VerifyIntegrity(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                lock (_fileLock)
                {
                    string fileContent = File.ReadAllText(filePath);
                    
                    // Parse the save data to extract checksum
                    var saveData = System.Text.Json.JsonSerializer.Deserialize<GameSaveData>(fileContent);
                    if (saveData == null || string.IsNullOrEmpty(saveData.Checksum))
                        return false;

                    // Create a copy without checksum for validation
                    var dataForValidation = new GameSaveData
                    {
                        Version = saveData.Version,
                        SaveTimestamp = saveData.SaveTimestamp,
                        GameVersion = saveData.GameVersion,
                        SystemData = saveData.SystemData,
                        Checksum = null // Exclude checksum from validation
                    };

                    string dataJson = System.Text.Json.JsonSerializer.Serialize(dataForValidation);
                    return ValidateChecksum(dataJson, saveData.Checksum);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Writes data to a file atomically using a temporary file and File.Move
        /// </summary>
        /// <param name="filePath">Target file path</param>
        /// <param name="data">Data to write</param>
        /// <param name="generateChecksum">Whether to generate and include checksum</param>
        public void WriteFileAtomically(string filePath, string data, bool generateChecksum = true)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempFilePath = filePath + ".tmp";

            try
            {
                lock (_fileLock)
                {
                    string finalData = data;
                    
                    if (generateChecksum)
                    {
                        // Parse the data to add checksum
                        var saveData = System.Text.Json.JsonSerializer.Deserialize<GameSaveData>(data);
                        if (saveData != null)
                        {
                            // Generate checksum for data without checksum field
                            var dataForChecksum = new GameSaveData
                            {
                                Version = saveData.Version,
                                SaveTimestamp = saveData.SaveTimestamp,
                                GameVersion = saveData.GameVersion,
                                SystemData = saveData.SystemData,
                                Checksum = null
                            };

                            string dataJson = System.Text.Json.JsonSerializer.Serialize(dataForChecksum);
                            saveData.Checksum = GenerateChecksum(dataJson);
                            finalData = System.Text.Json.JsonSerializer.Serialize(saveData);
                        }
                    }

                    // Write to temporary file first
                    File.WriteAllText(tempFilePath, finalData);

                    // Atomically move temporary file to target location
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    File.Move(tempFilePath, filePath);
                }
            }
            catch (Exception)
            {
                // Clean up temporary file if it exists
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                throw;
            }
        }

        /// <summary>
        /// Reads a file with file locking to prevent concurrent access
        /// </summary>
        /// <param name="filePath">Path to the file to read</param>
        /// <returns>File content as string</returns>
        public string ReadFileWithLock(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            lock (_fileLock)
            {
                return File.ReadAllText(filePath);
            }
        }

        /// <summary>
        /// Checks if a file is currently locked by another process
        /// </summary>
        /// <param name="filePath">Path to the file to check</param>
        /// <returns>True if file is locked, false otherwise</returns>
        public bool IsFileLocked(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
        }

        /// <summary>
        /// Waits for a file to become available (not locked) with timeout
        /// </summary>
        /// <param name="filePath">Path to the file to wait for</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>True if file became available, false if timeout occurred</returns>
        public bool WaitForFileAvailable(string filePath, int timeoutMs = 5000)
        {
            var startTime = DateTime.UtcNow;
            
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                if (!IsFileLocked(filePath))
                    return true;

                Thread.Sleep(100);
            }

            return false;
        }
    }
}