using System;

namespace HiddenHorizons
{
    /// <summary>
    /// Metadata information for a save slot, used for save slot selection UI
    /// </summary>
    public class SaveSlotMetadata
    {
        /// <summary>
        /// Unique identifier for this save slot
        /// </summary>
        public int SlotId { get; set; }

        /// <summary>
        /// Timestamp of the last save operation for this slot
        /// </summary>
        public DateTime LastSaveTime { get; set; }

        /// <summary>
        /// Total time played in this save slot
        /// </summary>
        public TimeSpan PlayTime { get; set; }

        /// <summary>
        /// Current in-game day number
        /// </summary>
        public int CurrentDay { get; set; }

        /// <summary>
        /// Name of the current zone the player is in
        /// </summary>
        public string CurrentZoneName { get; set; }

        /// <summary>
        /// Current biome type the player is exploring
        /// </summary>
        public BiomeType CurrentBiome { get; set; }

        /// <summary>
        /// Total number of zones visited by the player
        /// </summary>
        public int ZonesVisited { get; set; }

        /// <summary>
        /// Total number of journal entries recorded
        /// </summary>
        public int JournalEntries { get; set; }

        /// <summary>
        /// Version of the game that created this save
        /// </summary>
        public string GameVersion { get; set; }

        /// <summary>
        /// Size of the save file in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SaveSlotMetadata()
        {
            CurrentZoneName = "Unknown";
            CurrentBiome = BiomeType.Plains;
            GameVersion = "1.0.0";
        }
    }
}