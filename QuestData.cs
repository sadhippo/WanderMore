using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HiddenHorizons;

public class QuestDataLoader
{
    public static List<Quest> LoadQuestsFromJson(string filePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);
            var questData = JsonSerializer.Deserialize<QuestDataFile>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });
            
            var quests = new List<Quest>();
            
            foreach (var questJson in questData.Quests)
            {
                var quest = CreateQuestFromJson(questJson);
                quests.Add(quest);
            }
            
            System.Console.WriteLine($"Loaded {quests.Count} quests from {filePath}");
            return quests;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load quests from {filePath}: {ex.Message}");
            return new List<Quest>();
        }
    }
    
    private static Quest CreateQuestFromJson(QuestJson questJson)
    {
        var quest = new Quest
        {
            Id = Guid.NewGuid(),
            Name = questJson.Name,
            Description = questJson.Description,
            Type = Enum.Parse<QuestType>(questJson.Type),
            QuestGiver = string.IsNullOrEmpty(questJson.QuestGiver) ? null : Enum.Parse<PoIType>(questJson.QuestGiver),
            QuestGiverName = questJson.QuestGiverName,
            RewardDescription = questJson.RewardDescription,
            Status = QuestStatus.NotStarted,
            Priority = questJson.Priority,
            IsRepeatable = questJson.IsRepeatable,
            MaxRepetitions = questJson.MaxRepetitions,
            ChainId = questJson.ChainId,
            ChainOrder = questJson.ChainOrder,
            NextQuestIds = questJson.NextQuestIds ?? new List<string>(),
            AlternativeQuestIds = questJson.AlternativeQuestIds ?? new List<string>()
        };
        
        // Load prerequisites
        foreach (var prereqJson in questJson.Prerequisites ?? new List<QuestPrerequisiteJson>())
        {
            var prerequisite = new QuestPrerequisite
            {
                Id = prereqJson.Id,
                Type = Enum.Parse<PrerequisiteType>(prereqJson.Type),
                IsRequired = prereqJson.IsRequired,
                Parameters = ConvertParameters(prereqJson.Parameters)
            };
            quest.Prerequisites.Add(prerequisite);
        }
        
        // Load activation conditions
        foreach (var condJson in questJson.ActivationConditions ?? new List<QuestConditionJson>())
        {
            var condition = new QuestCondition
            {
                Id = condJson.Id,
                Description = condJson.Description,
                Type = Enum.Parse<PrerequisiteType>(condJson.Type),
                Parameters = ConvertParameters(condJson.Parameters)
            };
            quest.ActivationConditions.Add(condition);
        }
        
        // Load objectives
        foreach (var objJson in questJson.Objectives)
        {
            var objective = new QuestObjective(
                objJson.Id,
                objJson.Description,
                Enum.Parse<QuestObjectiveType>(objJson.Type)
            );
            
            objective.Parameters = ConvertParameters(objJson.Parameters);
            quest.Objectives.Add(objective);
        }
        
        // Load branches
        foreach (var branchJson in questJson.Branches ?? new List<QuestBranchJson>())
        {
            var branch = new QuestBranch
            {
                Id = branchJson.Id,
                Name = branchJson.Name,
                Description = branchJson.Description,
                NextQuestIds = branchJson.NextQuestIds ?? new List<string>(),
                BranchData = branchJson.BranchData ?? new Dictionary<string, object>()
            };
            
            // Load branch conditions
            foreach (var condJson in branchJson.Conditions ?? new List<QuestConditionJson>())
            {
                var condition = new QuestCondition
                {
                    Id = condJson.Id,
                    Description = condJson.Description,
                    Type = Enum.Parse<PrerequisiteType>(condJson.Type),
                    Parameters = ConvertParameters(condJson.Parameters)
                };
                branch.Conditions.Add(condition);
            }
            
            // Load branch reward
            if (branchJson.Reward != null)
            {
                branch.Reward = new QuestReward
                {
                    Description = branchJson.Reward.Description,
                    StatBonuses = branchJson.Reward.StatBonuses ?? new Dictionary<string, int>(),
                    UnlockedQuests = branchJson.Reward.UnlockedQuests ?? new List<string>(),
                    CustomRewards = branchJson.Reward.CustomRewards ?? new Dictionary<string, object>()
                };
            }
            
            quest.Branches.Add(branch);
        }
        
        // Load multiple rewards
        foreach (var rewardKvp in questJson.Rewards ?? new Dictionary<string, QuestRewardJson>())
        {
            var outcome = Enum.Parse<QuestOutcome>(rewardKvp.Key);
            var reward = new QuestReward
            {
                Description = rewardKvp.Value.Description,
                StatBonuses = rewardKvp.Value.StatBonuses ?? new Dictionary<string, int>(),
                UnlockedQuests = rewardKvp.Value.UnlockedQuests ?? new List<string>(),
                CustomRewards = rewardKvp.Value.CustomRewards ?? new Dictionary<string, object>()
            };
            quest.Rewards[outcome] = reward;
        }
        
        return quest;
    }
    
    private static Dictionary<string, object> ConvertParameters(Dictionary<string, object> parameters)
    {
        var converted = new Dictionary<string, object>();
        
        foreach (var param in parameters)
        {
            object value = param.Value;
            
            // Handle JsonElement conversion
            if (value is JsonElement jsonElement)
            {
                value = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.GetInt32(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => jsonElement.ToString()
                };
            }
            
            // Convert string values to appropriate enum types
            if (param.Key.EndsWith("_type") && value is string valueStr)
            {
                value = param.Key switch
                {
                    "weather_type" => Enum.Parse<WeatherType>(valueStr),
                    "poi_type" => Enum.Parse<PoIType>(valueStr),
                    "biome_type" => Enum.Parse<BiomeType>(valueStr),
                    "time_of_day" => Enum.Parse<TimeOfDay>(valueStr),
                    _ => valueStr
                };
            }
            
            converted[param.Key] = value;
        }
        
        return converted;
    }
}

// JSON data structure classes
public class QuestDataFile
{
    public List<QuestJson> Quests { get; set; } = new List<QuestJson>();
    public List<QuestChainJson> QuestChains { get; set; } = new List<QuestChainJson>();
    public List<QuestTemplateJson> QuestTemplates { get; set; } = new List<QuestTemplateJson>();
}

public class QuestJson
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string QuestGiver { get; set; }
    public string QuestGiverName { get; set; }
    public string RewardDescription { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsRepeatable { get; set; } = false;
    public int MaxRepetitions { get; set; } = 1;
    
    // Enhanced properties
    public string ChainId { get; set; }
    public int ChainOrder { get; set; }
    public List<string> NextQuestIds { get; set; } = new List<string>();
    public List<string> AlternativeQuestIds { get; set; } = new List<string>();
    
    // Prerequisites and conditions
    public List<QuestPrerequisiteJson> Prerequisites { get; set; } = new List<QuestPrerequisiteJson>();
    public List<QuestConditionJson> ActivationConditions { get; set; } = new List<QuestConditionJson>();
    
    // Objectives and branching
    public List<QuestObjectiveJson> Objectives { get; set; } = new List<QuestObjectiveJson>();
    public List<QuestBranchJson> Branches { get; set; } = new List<QuestBranchJson>();
    
    // Multiple rewards based on outcome
    public Dictionary<string, QuestRewardJson> Rewards { get; set; } = new Dictionary<string, QuestRewardJson>();
}

public class QuestObjectiveJson
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public bool IsOptional { get; set; } = false;
    public List<QuestConditionJson> Conditions { get; set; } = new List<QuestConditionJson>();
}

public class QuestPrerequisiteJson
{
    public string Id { get; set; }
    public string Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public bool IsRequired { get; set; } = true;
}

public class QuestConditionJson
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}

public class QuestBranchJson
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<QuestConditionJson> Conditions { get; set; } = new List<QuestConditionJson>();
    public List<string> NextQuestIds { get; set; } = new List<string>();
    public QuestRewardJson Reward { get; set; }
    public Dictionary<string, object> BranchData { get; set; } = new Dictionary<string, object>();
}

public class QuestRewardJson
{
    public string Description { get; set; }
    public Dictionary<string, int> StatBonuses { get; set; } = new Dictionary<string, int>();
    public List<string> UnlockedQuests { get; set; } = new List<string>();
    public Dictionary<string, object> CustomRewards { get; set; } = new Dictionary<string, object>();
}

public class QuestChainJson
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> QuestIds { get; set; } = new List<string>();
    public bool IsLinear { get; set; } = true;
    public Dictionary<string, object> ChainData { get; set; } = new Dictionary<string, object>();
}

public class QuestTemplateJson
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public List<QuestObjectiveJson> ObjectiveTemplates { get; set; } = new List<QuestObjectiveJson>();
    public Dictionary<string, List<string>> VariableOptions { get; set; } = new Dictionary<string, List<string>>();
}