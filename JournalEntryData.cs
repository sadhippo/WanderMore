using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Content;

namespace HiddenHorizons;

public class JournalEntryData
{
    private static JournalEntryData _instance;
    private Dictionary<string, object> _entries;
    private Random _random;

    public static JournalEntryData Instance => _instance ??= new JournalEntryData();

    private JournalEntryData()
    {
        _random = new Random();
        _entries = new Dictionary<string, object>();
    }

    public void LoadContent(ContentManager content)
    {
        try
        {
            string jsonPath = Path.Combine(content.RootDirectory, "data", "journal_entries.json");
            string jsonContent = File.ReadAllText(jsonPath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            _entries = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, options);
            System.Console.WriteLine("Journal entries loaded successfully!");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load journal entries: {ex.Message}");
            // Initialize with empty dictionary to prevent crashes
            _entries = new Dictionary<string, object>();
        }
    }

    public string GetRandomTitle(string category, string subcategory = null)
    {
        try
        {
            var titles = GetTitles(category, subcategory);
            return titles.Count > 0 ? titles[_random.Next(titles.Count)] : "Personal Reflection";
        }
        catch
        {
            return "Personal Reflection";
        }
    }

    public string GetRandomMessage(string category, string subcategory = null, Dictionary<string, object> replacements = null)
    {
        try
        {
            var messages = GetMessages(category, subcategory);
            if (messages.Count == 0)
                return "Another day of adventure continues.";

            string message = messages[_random.Next(messages.Count)];
            
            // Apply replacements if provided
            if (replacements != null)
            {
                foreach (var replacement in replacements)
                {
                    message = message.Replace($"{{{replacement.Key}}}", replacement.Value.ToString());
                }
            }
            
            return message;
        }
        catch
        {
            return "Another day of adventure continues.";
        }
    }

    private List<string> GetTitles(string category, string subcategory = null)
    {
        var titles = new List<string>();
        
        try
        {
            if (_entries.TryGetValue(category, out var categoryData))
            {
                var categoryDict = JsonSerializer.Deserialize<Dictionary<string, object>>(categoryData.ToString());
                
                if (subcategory != null && categoryDict.TryGetValue(subcategory, out var subcategoryData))
                {
                    var subcategoryDict = JsonSerializer.Deserialize<Dictionary<string, object>>(subcategoryData.ToString());
                    if (subcategoryDict.TryGetValue("titles", out var titlesData))
                    {
                        titles = JsonSerializer.Deserialize<List<string>>(titlesData.ToString());
                    }
                }
                else if (categoryDict.TryGetValue("titles", out var titlesData))
                {
                    titles = JsonSerializer.Deserialize<List<string>>(titlesData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting titles for {category}/{subcategory}: {ex.Message}");
        }
        
        return titles ?? new List<string>();
    }

    private List<string> GetMessages(string category, string subcategory = null)
    {
        var messages = new List<string>();
        
        try
        {
            if (_entries.TryGetValue(category, out var categoryData))
            {
                var categoryDict = JsonSerializer.Deserialize<Dictionary<string, object>>(categoryData.ToString());
                
                if (subcategory != null && categoryDict.TryGetValue(subcategory, out var subcategoryData))
                {
                    var subcategoryDict = JsonSerializer.Deserialize<Dictionary<string, object>>(subcategoryData.ToString());
                    if (subcategoryDict.TryGetValue("messages", out var messagesData))
                    {
                        messages = JsonSerializer.Deserialize<List<string>>(messagesData.ToString());
                    }
                }
                else if (categoryDict.TryGetValue("messages", out var messagesData))
                {
                    messages = JsonSerializer.Deserialize<List<string>>(messagesData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting messages for {category}/{subcategory}: {ex.Message}");
        }
        
        return messages ?? new List<string>();
    }

    // Convenience methods for specific entry types
    public (string title, string message) GetLevelUpEntry(int level)
    {
        var replacements = new Dictionary<string, object> { { "level", level } };
        return (
            GetRandomTitle("levelUp"),
            GetRandomMessage("levelUp", null, replacements)
        );
    }

    public (string title, string message) GetExperienceMilestoneEntry()
    {
        return (
            GetRandomTitle("experienceMilestone"),
            GetRandomMessage("experienceMilestone")
        );
    }

    public (string title, string message) GetHungerEntry(bool isLow)
    {
        string subcategory = isLow ? "low" : "satisfied";
        return (
            GetRandomTitle("hunger", subcategory),
            GetRandomMessage("hunger", subcategory)
        );
    }

    public (string title, string message) GetTirednessEntry(bool isHigh)
    {
        string subcategory = isHigh ? "high" : "rested";
        return (
            GetRandomTitle("tiredness", subcategory),
            GetRandomMessage("tiredness", subcategory)
        );
    }

    public (string title, string message) GetMoodEntry(bool isHigh)
    {
        string subcategory = isHigh ? "high" : "low";
        return (
            GetRandomTitle("mood", subcategory),
            GetRandomMessage("mood", subcategory)
        );
    }

    public (string title, string message) GetWeatherEntry(bool isPositive)
    {
        string subcategory = isPositive ? "positive" : "negative";
        return (
            GetRandomTitle("weather", subcategory),
            GetRandomMessage("weather", subcategory)
        );
    }

    public (string title, string message) GetPoIEntry(string poiType)
    {
        string subcategory = poiType.ToLower();
        return (
            GetRandomTitle("poi", subcategory),
            GetRandomMessage("poi", subcategory)
        );
    }

    public (string title, string message) GetPersonalReflectionEntry(bool mixedConditions = false)
    {
        string subcategory = mixedConditions ? "mixed_conditions" : "general";
        return (
            GetRandomTitle("personalReflection", subcategory),
            GetRandomMessage("personalReflection", subcategory)
        );
    }

    public (string title, string message) GetDailySummaryEntry(string summaryType, string conditions = null)
    {
        var replacements = conditions != null ? new Dictionary<string, object> { { "conditions", conditions } } : null;
        return (
            GetRandomTitle("dailySummary", summaryType),
            GetRandomMessage("dailySummary", summaryType, replacements)
        );
    }
}