using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class PointOfInterest
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
    
    // Interaction state
    public bool HasBeenInteracted { get; set; }
    public DateTime LastInteractionTime { get; set; }
    public int InteractionCount { get; set; }
    
    // Quest and story integration
    public List<string> AssociatedQuests { get; set; } = new List<string>();
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    
    // JSON data access
    private PoIDataLoader _dataLoader;
    
    // Collision
    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
    }
    
    public bool IsInRange(Vector2 otherPosition, float range)
    {
        return Vector2.Distance(Position, otherPosition) <= range;
    }
    
    // Set the data loader for JSON access
    public void SetDataLoader(PoIDataLoader dataLoader)
    {
        _dataLoader = dataLoader;
        
        // Update name and description from JSON data if available
        UpdateFromJsonData();
    }
    
    private void UpdateFromJsonData()
    {
        if (_dataLoader == null) return;
        
        // Try to get data from JSON definitions
        var plantDef = _dataLoader.GetPlantDefinition(Type);
        if (plantDef != null)
        {
            Name = plantDef.Name;
            Description = plantDef.Description;
            return;
        }
        
        var buildingDef = _dataLoader.GetBuildingDefinition(Type);
        if (buildingDef != null)
        {
            Name = buildingDef.Name;
            Description = buildingDef.Description;
            return;
        }
        
        var npcDef = _dataLoader.GetNPCDefinition(Type);
        if (npcDef != null)
        {
            Name = npcDef.Name;
            Description = npcDef.Description;
            return;
        }
        
        // Fallback to default names if no JSON data found
        if (string.IsNullOrEmpty(Name))
        {
            Name = GetFallbackName();
        }
        
        if (string.IsNullOrEmpty(Description))
        {
            Description = GetFallbackDescription();
        }
    }
    
    private string GetFallbackName()
    {
        return Type switch
        {
            PoIType.Ranger => "Forest Ranger",
            PoIType.Priest => "Wandering Priest",
            PoIType.Warrior => "Brave Knight",
            PoIType.Scholar => "Learned Scholar",
            PoIType.Hermit => "Wise Hermit",
            PoIType.Mermaid => "Lake Guardian",
            PoIType.ArchmageEldren => "Archmage Eldren",
            PoIType.ThrainStonehand => "Thrain Stonehand",
            PoIType.BrotherColren => "Brother Colren",
            PoIType.ElderMarnic => "Elder Marnic",
            PoIType.AxemanTorvik => "Axeman Torvik",
            PoIType.KnightRoland => "Knight Roland",
            PoIType.KnightAldric => "Knight Aldric",
            PoIType.SpellknightSerel => "Spellknight Serel",
            PoIType.MaraTheStrong => "Mara the Strong",
            PoIType.TillerHobben => "Tiller Hobben",
            PoIType.NoviceBran => "Novice Bran",
            PoIType.ShadowRell => "Shadow Rell",
            PoIType.SmithDorrik => "Smith Dorrik",
            PoIType.ArlenTheSwift => "Arlen the Swift",
            PoIType.MinerVorn => "Miner Vorn",
            PoIType.Farmer => "Village Farmer",
            PoIType.Inn => "Inn",
            PoIType.Cottage => "Cottage",
            PoIType.Castle => "Castle",
            PoIType.Mine => "Mine",
            PoIType.BerryBush => "Berry Bush",
            PoIType.HeartberryBush => "Heartberry Bush",
            PoIType.NightgrapeCluster => "Nightgrape Cluster",
            _ => Type.ToString()
        };
    }
    
    private string GetFallbackDescription()
    {
        return Type switch
        {
            PoIType.Ranger => "A skilled ranger who knows every path through the wilderness.",
            PoIType.Priest => "A holy wanderer offering blessings to travelers.",
            PoIType.Warrior => "A seasoned warrior who has seen many battles.",
            PoIType.Scholar => "A wise scholar researching ancient knowledge and mysteries.",
            PoIType.Hermit => "A wise hermit who speaks in riddles about the world's mysteries.",
            PoIType.Mermaid => "A mystical mermaid who sings haunting melodies.",
            PoIType.ArchmageEldren => "A powerful archmage wielding ancient magical knowledge.",
            PoIType.ThrainStonehand => "A master dwarf craftsman known for his stonework.",
            PoIType.BrotherColren => "A devoted monk spreading peace and wisdom.",
            PoIType.ElderMarnic => "An ancient elder with knowledge of forgotten times.",
            PoIType.AxemanTorvik => "A fierce warrior wielding a mighty battle axe.",
            PoIType.KnightRoland => "A noble knight upholding justice and honor.",
            PoIType.KnightAldric => "A seasoned knight with tales of many battles.",
            PoIType.SpellknightSerel => "A rare warrior who combines magic and swordplay.",
            PoIType.MaraTheStrong => "A powerful warrior known for her incredible strength.",
            PoIType.TillerHobben => "A hardworking farmer who knows the land well.",
            PoIType.NoviceBran => "A young apprentice eager to learn and help.",
            PoIType.ShadowRell => "A mysterious figure who moves like shadow itself.",
            PoIType.SmithDorrik => "A master blacksmith forging weapons and tools.",
            PoIType.ArlenTheSwift => "A nimble scout known for incredible speed and agility.",
            PoIType.MinerVorn => "A grizzled miner who knows every tunnel and vein.",
            PoIType.Farmer => "A humble farmer tending to crops and livestock.",
            PoIType.Inn => "A welcoming inn that provides rest for weary travelers.",
            PoIType.Cottage => "A peaceful dwelling where time seems to slow down.",
            PoIType.Castle => "An imposing castle that dominates the landscape.",
            PoIType.Mine => "An old mining operation with valuable resources.",
            PoIType.BerryBush => "A bush laden with fresh berries.",
            PoIType.HeartberryBush => "Sweet berries perfect for tarts and healing tonics.",
            PoIType.NightgrapeCluster => "Grapes that glow faintly at night, fermented into mana elixirs.",
            _ => $"A {Type.ToString().ToLower()} of interest."
        };
    }
    
    // Interaction methods for future expansion
    public virtual InteractionResult Interact(Adventurer adventurer)
    {
        HasBeenInteracted = true;
        LastInteractionTime = DateTime.Now;
        InteractionCount++;
        
        // Handle item collection for gatherable PoIs
        HandleItemCollection(adventurer);
        
        return new InteractionResult
        {
            Success = true,
            Message = GetInteractionMessage(),
            Type = GetInteractionType()
        };
    }
    
    protected virtual void HandleItemCollection(Adventurer adventurer)
    {
        // Only collect items on first interaction - all resources are one-time only
        if (InteractionCount > 1) 
        {

            return;
        }
        
        var random = new Random();
        
        // Try to use JSON harvest data first
        if (_dataLoader != null)
        {
            var plantDef = _dataLoader.GetPlantDefinition(Type);
            if (plantDef?.Harvest != null)
            {
                var harvest = plantDef.Harvest;
                
                // Check success chance
                if (random.NextDouble() <= harvest.SuccessChance)
                {
                    int quantity = random.Next(harvest.MinQuantity, harvest.MaxQuantity + 1);
                    adventurer.CollectItem(harvest.ItemId, quantity);
                    System.Console.WriteLine($"[POI] Collected {quantity} {harvest.ItemId} from {plantDef.Name}");
                }
                else
                {

                }
                return;
            }
        }
        
        // Fallback to hardcoded harvest logic for non-gatherable PoIs or missing JSON data
        switch (Type)
        {
            case PoIType.BerryBush:
                int berryCount = random.Next(2, 6); // 2-5 berries
                adventurer.CollectItem("berries", berryCount);
                System.Console.WriteLine($"[POI] Collected {berryCount} berries from bush");
                break;
                
            case PoIType.Chicken:
                // 50% chance to get an egg
                if (random.NextDouble() < 0.5)
                {
                    adventurer.CollectItem("egg", 1);
                    System.Console.WriteLine("[POI] Collected 1 egg from chicken");
                }
                break;
                
            case PoIType.Mine:
                // Random chance for stone or rare minerals
                if (random.NextDouble() < 0.8) // 80% chance for stone
                {
                    int stoneCount = random.Next(1, 4);
                    adventurer.CollectItem("stone", stoneCount);
                    System.Console.WriteLine($"[POI] Collected {stoneCount} stone from mine");
                }
                if (random.NextDouble() < 0.1) // 10% chance for shiny stone
                {
                    adventurer.CollectItem("shiny_stone", 1);
                    System.Console.WriteLine("[POI] Found a shiny stone in the mine!");
                }
                break;
                
            case PoIType.Cottage:
                // Sometimes find herbs or flowers
                if (random.NextDouble() < 0.3) // 30% chance
                {
                    adventurer.CollectItem("herb", 1);
                    System.Console.WriteLine("[POI] Found medicinal herbs near the cottage");
                }
                break;
        }
    }
    

    
    protected virtual string GetInteractionMessage()
    {
        // Show different messages for harvested vs fresh resources
        if (IsGatherableResource() && InteractionCount > 1)
        {
            return $"The {Name?.ToLower() ?? Type.ToString().ToLower()} has already been harvested and is now bare.";
        }
        
        // Try to use JSON description first
        if (_dataLoader != null)
        {
            var plantDef = _dataLoader.GetPlantDefinition(Type);
            if (plantDef != null)
            {
                return plantDef.Description;
            }
            
            var buildingDef = _dataLoader.GetBuildingDefinition(Type);
            if (buildingDef != null)
            {
                return buildingDef.Description;
            }
            
            var npcDef = _dataLoader.GetNPCDefinition(Type);
            if (npcDef != null)
            {
                return npcDef.Description;
            }
        }
        
        // Fallback to hardcoded messages for types not in JSON or missing data loader
        return Type switch
        {
            PoIType.Cat => "The cat purrs contentedly and follows you for a short distance.",
            PoIType.Dog => "The friendly dog wags its tail and seems eager to play.",
            PoIType.Unicorn => "The majestic unicorn allows you to approach, a rare honor indeed.",
            PoIType.BerryBush => "You gather fresh berries from the bush, feeling nourished.",
            PoIType.Chicken => InteractionCount > 1 ? "The chicken clucks peacefully but has no more eggs to give." : "You carefully collect some fresh eggs from the friendly chicken.",
            PoIType.Mine => InteractionCount > 1 ? "The mine has been thoroughly explored and is now empty." : "The old mine echoes with memories of industrious days.",
            
            _ => Description ?? $"You examine the {Name?.ToLower() ?? Type.ToString().ToLower()} with curiosity."
        };
    }
    
    protected virtual bool IsGatherableResource()
    {
        return Type switch
        {
            PoIType.BerryBush or PoIType.HeartberryBush or PoIType.NightgrapeCluster or 
            PoIType.MoonblossomVine or PoIType.AmberfruiGrove or PoIType.SunappleTree or 
            PoIType.LemoraPlant or PoIType.CrimsonberryTree or PoIType.DuskwineFruit or 
            PoIType.TandemfruitTree or PoIType.TwintideVine or PoIType.PlumpFiglet or 
            PoIType.CobaltCherryTree or PoIType.FirepepperBush or PoIType.GoldbloomTree or 
            PoIType.AutumnthornShrub or PoIType.MandorTree or PoIType.SunburstTree or 
            PoIType.RoseappleTree or PoIType.BarrowfruitBarrel or PoIType.FrostberryBush or 
            PoIType.BanavaTree or PoIType.RedthornShrub or PoIType.VineheartBush or 
            PoIType.StarberryCluster or PoIType.MelonoakTree or PoIType.Mine or PoIType.Chicken => true,
            _ => false
        };
    }
    
    protected virtual InteractionType GetInteractionType()
    {
        return Type switch
        {
            PoIType.Ranger or PoIType.Priest or PoIType.Warrior or PoIType.Scholar or 
            PoIType.Hermit or PoIType.Adventurer or PoIType.Mermaid or
            PoIType.ArchmageEldren or PoIType.ThrainStonehand or PoIType.BrotherColren or
            PoIType.ElderMarnic or PoIType.AxemanTorvik or PoIType.KnightRoland or
            PoIType.KnightAldric or PoIType.SpellknightSerel or PoIType.MaraTheStrong or
            PoIType.TillerHobben or PoIType.NoviceBran or PoIType.ShadowRell or
            PoIType.SmithDorrik or PoIType.ArlenTheSwift or PoIType.MinerVorn or
            PoIType.Farmer => InteractionType.Conversation,
            
            PoIType.Inn or PoIType.Cottage or PoIType.Castle or PoIType.Chapel or 
            PoIType.Farmhouse or PoIType.Hut => InteractionType.Building,
            
            PoIType.Mine or PoIType.Oracle => InteractionType.Exploration,
            
            PoIType.Cat or PoIType.Dog or PoIType.Sheep or PoIType.Chicken or 
            PoIType.Pig or PoIType.Unicorn => InteractionType.Animal,
            
            PoIType.Skeleton or PoIType.Dragon or PoIType.Minotaur or 
            PoIType.Centaur or PoIType.Golem => InteractionType.Combat,
            
            // All gatherable plants are exploration type
            PoIType.BerryBush or PoIType.HeartberryBush or PoIType.NightgrapeCluster or 
            PoIType.MoonblossomVine or PoIType.AmberfruiGrove or PoIType.SunappleTree or 
            PoIType.LemoraPlant or PoIType.CrimsonberryTree or PoIType.DuskwineFruit or 
            PoIType.TandemfruitTree or PoIType.TwintideVine or PoIType.PlumpFiglet or 
            PoIType.CobaltCherryTree or PoIType.FirepepperBush or PoIType.GoldbloomTree or 
            PoIType.AutumnthornShrub or PoIType.MandorTree or PoIType.SunburstTree or 
            PoIType.RoseappleTree or PoIType.BarrowfruitBarrel or PoIType.FrostberryBush or 
            PoIType.BanavaTree or PoIType.RedthornShrub or PoIType.VineheartBush or 
            PoIType.StarberryCluster or PoIType.MelonoakTree => InteractionType.Exploration,
            
            _ => InteractionType.Examine
        };
    }
}

public enum PoIType
{
    // NPCs
    Ranger,
    Priest, 
    Warrior,
    Adventurer,
    Scholar,
    Hermit,
    Mermaid,
    
    // Additional NPCs from JSON
    ArchmageEldren,
    ThrainStonehand,
    BrotherColren,
    ElderMarnic,
    AxemanTorvik,
    KnightRoland,
    KnightAldric,
    SpellknightSerel,
    MaraTheStrong,
    TillerHobben,
    NoviceBran,
    ShadowRell,
    SmithDorrik,
    ArlenTheSwift,
    MinerVorn,
    Farmer,
    
    // Monsters
    Skeleton,
    Centaur,
    Dragon,
    Minotaur,
    Golem,
    
    // Animals
    Cat,
    Dog,
    Chicken,
    Pig,
    Lizard,
    Bat,
    Unicorn,
    Sheep,
    Deer, // Additional for forests
    
    // Buildings
    Farmhouse,
    Inn,
    Cottage,
    Hut,
    Castle,
    Chapel,
    Oracle,
    SkullFortress,
    HauntedHouse,
    TreeHouse,
    Mine,
    
    // Additional Buildings
    HouseA,
    CottageA,
    TownhousePumpkins,
    MushroomHouse,
    ForestCabin,
    CrystalShrine,
    LargeTavern,
    WitchHut,
    BlacksmithForge,
    Windmill,
    StorageShed,
    DeadTreeLandmark,
    BrokenTowerA,
    HouseB,
    HouseC,
    Stable,
    TombA,
    Well,
    HouseD,
    TowerRuin,
    BrokenTowerB,
    GoldArchwayA,
    GoldArchwayB,
    BrokenTowerC,
    CrystalGate,
    ManaObeliskA,
    ManaObeliskB,
    MagicPortal,
    
    // Resources
    BerryBush,
    
    // Gatherable Plants - Cooking
    HeartberryBush,
    AmberfruiGrove,
    LemoraPlant,
    PlumpFiglet,
    FirepepperBush,
    MandorTree,
    SunburstTree,
    BanavaTree,
    MelonoakTree,
    
    // Gatherable Plants - Alchemy
    NightgrapeCluster,
    MoonblossomVine,
    SunappleTree,
    CrimsonberryTree,
    DuskwineFruit,
    TandemfruitTree,
    TwintideVine,
    CobaltCherryTree,
    GoldbloomTree,
    AutumnthornShrub,
    RoseappleTree,
    BarrowfruitBarrel,
    FrostberryBush,
    RedthornShrub,
    VineheartBush,
    StarberryCluster
}

public enum InteractionType
{
    Conversation,
    Building,
    Exploration,
    Animal,
    Combat,
    Examine,
    Quest,
    Trade,
    Rest
}

public class InteractionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public InteractionType Type { get; set; }
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}