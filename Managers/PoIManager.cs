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
    private LightingManager _lightingManager;
    private Dictionary<Guid, Light> _poiLights; // Track lights for each PoI
    private PoIDataLoader _poiDataLoader;
    private HashSet<PoIType> _loggedSuccessfulSprites; // Track which sprites we've logged to avoid spam
    private HashSet<PoIType> _loggedSpriteNames; // Track which sprite names we've logged
    private HashSet<string> _loggedMissingSheets; // Track which missing sheets we've logged
    private HashSet<PoIType> _loggedMissingSprites; // Track which missing sprite warnings we've logged

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
        _poiLights = new Dictionary<Guid, Light>();
        _poiTilesheetManager = new TilesheetManager();
        _poiDataLoader = new PoIDataLoader(assetManager.GetContent());
        _loggedSuccessfulSprites = new HashSet<PoIType>();
        _loggedSpriteNames = new HashSet<PoIType>();
        _loggedMissingSheets = new HashSet<string>();
        _loggedMissingSprites = new HashSet<PoIType>();
    }

    public void SetLightingManager(LightingManager lightingManager)
    {
        _lightingManager = lightingManager;
        
        // Add lights to existing PoIs if lighting manager is available
        if (_lightingManager != null)
        {
            foreach (var poi in _allPoIs)
            {
                AddLightToPoI(poi);
            }
        }
    }

    public void LoadContent()
    {
        try
        {
            // Load PoI tilesheets
            var poiSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/fantasytilesheet_PoI");
            var buildingSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/fantasytilesheet_PoIBuildings");
            var building1Sheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/Building1_PoI");
            var objectsSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/fantasytilesheet_objects");
            var gatherableSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/fruitsnplants_PoI");
            var npcSheet = _assetManager.GetContent().Load<Texture2D>("tilesheet/NPC_PoI");
            
            _poiTilesheetManager.LoadTilesheet("poi", poiSheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("buildings", buildingSheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("building1", building1Sheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("objects", objectsSheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("gatherable", gatherableSheet, 32, 32);
            _poiTilesheetManager.LoadTilesheet("npc", npcSheet, 32, 32);
            
            // Load PoI definitions from JSON
            _poiDataLoader.LoadDefinitions();
            
            // Update existing PoIs with data loader
            UpdateExistingPoIsWithDataLoader();
            
            DefinePoISprites();

        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load PoI content: {ex.Message}");
        }
    }

    private void UpdateExistingPoIsWithDataLoader()
    {
        foreach (var poi in _allPoIs)
        {
            poi.SetDataLoader(_poiDataLoader);
        }

    }
    
    private void DefinePoISprites()
    {
        // Define sprites from JSON data first (primary source)
        DefineSpritesFromJson();
        
        // Legacy hardcoded sprites for PoIs not yet migrated to JSON
        // TODO: Move these to JSON files and remove hardcoded definitions
        
        // Landmarks (not yet in JSON)
        _poiTilesheetManager.DefineTile("deadtreelandmark", "objects", 5, 9);
        
        // Legacy berry bush (not yet in JSON)
        _poiTilesheetManager.DefineTile("berrybush", "objects", 4, 9);
    }
    
    private void DefineSpritesFromJson()
    {
        var definitions = _poiDataLoader.GetDefinitions();
        if (definitions == null) 
        {
            System.Console.WriteLine("[POI SPRITES] No definitions loaded from JSON");
            return;
        }
        
        System.Console.WriteLine($"[POI SPRITES] Starting sprite definition from JSON...");
        System.Console.WriteLine($"[POI SPRITES] Gatherables: {definitions.GatherablePlants?.Count ?? 0}");
        System.Console.WriteLine($"[POI SPRITES] Buildings: {definitions.Buildings?.Count ?? 0}");
        System.Console.WriteLine($"[POI SPRITES] NPCs: {definitions.NPCs?.Count ?? 0}");
        System.Console.WriteLine($"[POI SPRITES] Animals: {definitions.Animals?.Count ?? 0}");
        System.Console.WriteLine($"[POI SPRITES] Monsters: {definitions.Monsters?.Count ?? 0}");
        
        // Define sprites from gatherable plants
        if (definitions.GatherablePlants != null)
        {
            foreach (var plant in definitions.GatherablePlants)
            {
                if (plant.Sprite != null)
                {
                    _poiTilesheetManager.DefineTile(
                        plant.Id, 
                        plant.Sprite.Sheet, 
                        plant.Sprite.X, 
                        plant.Sprite.Y
                    );
                    System.Console.WriteLine($"[POI SPRITES] Defined gatherable: '{plant.Id}' (Type: {plant.Type}) -> {plant.Sprite.Sheet}[{plant.Sprite.X},{plant.Sprite.Y}]");
                }
                else
                {
                    System.Console.WriteLine($"[POI SPRITES] WARNING: Gatherable {plant.Id} has no sprite definition");
                }
            }
        }
        
        // Define sprites from buildings
        if (definitions.Buildings != null)
        {
            foreach (var building in definitions.Buildings)
            {
                if (building.Sprite != null)
                {
                    _poiTilesheetManager.DefineTile(
                        building.Id, 
                        building.Sprite.Sheet, 
                        building.Sprite.X, 
                        building.Sprite.Y
                    );
                    System.Console.WriteLine($"[POI SPRITES] Defined building: '{building.Id}' (Type: {building.Type}) -> {building.Sprite.Sheet}[{building.Sprite.X},{building.Sprite.Y}]");
                }
                else
                {
                    System.Console.WriteLine($"[POI SPRITES] WARNING: Building {building.Id} has no sprite definition");
                }
            }
        }
        
        // Define sprites from NPCs
        if (definitions.NPCs != null)
        {
            foreach (var npc in definitions.NPCs)
            {
                if (npc.Sprite != null)
                {
                    _poiTilesheetManager.DefineTile(
                        npc.Id, 
                        npc.Sprite.Sheet, 
                        npc.Sprite.X, 
                        npc.Sprite.Y
                    );
                    System.Console.WriteLine($"[POI SPRITES] Defined NPC: {npc.Id} -> {npc.Sprite.Sheet}[{npc.Sprite.X},{npc.Sprite.Y}]");
                }
                else
                {
                    System.Console.WriteLine($"[POI SPRITES] WARNING: NPC {npc.Id} has no sprite definition");
                }
            }
        }
        
        // Define sprites from animals
        if (definitions.Animals != null)
        {
            foreach (var animal in definitions.Animals)
            {
                if (animal.Sprite != null)
                {
                    _poiTilesheetManager.DefineTile(
                        animal.Id, 
                        animal.Sprite.Sheet, 
                        animal.Sprite.X, 
                        animal.Sprite.Y
                    );
                    System.Console.WriteLine($"[POI SPRITES] Defined animal: '{animal.Id}' (Type: {animal.Type}) -> {animal.Sprite.Sheet}[{animal.Sprite.X},{animal.Sprite.Y}]");
                }
                else
                {
                    System.Console.WriteLine($"[POI SPRITES] WARNING: Animal {animal.Id} has no sprite definition");
                }
            }
        }
        
        // Define sprites from monsters
        if (definitions.Monsters != null)
        {
            foreach (var monster in definitions.Monsters)
            {
                if (monster.Sprite != null)
                {
                    _poiTilesheetManager.DefineTile(
                        monster.Id, 
                        monster.Sprite.Sheet, 
                        monster.Sprite.X, 
                        monster.Sprite.Y
                    );
                    System.Console.WriteLine($"[POI SPRITES] Defined monster: '{monster.Id}' (Type: {monster.Type}) -> {monster.Sprite.Sheet}[{monster.Sprite.X},{monster.Sprite.Y}]");
                }
                else
                {
                    System.Console.WriteLine($"[POI SPRITES] WARNING: Monster {monster.Id} has no sprite definition");
                }
            }
        }
    }

    public void GeneratePoIsForZone(Zone zone, int chunkSize, int tileSize)
    {
        // Check if this zone already has PoIs generated
        var existingPoIs = _allPoIs.Where(poi => poi.ZoneId == zone.Id).ToList();
        if (existingPoIs.Any())
        {
            return;
        }
        
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
                
                // Add lighting if lighting manager is available
                AddLightToPoI(poi);
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
        
        System.Console.WriteLine($"[POI GENERATION] Generated {zonePoIs.Count} PoIs for zone {zone.Name}");

    }

    private int CalculatePoICount(Zone zone)
    {
        // Base count varies by biome and zone size
        float baseCount = (zone.Width * zone.Height) / 400f; // Roughly 1 PoI per 400 tiles
        
        // Use JSON biome spawn configuration if available
        var biomeConfig = _poiDataLoader.GetBiomeSpawnConfig(zone.BiomeType);
        float biomeMultiplier = biomeConfig.BasePOICount;
        
        // If no JSON config available, fallback to hardcoded values
        if (biomeMultiplier == 0)
        {
            biomeMultiplier = zone.BiomeType switch
            {
                BiomeType.Forest => 1.2f,      // More life in forests
                BiomeType.DenseForest => 0.8f, // Harder to build in dense forest
                BiomeType.Plains => 1.5f,      // Easy to settle plains
                BiomeType.Lake => 0.9f,        // Some lakeside settlements
                BiomeType.Mountain => 0.7f,    // Harsh mountain conditions
                BiomeType.Swamp => 0.6f,       // Difficult swamp terrain
                _ => 1.0f
            };
        }
        
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
            
            attempts++;
        }
        while (attempts < 100 && !IsValidTerrainForPoI(zone, tileX, tileY));
        
        if (attempts >= 100) 
        {
            System.Console.WriteLine($"Warning: Could not find valid position for {poiType} after 100 attempts in zone {zone.Name}");
            return null; // Couldn't find valid position
        }
        
        var finalTerrain = zone.Terrain[tileX, tileY];
        
        var poi = new PointOfInterest
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
        
        // Set the data loader so the PoI can use JSON data for interactions
        poi.SetDataLoader(_poiDataLoader);
        
        System.Console.WriteLine($"[POI SPAWN] Created {poiType} at {position} in zone {zone.Name}");
        
        return poi;
    }

    private List<PoIType> GetAvailablePoITypes(BiomeType biome)
    {
        var availableTypes = new List<PoIType>();
        
        // Load from JSON data first (primary source)
        var gatherableTypes = GetGatherablePoITypesFromJson(biome);
        var buildingTypes = GetBuildingPoITypesFromJson(biome);
        var npcTypes = GetNPCPoITypesFromJson(biome);
        var animalTypes = GetAnimalPoITypesFromJson(biome);
        var monsterTypes = GetMonsterPoITypesFromJson(biome);
        
        availableTypes.AddRange(gatherableTypes);
        availableTypes.AddRange(buildingTypes);
        availableTypes.AddRange(npcTypes);
        availableTypes.AddRange(animalTypes);
        availableTypes.AddRange(monsterTypes);
        
        // Add legacy hardcoded types for PoIs not yet migrated to JSON (only BerryBush remains)
        if (biome == BiomeType.Forest || biome == BiomeType.Plains || biome == BiomeType.DenseForest)
        {
            availableTypes.Add(PoIType.BerryBush);
        }
        
        if (availableTypes.Count == 0)
        {
            availableTypes.Add(PoIType.BerryBush); // Always have at least one type
        }
        
        return availableTypes;
    }
    
    private List<PoIType> GetGatherablePoITypesFromJson(BiomeType biome)
    {
        var gatherableTypes = new List<PoIType>();
        var gatherablePlants = _poiDataLoader.GetGatherablePlantsForBiome(biome);
        
        foreach (var plant in gatherablePlants)
        {
            if (Enum.TryParse<PoIType>(plant.Type, out var poiType))
            {
                // Add multiple instances based on spawn weight
                int instances = Math.Max(1, (int)(plant.SpawnWeight * 3));
                for (int i = 0; i < instances; i++)
                {
                    gatherableTypes.Add(poiType);
                }
            }
        }
        
        return gatherableTypes;
    }
    
    private List<PoIType> GetBuildingPoITypesFromJson(BiomeType biome)
    {
        var buildingTypes = new List<PoIType>();
        var buildings = _poiDataLoader.GetBuildingsForBiome(biome);
        
        foreach (var building in buildings)
        {
            if (Enum.TryParse<PoIType>(building.Type, out var poiType))
            {
                // Add multiple instances based on spawn weight
                int instances = Math.Max(1, (int)(building.SpawnWeight * 3));
                for (int i = 0; i < instances; i++)
                {
                    buildingTypes.Add(poiType);
                }
            }
        }
        
        return buildingTypes;
    }
    
    private List<PoIType> GetNPCPoITypesFromJson(BiomeType biome)
    {
        var npcTypes = new List<PoIType>();
        var npcs = _poiDataLoader.GetNPCsForBiome(biome);
        
        foreach (var npc in npcs)
        {
            if (Enum.TryParse<PoIType>(npc.Type, out var poiType))
            {
                // Add multiple instances based on spawn weight
                int instances = Math.Max(1, (int)(npc.SpawnWeight * 3));
                for (int i = 0; i < instances; i++)
                {
                    npcTypes.Add(poiType);
                }
            }
        }
        
        return npcTypes;
    }
    
    private List<PoIType> GetAnimalPoITypesFromJson(BiomeType biome)
    {
        var animalTypes = new List<PoIType>();
        var animals = _poiDataLoader.GetAnimalsForBiome(biome);
        
        foreach (var animal in animals)
        {
            if (Enum.TryParse<PoIType>(animal.Type, out var poiType))
            {
                // Add multiple instances based on spawn weight
                int instances = Math.Max(1, (int)(animal.SpawnWeight * 3));
                for (int i = 0; i < instances; i++)
                {
                    animalTypes.Add(poiType);
                }
            }
        }
        
        return animalTypes;
    }
    
    private List<PoIType> GetMonsterPoITypesFromJson(BiomeType biome)
    {
        var monsterTypes = new List<PoIType>();
        var monsters = _poiDataLoader.GetMonstersForBiome(biome);
        
        foreach (var monster in monsters)
        {
            if (Enum.TryParse<PoIType>(monster.Type, out var poiType))
            {
                // Add multiple instances based on spawn weight
                int instances = Math.Max(1, (int)(monster.SpawnWeight * 3));
                for (int i = 0; i < instances; i++)
                {
                    monsterTypes.Add(poiType);
                }
            }
        }
        
        return monsterTypes;
    }

    private bool IsValidTerrainForPoI(Zone zone, int tileX, int tileY)
    {
        // Debug: Check bounds
        if (tileX < 0 || tileX >= zone.Width || tileY < 0 || tileY >= zone.Height)
        {

            return false;
        }
        
        // Debug: Check terrain array bounds
        if (zone.Terrain == null)
        {

            return false;
        }
        
        if (tileX >= zone.Terrain.GetLength(0) || tileY >= zone.Terrain.GetLength(1))
        {

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
        // Check JSON definitions first (primary source)
        var plantDef = _poiDataLoader.GetPlantDefinition(type);
        if (plantDef != null)
        {
            return plantDef.Name;
        }
        
        var buildingDef = _poiDataLoader.GetBuildingDefinition(type);
        if (buildingDef != null)
        {
            return buildingDef.Name;
        }
        
        var npcDef = _poiDataLoader.GetNPCDefinition(type);
        if (npcDef != null)
        {
            return npcDef.Name;
        }
        
        var animalDef = _poiDataLoader.GetAnimalDefinition(type);
        if (animalDef != null)
        {
            return animalDef.Name;
        }
        
        var monsterDef = _poiDataLoader.GetMonsterDefinition(type);
        if (monsterDef != null)
        {
            return monsterDef.Name;
        }
        
        // Legacy fallback for PoIs not yet migrated to JSON (only BerryBush remains)
        var names = GetPoINames(type);
        return names.Length > 0 ? names[_random.Next(names.Length)] : type.ToString();
    }

    private void AddLightToPoI(PointOfInterest poi)
    {
        if (_lightingManager == null || _poiLights.ContainsKey(poi.Id))
            return;

        Light light = null;

        // First try to get lighting from JSON data
        var buildingDef = _poiDataLoader.GetBuildingDefinition(poi.Type);
        if (buildingDef?.Lighting != null && buildingDef.Lighting.Enabled)
        {
            var lightingDef = buildingDef.Lighting;
            var lightColor = GetLightColorFromString(lightingDef.Color);
            light = _lightingManager.AddLight(poi.Position, lightColor, lightingDef.Radius, lightingDef.Intensity);

        }
        else
        {
            // Check NPC lighting from JSON
            var npcDef = _poiDataLoader.GetNPCDefinition(poi.Type);
            if (npcDef?.Lighting != null && npcDef.Lighting.Enabled)
            {
                var lightingDef = npcDef.Lighting;
                var lightColor = GetLightColorFromString(lightingDef.Color);
                light = _lightingManager.AddLight(poi.Position, lightColor, lightingDef.Radius, lightingDef.Intensity);

            }
            else
            {
                // Fallback to hardcoded lighting for types not in JSON
                light = poi.Type switch
                {
                    // NPCs - they carry torches/lanterns
                    PoIType.Ranger or PoIType.Priest or PoIType.Warrior or PoIType.Scholar or 
                    PoIType.Hermit or PoIType.Adventurer or PoIType.Mermaid => 
                        _lightingManager.AddLight(poi.Position, LightPresets.Torch, 150f, 0.8f),

                    // Buildings - they have windows/hearths/torches
                    PoIType.Inn or PoIType.Cottage or PoIType.Farmhouse or PoIType.Hut => 
                        _lightingManager.AddLight(poi.Position, LightPresets.WindowLight, 200f, 1.0f),

                    PoIType.Castle or PoIType.Chapel => 
                        _lightingManager.AddLight(poi.Position, LightPresets.Torch, 250f, 1.2f),

                    PoIType.Mine => 
                        _lightingManager.AddLight(poi.Position, LightPresets.Lantern, 180f, 0.9f),

                    // Spooky places - eerie lighting
                    PoIType.SkullFortress or PoIType.HauntedHouse => 
                        _lightingManager.AddLight(poi.Position, new Color(150, 255, 150), 160f, 0.7f), // Eerie green

                    // Oracle - mystical lighting
                    PoIType.Oracle => 
                        _lightingManager.AddLight(poi.Position, new Color(200, 150, 255), 180f, 0.8f), // Mystical purple

                    // TreeHouse - natural warm light
                    PoIType.TreeHouse => 
                        _lightingManager.AddLight(poi.Position, LightPresets.Campfire, 170f, 0.9f),

                    // No lights for animals, monsters, or resources
                    _ => null
                };

                if (light != null)
                {

                }
            }
        }

        if (light != null)
        {
            // Lights should only be visible during dark periods
            light.Enabled = false; // Will be controlled by time of day
            _poiLights[poi.Id] = light;
            System.Console.WriteLine($"[POI LIGHT] Added light for {poi.Type} at {poi.Position} in zone {poi.ZoneId}");
        }
    }

    private Color GetLightColorFromString(string colorName)
    {
        return colorName switch
        {
            "Torch" => LightPresets.Torch,
            "Campfire" => LightPresets.Campfire,
            "Lantern" => LightPresets.Lantern,
            "Moonlight" => LightPresets.Moonlight,
            "WindowLight" => LightPresets.WindowLight,
            "Mystical" => new Color(200, 150, 255), // Mystical purple
            "EerieGreen" => new Color(150, 255, 150), // Eerie green
            _ => LightPresets.WindowLight // Default fallback
        };
    }

    private string[] GetPoINames(PoIType type)
    {
        // Only legacy types that aren't in JSON yet
        return type switch
        {
            PoIType.BerryBush => new[] { "Wild Berry Bush", "Ripe Berry Patch", "Sweet Berry Bush", "Forest Berry Bush" },
            _ => new[] { $"Mysterious {type}" }
        };
    }

    private string GeneratePoIDescription(PoIType type, BiomeType biome)
    {
        // Check JSON definitions first (primary source)
        var plantDef = _poiDataLoader.GetPlantDefinition(type);
        if (plantDef != null)
        {
            return plantDef.Description;
        }
        
        var buildingDef = _poiDataLoader.GetBuildingDefinition(type);
        if (buildingDef != null)
        {
            return buildingDef.Description;
        }
        
        var npcDef = _poiDataLoader.GetNPCDefinition(type);
        if (npcDef != null)
        {
            return npcDef.Description;
        }
        
        var animalDef = _poiDataLoader.GetAnimalDefinition(type);
        if (animalDef != null)
        {
            return animalDef.Description;
        }
        
        var monsterDef = _poiDataLoader.GetMonsterDefinition(type);
        if (monsterDef != null)
        {
            return monsterDef.Description;
        }
        
        // Legacy fallback for PoIs not yet migrated to JSON (only BerryBush remains)
        return type switch
        {
            PoIType.BerryBush => $"A wild berry bush growing naturally in the {biome.ToString().ToLower()}, heavy with ripe fruit.",
            _ => $"An interesting {type.ToString().ToLower()} found in the {biome.ToString().ToLower()}."
        };
    }

    public void SetLightsEnabled(bool enabled)
    {
        foreach (var light in _poiLights.Values)
        {
            light.Enabled = enabled;
        }
    }

    public void SetCurrentZone(string zoneId)
    {
        // Clean up lights from the previous zone if we're changing zones
        if (_currentZoneId != null && _currentZoneId != zoneId)
        {
            var oldZonePoIs = _allPoIs.Where(poi => poi.ZoneId == _currentZoneId).ToList();
            foreach (var poi in oldZonePoIs)
            {
                if (_poiLights.ContainsKey(poi.Id))
                {
                    var light = _poiLights[poi.Id];
                    _lightingManager?.RemoveLight(light);
                    _poiLights.Remove(poi.Id);
                    System.Console.WriteLine($"[POI LIGHT] Removed light for {poi.Type} when leaving zone {_currentZoneId}");
                }
            }
        }
        
        _currentZoneId = zoneId;
        var zonePoIs = _allPoIs.Where(poi => poi.ZoneId == zoneId).ToList();
        
        // Add lights for PoIs in the new zone
        foreach (var poi in zonePoIs)
        {
            if (!_poiLights.ContainsKey(poi.Id))
            {
                AddLightToPoI(poi);
            }
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
            
            // Check if the tilesheet exists
            var sheet = _poiTilesheetManager.GetSheet(sheetName);
            if (sheet == null)
            {
                if (!_loggedMissingSheets.Contains(sheetName))
                {
                    System.Console.WriteLine($"[POI RENDER] ERROR: Tilesheet '{sheetName}' not loaded!");
                    _loggedMissingSheets.Add(sheetName);
                }
                continue;
            }
            
            // Check if the tile definition exists
            var tileRect = _poiTilesheetManager.GetTileRect(spriteName);
            if (tileRect == null)
            {
                // Only log warning once per PoI type to avoid spam
                if (!_loggedMissingSprites.Contains(poi.Type))
                {
                    System.Console.WriteLine($"[POI RENDER] WARNING: No tile definition found for {poi.Type} (sprite: '{spriteName}', sheet: {sheetName})");
                    _loggedMissingSprites.Add(poi.Type);
                }
            }
            else
            {
                // Only log first time we successfully find a sprite (to avoid spam)
                if (!_loggedSuccessfulSprites.Contains(poi.Type))
                {
                    System.Console.WriteLine($"[POI RENDER] SUCCESS: Found sprite for {poi.Type} (sprite: '{spriteName}', sheet: {sheetName})");
                    _loggedSuccessfulSprites.Add(poi.Type);
                }
            }
            
            _poiTilesheetManager.DrawTile(spriteBatch, spriteName, sheetName, poi.Position, Color.White);
        }
    }

    private string GetSpriteNameForPoI(PoIType type)
    {
        // Check JSON definitions first - gatherables
        var plantDef = _poiDataLoader.GetPlantDefinition(type);
        if (plantDef != null)
        {
            if (!_loggedSpriteNames.Contains(type))
            {
                System.Console.WriteLine($"[POI SPRITE NAME] {type} -> '{plantDef.Id}' (from gatherable JSON)");
                _loggedSpriteNames.Add(type);
            }
            return plantDef.Id;
        }
        
        // Check JSON definitions - buildings
        var buildingDef = _poiDataLoader.GetBuildingDefinition(type);
        if (buildingDef != null)
        {
            if (!_loggedSpriteNames.Contains(type))
            {
                System.Console.WriteLine($"[POI SPRITE NAME] {type} -> '{buildingDef.Id}' (from building JSON)");
                _loggedSpriteNames.Add(type);
            }
            return buildingDef.Id;
        }
        
        // Check JSON definitions - NPCs
        var npcDef = _poiDataLoader.GetNPCDefinition(type);
        if (npcDef != null)
        {
            if (!_loggedSpriteNames.Contains(type))
            {
                System.Console.WriteLine($"[POI SPRITE NAME] {type} -> '{npcDef.Id}' (from NPC JSON)");
                _loggedSpriteNames.Add(type);
            }
            return npcDef.Id;
        }
        
        // Check JSON definitions - animals
        var animalDef = _poiDataLoader.GetAnimalDefinition(type);
        if (animalDef != null)
        {
            if (!_loggedSpriteNames.Contains(type))
            {
                System.Console.WriteLine($"[POI SPRITE NAME] {type} -> '{animalDef.Id}' (from animal JSON)");
                _loggedSpriteNames.Add(type);
            }
            return animalDef.Id;
        }
        
        // Check JSON definitions - monsters
        var monsterDef = _poiDataLoader.GetMonsterDefinition(type);
        if (monsterDef != null)
        {
            if (!_loggedSpriteNames.Contains(type))
            {
                System.Console.WriteLine($"[POI SPRITE NAME] {type} -> '{monsterDef.Id}' (from monster JSON)");
                _loggedSpriteNames.Add(type);
            }
            return monsterDef.Id;
        }
        
        // Fallback to hardcoded mappings for types not in JSON (only BerryBush remains)
        string fallbackName = type switch
        {
            PoIType.BerryBush => "berrybush",
            _ => type.ToString().ToLower()
        };
        
        if (!_loggedSpriteNames.Contains(type))
        {
            System.Console.WriteLine($"[POI SPRITE NAME] {type} -> '{fallbackName}' (fallback)");
            _loggedSpriteNames.Add(type);
        }
        
        return fallbackName;
    }

    private string GetSheetNameForPoI(PoIType type)
    {
        // Check JSON definitions first - gatherables
        var plantDef = _poiDataLoader.GetPlantDefinition(type);
        if (plantDef?.Sprite != null)
        {
            return plantDef.Sprite.Sheet;
        }
        
        // Check JSON definitions - buildings
        var buildingDef = _poiDataLoader.GetBuildingDefinition(type);
        if (buildingDef?.Sprite != null)
        {
            return buildingDef.Sprite.Sheet;
        }
        
        // Check JSON definitions - NPCs
        var npcDef = _poiDataLoader.GetNPCDefinition(type);
        if (npcDef?.Sprite != null)
        {
            return npcDef.Sprite.Sheet;
        }
        
        // Check JSON definitions - animals
        var animalDef = _poiDataLoader.GetAnimalDefinition(type);
        if (animalDef?.Sprite != null)
        {
            return animalDef.Sprite.Sheet;
        }
        
        // Check JSON definitions - monsters
        var monsterDef = _poiDataLoader.GetMonsterDefinition(type);
        if (monsterDef?.Sprite != null)
        {
            return monsterDef.Sprite.Sheet;
        }
        
        // Fallback to hardcoded mappings for types not in JSON (only BerryBush remains)
        return type switch
        {
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
        System.Console.WriteLine($"[POI CLEANUP] Clearing {poisToRemove.Count} PoIs for zone {zoneId}");
        
        foreach (var poi in poisToRemove)
        {
            // Remove associated light if it exists
            if (_poiLights.ContainsKey(poi.Id))
            {
                var light = _poiLights[poi.Id];
                _lightingManager?.RemoveLight(light);
                _poiLights.Remove(poi.Id);
                System.Console.WriteLine($"[POI LIGHT] Removed light for {poi.Type} when clearing zone {zoneId}");
            }
            
            _allPoIs.Remove(poi);
        }
        
        // Remove the entire chunk dictionary for this zone
        if (_chunkPoIsByZone.ContainsKey(zoneId))
        {
            _chunkPoIsByZone.Remove(zoneId);
        }
        

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

    // DEBUG METHOD - Spawns all PoI types in a grid for testing
    // Call this after zone generation to test all PoI types
    // Remove this method when testing is complete
    public void DEBUG_SpawnAllPoITypes(Zone zone, int tileSize = 32)
    {
        System.Console.WriteLine("[DEBUG] Spawning all PoI types in zone...");
        
        // Get all PoI types from the enum
        var allPoITypes = Enum.GetValues<PoIType>();
        
        // Starting position near adventurer spawn (1000, 1000) = tile (31, 31)
        // Spawn in a grid starting slightly above and to the left
        int startX = 25; // Tile 25 = 800 pixels (adventurer is at 1000)
        int startY = 25; // Tile 25 = 800 pixels
        int spacing = 4; // 4 tiles between each PoI
        int columns = 10; // 10 PoIs per row
        
        int currentX = startX;
        int currentY = startY;
        int column = 0;
        
        foreach (var poiType in allPoITypes)
        {
            // Calculate position
            Vector2 position = new Vector2(currentX * tileSize, currentY * tileSize);
            
            // Create the PoI
            var poi = new PointOfInterest
            {
                Id = Guid.NewGuid(),
                Type = poiType,
                Position = position,
                Name = GeneratePoIName(poiType),
                Description = $"DEBUG: {poiType}",
                IsDiscovered = true,
                IsInteractable = true,
                InteractionRange = 48f,
                ZoneId = zone.Id
            };
            
            poi.SetDataLoader(_poiDataLoader);
            _allPoIs.Add(poi);
            AddLightToPoI(poi);
            
            System.Console.WriteLine($"[DEBUG] Spawned {poiType} at ({currentX}, {currentY})");
            
            // Move to next position
            column++;
            if (column >= columns)
            {
                // Move to next row
                column = 0;
                currentX = startX;
                currentY += spacing;
            }
            else
            {
                // Move to next column
                currentX += spacing;
            }
        }
        
        System.Console.WriteLine($"[DEBUG] Spawned {allPoITypes.Length} PoI types total");
        System.Console.WriteLine($"[DEBUG] Total PoIs in _allPoIs list: {_allPoIs.Count}");
        System.Console.WriteLine($"[DEBUG] PoIs for zone {zone.Id}: {_allPoIs.Count(p => p.ZoneId == zone.Id)}");
    }

    // DEBUG METHOD - Diagnose phantom lights
    // Call this to compare PoI lights with actual lights in the lighting manager
    // Remove this method when testing is complete
    public void DEBUG_DiagnosePhantomLights()
    {
        if (_lightingManager == null)
        {
            System.Console.WriteLine("[DEBUG LIGHTS] LightingManager is null");
            return;
        }

        var allLights = _lightingManager.GetLights();
        System.Console.WriteLine($"[DEBUG LIGHTS] === LIGHT DIAGNOSTIC ===");
        System.Console.WriteLine($"[DEBUG LIGHTS] Total lights in LightingManager: {allLights.Count}");
        System.Console.WriteLine($"[DEBUG LIGHTS] Total PoI lights tracked: {_poiLights.Count}");
        System.Console.WriteLine($"[DEBUG LIGHTS] Total PoIs in current zone: {_allPoIs.Count(p => p.ZoneId == _currentZoneId)}");
        System.Console.WriteLine($"[DEBUG LIGHTS] Total PoIs across all zones: {_allPoIs.Count}");

        // Find lights that aren't tracked as PoI lights
        var trackedLightPositions = _poiLights.Values.Select(l => l.Position).ToHashSet();
        var untrackedLights = allLights.Where(l => !trackedLightPositions.Contains(l.Position)).ToList();
        
        System.Console.WriteLine($"[DEBUG LIGHTS] Untracked lights (phantom candidates): {untrackedLights.Count}");
        
        if (untrackedLights.Count > 0)
        {
            System.Console.WriteLine($"[DEBUG LIGHTS] First 10 untracked light positions:");
            foreach (var light in untrackedLights.Take(10))
            {
                System.Console.WriteLine($"[DEBUG LIGHTS]   - Position: {light.Position}, Enabled: {light.Enabled}, Color: {light.Color}, Radius: {light.Radius}");
            }
        }

        // Check for PoIs without lights
        var poisWithoutLights = _allPoIs.Where(p => !_poiLights.ContainsKey(p.Id)).ToList();
        System.Console.WriteLine($"[DEBUG LIGHTS] PoIs without lights: {poisWithoutLights.Count}");
        
        if (poisWithoutLights.Count > 0)
        {
            System.Console.WriteLine($"[DEBUG LIGHTS] First 10 PoIs without lights:");
            foreach (var poi in poisWithoutLights.Take(10))
            {
                System.Console.WriteLine($"[DEBUG LIGHTS]   - {poi.Type} at {poi.Position} in zone {poi.ZoneId}");
            }
        }
    }
}