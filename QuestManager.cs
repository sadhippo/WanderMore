using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenHorizons;

public class QuestManager
{
    private List<Quest> _allQuests;
    private List<Quest> _activeQuests;
    private List<Quest> _completedQuests;
    private JournalManager _journalManager;
    private WeatherManager _weatherManager;
    private PoIManager _poiManager;
    private TimeManager _timeManager;
    private string _questDataPath;
    
    // Events
    public event Action<Quest> QuestStarted;
    public event Action<Quest> QuestCompleted;
    public event Action<Quest, QuestObjective> ObjectiveCompleted;
    
    public QuestManager(JournalManager journalManager, WeatherManager weatherManager, 
                       PoIManager poiManager, TimeManager timeManager, string questDataPath = "Content/data/quests.json")
    {
        _journalManager = journalManager;
        _weatherManager = weatherManager;
        _poiManager = poiManager;
        _timeManager = timeManager;
        _questDataPath = questDataPath;
        
        _allQuests = new List<Quest>();
        _activeQuests = new List<Quest>();
        _completedQuests = new List<Quest>();
        
        // Subscribe to events from other systems
        _weatherManager.WeatherChanged += OnWeatherChanged;
        _poiManager.PoIInteracted += OnPoIInteracted;
        
        // Load quests from JSON
        LoadQuestsFromFile();
    }
    
    private void LoadQuestsFromFile()
    {
        try
        {
            _allQuests = QuestDataLoader.LoadQuestsFromJson(_questDataPath);
            
            // Set the game day started for all loaded quests
            foreach (var quest in _allQuests)
            {
                quest.GameDayStarted = _timeManager.CurrentDay;
            }
            
            System.Console.WriteLine($"Quest system initialized with {_allQuests.Count} available quests");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load quest data: {ex.Message}");
            System.Console.WriteLine("Quest system will continue with no quests available");
        }
    }
    
    public void ReloadQuests()
    {
        // Useful for development - reload quests without restarting
        var previousActiveQuests = new List<Quest>(_activeQuests);
        var previousCompletedQuests = new List<Quest>(_completedQuests);
        
        LoadQuestsFromFile();
        
        // Try to restore active/completed quest states
        // This is a simple approach - in production you might want more sophisticated state management
        System.Console.WriteLine("Quests reloaded from file");
    }
    
    public void OnPoIInteracted(PointOfInterest poi, Adventurer adventurer)
    {
        // Check if this PoI can give quests
        TryGiveQuest(poi);
        
        // Check if this interaction completes any quest objectives
        CheckLocationObjectives(poi);
    }
    
    public void OnZoneEntered(Zone zone)
    {
        // Check if entering this zone completes any exploration objectives
        CheckZoneExplorationObjectives(zone);
    }
    
    private void TryGiveQuest(PointOfInterest poi)
    {
        // Find available quests from this quest giver
        var availableQuests = _allQuests.Where(q => 
            q.QuestGiver == poi.Type && 
            q.Status == QuestStatus.NotStarted &&
            !_activeQuests.Any(aq => aq.QuestGiver == poi.Type) // Don't give multiple quests from same NPC
        ).ToList();
        
        if (availableQuests.Any())
        {
            var quest = availableQuests.First();
            StartQuest(quest);
            
            // Add journal entry about receiving the quest
            _journalManager.OnSpecialEvent($"Quest Received: {quest.Name}", 
                $"{poi.Name} has given you a new quest: {quest.Description}");
        }
    }
    
    private void CheckLocationObjectives(PointOfInterest poi)
    {
        foreach (var quest in _activeQuests.ToList())
        {
            var locationObjectives = quest.Objectives.Where(obj => 
                !obj.IsCompleted && 
                obj.Type == QuestObjectiveType.VisitLocation
            ).ToList();
            
            foreach (var objective in locationObjectives)
            {
                if (objective.Parameters.ContainsKey("poi_type"))
                {
                    var requiredPoiType = (PoIType)objective.Parameters["poi_type"];
                    if (poi.Type == requiredPoiType)
                    {
                        CompleteObjective(quest, objective);
                    }
                }
            }
        }
    }
    
    private void CheckZoneExplorationObjectives(Zone zone)
    {
        foreach (var quest in _activeQuests.ToList())
        {
            var explorationObjectives = quest.Objectives.Where(obj => 
                !obj.IsCompleted && 
                obj.Type == QuestObjectiveType.ExploreZone
            ).ToList();
            
            foreach (var objective in explorationObjectives)
            {
                if (objective.Parameters.ContainsKey("biome_type"))
                {
                    var requiredBiome = (BiomeType)objective.Parameters["biome_type"];
                    if (zone.BiomeType == requiredBiome)
                    {
                        CompleteObjective(quest, objective);
                        
                        // Add special journal entry for exploration quest completion
                        _journalManager.OnSpecialEvent("Quest Progress", 
                            $"Explored {zone.BiomeType} biome as requested - quest objective completed!");
                    }
                }
            }
        }
    }
    
    private void OnWeatherChanged(WeatherType newWeather)
    {
        // Check if any active quests have weather objectives
        foreach (var quest in _activeQuests.ToList())
        {
            var weatherObjectives = quest.Objectives.Where(obj => 
                !obj.IsCompleted && 
                obj.Type == QuestObjectiveType.WitnessWeather
            ).ToList();
            
            foreach (var objective in weatherObjectives)
            {
                if (objective.Parameters.ContainsKey("weather_type"))
                {
                    var requiredWeather = objective.Parameters["weather_type"] is WeatherType weather ? weather : 
                                      Enum.Parse<WeatherType>(objective.Parameters["weather_type"].ToString());
                    if (newWeather == requiredWeather)
                    {
                        CompleteObjective(quest, objective);
                        
                        // Add special journal entry for weather quest completion
                        _journalManager.OnSpecialEvent("Quest Progress", 
                            $"Witnessed {newWeather} as requested - quest objective completed!");
                    }
                }
            }
        }
    }
    
    public void StartQuest(Quest quest)
    {
        quest.Status = QuestStatus.Active;
        quest.StartTime = DateTime.Now;
        quest.GameDayStarted = _timeManager.CurrentDay;
        
        _activeQuests.Add(quest);
        QuestStarted?.Invoke(quest);
        
        System.Console.WriteLine($"Quest Started: {quest.Name}");
    }
    
    private void CompleteObjective(Quest quest, QuestObjective objective)
    {
        quest.CompleteObjective(objective.Id);
        ObjectiveCompleted?.Invoke(quest, objective);
        
        System.Console.WriteLine($"Objective Completed: {objective.Description}");
        
        // Check if quest is now complete
        if (quest.IsCompleted())
        {
            CompleteQuest(quest);
        }
    }
    
    private void CompleteQuest(Quest quest)
    {
        quest.Status = QuestStatus.Completed;
        quest.CompletionTime = DateTime.Now;
        
        _activeQuests.Remove(quest);
        _completedQuests.Add(quest);
        
        QuestCompleted?.Invoke(quest);
        
        // Add journal entry for quest completion
        _journalManager.OnSpecialEvent($"Quest Completed: {quest.Name}", 
            $"Successfully completed the quest given by {quest.QuestGiverName}. {quest.RewardDescription}");
        
        System.Console.WriteLine($"Quest Completed: {quest.Name}");
    }
    
    // Public query methods
    public List<Quest> GetActiveQuests() => _activeQuests.ToList();
    public List<Quest> GetCompletedQuests() => _completedQuests.ToList();
    public int GetActiveQuestCount() => _activeQuests.Count;
    public int GetCompletedQuestCount() => _completedQuests.Count;
    
    public Quest GetQuestById(Guid id) => _allQuests.FirstOrDefault(q => q.Id == id);
    
    public void PrintQuestStatus()
    {
        System.Console.WriteLine($"=== Quest Status ===");
        System.Console.WriteLine($"Total Quests: {_allQuests.Count}");
        System.Console.WriteLine($"Active Quests: {_activeQuests.Count}");
        System.Console.WriteLine($"Completed Quests: {_completedQuests.Count}");
        
        if (_activeQuests.Any())
        {
            System.Console.WriteLine("Active Quests:");
            foreach (var quest in _activeQuests)
            {
                System.Console.WriteLine($"  - {quest.Name} (from {quest.QuestGiverName})");
                var currentObj = quest.GetCurrentObjective();
                if (currentObj != null)
                {
                    System.Console.WriteLine($"    Current: {currentObj.Description}");
                }
            }
        }
        
        if (_completedQuests.Any())
        {
            System.Console.WriteLine("Completed Quests:");
            foreach (var quest in _completedQuests)
            {
                System.Console.WriteLine($"  - {quest.Name}");
            }
        }
        System.Console.WriteLine($"==================");
    }
}