using System.Collections.Generic;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for ZoneManager state
    /// </summary>
    public class ZoneManagerSaveData
    {
        public string CurrentZoneId { get; set; }
        public Dictionary<string, ZoneSaveData> Zones { get; set; }
        public int RandomSeed { get; set; }
    }
}