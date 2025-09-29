using System;

namespace HiddenHorizons
{
    /// <summary>
    /// Interface that must be implemented by any game system that needs to persist data
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique identifier for this saveable system
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Gets the current state data that should be saved
        /// </summary>
        /// <returns>Serializable object containing the system's state</returns>
        object GetSaveData();

        /// <summary>
        /// Loads and applies the provided save data to restore the system's state
        /// </summary>
        /// <param name="data">Previously saved state data</param>
        void LoadSaveData(object data);

        /// <summary>
        /// Version number for this system's save data format
        /// Used for migration when save format changes
        /// </summary>
        int SaveVersion { get; }
    }
}