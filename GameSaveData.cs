using System;
using System.Collections.Generic;

namespace HiddenHorizons
{
    /// <summary>
    /// Root save data structure containing all game state information
    /// </summary>
    public class GameSaveData
    {
        /// <summary>
        /// Save format version for migration purposes
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Timestamp when this save was created
        /// </summary>
        public DateTime SaveTimestamp { get; set; }

        /// <summary>
        /// Version of the game that created this save
        /// </summary>
        public string GameVersion { get; set; }

        /// <summary>
        /// Dictionary containing save data from all registered ISaveable systems
        /// Key is the SaveKey from ISaveable, Value is the serialized data
        /// </summary>
        public Dictionary<string, object> SystemData { get; set; }

        /// <summary>
        /// SHA-256 checksum for integrity verification
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GameSaveData()
        {
            SystemData = new Dictionary<string, object>();
            SaveTimestamp = DateTime.UtcNow;
            GameVersion = "1.0.0"; // This should be set from actual game version
        }
    }
}