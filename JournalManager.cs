using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class JournalManager : ISaveable
{
    private List<JournalEntry> _entries;
    private HashSet<string> _visitedZones;
    private HashSet<string> _discoveredBiomes;
    private TimeManager _timeManager;
    private int _totalZonesVisited;
    private int _totalDaysExplored;

    // Events for UI updates
    public event Action<JournalEntry> NewEntryAdded;
    public event Action<string> NewZoneDiscovered;
    public event Action<BiomeType> NewBiomeDiscovered;

    public JournalManager(TimeManager timeManager)
    {
        _timeManager = timeManager;
        _entries = new List<JournalEntry>();
        _visitedZones = new HashSet<string>();
        _discoveredBiomes = new HashSet<string>();
        
        // Subscribe to time events for automatic entries
        _timeManager.DayChanged += OnDayChanged;
        
        // Add initial entry
        AddEntry(JournalEntryType.GameStart, "Adventure begins!", 
            "A new journey starts in this mysterious world of endless horizons.");
    }

    public void OnZoneEntered(Zone zone)
    {
        string zoneKey = $"{zone.Id}_{zone.BiomeType}";
        bool isNewZone = !_visitedZones.Contains(zoneKey);
        bool isNewBiome = !_discoveredBiomes.Contains(zone.BiomeType.ToString());
        
        if (isNewZone)
        {
            _visitedZones.Add(zoneKey);
            _totalZonesVisited++;
            
            // Create zone discovery entry
            string description = GenerateZoneDescription(zone, isNewBiome);
            AddEntry(JournalEntryType.ZoneDiscovery, $"Discovered {zone.Name}", description);
            
            NewZoneDiscovered?.Invoke(zone.Name);
        }
        
        if (isNewBiome)
        {
            _discoveredBiomes.Add(zone.BiomeType.ToString());
            string biomeDescription = GenerateBiomeDescription(zone.BiomeType);
            AddEntry(JournalEntryType.BiomeDiscovery, $"First {zone.BiomeType} Biome", biomeDescription);
            
            NewBiomeDiscovered?.Invoke(zone.BiomeType);
        }
    }

    public void OnWeatherChanged(WeatherType weather, string seasonName)
    {
        if (weather != WeatherType.Clear)
        {
            string description = GenerateWeatherDescription(weather, seasonName);
            AddEntry(JournalEntryType.WeatherEvent, $"Weather: {weather}", description);
        }
    }

    public void OnSpecialEvent(string title, string description)
    {
        AddEntry(JournalEntryType.SpecialEvent, title, description);
    }

    private void OnDayChanged(int day)
    {
        _totalDaysExplored = day;
        
        // Add milestone entries
        if (day == 7)
        {
            AddEntry(JournalEntryType.Milestone, "One Week of Exploration", 
                $"A full week has passed. Visited {_totalZonesVisited} zones and discovered {_discoveredBiomes.Count} different biomes.");
        }
        else if (day % 30 == 0) // Every 30 days
        {
            int months = day / 30;
            AddEntry(JournalEntryType.Milestone, $"{months} Month{(months > 1 ? "s" : "")} of Adventure", 
                $"Time flies during exploration. The journey continues with {_totalZonesVisited} zones explored.");
        }
    }

    private void AddEntry(JournalEntryType type, string title, string description)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Description = description,
            Timestamp = DateTime.Now,
            GameDay = _timeManager.CurrentDay,
            GameTime = _timeManager.GetTimeString(),
            Season = _timeManager.GetSeasonName()
        };
        
        _entries.Add(entry);
        NewEntryAdded?.Invoke(entry);
        
        System.Console.WriteLine($"Journal: {entry.Title} - {entry.Description}");
    }

    private string GenerateZoneDescription(Zone zone, bool isNewBiome)
    {
        var descriptions = new List<string>();
        
        // Base zone description
        descriptions.Add($"Entered the {zone.BiomeType.ToString().ToLower()} region of {zone.Name}.");
        
        if (isNewBiome)
        {
            descriptions.Add($"This is the first {zone.BiomeType.ToString().ToLower()} biome discovered!");
        }
        
        // Add zone-specific details
        switch (zone.BiomeType)
        {
            case BiomeType.Forest:
                descriptions.Add("Tall trees sway gently in the breeze, creating dappled shadows on the forest floor.");
                break;
            case BiomeType.DenseForest:
                descriptions.Add("The canopy is so thick here that little sunlight reaches the ground.");
                break;
            case BiomeType.Plains:
                descriptions.Add("Wide open grasslands stretch to the horizon under an endless sky.");
                break;
            case BiomeType.Lake:
                descriptions.Add("Crystal clear waters reflect the sky like a perfect mirror.");
                break;
            case BiomeType.Mountain:
                descriptions.Add("Rocky peaks and stone formations dominate the landscape.");
                break;
            case BiomeType.Swamp:
                descriptions.Add("Misty wetlands with mysterious pools and twisted vegetation.");
                break;
        }
        
        return string.Join(" ", descriptions);
    }

    private string GenerateBiomeDescription(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => "Discovered forests - peaceful woodlands with scattered trees and open clearings perfect for exploration.",
            BiomeType.DenseForest => "Found dense forests - thick woodlands where ancient trees create a natural cathedral of green.",
            BiomeType.Plains => "Encountered plains - vast grasslands that seem to stretch on forever under open skies.",
            BiomeType.Lake => "Discovered lakes - serene bodies of water that bring life and tranquility to the landscape.",
            BiomeType.Mountain => "Found mountains - majestic stone formations that reach toward the clouds.",
            BiomeType.Swamp => "Encountered swamplands - mysterious wetlands shrouded in mist and filled with unique life.",
            _ => "Discovered a new type of terrain with its own unique characteristics."
        };
    }

    private string GenerateWeatherDescription(WeatherType weather, string season)
    {
        return weather switch
        {
            WeatherType.Rain => $"Rain falls gently across the landscape during this {season.ToLower()} day, bringing life to the plants and creating a peaceful atmosphere.",
            WeatherType.Snow => $"Snow blankets the world in pristine white, transforming the landscape into a winter wonderland during {season.ToLower()}.",
            WeatherType.Fog => $"Thick fog rolls across the terrain, creating an air of mystery and limiting visibility during this {season.ToLower()} day.",
            WeatherType.Cloudy => $"Clouds gather overhead, casting interesting shadows across the landscape during {season.ToLower()}.",
            _ => $"The weather changes, bringing new atmosphere to this {season.ToLower()} exploration."
        };
    }

    // Query methods for UI and statistics
    public List<JournalEntry> GetRecentEntries(int count = 10)
    {
        return _entries.TakeLast(count).Reverse().ToList();
    }

    public List<JournalEntry> GetEntriesByType(JournalEntryType type)
    {
        return _entries.Where(e => e.Type == type).ToList();
    }

    public List<JournalEntry> GetEntriesByDay(int day)
    {
        return _entries.Where(e => e.GameDay == day).ToList();
    }

    public JournalStatistics GetStatistics()
    {
        return new JournalStatistics
        {
            TotalEntries = _entries.Count,
            ZonesVisited = _totalZonesVisited,
            BiomesDiscovered = _discoveredBiomes.Count,
            DaysExplored = _totalDaysExplored,
            WeatherEventsRecorded = _entries.Count(e => e.Type == JournalEntryType.WeatherEvent),
            MilestonesReached = _entries.Count(e => e.Type == JournalEntryType.Milestone)
        };
    }

    // ISaveable implementation
    public string SaveKey => "JournalManager";
    public int SaveVersion => 1;

    public object GetSaveData()
    {
        return new JournalSaveData
        {
            Entries = new List<JournalEntry>(_entries),
            VisitedZones = new HashSet<string>(_visitedZones),
            DiscoveredBiomes = new HashSet<string>(_discoveredBiomes),
            Statistics = GetStatistics(),
            TotalZonesVisited = _totalZonesVisited,
            TotalDaysExplored = _totalDaysExplored
        };
    }

    public void LoadSaveData(object data)
    {
        if (data is not JournalSaveData saveData)
        {
            throw new ArgumentException("Invalid save data type for JournalManager", nameof(data));
        }

        // Clear existing data
        _entries.Clear();
        _visitedZones.Clear();
        _discoveredBiomes.Clear();

        // Load saved data
        _entries.AddRange(saveData.Entries ?? new List<JournalEntry>());
        _visitedZones = new HashSet<string>(saveData.VisitedZones ?? new HashSet<string>());
        _discoveredBiomes = new HashSet<string>(saveData.DiscoveredBiomes ?? new HashSet<string>());
        _totalZonesVisited = saveData.TotalZonesVisited;
        _totalDaysExplored = saveData.TotalDaysExplored;

        // Trigger events for restored data to update UI
        TriggerRestorationEvents();
    }

    private void TriggerRestorationEvents()
    {
        // Trigger events for each discovered zone
        foreach (var zone in _visitedZones)
        {
            // Extract zone name from the zone key format "{zone.Id}_{zone.BiomeType}"
            var parts = zone.Split('_');
            if (parts.Length >= 2)
            {
                var zoneName = string.Join("_", parts.Take(parts.Length - 1));
                NewZoneDiscovered?.Invoke(zoneName);
            }
        }

        // Trigger events for each discovered biome
        foreach (var biome in _discoveredBiomes)
        {
            if (Enum.TryParse<BiomeType>(biome, out var biomeType))
            {
                NewBiomeDiscovered?.Invoke(biomeType);
            }
        }

        // Trigger event for the most recent entry to update UI
        if (_entries.Count > 0)
        {
            var mostRecentEntry = _entries.Last();
            NewEntryAdded?.Invoke(mostRecentEntry);
        }
    }
}

public class JournalEntry
{
    public Guid Id { get; set; }
    public JournalEntryType Type { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
    public int GameDay { get; set; }
    public string GameTime { get; set; }
    public string Season { get; set; }
}

public enum JournalEntryType
{
    GameStart,
    ZoneDiscovery,
    BiomeDiscovery,
    WeatherEvent,
    Milestone,
    SpecialEvent,
    // Future expansion:
    // ItemFound,
    // NPCMet,
    // POIDiscovered,
    // QuestCompleted
}

public class JournalStatistics
{
    public int TotalEntries { get; set; }
    public int ZonesVisited { get; set; }
    public int BiomesDiscovered { get; set; }
    public int DaysExplored { get; set; }
    public int WeatherEventsRecorded { get; set; }
    public int MilestonesReached { get; set; }
}