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
    
    // Collision
    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
    }
    
    public bool IsInRange(Vector2 otherPosition, float range)
    {
        return Vector2.Distance(Position, otherPosition) <= range;
    }
    
    // Interaction methods for future expansion
    public virtual InteractionResult Interact(Adventurer adventurer)
    {
        HasBeenInteracted = true;
        LastInteractionTime = DateTime.Now;
        InteractionCount++;
        
        return new InteractionResult
        {
            Success = true,
            Message = GetInteractionMessage(),
            Type = GetInteractionType()
        };
    }
    
    protected virtual string GetInteractionMessage()
    {
        return Type switch
        {
            PoIType.Ranger => "The ranger shares knowledge of safe paths through the wilderness.",
            PoIType.Priest => "The priest offers a blessing for your journey ahead.",
            PoIType.Warrior => "The warrior tells tales of distant battles and adventures.",
            PoIType.Scholar => "The scholar shares ancient wisdom and forgotten lore.",
            PoIType.Hermit => "The hermit speaks in riddles about the mysteries of the world.",
            PoIType.Mermaid => "The mermaid sings a haunting melody about the depths below.",
            PoIType.Inn => "You rest at the inn, feeling refreshed and ready to continue.",
            PoIType.Cottage => "A peaceful dwelling where time seems to slow down.",
            PoIType.Castle => "The ancient castle holds secrets of a bygone era.",
            PoIType.Mine => "The old mine echoes with memories of industrious days.",
            PoIType.Cat => "The cat purrs contentedly and follows you for a short distance.",
            PoIType.Dog => "The friendly dog wags its tail and seems eager to play.",
            PoIType.Unicorn => "The majestic unicorn allows you to approach, a rare honor indeed.",
            _ => $"You examine the {Type.ToString().ToLower()} with curiosity."
        };
    }
    
    protected virtual InteractionType GetInteractionType()
    {
        return Type switch
        {
            PoIType.Ranger or PoIType.Priest or PoIType.Warrior or PoIType.Scholar or 
            PoIType.Hermit or PoIType.Adventurer or PoIType.Mermaid => InteractionType.Conversation,
            
            PoIType.Inn or PoIType.Cottage or PoIType.Castle or PoIType.Chapel or 
            PoIType.Farmhouse or PoIType.Hut => InteractionType.Building,
            
            PoIType.Mine or PoIType.Oracle => InteractionType.Exploration,
            
            PoIType.Cat or PoIType.Dog or PoIType.Sheep or PoIType.Chicken or 
            PoIType.Pig or PoIType.Unicorn => InteractionType.Animal,
            
            PoIType.Skeleton or PoIType.Dragon or PoIType.Minotaur or 
            PoIType.Centaur or PoIType.Golem => InteractionType.Combat,
            
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
    Mine
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