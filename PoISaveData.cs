using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for the PoI system
    /// Contains all discovered PoIs and their spatial organization
    /// </summary>
    public class PoISaveData
    {
        public List<PointOfInterestSaveData> AllPoIs { get; set; } = new List<PointOfInterestSaveData>();
        public Dictionary<Point, List<Guid>> ChunkPoIMapping { get; set; } = new Dictionary<Point, List<Guid>>();
    }

    /// <summary>
    /// Save data structure for individual Points of Interest
    /// Contains all state information needed to restore a PoI
    /// </summary>
    public class PointOfInterestSaveData
    {
        public Guid Id { get; set; }
        public PoIType Type { get; set; }
        public Vector2 Position { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDiscovered { get; set; }
        public bool IsInteractable { get; set; }
        public float InteractionRange { get; set; }
        public string ZoneId { get; set; }
        
        // Interaction history
        public bool HasBeenInteracted { get; set; }
        public DateTime? LastInteractionTime { get; set; }
        public int InteractionCount { get; set; }
        
        // Quest and story integration
        public List<string> AssociatedQuests { get; set; } = new List<string>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}