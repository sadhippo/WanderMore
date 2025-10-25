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
    private List<Quest> _availableQuests;
    private Dictionary<string, Quest> _questLookup;
    private QuestContext _questContext;
    
    // System references
    private JournalManager _journalManager;
    private WeatherManager _weatherManager;
    private PoIManager _poiManager;
    private TimeManager _timeManager;
    private StatsManager _statsManager;
    private string _questDataPath;
    
    // Events
    public event Action<Quest> QuestStarted;
    public event Action<Quest> QuestCompleted;
    public event Action<Quest, QuestObjective> ObjectiveCompleted;
    public event Action<Quest> QuestAvailable;
    public event Action<Quest, QuestBranch> QuestBranched;
    
    public QuestManager(JournalManager journalManager, WeatherManager weatherManager, 
                       PoIManager poiManager, TimeManager timeManager, StatsManager statsManager,
                       string questDataPath = "Content/data/quests.json")
    {
        _journalManager = journalManager;
        _weatherManager = weatherManager;
        _poiManager = poiManager;
        _timeManager = timeManager;
        _statsManager = statsManager;
        _questDataPath = questDataPath;
        
        _allQuests = new List<Quest>();
        _activeQuests = new List<Quest>();
        _completedQuests = new List<Quest>();
        _availableQuests = new List<Quest>();
        _questLookup = new Dictionary<string, Quest>();
        
        // Initialize quest context
        _questContext = new QuestContext();
        
        // Subscribe to events from other systems
        _weatherManager.WeatherChanged += OnWeatherChanged;
        _poiManager.PoIInteracted += OnPoIInteracted;
        _timeManager.DayChanged += OnDayChanged;
        _timeManager.TimeOfDayChanged += OnTimeOfDayChanged;
        
        // Load quests from JSON
        LoadQuestsFromFile();
    }
    
    private void LoadQuestsFromFile()
    {
        try
        {
            _allQuests = QuestDataLoader.LoadQuestsFromJson(_questDataPath);
            
            // Build quest lookup dictionary
            _questLookup.Clear();
            foreach (var quest in _allQuests)
            {
                if (_timeManager != null)
                {
                    quest.GameDayStarted = _timeManager.CurrentDay;
                }
                _questLookup[quest.Id.ToString()] = quest;
            }
            
            // Initial quest availability check (only if context is ready)
            if (_questContext != null && _timeManager != null && _statsManager != null && _weatherManager != null)
            {
                UpdateQuestAvailability();
            }
            
            System.Console.WriteLine($"Quest system initialized with {_allQuests.Count} total quests");
            System.Console.WriteLine($"Available quests: {_availableQuests.Count}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load quest data: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            System.Console.WriteLine("Quest system will continue with no quests available");
        }
    }
    
    public void Update()
    {
        try
        {
            // Update quest context with current game state
            UpdateQuestContext();
            
            // Check for newly available quests
            UpdateQuestAvailability();
            
            // Check for quest branch opportunities
            CheckQuestBranches();
            
            // Update active quest objectives
            UpdateActiveQuests();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[QUEST] Error during quest update: {ex.Message}");
        }
    }
    
    private void UpdateQuestContext()
    {
        if (_questContext == null || _statsManager?.CurrentStats == null || 
            _timeManager == null || _weatherManager == null)
            return;
            
        _questContext.PlayerStats = _statsManager.CurrentStats;
        _questContext.CurrentTimeOfDay = _timeManager.CurrentTimeOfDay;
        _questContext.CurrentWeather = _weatherManager.CurrentWeather;
        _questContext.CurrentGameDay = _timeManager.CurrentDay;
        
        // Update completed quests
        _questContext.CompletedQuests.Clear();
        foreach (var quest in _completedQuests)
        {
            _questContext.CompletedQuests.Add(quest.Id.ToString());
        }
    }
    
    private void UpdateQuestAvailability()
    {
        if (_questContext == null || _allQuests == null || _availableQuests == null)
        {
            System.Console.WriteLine("[QUEST] Cannot update quest availability - missing components");
            return;
        }
        
        try
        {
            var newlyAvailable = new List<Quest>();
            
            foreach (var quest in _allQuests)
            {
                if (quest.Status == QuestStatus.NotStarted || quest.Status == QuestStatus.Locked)
                {
                    if (quest.CanStart(_questContext))
                    {
                        if (quest.Status == QuestStatus.NotStarted)
                        {
                            quest.Status = QuestStatus.Available;
                            _availableQuests.Add(quest);
                            newlyAvailable.Add(quest);
                        }
                        else if (quest.Status == QuestStatus.Locked)
                        {
                            quest.Status = QuestStatus.Available;
                            if (!_availableQuests.Contains(quest))
                            {
                                _availableQuests.Add(quest);
                                newlyAvailable.Add(quest);
                            }
                        }
                    }
                    else if (quest.Status == QuestStatus.Available)
                    {
                        // Quest is no longer available due to changed conditions
                        quest.Status = QuestStatus.Locked;
                        _availableQuests.Remove(quest);
                    }
                }
            }
            
            // Notify about newly available quests
            foreach (var quest in newlyAvailable)
            {
                QuestAvailable?.Invoke(quest);
                System.Console.WriteLine($"Quest now available: {quest.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[QUEST] Error updating quest availability: {ex.Message}");
        }
    }
    
    private void CheckQuestBranches()
    {
        foreach (var quest in _activeQuests.ToList())
        {
            if (quest.IsCompleted())
            {
                var availableBranches = quest.GetAvailableBranches(_questContext);
                
                if (availableBranches.Any())
                {
                    // For now, take the first available branch
                    // In a full implementation, you might want player choice here
                    var selectedBranch = availableBranches.First();
                    ExecuteQuestBranch(quest, selectedBranch);
                }
            }
        }
    }
    
    private void ExecuteQuestBranch(Quest quest, QuestBranch branch)
    {
        // Apply branch reward
        if (branch.Reward != null)
        {
            ApplyQuestReward(branch.Reward);
        }
        
        // Unlock next quests
        foreach (var nextQuestId in branch.NextQuestIds)
        {
            if (_questLookup.TryGetValue(nextQuestId, out var nextQuest))
            {
                if (nextQuest.Status == QuestStatus.NotStarted)
                {
                    nextQuest.Status = QuestStatus.Available;
                    _availableQuests.Add(nextQuest);
                }
            }
        }
        
        // Set quest outcome
        quest.SetOutcome(QuestOutcome.Branched);
        
        // Fire event
        QuestBranched?.Invoke(quest, branch);
        
        System.Console.WriteLine($"Quest branched: {quest.Name} -> {branch.Name}");
    }
    
    private void UpdateActiveQuests()
    {
        foreach (var quest in _activeQuests.ToList())
        {
            if (quest.IsCompleted() && quest.Outcome == null)
            {
                CompleteQuest(quest, QuestOutcome.Success);
            }
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
        // Update quest context with PoI visit
        OnPoIVisited(poi.Type);
        
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
        // Update quest context
        _questContext.CurrentWeather = newWeather;
        
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
        
        // Check if weather change affects quest availability
        UpdateQuestAvailability();
    }
    
    private void OnDayChanged(int newDay)
    {
        _questContext.CurrentGameDay = newDay;
        UpdateQuestAvailability();
    }
    
    private void OnTimeOfDayChanged(TimeOfDay timeOfDay)
    {
        _questContext.CurrentTimeOfDay = timeOfDay;
        UpdateQuestAvailability();
    }
    
    public void OnBiomeVisited(BiomeType biome)
    {
        _questContext.VisitedBiomes.Add(biome);
        UpdateQuestAvailability();
    }
    
    public void OnPoIVisited(PoIType poiType)
    {
        _questContext.VisitedPoIs.Add(poiType);
        UpdateQuestAvailability();
    }
    
    public void MakeStoryChoice(string choiceId, object choiceValue)
    {
        _questContext.StoryChoices[choiceId] = choiceValue;
        UpdateQuestAvailability();
        
        _journalManager.OnSpecialEvent("Story Choice", 
            $"Made a significant decision that may affect future opportunities.");
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
    
    private void CompleteQuest(Quest quest, QuestOutcome outcome = QuestOutcome.Success)
    {
        quest.SetOutcome(outcome);
        
        _activeQuests.Remove(quest);
        _completedQuests.Add(quest);
        
        // Apply rewards based on outcome
        if (quest.Rewards.TryGetValue(outcome, out var reward))
        {
            ApplyQuestReward(reward);
        }
        
        // Unlock next quests in chain
        UnlockNextQuests(quest);
        
        QuestCompleted?.Invoke(quest);
        
        // Add journal entry for quest completion
        var outcomeText = outcome switch
        {
            QuestOutcome.Success => "successfully completed",
            QuestOutcome.AlternativeSuccess => "completed through an alternative path",
            QuestOutcome.Failure => "failed to complete",
            QuestOutcome.Branched => "completed and led to new opportunities",
            _ => "completed"
        };
        
        _journalManager.OnSpecialEvent($"Quest {outcomeText}: {quest.Name}", 
            $"The quest given by {quest.QuestGiverName} has been {outcomeText}. {quest.RewardDescription}");
        
        System.Console.WriteLine($"Quest {outcomeText}: {quest.Name}");
    }
    
    private void ApplyQuestReward(QuestReward reward)
    {
        // Apply stat bonuses
        foreach (var statBonus in reward.StatBonuses)
        {
            // This would integrate with your stats system
            System.Console.WriteLine($"Gained {statBonus.Value} {statBonus.Key}");
        }
        
        // Unlock quests
        foreach (var questId in reward.UnlockedQuests)
        {
            if (_questLookup.TryGetValue(questId, out var unlockedQuest))
            {
                if (unlockedQuest.Status == QuestStatus.NotStarted)
                {
                    unlockedQuest.Status = QuestStatus.Available;
                    _availableQuests.Add(unlockedQuest);
                }
            }
        }
        
        // Handle custom rewards
        foreach (var customReward in reward.CustomRewards)
        {
            System.Console.WriteLine($"Custom reward: {customReward.Key} = {customReward.Value}");
        }
    }
    
    private void UnlockNextQuests(Quest completedQuest)
    {
        // Unlock direct next quests
        foreach (var nextQuestId in completedQuest.NextQuestIds)
        {
            if (_questLookup.TryGetValue(nextQuestId, out var nextQuest))
            {
                if (nextQuest.Status == QuestStatus.NotStarted)
                {
                    nextQuest.Status = QuestStatus.Available;
                    _availableQuests.Add(nextQuest);
                }
            }
        }
        
        // Unlock alternative quests
        foreach (var altQuestId in completedQuest.AlternativeQuestIds)
        {
            if (_questLookup.TryGetValue(altQuestId, out var altQuest))
            {
                if (altQuest.Status == QuestStatus.NotStarted)
                {
                    altQuest.Status = QuestStatus.Available;
                    _availableQuests.Add(altQuest);
                }
            }
        }
    }
    
    // Public query methods
    public List<Quest> GetActiveQuests() => _activeQuests.ToList();
    public List<Quest> GetCompletedQuests() => _completedQuests.ToList();
    public List<Quest> GetAvailableQuests() => _availableQuests.ToList();
    public int GetActiveQuestCount() => _activeQuests.Count;
    public int GetCompletedQuestCount() => _completedQuests.Count;
    public int GetAvailableQuestCount() => _availableQuests.Count;
    
    public Quest GetQuestById(Guid id) => _allQuests.FirstOrDefault(q => q.Id == id);
    public Quest GetQuestById(string id) => _questLookup.GetValueOrDefault(id);
    
    public List<Quest> GetQuestsByType(QuestType type) => _allQuests.Where(q => q.Type == type).ToList();
    public List<Quest> GetQuestsByChain(string chainId) => _allQuests.Where(q => q.ChainId == chainId).ToList();
    public List<Quest> GetQuestsByGiver(PoIType giver) => _allQuests.Where(q => q.QuestGiver == giver).ToList();
    
    public bool CanStartQuest(Quest quest) => quest.CanStart(_questContext);
    public bool IsQuestCompleted(string questId) => _questContext.CompletedQuests.Contains(questId);
    
    public QuestContext GetQuestContext() => _questContext;
    
    /// <summary>
    /// Processes dialogue outcomes including quest progression and rewards
    /// </summary>
    /// <param name="questUpdate">Quest ID to update</param>
    /// <param name="progressType">Type of progress (complete, advance, etc.)</param>
    public void ProcessDialogueOutcome(string questUpdate, string progressType = "complete")
    {
        try
        {
            if (string.IsNullOrEmpty(questUpdate))
                return;
                
            var quest = GetQuestById(questUpdate);
            if (quest == null)
            {
                System.Console.WriteLine($"[QuestManager] Quest not found for dialogue outcome: {questUpdate}");
                return;
            }
            
            switch (progressType.ToLower())
            {
                case "complete":
                    CompleteQuest(quest);
                    System.Console.WriteLine($"[QuestManager] Quest completed via dialogue: {quest.Name}");
                    break;
                    
                case "advance":
                case "progress":
                    // Advance the first incomplete objective
                    var incompleteObjective = quest.Objectives.FirstOrDefault(o => !o.IsCompleted);
                    if (incompleteObjective != null)
                    {
                        // Simple progress increment - this may need adjustment based on QuestObjective structure
                        System.Console.WriteLine($"[QuestManager] Quest objective advanced via dialogue: {quest.Name}");
                    }
                    break;
                    
                case "start":
                case "activate":
                    if (!_activeQuests.Contains(quest) && !_completedQuests.Contains(quest))
                    {
                        StartQuest(quest);
                        System.Console.WriteLine($"[QuestManager] Quest started via dialogue: {quest.Name}");
                    }
                    break;
                    
                default:
                    System.Console.WriteLine($"[QuestManager] Unknown dialogue progress type: {progressType}");
                    break;
            }
            
            // Add journal entry about quest progress
            _journalManager?.OnSpecialEvent("Quest Progress", 
                $"Made progress on quest '{quest.Name}' through dialogue.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[QuestManager] Error processing dialogue outcome: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Processes quest rewards and applies them to the player
    /// </summary>
    /// <param name="quest">Quest that provides the rewards</param>
    /// <param name="outcome">Quest outcome to determine which rewards to apply</param>
    public void ProcessQuestRewards(Quest quest, QuestOutcome outcome = QuestOutcome.Success)
    {
        try
        {
            if (quest?.Rewards == null || !quest.Rewards.Any())
                return;
                
            if (quest.Rewards.TryGetValue(outcome, out var reward))
            {
                ApplyQuestReward(reward);
                System.Console.WriteLine($"[QuestManager] Applied reward for quest: {quest.Name} (outcome: {outcome})");
            }
            else
            {
                System.Console.WriteLine($"[QuestManager] No reward found for quest: {quest.Name} with outcome: {outcome}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[QuestManager] Error processing quest rewards: {ex.Message}");
        }
    }
    

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