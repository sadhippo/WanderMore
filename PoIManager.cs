using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class PoIManager
{
    private Dictionary<Point, List<PointOfInterest>> _chunkPoIs;
    private List<PointOfInterest> _allPoIs;
    private AssetManager _assetManager;
    private TilesheetManager _poiTilesheetManager;
    private Random _random;
    private JournalManager _journalManager;

    // Events for other systems to subscribe to
    public event Action<PointOfInterest> PoIDiscovered;
    public event Action<PointOfInterest, Adventurer> PoIInteracted;
    public event Action<PointOfInterest> PoIApproached;

    public PoIManager(AssetManager assetManager, JournalManager journalManager, int seed = 0)
    {
        _assetManager = assetManager;
        _journalManager = journalManager;
        _chunkPoIs = new Dictionary<Point, List<PointOfInterest>>();
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
            
            _poiTilesheetManager.LoadTilesheet("poi", poiSheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("buildings", buildingSheet, 32, 32);
            
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
    }

    public void GeneratePoIsForZone(Zone zone, int chunkSize, int tileSize)
    {
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
        foreach (var poi in zonePoIs)
        {
            Point chunkCoord = new Point(
                (int)(poi.Position.X / (chunkSize * tileSize)),
                (int)(poi.Position.Y / (chunkSize * tileSize))
            );
            
            if (!_chunkPoIs.ContainsKey(chunkCoord))
            {
                _chunkPoIs[chunkCoord] = new List<PointOfInterest>();
            }
            _chunkPoIs[chunkCoord].Add(poi);
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
        
        // Find valid position (not on water or stone)
        Vector2 position;
        int attempts = 0;
        do
        {
            int x = _random.Next(2, zone.Width - 2); // Leave border space
            int y = _random.Next(2, zone.Height - 2);
            position = new Vector2(x * tileSize, y * tileSize);
            attempts++;
        }
        while (attempts < 50 && !IsValidTerrainForPoI(zone, (int)(position.X / tileSize), (int)(position.Y / tileSize)));
        
        if (attempts >= 50) return null; // Couldn't find valid position
        
        return new PointOfInterest
        {
            Id = Guid.NewGuid(),
            Type = poiType,
            Position = position,
            Name = GeneratePoIName(poiType),
            Description = GeneratePoIDescription(poiType, zone.BiomeType),
            IsDiscovered = false,
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
                PoIType.Dog, PoIType.Cat, PoIType.Unicorn, PoIType.Deer 
            },
            BiomeType.DenseForest => new List<PoIType> 
            { 
                PoIType.Hermit, PoIType.TreeHouse, PoIType.Bat, PoIType.Skeleton,
                PoIType.HauntedHouse, PoIType.Lizard 
            },
            BiomeType.Plains => new List<PoIType> 
            { 
                PoIType.Farmhouse, PoIType.Inn, PoIType.Warrior, PoIType.Adventurer,
                PoIType.Sheep, PoIType.Chicken, PoIType.Pig, PoIType.Dog 
            },
            BiomeType.Lake => new List<PoIType> 
            { 
                PoIType.Mermaid, PoIType.Cottage, PoIType.Cat, PoIType.Lizard 
            },
            BiomeType.Mountain => new List<PoIType> 
            { 
                PoIType.Mine, PoIType.Castle, PoIType.Golem, PoIType.Dragon,
                PoIType.Hermit, PoIType.Bat 
            },
            BiomeType.Swamp => new List<PoIType> 
            { 
                PoIType.Hut, PoIType.HauntedHouse, PoIType.Skeleton, PoIType.Lizard,
                PoIType.Bat, PoIType.Minotaur 
            },
            _ => new List<PoIType> { PoIType.Cottage, PoIType.Cat, PoIType.Dog }
        };
    }

    private bool IsValidTerrainForPoI(Zone zone, int tileX, int tileY)
    {
        if (tileX < 0 || tileX >= zone.Width || tileY < 0 || tileY >= zone.Height)
            return false;
            
        var terrain = zone.Terrain[tileX, tileY];
        return terrain == TerrainType.Grass || terrain == TerrainType.Dirt;
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
            _ => $"An interesting {type.ToString().ToLower()} found in the {biome.ToString().ToLower()}."
        };
    }

    public void Update(Vector2 playerPosition, float interactionRange = 32f)
    {
        // Check for PoI discoveries and interactions
        foreach (var poi in _allPoIs)
        {
            float distance = Vector2.Distance(playerPosition, poi.Position);
            
            // Discovery check (larger range)
            if (!poi.IsDiscovered && distance <= poi.InteractionRange * 2f)
            {
                poi.IsDiscovered = true;
                PoIDiscovered?.Invoke(poi);
                
                // Add journal entry
                _journalManager.OnSpecialEvent($"Discovered {poi.Name}", poi.Description);
            }
            
            // Approach check
            if (poi.IsDiscovered && distance <= poi.InteractionRange)
            {
                PoIApproached?.Invoke(poi);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var poi in _allPoIs)
        {
            if (poi.IsDiscovered)
            {
                string spriteName = GetSpriteNameForPoI(poi.Type);
                string sheetName = IsBuilding(poi.Type) ? "buildings" : "poi";
                
                _poiTilesheetManager.DrawTile(spriteBatch, spriteName, sheetName, poi.Position, Color.White);
            }
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
            _ => type.ToString().ToLower()
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

    public List<PointOfInterest> GetNearbyPoIs(Vector2 position, float range)
    {
        return _allPoIs.Where(poi => 
            poi.IsDiscovered && 
            Vector2.Distance(position, poi.Position) <= range
        ).ToList();
    }

    public PointOfInterest GetPoIAt(Vector2 position, float tolerance = 32f)
    {
        return _allPoIs.FirstOrDefault(poi => 
            poi.IsDiscovered && 
            Vector2.Distance(position, poi.Position) <= tolerance
        );
    }
}