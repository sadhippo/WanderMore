using System;
using System.Collections.Generic;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for the Journal system
    /// Contains all journal entries, exploration data, and statistics
    /// </summary>
    public class JournalSaveData
    {
        public List<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
        public HashSet<string> VisitedZones { get; set; } = new HashSet<string>();
        public HashSet<string> DiscoveredBiomes { get; set; } = new HashSet<string>();
        public JournalStatistics Statistics { get; set; } = new JournalStatistics();
        
        // Additional tracking data
        public int TotalZonesVisited { get; set; }
        public int TotalDaysExplored { get; set; }
    }
}