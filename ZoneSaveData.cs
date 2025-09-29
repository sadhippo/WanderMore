using System.Collections.Generic;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for individual Zone state
    /// </summary>
    public class ZoneSaveData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public BiomeType BiomeType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int WorldX { get; set; }
        public int WorldY { get; set; }
        public Dictionary<Direction, string> Connections { get; set; }
        public Dictionary<Direction, bool> GeneratedConnections { get; set; }
        public bool[][] ExploredTiles { get; set; }
        
        // Note: Terrain and Objects are procedurally generated, not saved
        // They will be regenerated based on the zone's properties and random seed
    }
}