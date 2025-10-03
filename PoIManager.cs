using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class PoIManager
{
    private Dictionary<string, Dictionary<Point, List<PointOfInterest>>> _chunkPoIsByZone;
    private List<PointOfInterest> _allPoIs;
    private AssetManager _assetManager;
    private TilesheetManager _poiTilesheetManager;
    private Random _random;
    private JournalManager _journalManager;
    private string _currentZoneId;

    // Events for other systems to subscribe to
    public event Action<PointOfInterest> PoIDiscovered;
    public event Action<PointOfInterest, Adventurer> PoIInteracted;
    public event Action<PointOfInterest> PoIApproached;

    public PoIManager(AssetManager assetManager, JournalManager journalManager, int seed = 0)
    {
        _assetManager = assetManager;
        _journalManager = journalManager;
        _chunkPoIsByZone = new Dictionary<string, Dictionary<Point, List<PointOfInterest>>>();
        _allPoIs = new List<PointOfInterest>();
        _random = seed == 0 ? new Random() : new Random(seed);
        _poiTilesheetManager = new TilesheetManager();
    }

    public void LoadContent()
    {
        try
        {
            // Load PoI tilesheets
            var poiSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/fantasytilesheet_PoI");
            var buildingSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/fantasytilesheet_PoIBuildings");
            var objectsSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/fantasytilesheet_objects");
            
            _poiTilesheetManager.LoadTilesheet("poi", poiSheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("buildings", buildingSheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("objects", objectsSheet, 32, 32);
            
            DefinePoISprites();
            System.Console.WriteLine("PoI content loaded successfully");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load PoI content: {ex.Message}");
        }
    }

    private void DefinePoISprites()
    {
        // NPCs
        _poiTilesheetManager.DefineTile("ranger", "poi", 4, 9);
        _poiTilesheetManager.DefineTile("priest", "poi", 10, 9);
        _poiTilesheetManager.DefineTile("warrior", "poi", 5, 9);
        _poiTilesheetManager.DefineTile("adventurer_npc", "poi", 9, 9);
        _poiTilesheetManager.DefineTile("scholar", "poi", 11, 9);
        _poiTilesheetManager.DefineTile("hermit", "poi", 16, 9);
        _poiTilesheetManager.DefineTile("mermaid", "poi", 12, 7);

        // Monsters
        _poiTilesheetManager.DefineTile("skeleton", "poi", 7, 7);
        _poiTilesheetManager.DefineTile("centaur", "poi", 15, 8);
        _poiTilesheetManager.DefineTile("dragon", "poi", 4, 7);
        _poiTilesheetManager.DefineTile("minotaur", "poi", 6, 8);
        _poiTilesheetManager.DefineTile("golem", "poi", 11, 8);

        // Animals
        _poiTilesheetManager.DefineTile("cat", "poi", 18, 3);
        _poiTilesheetManager.DefineTile("dog", "poi", 17, 3);
        _poiTilesheetManager.DefineTile("chicken", "poi", 4, 3);
        _poiTilesheetManager.DefineTile("pig", "poi", 5, 3);
        _poiTilesheetManager.DefineTile("lizard", "poi", 11, 5);
        _poiTilesheetManager.DefineTile("bat", "poi", 7, 5);
        _poiTilesheetManager.DefineTile("unicorn", "poi", 5, 5);
        _poiTilesheetManager.DefineTile("sheep", "poi", 15, 4);

        // Buildings
        _poiTilesheetManager.DefineTile("farmhouse", "buildings", 4, 2);
        _poiTilesheetManager.DefineTile("inn", "buildings", 4, 3);
        _poiTilesheetManager.DefineTile("cottage", "buildings", 5, 2);
        _poiTilesheetManager.DefineTile("hut", "buildings", 8, 2);
        _poiTilesheetManager.DefineTile("castle", "buildings", 5, 3);
        _poiTilesheetManager.DefineTile("chapel", "buildings", 7, 3);
        _poiTilesheetManager.DefineTile("oracle", "buildings", 8, 3);
        _poiTilesheetManager.DefineTile("skull_fortress", "buildings", 12, 2);
        _poiTilesheetManager.DefineTile("haunted_house", "buildings", 11, 2);
        _poiTilesheetManager.DefineTile("tree_house", "buildings", 11, 3);
        _poiTilesheetManager.DefineTile("mine", "buildings", 13, 3);
        
        // Resources - Note: berry bush is in objects sheet, need to load it separately
        _poiTilesheetManager.DefineTile("berrybush", "objects", 4, 9);
    }

    public void GeneratePoIsForZone(Zone zone, int chunkSize, int tileSize)
    {
        // Check if this zone already has PoIs generated
        var existingPoIs = _allPoIs.Where(poi => poi.ZoneId == zone.Id).ToList();
        if (existingPoIs.Any())
        {
            System.Console.WriteLine($"[POI DEBUG] Zone {zone.Name} already has {existingPoIs.Count} PoIs, skipping generation");
            foreach (var poi in existingPoIs)
            {
                System.Console.WriteLine($"[POI DEBUG] Existing: {poi.Type} at {poi.Position}");
            }
            return;
        }
        
        System.Console.WriteLine($"[POI DEBUG] Starting PoI generation for zone {zone.Name} (currently has {_allPoIs.Count} total PoIs)");
        
        var zonePoIs = new List<PointOfInterest>();
        
        // Generate PoIs based on biome type and zone characteristics
        int poiCount = CalculatePoICount(zone);
        
        for (int i = 0; i < poiCount; i++)
        {
            var poi = GenerateRandomPoI(zone, tileSize);
            if (poi != null && IsValidPoIPlacement(poi, zone, zonePoIs))
            {
                zonePoIs.Add(poi);
                _allPoIs.Add(poi);
            }
        }
        
        // Store PoIs by chunk for efficient loading/unloading
        // Initialize zone's chunk dictionary if it doesn't exist
        if (!_chunkPoIsByZone.ContainsKey(zone.Id))
        {
            _chunkPoIsByZone[zone.Id] = new Dictionary<Point, List<PointOfInterest>>();
        }
        
        var zoneChunks = _chunkPoIsByZone[zone.Id];
        foreach (var poi in zonePoIs)
        {
            Point chunkCoord = new Point(
                (int)(poi.Position.X / (chunkSize * tileSize)),
                (int)(poi.Position.Y / (chunkSize * tileSize))
            );
            
            if (!zoneChunks.ContainsKey(chunkCoord))
            {
                zoneChunks[chunkCoord] = new List<PointOfInterest>();
            }
            zoneChunks[chunkCoord].Add(poi);
        }
        
        System.Console.WriteLine($"Generated {zonePoIs.Count} PoIs for zone {zone.Name}");
    }

    private int CalculatePoICount(Zone zone)
    {
        // Base count varies by biome and zone size
        float baseCount = (zone.Width * zone.Height) / 400f; // Roughly 1 PoI per 400 tiles
        
        float biomeMultiplier = zone.BiomeType switch
        {
            BiomeType.Forest => 1.2f,      // More life in forests
            BiomeType.DenseForest => 0.8f, // Harder to build in dense forest
            BiomeType.Plains => 1.5f,      // Easy to settle plains
            BiomeType.Lake => 0.9f,        // Some lakeside settlements
            BiomeType.Mountain => 0.7f,    // Harsh mountain conditions
            BiomeType.Swamp => 0.6f,       // Difficult swamp terrain
            _ => 1.0f
        };
        
        return Math.Max(1, (int)(baseCount * biomeMultiplier * _random.NextSingle() * 2f));
    }

    private PointOfInterest GenerateRandomPoI(Zone zone, int tileSize)
    {
        // Choose PoI type based on biome
        var availableTypes = GetAvailablePoITypes(zone.BiomeType);
        if (availableTypes.Count == 0) return null;
        
        var poiType = availableTypes[_random.Next(availableTypes.Count)];
        
        // Find valid position (not on water, stone, or existing objects)
        Vector2 position;
        int attempts = 0;
        int tileX, tileY;
        do
        {
            // Ensure we stay within zone bounds with proper padding
            // Use proportional padding for skinny zones
            int paddingX = Math.Max(5, zone.Width / 8); // At least 5 tiles, or 1/8 of zone width
            int paddingY = Math.Max(5, zone.Height / 8); // At least 5 tiles, or 1/8 of zone height
            
            tileX = _random.Next(paddingX, zone.Width - paddingX);
            tileY = _random.Next(paddingY, zone.Height - paddingY);
            position = new Vector2(tileX * tileSize, tileY * tileSize);
            
            // Debug: Show zone world offset
            if (attempts <= 3)
            {
                System.Console.WriteLine($"[ZONE DEBUG] Zone {zone.Name} world offset: ({zone.WorldX}, {zone.WorldY})");
            }
            attempts++;
            
            // Debug: Show coordinate conversion for first few attempts
            if (attempts <= 3)
            {
                System.Console.WriteLine($"[COORD DEBUG] Attempt {attempts}: tileX={tileX}, tileY={tileY}, position={position}, zoneSize={zone.Width}x{zone.Height}");
            }
        }
        while (attempts < 100 && !IsValidTerrainForPoI(zone, tileX, tileY));
        
        if (attempts >= 100) 
        {
            System.Console.WriteLine($"Warning: Could not find valid position for {poiType} after 100 attempts in zone {zone.Name}");
            return null; // Couldn't find valid position
        }
        
        // Debug: Check what terrain type we're placing on
        var finalTerrain = zone.Terrain[tileX, tileY];
        System.Console.WriteLine($"[PoI DEBUG] Placing {poiType} at world pos {position} -> tile ({tileX}, {tileY}) on {finalTerrain} terrain in {zone.Name} (zone size: {zone.Width}x{zone.Height}, terrain array: {zone.Terrain.GetLength(0)}x{zone.Terrain.GetLength(1)})");
        System.Console.WriteLine($"[PoI DEBUG] Zone bounds: (0,0) to ({zone.Width * 32},{zone.Height * 32}) = {zone.Width * 32}x{zone.Height * 32} pixels");
        
        return new PointOfInterest
        {
            Id = Guid.NewGuid(),
            Type = poiType,
            Position = position,
            Name = GeneratePoIName(poiType),
            Description = GeneratePoIDescription(poiType, zone.BiomeType),
            IsDiscovered = true, // PoIs are now visible from zone creation
            IsInteractable = true,
            InteractionRange = 48f, // 1.5 tiles
            ZoneId = zone.Id
        };
    }

    private List<PoIType> GetAvailablePoITypes(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Forest => new List<PoIType> 
            { 
                PoIType.Ranger, PoIType.Hermit, PoIType.TreeHouse, PoIType.Cottage, 
                PoIType.Dog, PoIType.Cat, PoIType.Unicorn, PoIType.Deer, PoIType.BerryBush 
            },
            BiomeType.DenseForest => new List<PoIType> 
            { 
                PoIType.Hermit, PoIType.TreeHouse, PoIType.Bat, PoIType.Skeleton,
                PoIType.HauntedHouse, PoIType.Lizard, PoIType.BerryBush 
            },
            BiomeType.Plains => new List<PoIType> 
            { 
                PoIType.Farmhouse, PoIType.Inn, PoIType.Warrior, PoIType.Adventurer,
                PoIType.Sheep, PoIType.Chicken, PoIType.Pig, PoIType.Dog, PoIType.BerryBush 
            },
            BiomeType.Lake => new List<PoIType> 
            { 
                PoIType.Mermaid, PoIType.Cottage, PoIType.Cat, PoIType.Lizard, PoIType.BerryBush 
            },
            BiomeType.Mountain => new List<PoIType> 
            { 
                PoIType.Mine, PoIType.Castle, PoIType.Golem, PoIType.Dragon,
                PoIType.Hermit, PoIType.Bat, PoIType.BerryBush 
            },
            BiomeType.Swamp => new List<PoIType> 
            { 
                PoIType.Hut, PoIType.HauntedHouse, PoIType.Skeleton, PoIType.Lizard,
                PoIType.Bat, PoIType.Minotaur, PoIType.BerryBush 
            },
            _ => new List<PoIType> { PoIType.Cottage, PoIType.Cat, PoIType.Dog }
        };
    }

    private bool IsValidTerrainForPoI(Zone zone, int tileX, int tileY)
    {
        // Debug: Check bounds
        if (tileX < 0 || tileX >= zone.Width || tileY < 0 || tileY >= zone.Height)
        {
            System.Console.WriteLine($"[BOUNDS DEBUG] Tile ({tileX}, {tileY}) is outside zone bounds ({zone.Width}x{zone.Height}) in {zone.Name}");
            return false;
        }
        
        // Debug: Check terrain array bounds
        if (zone.Terrain == null)
        {
            System.Console.WriteLine($"[TERRAIN DEBUG] Terrain array is null in {zone.Name}");
            return false;
        }
        
        if (tileX >= zone.Terrain.GetLength(0) || tileY >= zone.Terrain.GetLength(1))
        {
            System.Console.WriteLine($"[TERRAIN DEBUG] Tile ({tileX}, {tileY}) is outside terrain array bounds ({zone.Terrain.GetLength(0)}x{zone.Terrain.GetLength(1)}) in {zone.Name}");
            return false;
        }
        
        // Ensure we're within zone bounds with proper padding
        // Use proportional padding for skinny zones
        int paddingX = Math.Max(5, zone.Width / 8); // At least 5 tiles, or 1/8 of zone width
        int paddingY = Math.Max(5, zone.Height / 8); // At least 5 tiles, or 1/8 of zone height
        
        if (tileX < paddingX || tileX >= zone.Width - paddingX || tileY < paddingY || tileY >= zone.Height - paddingY)
            return false;
            
        var terrain = zone.Terrain[tileX, tileY];
        
        // Only allow PoIs on grass or dirt terrain
        if (terrain != TerrainType.Grass && terrain != TerrainType.Dirt)
        {
            // Debug: Log invalid terrain types
            if (terrain == TerrainType.Stone)
            {
                System.Console.WriteLine($"[TERRAIN DEBUG] Rejecting stone terrain at ({tileX}, {tileY}) in {zone.Name}");
                return false;
            }
            return false;
        }
        
        // Check if there are any objects at this position (trees, bushes, etc.)
        Vector2 tilePosition = new Vector2(tileX * 32, tileY * 32);
        
        foreach (var obj in zone.Objects)
        {
            // Check if any object is at this exact tile position
            if (Vector2.Distance(obj.Position, tilePosition) < 16f) // Half a tile tolerance
            {
                return false; // Position is occupied by an object
            }
        }
        
        return true; // Position is clear
    }

    private bool IsValidPoIPlacement(PointOfInterest newPoI, Zone zone, List<PointOfInterest> existingPoIs)
    {
        // Ensure minimum distance between PoIs
        float minDistance = 96f; // 3 tiles
        
        foreach (var existing in existingPoIs)
        {
            if (Vector2.Distance(newPoI.Position, existing.Position) < minDistance)
            {
                return false;
            }
        }
        
        return true;
    }

    private string GeneratePoIName(PoIType type)
    {
        var names = GetPoINames(type);
        return names[_random.Next(names.Length)];
    }

    private string[] GetPoINames(PoIType type)
    {
        return type switch
        {
            PoIType.Ranger => new[] { "Forest Warden", "Woodland Guide", "Nature's Guardian", "Trail Keeper" },
            PoIType.Priest => new[] { "Village Cleric", "Holy Wanderer", "Divine Healer", "Sacred Guardian" },
            PoIType.Warrior => new[] { "Brave Knight", "Seasoned Fighter", "Battle Veteran", "Noble Defender" },
            PoIType.Hermit => new[] { "Wise Hermit", "Mountain Sage", "Forest Recluse", "Ancient Mystic" },
            PoIType.Scholar => new[] { "Learned Scholar", "Ancient Historian", "Wise Researcher", "Knowledge Seeker" },
            PoIType.Mermaid => new[] { "Lake Guardian", "Water Spirit", "Aquatic Maiden", "River Nymph" },
            PoIType.Inn => new[] { "Traveler's Rest", "Cozy Hearth Inn", "Waypoint Tavern", "Journey's End" },
            PoIType.Cottage => new[] { "Peaceful Cottage", "Humble Dwelling", "Quiet Home", "Rustic Retreat" },
            PoIType.Castle => new[] { "Ancient Fortress", "Noble Castle", "Royal Stronghold", "Majestic Keep" },
            PoIType.Mine => new[] { "Old Mine Shaft", "Abandoned Quarry", "Deep Excavation", "Mineral Cavern" },
            PoIType.BerryBush => new[] { "Wild Berry Bush", "Ripe Berry Patch", "Sweet Berry Bush", "Forest Berry Bush" },
            _ => new[] { $"Mysterious {type}" }
        };
    }

    private string GeneratePoIDescription(PoIType type, BiomeType biome)
    {
        // Generate contextual descriptions based on type and biome
        return type switch
        {
            PoIType.Ranger => $"A skilled ranger who knows every path through this {biome.ToString().ToLower()}.",
            PoIType.Inn => $"A welcoming inn that provides rest for weary travelers in the {biome.ToString().ToLower()}.",
            PoIType.Hermit => $"A wise hermit who has chosen solitude in this remote {biome.ToString().ToLower()}.",
            PoIType.Castle => $"An imposing castle that dominates the {biome.ToString().ToLower()} landscape.",
            PoIType.Mine => $"An old mining operation that once extracted valuable resources from the {biome.ToString().ToLower()}.",
            PoIType.BerryBush => $"A wild berry bush growing naturally in the {biome.ToString().ToLower()}, heavy with ripe fruit.",
            _ => $"An interesting {type.ToString().ToLower()} found in the {biome.ToString().ToLower()}."
        };
    }

    public void SetCurrentZone(string zoneId)
    {
        _currentZoneId = zoneId;
        var zonePoIs = _allPoIs.Where(poi => poi.ZoneId == zoneId).ToList();
        System.Console.WriteLine($"[POI DEBUG] SetCurrentZone called: {zoneId}, found {zonePoIs.Count} PoIs for this zone");
        foreach (var poi in zonePoIs)
        {
            System.Console.WriteLine($"[POI DEBUG]   - {poi.Type} at {poi.Position} in zone {poi.ZoneId}");
        }
    }
    
    public void Update(Vector2 playerPosition, float interactionRange = 32f, string currentZoneId = null)
    {
        // Update current zone if provided
        if (currentZoneId != null)
        {
            _currentZoneId = currentZoneId;
        }
        
        // Only check PoIs from the current zone
        var currentZonePoIs = _currentZoneId != null ? 
            _allPoIs.Where(poi => poi.ZoneId == _currentZoneId).ToList() : 
            _allPoIs;
            
        // Check for PoI approaches and interactions
        foreach (var poi in currentZonePoIs)
        {
            float distance = Vector2.Distance(playerPosition, poi.Position);
            
            // Approach check - add journal entry on first approach
            if (distance <= poi.InteractionRange)
            {
                PoIApproached?.Invoke(poi);
                
                // Add journal entry on first close approach (you could add a flag to prevent spam)
                // _journalManager.OnSpecialEvent($"Approached {poi.Name}", poi.Description);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, string currentZoneId = null)
    {
        // Only draw PoIs from the current zone
        var currentZonePoIs = currentZoneId != null ? 
            _allPoIs.Where(poi => poi.ZoneId == currentZoneId).ToList() : 
            _allPoIs;
            
        foreach (var poi in currentZonePoIs)
        {
            string spriteName = GetSpriteNameForPoI(poi.Type);
            string sheetName = GetSheetNameForPoI(poi.Type);
            
            _poiTilesheetManager.DrawTile(spriteBatch, spriteName, sheetName, poi.Position, Color.White);
        }
    }

    private string GetSpriteNameForPoI(PoIType type)
    {
        return type switch
        {
            PoIType.Adventurer => "adventurer_npc",
            PoIType.SkullFortress => "skull_fortress",
            PoIType.HauntedHouse => "haunted_house",
            PoIType.TreeHouse => "tree_house",
            PoIType.BerryBush => "berrybush",
            _ => type.ToString().ToLower()
        };
    }

    private string GetSheetNameForPoI(PoIType type)
    {
        return type switch
        {
            PoIType.Farmhouse or PoIType.Inn or PoIType.Cottage or PoIType.Hut or
            PoIType.Castle or PoIType.Chapel or PoIType.Oracle or PoIType.SkullFortress or
            PoIType.HauntedHouse or PoIType.TreeHouse or PoIType.Mine => "buildings",
            PoIType.BerryBush => "objects",
            _ => "poi"
        };
    }

    private bool IsBuilding(PoIType type)
    {
        return type switch
        {
            PoIType.Farmhouse or PoIType.Inn or PoIType.Cottage or PoIType.Hut or
            PoIType.Castle or PoIType.Chapel or PoIType.Oracle or PoIType.SkullFortress or
            PoIType.HauntedHouse or PoIType.TreeHouse or PoIType.Mine => true,
            _ => false
        };
    }

    public List<PointOfInterest> GetNearbyPoIs(Vector2 position, float range, string currentZoneId = null)
    {
        // Use provided zone ID, or fall back to current zone, or all PoIs
        string zoneToUse = currentZoneId ?? _currentZoneId;
        var currentZonePoIs = zoneToUse != null ? 
            _allPoIs.Where(poi => poi.ZoneId == zoneToUse).ToList() : 
            _allPoIs;
            
        return currentZonePoIs.Where(poi => 
            poi.IsDiscovered && 
            Vector2.Distance(position, poi.Position) <= range
        ).ToList();
    }

    public PointOfInterest GetPoIAt(Vector2 position, float tolerance = 32f, string currentZoneId = null)
    {
        // Use provided zone ID, or fall back to current zone, or all PoIs
        string zoneToUse = currentZoneId ?? _currentZoneId;
        var currentZonePoIs = zoneToUse != null ? 
            _allPoIs.Where(poi => poi.ZoneId == zoneToUse).ToList() : 
            _allPoIs;
            
        return currentZonePoIs.FirstOrDefault(poi => 
            poi.IsDiscovered && 
            Vector2.Distance(position, poi.Position) <= tolerance
        );
    }

    public void ClearPoIsForZone(string zoneId)
    {
        // Remove PoIs for a specific zone (useful for regeneration)
        var poisToRemove = _allPoIs.Where(poi => poi.ZoneId == zoneId).ToList();
        
        foreach (var poi in poisToRemove)
        {
            _allPoIs.Remove(poi);
        }
        
        // Remove the entire chunk dictionary for this zone
        if (_chunkPoIsByZone.ContainsKey(zoneId))
        {
            _chunkPoIsByZone.Remove(zoneId);
        }
        
        System.Console.WriteLine($"Cleared {poisToRemove.Count} PoIs for zone {zoneId}");
    }

    public int GetPoICountForZone(string zoneId)
    {
        return _allPoIs.Count(poi => poi.ZoneId == zoneId);
    }
    
    public List<PointOfInterest> GetPoIsForZone(string zoneId)
    {
        return _allPoIs.Where(poi => poi.ZoneId == zoneId).ToList();
    }
    
    public void TriggerInteraction(PointOfInterest poi, Adventurer adventurer)
    {
        // Fire the interaction event for quest system and other subscribers
        PoIInteracted?.Invoke(poi, adventurer);
    }
}