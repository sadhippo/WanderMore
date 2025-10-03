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
                var quest = new Quest
                {
                    Id = Guid.NewGuid(),
                    Name = questJson.Name,
                    Description = questJson.Description,
                    Type = Enum.Parse<QuestType>(questJson.Type),
                    QuestGiver = Enum.Parse<PoIType>(questJson.QuestGiver),
                    QuestGiverName = questJson.QuestGiverName,
                    RewardDescription = questJson.RewardDescription,
                    Status = QuestStatus.NotStarted
                };
                
                // Convert objectives
                foreach (var objJson in questJson.Objectives)
                {
                    var objective = new QuestObjective(
                        objJson.Id,
                        objJson.Description,
                        Enum.Parse<QuestObjectiveType>(objJson.Type)
                    );
                    
                    // Convert parameters
                    foreach (var param in objJson.Parameters)
                    {
                        object value = param.Value;
                        
                        // Convert JsonElement to string first, then to enums
                        if (param.Key == "weather_type")
                        {
                            string weatherStr = value.ToString();
                            value = Enum.Parse<WeatherType>(weatherStr);
                        }
                        else if (param.Key == "poi_type")
                        {
                            string poiStr = value.ToString();
                            value = Enum.Parse<PoIType>(poiStr);
                        }
                        else if (param.Key == "biome_type")
                        {
                            string biomeStr = value.ToString();
                            value = Enum.Parse<BiomeType>(biomeStr);
                        }
                        else if (param.Key == "biome_type" && value is string biomeStr)
                        {
                            value = Enum.Parse<BiomeType>(biomeStr);
                        }
                        
                        objective.Parameters[param.Key] = value;
                    }
                    
                    quest.Objectives.Add(objective);
                }
                
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
}

// JSON data structure classes
public class QuestDataFile
{
    public List<QuestJson> Quests { get; set; } = new List<QuestJson>();
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
    public List<QuestObjectiveJson> Objectives { get; set; } = new List<QuestObjectiveJson>();
}

public class QuestObjectiveJson
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}