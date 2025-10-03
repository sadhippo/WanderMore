using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class Quest
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public QuestType Type { get; set; }
    public QuestStatus Status { get; set; }
    public PoIType? QuestGiver { get; set; }
    public string QuestGiverName { get; set; }
    
    // Objectives
    public List<QuestObjective> Objectives { get; set; } = new List<QuestObjective>();
    
    // Rewards
    public string RewardDescription { get; set; }
    
    // Metadata
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public int GameDayStarted { get; set; }
    
    public Quest()
    {
        Id = Guid.NewGuid();
        Status = QuestStatus.NotStarted;
    }
    
    public bool IsCompleted()
    {
        return Objectives.All(obj => obj.IsCompleted);
    }
    
    public void CompleteObjective(string objectiveId)
    {
        var objective = Objectives.FirstOrDefault(o => o.Id == objectiveId);
        if (objective != null)
        {
            objective.IsCompleted = true;
            objective.CompletionTime = DateTime.Now;
        }
    }
    
    public QuestObjective GetCurrentObjective()
    {
        return Objectives.FirstOrDefault(o => !o.IsCompleted);
    }
}

public class QuestObjective
{
    public string Id { get; set; }
    public string Description { get; set; }
    public QuestObjectiveType Type { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletionTime { get; set; }
    
    // Objective-specific data
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    
    public QuestObjective(string id, string description, QuestObjectiveType type)
    {
        Id = id;
        Description = description;
        Type = type;
        IsCompleted = false;
    }
}

public enum QuestType
{
    Exploration,    // Visit locations
    Observation,    // Witness events (weather, etc.)
    Interaction,    // Talk to NPCs
    Collection,     // Future: gather items
    Survival        // Future: survive conditions
}

public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
    Failed,
    Abandoned
}

public enum QuestObjectiveType
{
    VisitLocation,      // Visit specific PoI or zone
    WitnessWeather,     // See specific weather event
    TalkToNPC,          // Interact with specific NPC
    ExploreZone,        // Enter specific biome type
    WaitForTime         // Wait for specific time/season
}