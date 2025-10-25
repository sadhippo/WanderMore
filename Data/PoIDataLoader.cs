using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HiddenHorizons;

public class PoIDataLoader
{
    private PoIDefinitions _definitions;
    private ContentManager _content;

    public PoIDataLoader(ContentManager content)
    {
        _content = content;
    }

    public void LoadDefinitions()
    {

        
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            _definitions = new PoIDefinitions();
            
            // Initialize all collections to prevent null reference issues
            _definitions.GatherablePlants = new List<GatherablePlantDefinition>();
            _definitions.Buildings = new List<BuildingDefinition>();
            _definitions.NPCs = new List<NPCDefinition>();
            _definitions.Animals = new List<AnimalDefinition>();
            _definitions.Monsters = new List<MonsterDefinition>();
            _definitions.BiomeSpawnRates = new Dictionary<string, BiomeSpawnConfig>();
            _definitions.RarityWeights = new Dictionary<string, float>();
            
            // Load each category with detailed logging
            LoadGatherables(options);
            LoadBuildings(options);
            LoadNPCs(options);
            LoadAnimals(options);
            LoadMonsters(options);
            LoadConfig(options);
            

        }
        catch (Exception ex)
        {

            
            // Create empty definitions as fallback
            _definitions = new PoIDefinitions
            {
                GatherablePlants = new List<GatherablePlantDefinition>(),
                Buildings = new List<BuildingDefinition>(),
                NPCs = new List<NPCDefinition>(),
                Animals = new List<AnimalDefinition>(),
                Monsters = new List<MonsterDefinition>(),
                BiomeSpawnRates = new Dictionary<string, BiomeSpawnConfig>(),
                RarityWeights = new Dictionary<string, float>()
            };
            

        }
    }
    
    private void LoadGatherables(JsonSerializerOptions options)
    {
        string jsonPath = Path.Combine(_content.RootDirectory, "data", "poi_gatherables.json");

        
        try
        {
            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"Warning: Gatherables file not found at {jsonPath}");
                _definitions.GatherablePlants = new List<GatherablePlantDefinition>();
                return;
            }
            
            string jsonContent = File.ReadAllText(jsonPath);

            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                System.Console.WriteLine("Warning: Gatherables file is empty");
                _definitions.GatherablePlants = new List<GatherablePlantDefinition>();
                return;
            }
            
            var gatherableData = JsonSerializer.Deserialize<GatherableData>(jsonContent, options);
            
            if (gatherableData?.GatherablePlants == null)
            {
                System.Console.WriteLine("Warning: No gatherable plants found in JSON or deserialization failed");
                _definitions.GatherablePlants = new List<GatherablePlantDefinition>();
                return;
            }
            
            _definitions.GatherablePlants = gatherableData.GatherablePlants;
            System.Console.WriteLine($"Successfully loaded {_definitions.GatherablePlants.Count} gatherable plants");
            
            // Validate loaded data
            foreach (var plant in _definitions.GatherablePlants)
            {
                if (string.IsNullOrEmpty(plant.Id))
                    System.Console.WriteLine($"Warning: Gatherable plant missing ID");
                if (plant.Sprite == null)
                    System.Console.WriteLine($"Warning: Gatherable plant '{plant.Id}' missing sprite definition");
            }
        }
        catch (JsonException jsonEx)
        {
            System.Console.WriteLine($"JSON parsing error in gatherables file: {jsonEx.Message}");
            System.Console.WriteLine($"JSON error path: {jsonEx.Path}");
            _definitions.GatherablePlants = new List<GatherablePlantDefinition>();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Unexpected error loading gatherables: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _definitions.GatherablePlants = new List<GatherablePlantDefinition>();
        }
    }
    
    private void LoadBuildings(JsonSerializerOptions options)
    {
        string jsonPath = Path.Combine(_content.RootDirectory, "data", "poi_buildings.json");
        System.Console.WriteLine($"Loading buildings from: {jsonPath}");
        
        try
        {
            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"Warning: Buildings file not found at {jsonPath}");
                _definitions.Buildings = new List<BuildingDefinition>();
                return;
            }
            
            string jsonContent = File.ReadAllText(jsonPath);

            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                System.Console.WriteLine("Warning: Buildings file is empty");
                _definitions.Buildings = new List<BuildingDefinition>();
                return;
            }
            
            var buildingData = JsonSerializer.Deserialize<BuildingData>(jsonContent, options);
            
            if (buildingData?.Buildings == null)
            {
                System.Console.WriteLine("Warning: No buildings found in JSON or deserialization failed");
                _definitions.Buildings = new List<BuildingDefinition>();
                return;
            }
            
            _definitions.Buildings = buildingData.Buildings;
            System.Console.WriteLine($"Successfully loaded {_definitions.Buildings.Count} buildings");
            
            // Validate loaded data
            foreach (var building in _definitions.Buildings)
            {
                if (string.IsNullOrEmpty(building.Id))
                    System.Console.WriteLine($"Warning: Building missing ID");
                if (building.Sprite == null)
                    System.Console.WriteLine($"Warning: Building '{building.Id}' missing sprite definition");
            }
        }
        catch (JsonException jsonEx)
        {
            System.Console.WriteLine($"JSON parsing error in buildings file: {jsonEx.Message}");
            System.Console.WriteLine($"JSON error path: {jsonEx.Path}");
            _definitions.Buildings = new List<BuildingDefinition>();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Unexpected error loading buildings: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _definitions.Buildings = new List<BuildingDefinition>();
        }
    }
    
    private void LoadNPCs(JsonSerializerOptions options)
    {
        string jsonPath = Path.Combine(_content.RootDirectory, "data", "poi_npcs.json");
        System.Console.WriteLine($"Loading NPCs from: {jsonPath}");
        
        try
        {
            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"Warning: NPCs file not found at {jsonPath}");
                _definitions.NPCs = new List<NPCDefinition>();
                return;
            }
            
            string jsonContent = File.ReadAllText(jsonPath);

            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                System.Console.WriteLine("Warning: NPCs file is empty");
                _definitions.NPCs = new List<NPCDefinition>();
                return;
            }
            
            var npcData = JsonSerializer.Deserialize<NPCData>(jsonContent, options);
            
            if (npcData?.NPCs == null)
            {
                System.Console.WriteLine("Warning: No NPCs found in JSON or deserialization failed");
                _definitions.NPCs = new List<NPCDefinition>();
                return;
            }
            
            _definitions.NPCs = npcData.NPCs;
            System.Console.WriteLine($"Successfully loaded {_definitions.NPCs.Count} NPCs");
            
            // Validate loaded data
            foreach (var npc in _definitions.NPCs)
            {
                if (string.IsNullOrEmpty(npc.Id))
                    System.Console.WriteLine($"Warning: NPC missing ID");
                if (npc.Sprite == null)
                    System.Console.WriteLine($"Warning: NPC '{npc.Id}' missing sprite definition");
            }
        }
        catch (JsonException jsonEx)
        {
            System.Console.WriteLine($"JSON parsing error in NPCs file: {jsonEx.Message}");
            System.Console.WriteLine($"JSON error path: {jsonEx.Path}");
            _definitions.NPCs = new List<NPCDefinition>();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Unexpected error loading NPCs: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _definitions.NPCs = new List<NPCDefinition>();
        }
    }
    
    private void LoadAnimals(JsonSerializerOptions options)
    {
        string jsonPath = Path.Combine(_content.RootDirectory, "data", "poi_animals.json");
        System.Console.WriteLine($"Loading animals from: {jsonPath}");
        
        try
        {
            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"Warning: Animals file not found at {jsonPath}");
                _definitions.Animals = new List<AnimalDefinition>();
                return;
            }
            
            string jsonContent = File.ReadAllText(jsonPath);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                System.Console.WriteLine("Warning: Animals file is empty");
                _definitions.Animals = new List<AnimalDefinition>();
                return;
            }
            
            var animalData = JsonSerializer.Deserialize<AnimalData>(jsonContent, options);
            
            if (animalData?.Animals == null)
            {
                System.Console.WriteLine("Warning: No animals found in JSON or deserialization failed");
                _definitions.Animals = new List<AnimalDefinition>();
                return;
            }
            
            _definitions.Animals = animalData.Animals;
            System.Console.WriteLine($"Successfully loaded {_definitions.Animals.Count} animals");
            
            // Validate loaded data
            foreach (var animal in _definitions.Animals)
            {
                if (string.IsNullOrEmpty(animal.Id))
                    System.Console.WriteLine($"Warning: Animal missing ID");
                if (animal.Sprite == null)
                    System.Console.WriteLine($"Warning: Animal '{animal.Id}' missing sprite definition");
            }
        }
        catch (JsonException jsonEx)
        {
            System.Console.WriteLine($"JSON parsing error in animals file: {jsonEx.Message}");
            System.Console.WriteLine($"JSON error path: {jsonEx.Path}");
            _definitions.Animals = new List<AnimalDefinition>();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Unexpected error loading animals: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _definitions.Animals = new List<AnimalDefinition>();
        }
    }
    
    private void LoadMonsters(JsonSerializerOptions options)
    {
        string jsonPath = Path.Combine(_content.RootDirectory, "data", "poi_monsters.json");
        System.Console.WriteLine($"Loading monsters from: {jsonPath}");
        
        try
        {
            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"Warning: Monsters file not found at {jsonPath}");
                _definitions.Monsters = new List<MonsterDefinition>();
                return;
            }
            
            string jsonContent = File.ReadAllText(jsonPath);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                System.Console.WriteLine("Warning: Monsters file is empty");
                _definitions.Monsters = new List<MonsterDefinition>();
                return;
            }
            
            var monsterData = JsonSerializer.Deserialize<MonsterData>(jsonContent, options);
            
            if (monsterData?.Monsters == null)
            {
                System.Console.WriteLine("Warning: No monsters found in JSON or deserialization failed");
                _definitions.Monsters = new List<MonsterDefinition>();
                return;
            }
            
            _definitions.Monsters = monsterData.Monsters;
            System.Console.WriteLine($"Successfully loaded {_definitions.Monsters.Count} monsters");
            
            // Validate loaded data
            foreach (var monster in _definitions.Monsters)
            {
                if (string.IsNullOrEmpty(monster.Id))
                    System.Console.WriteLine($"Warning: Monster missing ID");
                if (monster.Sprite == null)
                    System.Console.WriteLine($"Warning: Monster '{monster.Id}' missing sprite definition");
            }
        }
        catch (JsonException jsonEx)
        {
            System.Console.WriteLine($"JSON parsing error in monsters file: {jsonEx.Message}");
            System.Console.WriteLine($"JSON error path: {jsonEx.Path}");
            _definitions.Monsters = new List<MonsterDefinition>();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Unexpected error loading monsters: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _definitions.Monsters = new List<MonsterDefinition>();
        }
    }
    
    private void LoadConfig(JsonSerializerOptions options)
    {
        string jsonPath = Path.Combine(_content.RootDirectory, "data", "poi_config.json");
        System.Console.WriteLine($"Loading config from: {jsonPath}");
        
        try
        {
            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"Warning: Config file not found at {jsonPath}");
                _definitions.BiomeSpawnRates = new Dictionary<string, BiomeSpawnConfig>();
                _definitions.RarityWeights = new Dictionary<string, float>();
                return;
            }
            
            string jsonContent = File.ReadAllText(jsonPath);

            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                System.Console.WriteLine("Warning: Config file is empty");
                _definitions.BiomeSpawnRates = new Dictionary<string, BiomeSpawnConfig>();
                _definitions.RarityWeights = new Dictionary<string, float>();
                return;
            }
            
            var configData = JsonSerializer.Deserialize<ConfigData>(jsonContent, options);
            
            if (configData == null)
            {
                System.Console.WriteLine("Warning: Config deserialization failed");
                _definitions.BiomeSpawnRates = new Dictionary<string, BiomeSpawnConfig>();
                _definitions.RarityWeights = new Dictionary<string, float>();
                return;
            }
            
            _definitions.BiomeSpawnRates = configData.BiomeSpawnRates ?? new Dictionary<string, BiomeSpawnConfig>();
            _definitions.RarityWeights = configData.RarityWeights ?? new Dictionary<string, float>();
            
            System.Console.WriteLine($"Successfully loaded config with {_definitions.BiomeSpawnRates.Count} biome configs and {_definitions.RarityWeights.Count} rarity weights");
            
            // Validate biome spawn rates
            foreach (var kvp in _definitions.BiomeSpawnRates)
            {
                if (kvp.Value == null)
                    System.Console.WriteLine($"Warning: Biome '{kvp.Key}' has null spawn config");
            }
        }
        catch (JsonException jsonEx)
        {
            System.Console.WriteLine($"JSON parsing error in config file: {jsonEx.Message}");
            System.Console.WriteLine($"JSON error path: {jsonEx.Path}");
            _definitions.BiomeSpawnRates = new Dictionary<string, BiomeSpawnConfig>();
            _definitions.RarityWeights = new Dictionary<string, float>();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Unexpected error loading config: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _definitions.BiomeSpawnRates = new Dictionary<string, BiomeSpawnConfig>();
            _definitions.RarityWeights = new Dictionary<string, float>();
        }
    }

    public List<GatherablePlantDefinition> GetGatherablePlantsForBiome(BiomeType biome)
    {
        if (_definitions?.GatherablePlants == null) return new List<GatherablePlantDefinition>();
        
        return _definitions.GatherablePlants
            .Where(plant => plant.Biomes.Contains(biome.ToString()))
            .ToList();
    }

    public GatherablePlantDefinition GetPlantDefinition(PoIType poiType)
    {
        if (_definitions?.GatherablePlants == null) return null;
        
        return _definitions.GatherablePlants
            .FirstOrDefault(plant => plant.Type == poiType.ToString());
    }

    public BiomeSpawnConfig GetBiomeSpawnConfig(BiomeType biome)
    {
        if (_definitions?.BiomeSpawnRates == null) 
            return new BiomeSpawnConfig { BasePOICount = 1.0f, GatherableMultiplier = 1.0f };
        
        if (_definitions.BiomeSpawnRates.TryGetValue(biome.ToString(), out var config))
            return config;
        
        return new BiomeSpawnConfig { BasePOICount = 1.0f, GatherableMultiplier = 1.0f };
    }

    public List<BuildingDefinition> GetBuildingsForBiome(BiomeType biome)
    {
        if (_definitions?.Buildings == null) return new List<BuildingDefinition>();
        
        return _definitions.Buildings
            .Where(building => building.Biomes.Contains(biome.ToString()))
            .ToList();
    }
    
    public List<NPCDefinition> GetNPCsForBiome(BiomeType biome)
    {
        if (_definitions?.NPCs == null) return new List<NPCDefinition>();
        
        return _definitions.NPCs
            .Where(npc => npc.Biomes.Contains(biome.ToString()))
            .ToList();
    }
    
    public BuildingDefinition GetBuildingDefinition(PoIType poiType)
    {
        if (_definitions?.Buildings == null) return null;
        
        return _definitions.Buildings
            .FirstOrDefault(building => building.Type == poiType.ToString());
    }
    
    public NPCDefinition GetNPCDefinition(PoIType poiType)
    {
        if (_definitions?.NPCs == null) return null;
        
        return _definitions.NPCs
            .FirstOrDefault(npc => npc.Type == poiType.ToString());
    }
    
    public List<AnimalDefinition> GetAnimalsForBiome(BiomeType biome)
    {
        if (_definitions?.Animals == null) return new List<AnimalDefinition>();
        
        return _definitions.Animals
            .Where(animal => animal.Biomes.Contains(biome.ToString()))
            .ToList();
    }
    
    public AnimalDefinition GetAnimalDefinition(PoIType poiType)
    {
        if (_definitions?.Animals == null) return null;
        
        return _definitions.Animals
            .FirstOrDefault(animal => animal.Type == poiType.ToString());
    }
    
    public List<MonsterDefinition> GetMonstersForBiome(BiomeType biome)
    {
        if (_definitions?.Monsters == null) return new List<MonsterDefinition>();
        
        return _definitions.Monsters
            .Where(monster => monster.Biomes.Contains(biome.ToString()))
            .ToList();
    }
    
    public MonsterDefinition GetMonsterDefinition(PoIType poiType)
    {
        if (_definitions?.Monsters == null) return null;
        
        return _definitions.Monsters
            .FirstOrDefault(monster => monster.Type == poiType.ToString());
    }

    public PoIDefinitions GetDefinitions() => _definitions;
}

public class PoIDefinitions
{
    public List<GatherablePlantDefinition> GatherablePlants { get; set; } = new();
    public List<BuildingDefinition> Buildings { get; set; } = new();
    public List<NPCDefinition> NPCs { get; set; } = new();
    public List<AnimalDefinition> Animals { get; set; } = new();
    public List<MonsterDefinition> Monsters { get; set; } = new();
    public Dictionary<string, BiomeSpawnConfig> BiomeSpawnRates { get; set; } = new();
    public Dictionary<string, float> RarityWeights { get; set; } = new();
}

// Container classes for JSON deserialization
public class GatherableData
{
    [JsonPropertyName("gatherable_plants")]
    public List<GatherablePlantDefinition> GatherablePlants { get; set; } = new();
}

public class BuildingData
{
    [JsonPropertyName("buildings")]
    public List<BuildingDefinition> Buildings { get; set; } = new();
}

public class NPCData
{
    [JsonPropertyName("npcs")]
    public List<NPCDefinition> NPCs { get; set; } = new();
}

public class ConfigData
{
    [JsonPropertyName("biome_spawn_rates")]
    public Dictionary<string, BiomeSpawnConfig> BiomeSpawnRates { get; set; } = new();
    
    [JsonPropertyName("rarity_weights")]
    public Dictionary<string, float> RarityWeights { get; set; } = new();
}

public class GatherablePlantDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("sprite")]
    public SpriteDefinition Sprite { get; set; }
    
    [JsonPropertyName("biomes")]
    public List<string> Biomes { get; set; } = new();
    
    [JsonPropertyName("spawn_weight")]
    public float SpawnWeight { get; set; }
    
    [JsonPropertyName("harvest")]
    public HarvestDefinition Harvest { get; set; }
}

public class SpriteDefinition
{
    [JsonPropertyName("sheet")]
    public string Sheet { get; set; }
    
    [JsonPropertyName("x")]
    public int X { get; set; }
    
    [JsonPropertyName("y")]
    public int Y { get; set; }
}

public class HarvestDefinition
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; }
    
    [JsonPropertyName("min_quantity")]
    public int MinQuantity { get; set; }
    
    [JsonPropertyName("max_quantity")]
    public int MaxQuantity { get; set; }
    
    [JsonPropertyName("success_chance")]
    public float SuccessChance { get; set; }
}

public class BuildingDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("sprite")]
    public SpriteDefinition Sprite { get; set; }
    
    [JsonPropertyName("biomes")]
    public List<string> Biomes { get; set; } = new();
    
    [JsonPropertyName("spawn_weight")]
    public float SpawnWeight { get; set; }
    
    [JsonPropertyName("interaction_type")]
    public string InteractionType { get; set; }
    
    [JsonPropertyName("services")]
    public List<string> Services { get; set; } = new();
    
    [JsonPropertyName("lighting")]
    public LightingDefinition Lighting { get; set; }
}

public class NPCDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("sprite")]
    public SpriteDefinition Sprite { get; set; }
    
    [JsonPropertyName("biomes")]
    public List<string> Biomes { get; set; } = new();
    
    [JsonPropertyName("spawn_weight")]
    public float SpawnWeight { get; set; }
    
    [JsonPropertyName("interaction_type")]
    public string InteractionType { get; set; }
    
    [JsonPropertyName("dialogue_topics")]
    public List<string> DialogueTopics { get; set; } = new();
    
    [JsonPropertyName("services")]
    public List<string> Services { get; set; } = new();
    
    [JsonPropertyName("lighting")]
    public LightingDefinition Lighting { get; set; }
}

public class LightingDefinition
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    
    [JsonPropertyName("color")]
    public string Color { get; set; }
    
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
    
    [JsonPropertyName("intensity")]
    public float Intensity { get; set; }
}

public class BiomeSpawnConfig
{
    [JsonPropertyName("base_poi_count")]
    public float BasePOICount { get; set; }
    
    [JsonPropertyName("gatherable_multiplier")]
    public float GatherableMultiplier { get; set; }
    
    [JsonPropertyName("building_multiplier")]
    public float BuildingMultiplier { get; set; }
    
    [JsonPropertyName("npc_multiplier")]
    public float NPCMultiplier { get; set; }
}

// Container classes for animals and monsters
public class AnimalData
{
    [JsonPropertyName("animals")]
    public List<AnimalDefinition> Animals { get; set; } = new();
}

public class MonsterData
{
    [JsonPropertyName("monsters")]
    public List<MonsterDefinition> Monsters { get; set; } = new();
}

public class AnimalDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("sprite")]
    public SpriteDefinition Sprite { get; set; }
    
    [JsonPropertyName("biomes")]
    public List<string> Biomes { get; set; } = new();
    
    [JsonPropertyName("spawn_weight")]
    public float SpawnWeight { get; set; }
    
    [JsonPropertyName("interaction_type")]
    public string InteractionType { get; set; }
    
    [JsonPropertyName("interaction_message")]
    public string InteractionMessage { get; set; }
    
    [JsonPropertyName("behavior")]
    public string Behavior { get; set; }
    
    [JsonPropertyName("loot")]
    public LootDefinition Loot { get; set; }
}

public class MonsterDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("sprite")]
    public SpriteDefinition Sprite { get; set; }
    
    [JsonPropertyName("biomes")]
    public List<string> Biomes { get; set; } = new();
    
    [JsonPropertyName("spawn_weight")]
    public float SpawnWeight { get; set; }
    
    [JsonPropertyName("interaction_type")]
    public string InteractionType { get; set; }
    
    [JsonPropertyName("interaction_message")]
    public string InteractionMessage { get; set; }
    
    [JsonPropertyName("threat_level")]
    public string ThreatLevel { get; set; }
    
    [JsonPropertyName("hostile")]
    public bool Hostile { get; set; }
}

public class LootDefinition
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; }
    
    [JsonPropertyName("chance")]
    public float Chance { get; set; }
}