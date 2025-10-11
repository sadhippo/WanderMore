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
    
    // Prerequisites and Conditions
    public List<QuestPrerequisite> Prerequisites { get; set; } = new List<QuestPrerequisite>();
    public List<QuestCondition> ActivationConditions { get; set; } = new List<QuestCondition>();
    
    // Objectives and Branching
    public List<QuestObjective> Objectives { get; set; } = new List<QuestObjective>();
    public List<QuestBranch> Branches { get; set; } = new List<QuestBranch>();
    
    // Rewards and Outcomes
    public string RewardDescription { get; set; }
    public Dictionary<QuestOutcome, QuestReward> Rewards { get; set; } = new Dictionary<QuestOutcome, QuestReward>();
    
    // Quest Chain Management
    public string ChainId { get; set; }
    public int ChainOrder { get; set; }
    public List<string> NextQuestIds { get; set; } = new List<string>();
    public List<string> AlternativeQuestIds { get; set; } = new List<string>();
    
    // Metadata
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public int GameDayStarted { get; set; }
    public QuestOutcome? Outcome { get; set; }
    public Dictionary<string, object> QuestData { get; set; } = new Dictionary<string, object>();
    
    // Priority and Availability
    public int Priority { get; set; } = 0;
    public bool IsRepeatable { get; set; } = false;
    public int MaxRepetitions { get; set; } = 1;
    public int CompletionCount { get; set; } = 0;
    
    public Quest()
    {
        Id = Guid.NewGuid();
        Status = QuestStatus.NotStarted;
    }
    
    public bool IsCompleted()
    {
        return Objectives.All(obj => obj.IsCompleted);
    }
    
    public bool CanStart(QuestContext context)
    {
        try
        {
            if (context == null) return false;
            
            return Prerequisites.All(prereq => prereq.IsMet(context)) &&
                   ActivationConditions.All(condition => condition.IsMet(context));
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[QUEST] Error checking if quest {Name} can start: {ex.Message}");
            return false;
        }
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
    
    public List<QuestBranch> GetAvailableBranches(QuestContext context)
    {
        return Branches.Where(branch => branch.CanActivate(context)).ToList();
    }
    
    public void SetOutcome(QuestOutcome outcome)
    {
        Outcome = outcome;
        Status = outcome switch
        {
            QuestOutcome.Success or QuestOutcome.AlternativeSuccess => QuestStatus.Completed,
            QuestOutcome.Failure => QuestStatus.Failed,
            QuestOutcome.Abandoned => QuestStatus.Abandoned,
            QuestOutcome.Branched => QuestStatus.Completed,
            _ => Status
        };
        CompletionTime = DateTime.Now;
        CompletionCount++;
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
    Survival,       // Future: survive conditions
    Story,          // Main story quests
    Side,           // Optional side quests
    Chain,          // Multi-part quest chains
    Branching       // Quests with multiple outcomes
}

public enum QuestStatus
{
    NotStarted,
    Available,      // Prerequisites met, can be started
    Active,
    Completed,
    Failed,
    Abandoned,
    Locked          // Prerequisites not met
}

public enum QuestObjectiveType
{
    VisitLocation,      // Visit specific PoI or zone
    WitnessWeather,     // See specific weather event
    TalkToNPC,          // Interact with specific NPC
    ExploreZone,        // Enter specific biome type
    WaitForTime,        // Wait for specific time/season
    CollectItem,        // Future: gather specific items
    ReachStat,          // Achieve certain stat level
    CompleteQuest,      // Complete another quest first
    Choice              // Make a story choice
}

public enum PrerequisiteType
{
    QuestCompleted,     // Another quest must be completed
    QuestNotCompleted,  // Another quest must NOT be completed
    StatMinimum,        // Player stat must be at least X
    StatMaximum,        // Player stat must be at most X
    TimeOfDay,          // Must be day/night
    Weather,            // Specific weather required
    Season,             // Specific season required
    GameDay,            // Must be after certain game day
    BiomeVisited,       // Must have visited specific biome
    PoIVisited,         // Must have visited specific PoI
    Choice              // Based on previous story choice
}

public enum QuestOutcome
{
    Success,            // Standard completion
    AlternativeSuccess, // Different but valid completion
    Failure,            // Quest failed
    Abandoned,          // Player abandoned
    Branched            // Led to different quest path
}

// Supporting classes for enhanced quest system
public class QuestPrerequisite
{
    public string Id { get; set; }
    public PrerequisiteType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public bool IsRequired { get; set; } = true;
    
    public bool IsMet(QuestContext context)
    {
        try
        {
            if (context == null) return false;
            
            return Type switch
            {
                PrerequisiteType.QuestCompleted => context.IsQuestCompleted(Parameters["quest_id"].ToString()),
                PrerequisiteType.QuestNotCompleted => !context.IsQuestCompleted(Parameters["quest_id"].ToString()),
                PrerequisiteType.StatMinimum => context.GetStatValue(Parameters["stat_name"].ToString()) >= (int)Parameters["min_value"],
                PrerequisiteType.StatMaximum => context.GetStatValue(Parameters["stat_name"].ToString()) <= (int)Parameters["max_value"],
                PrerequisiteType.TimeOfDay => context.CurrentTimeOfDay == (TimeOfDay)Parameters["time_of_day"],
                PrerequisiteType.Weather => context.CurrentWeather == (WeatherType)Parameters["weather_type"],
                PrerequisiteType.GameDay => context.CurrentGameDay >= (int)Parameters["min_day"],
                PrerequisiteType.BiomeVisited => context.HasVisitedBiome((BiomeType)Parameters["biome_type"]),
                PrerequisiteType.PoIVisited => context.HasVisitedPoI((PoIType)Parameters["poi_type"]),
                PrerequisiteType.Choice => context.GetChoiceValue(Parameters["choice_id"].ToString()) == Parameters["choice_value"],
                _ => true
            };
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[QUEST] Error checking prerequisite {Id} of type {Type}: {ex.Message}");
            return false;
        }
    }
}

public class QuestCondition
{
    public string Id { get; set; }
    public string Description { get; set; }
    public PrerequisiteType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    
    public bool IsMet(QuestContext context)
    {
        // Same logic as prerequisites but for ongoing conditions
        return new QuestPrerequisite { Type = Type, Parameters = Parameters }.IsMet(context);
    }
}

public class QuestBranch
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<QuestCondition> Conditions { get; set; } = new List<QuestCondition>();
    public List<string> NextQuestIds { get; set; } = new List<string>();
    public QuestReward Reward { get; set; }
    public Dictionary<string, object> BranchData { get; set; } = new Dictionary<string, object>();
    
    public bool CanActivate(QuestContext context)
    {
        return Conditions.All(condition => condition.IsMet(context));
    }
}

public class QuestReward
{
    public string Description { get; set; }
    public Dictionary<string, int> StatBonuses { get; set; } = new Dictionary<string, int>();
    public List<string> UnlockedQuests { get; set; } = new List<string>();
    public Dictionary<string, object> CustomRewards { get; set; } = new Dictionary<string, object>();
}

public class QuestContext
{
    public AdventurerStats PlayerStats { get; set; }
    public TimeOfDay CurrentTimeOfDay { get; set; }
    public WeatherType CurrentWeather { get; set; }
    public int CurrentGameDay { get; set; }
    public HashSet<string> CompletedQuests { get; set; } = new HashSet<string>();
    public HashSet<BiomeType> VisitedBiomes { get; set; } = new HashSet<BiomeType>();
    public HashSet<PoIType> VisitedPoIs { get; set; } = new HashSet<PoIType>();
    public Dictionary<string, object> StoryChoices { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> WorldState { get; set; } = new Dictionary<string, object>();
    
    public bool IsQuestCompleted(string questId) => CompletedQuests.Contains(questId);
    public bool HasVisitedBiome(BiomeType biome) => VisitedBiomes.Contains(biome);
    public bool HasVisitedPoI(PoIType poi) => VisitedPoIs.Contains(poi);
    public object GetChoiceValue(string choiceId) => StoryChoices.GetValueOrDefault(choiceId);
    
    public int GetStatValue(string statName)
    {
        if (PlayerStats == null) return 0;
        
        return statName.ToLower() switch
        {
            "level" => PlayerStats.Level,
            "experience" => (int)PlayerStats.Experience,
            "hunger" => (int)PlayerStats.Hunger,
            "tiredness" => (int)PlayerStats.Tiredness,
            "comfort" => (int)PlayerStats.Comfort,
            "mood" => (int)PlayerStats.Mood,
            _ => 0
        };
    }
}