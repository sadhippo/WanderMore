using System.Collections.Generic;

namespace HiddenHorizons;

/// <summary>
/// Centralized helper for PoI interaction logic to avoid duplication between systems
/// Uses HashSets for better performance and maintainability
/// </summary>
public static class PoIInteractionHelper
{
    // Define interactable PoI types in organized sets for better maintainability
    private static readonly HashSet<PoIType> InteractableBuildings = new()
    {
        PoIType.Inn, PoIType.Cottage, PoIType.Farmhouse, PoIType.Castle, PoIType.Chapel,
        PoIType.Hut, PoIType.Mine, PoIType.Oracle, PoIType.TreeHouse, PoIType.SkullFortress, 
        PoIType.HauntedHouse,
        
        // Additional Buildings
        PoIType.HouseA, PoIType.CottageA, PoIType.TownhousePumpkins, PoIType.MushroomHouse,
        PoIType.ForestCabin, PoIType.CrystalShrine, PoIType.LargeTavern, PoIType.WitchHut,
        PoIType.BlacksmithForge, PoIType.Windmill, PoIType.StorageShed, PoIType.DeadTreeLandmark,
        PoIType.BrokenTowerA, PoIType.HouseB, PoIType.HouseC, PoIType.Stable, PoIType.TombA,
        PoIType.Well, PoIType.HouseD, PoIType.TowerRuin, PoIType.BrokenTowerB,
        PoIType.GoldArchwayA, PoIType.GoldArchwayB, PoIType.BrokenTowerC, PoIType.CrystalGate,
        PoIType.ManaObeliskA, PoIType.ManaObeliskB, PoIType.MagicPortal
    };
    
    private static readonly HashSet<PoIType> InteractableNPCs = new()
    {
        PoIType.Ranger, PoIType.Priest, PoIType.Warrior, PoIType.Scholar, PoIType.Hermit,
        PoIType.Adventurer, PoIType.Mermaid
    };
    
    private static readonly HashSet<PoIType> InteractableAnimals = new()
    {
        PoIType.Cat, PoIType.Dog, PoIType.Unicorn, PoIType.Sheep, PoIType.Chicken,
        PoIType.Pig, PoIType.Deer, PoIType.Lizard, PoIType.Bat
    };
    
    private static readonly HashSet<PoIType> InteractableResources = new()
    {
        // Original resource
        PoIType.BerryBush,
        
        // All gatherable plants from JSON data
        PoIType.HeartberryBush, PoIType.NightgrapeCluster, PoIType.MoonblossomVine,
        PoIType.AmberfruiGrove, PoIType.SunappleTree, PoIType.LemoraPlant,
        PoIType.CrimsonberryTree, PoIType.DuskwineFruit, PoIType.TandemfruitTree,
        PoIType.TwintideVine, PoIType.PlumpFiglet, PoIType.CobaltCherryTree,
        PoIType.FirepepperBush, PoIType.GoldbloomTree, PoIType.AutumnthornShrub,
        PoIType.MandorTree, PoIType.SunburstTree, PoIType.RoseappleTree,
        PoIType.BarrowfruitBarrel, PoIType.FrostberryBush, PoIType.BanavaTree,
        PoIType.RedthornShrub, PoIType.VineheartBush, PoIType.StarberryCluster,
        PoIType.MelonoakTree
    };
    
    // Dangerous/non-interactable PoI types
    private static readonly HashSet<PoIType> DangerousPoIs = new()
    {
        PoIType.Skeleton, PoIType.Dragon, PoIType.Minotaur, PoIType.Golem, PoIType.Centaur
    };

    /// <summary>
    /// Determines if a PoI type is interactable by the adventurer
    /// </summary>
    public static bool IsInteractablePoI(PoIType poiType)
    {
        // Check if it's explicitly dangerous/non-interactable
        if (DangerousPoIs.Contains(poiType))
            return false;
        
        // Check if it's in any of our interactable categories
        return InteractableBuildings.Contains(poiType) ||
               InteractableNPCs.Contains(poiType) ||
               InteractableAnimals.Contains(poiType) ||
               InteractableResources.Contains(poiType);
    }
}