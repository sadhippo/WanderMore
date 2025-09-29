# Quest System Integration Documentation

## Overview

This document describes the integration patterns and examples for the quest system within the Hidden Horizons save/load architecture. The quest system is designed to be extensible, maintainable, and seamlessly integrated with the existing save system.

## Architecture Overview

The quest system follows the established ISaveable pattern used throughout the game, ensuring consistent save/load behavior and easy integration with the SaveManager.

### Core Components

1. **QuestManager**: Main quest system controller implementing ISaveable
2. **QuestSaveData**: Root save data structure containing all quest state
3. **QuestInstanceSaveData**: Individual quest instance data
4. **QuestObjectiveSaveData**: Quest objective progress tracking
5. **QuestChainProgressSaveData**: Quest chain and storyline progression

## Integration Patterns

### 1. ISaveable Implementation Pattern

The QuestManager implements the ISaveable interface following the established pattern:

```csharp
public class QuestManager : ISaveable
{
    public string SaveKey => "QuestManager";
    public int SaveVersion => 1;
    
    public object GetSaveData()
    {
        return new QuestSaveData
        {
            ActiveQuests = _activeQuests.Values.ToList(),
            CompletedQuestIds = _completedQuests.ToList(),
            QuestVariables = new Dictionary<string, object>(_globalQuestVariables),
            // ... other data
        };
    }
    
    public void LoadSaveData(object data)
    {
        if (data is not QuestSaveData questData)
            throw new ArgumentException("Invalid save data type");
            
        // Clear existing state
        _activeQuests.Clear();
        _completedQuests.Clear();
        
        // Load saved state
        foreach (var quest in questData.ActiveQuests)
        {
            _activeQuests[quest.QuestId] = quest;
        }
        // ... load other data
    }
}
```

### 2. SaveManager Integration Pattern

The SaveManager automatically handles quest system serialization through:

```csharp
// In SaveManager.DeserializeSystemData method
"QuestManager" => JsonSerializer.Deserialize<QuestSaveData>(jsonElement.GetRawText(), _jsonOptions),
```

### 3. Auto-Save Integration Pattern

Quest system events trigger automatic saves:

```csharp
// In SaveManager.SubscribeToAutoSaveTriggers method
if (questManager != null && _autoSaveConfig.SaveOnSignificantEvents)
{
    questManager.QuestCompleted += (sender, args) =>
    {
        TriggerAutoSave(AutoSaveTrigger.SignificantEvent, $"Quest completed: {args.Quest.QuestTemplateId}");
    };
}
```

### 4. Event-Driven Architecture Pattern

The quest system uses events for loose coupling:

```csharp
public class QuestManager
{
    public event EventHandler<QuestEventArgs>? QuestStarted;
    public event EventHandler<QuestEventArgs>? QuestCompleted;
    public event EventHandler<QuestObjectiveEventArgs>? ObjectiveUpdated;
    public event EventHandler? QuestDataChanged;
}
```

## Integration Examples

### Example 1: Basic Quest System Setup

```csharp
// In Game1.cs or main game initialization
public class Game1 : Game
{
    private SaveManager _saveManager;
    private QuestManager _questManager;
    
    protected override void LoadContent()
    {
        // Initialize managers
        _questManager = new QuestManager();
        _saveManager = new SaveManager();
        
        // Register quest system with save manager
        _saveManager.RegisterSaveable(_questManager);
        
        // Subscribe to auto-save triggers
        _saveManager.SubscribeToAutoSaveTriggers(
            questManager: _questManager
        );
    }
}
```

### Example 2: Quest Creation and Management

```csharp
// Starting a new quest
public void StartExplorationQuest()
{
    var questId = _questManager.StartQuest("explore_forest_biome");
    
    // Set up quest objectives
    _questManager.UpdateObjectiveProgress(questId, "visit_forest", 0);
    _questManager.UpdateObjectiveProgress(questId, "find_ancient_tree", 0);
}

// Updating quest progress
public void OnBiomeEntered(BiomeType biome)
{
    if (biome == BiomeType.Forest)
    {
        // Find active exploration quests and update progress
        foreach (var quest in _questManager.ActiveQuests)
        {
            if (quest.QuestTemplateId == "explore_forest_biome")
            {
                _questManager.UpdateObjectiveProgress(quest.QuestId, "visit_forest", 1);
            }
        }
    }
}
```

### Example 3: Quest Chain Management

```csharp
// Managing quest chains
public void HandleQuestCompletion(QuestInstanceSaveData completedQuest)
{
    switch (completedQuest.QuestTemplateId)
    {
        case "explore_forest_biome":
            // Start next quest in exploration chain
            _questManager.StartQuest("explore_mountain_biome");
            break;
            
        case "find_ancient_artifacts":
            // Update chain progress
            _questManager.SetQuestVariable("artifacts_found", 
                (int)_questManager.GetQuestVariable("artifacts_found") + 1);
            break;
    }
}
```

### Example 4: Integration with Other Systems

```csharp
// Journal system integration
public class JournalManager
{
    private QuestManager _questManager;
    
    public void OnNewEntryAdded(JournalEntry entry)
    {
        // Check if this entry completes any quest objectives
        if (entry.Type == JournalEntryType.Discovery)
        {
            foreach (var quest in _questManager.ActiveQuests)
            {
                if (quest.QuestTemplateId.Contains("discovery"))
                {
                    _questManager.UpdateObjectiveProgress(quest.QuestId, "make_discovery", 1);
                }
            }
        }
    }
}

// PoI system integration
public class PoIManager
{
    private QuestManager _questManager;
    
    public void OnPoIDiscovered(PointOfInterest poi)
    {
        // Update quest progress for location-based quests
        foreach (var quest in _questManager.ActiveQuests)
        {
            if (quest.QuestState.ContainsKey("target_poi_type") && 
                quest.QuestState["target_poi_type"].Equals(poi.Type.ToString()))
            {
                _questManager.UpdateObjectiveProgress(quest.QuestId, "find_poi", 1);
            }
        }
    }
}
```

## Save Data Structure Examples

### Complete Quest Save Data

```json
{
  "ActiveQuests": [
    {
      "QuestId": "12345678-1234-1234-1234-123456789abc",
      "QuestTemplateId": "explore_forest_biome",
      "Status": "Active",
      "StartTime": "2024-01-15T10:30:00Z",
      "Objectives": {
        "visit_forest": {
          "ObjectiveId": "visit_forest",
          "IsCompleted": true,
          "CurrentProgress": 1,
          "TargetProgress": 1
        },
        "find_ancient_tree": {
          "ObjectiveId": "find_ancient_tree",
          "IsCompleted": false,
          "CurrentProgress": 0,
          "TargetProgress": 1
        }
      },
      "QuestState": {
        "forest_zones_visited": ["forest_01", "forest_02"],
        "trees_examined": 3
      },
      "CurrentStep": 1,
      "Priority": 1
    }
  ],
  "CompletedQuestIds": [
    "87654321-4321-4321-4321-cba987654321"
  ],
  "QuestVariables": {
    "player_reputation": 150,
    "artifacts_found": 2,
    "biomes_explored": 4
  },
  "ChainProgress": [
    {
      "ChainId": "exploration_chain",
      "CurrentChainStep": 2,
      "IsChainCompleted": false,
      "ChainVariables": {
        "chain_start_day": 5,
        "bonus_rewards_earned": true
      }
    }
  ],
  "FailedQuestIds": [],
  "LastUpdated": "2024-01-15T14:45:00Z"
}
```

## Best Practices

### 1. Event Handling

- Use events for loose coupling between systems
- Always check for null event handlers before invoking
- Provide meaningful event arguments with relevant data

### 2. Save Data Design

- Keep save data structures simple and serializable
- Use dictionaries for flexible, extensible data storage
- Include version information for future migration support

### 3. Error Handling

- Implement graceful degradation for quest system failures
- Log errors without breaking the save/load process
- Provide fallback behavior for corrupted quest data

### 4. Performance Considerations

- Avoid frequent auto-saves for minor quest updates
- Use delta saves for large quest datasets
- Implement lazy loading for quest templates and data

### 5. Extensibility

- Design quest data structures to accommodate future quest types
- Use template-based quest definitions for flexibility
- Implement plugin-style architecture for custom quest behaviors

## Testing Patterns

### Unit Test Example

```csharp
[Test]
public void QuestManager_SaveLoad_PreservesQuestState()
{
    // Arrange
    var questManager = new QuestManager();
    var questId = questManager.StartQuest("test_quest");
    questManager.UpdateObjectiveProgress(questId, "test_objective", 5);
    questManager.SetQuestVariable("test_var", "test_value");
    
    // Act - Save
    var saveData = questManager.GetSaveData();
    
    // Create new instance and load
    var newQuestManager = new QuestManager();
    newQuestManager.LoadSaveData(saveData);
    
    // Assert
    Assert.AreEqual(1, newQuestManager.ActiveQuestCount);
    Assert.AreEqual("test_value", newQuestManager.GetQuestVariable("test_var"));
    
    var loadedQuest = newQuestManager.GetQuest(questId);
    Assert.IsNotNull(loadedQuest);
    Assert.AreEqual("test_quest", loadedQuest.QuestTemplateId);
}
```

## Migration and Versioning

### Version Migration Example

```csharp
public class QuestSaveDataMigration
{
    public static QuestSaveData MigrateFromV1ToV2(QuestSaveData v1Data)
    {
        // Example: Add new fields introduced in version 2
        foreach (var quest in v1Data.ActiveQuests)
        {
            if (!quest.QuestState.ContainsKey("priority"))
            {
                quest.Priority = 0; // Default priority for old quests
            }
        }
        
        return v1Data;
    }
}
```

This integration documentation provides a comprehensive guide for implementing and extending the quest system within the existing save/load architecture, ensuring consistency and maintainability.