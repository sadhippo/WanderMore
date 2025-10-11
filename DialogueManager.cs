using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HiddenHorizons;

/// <summary>
/// Manages dialogue system including loading, conversation flow, and outcome processing
/// </summary>
public class DialogueManager
{
    // System References
    private QuestManager _questManager;
    private StatsManager _statsManager;
    private JournalManager _journalManager;
    private AssetManager _assetManager;
    
    // Dialogue Data
    private Dictionary<string, DialogueTree> _dialogueTrees;
    private Dictionary<string, DialogueNode> _allDialogueNodes;
    
    // State Management
    private ConversationState _currentConversation;
    private EventCardBox _eventCardBox;
    
    // Events
    public event Action<DialogueNode> DialogueTriggered;
    public event Action ConversationEnded;
    public event Action<DialogueChoice> ChoiceSelected;
    public event Action ConversationStarted;
    
    public DialogueManager(QuestManager questManager, StatsManager statsManager, 
                          JournalManager journalManager, AssetManager assetManager,
                          EventCardBox eventCardBox)
    {
        _questManager = questManager;
        _statsManager = statsManager;
        _journalManager = journalManager;
        _assetManager = assetManager;
        _eventCardBox = eventCardBox;
        
        _dialogueTrees = new Dictionary<string, DialogueTree>();
        _allDialogueNodes = new Dictionary<string, DialogueNode>();
        _currentConversation = null;
        
        // Subscribe to EventCardBox events
        if (_eventCardBox != null)
        {
            _eventCardBox.ChoiceSelected += OnChoiceSelected;
            _eventCardBox.DialogueClosed += OnDialogueClosed;
        }
        
        System.Console.WriteLine("[DialogueManager] Initialized");
    }
    
    /// <summary>
    /// Loads dialogue data from JSON file
    /// </summary>
    /// <param name="dataPath">Path to dialogue JSON file</param>
    public void LoadDialogueData(string dataPath = "Content/data/dialogues.json")
    {
        try
        {
            _dialogueTrees = DialogueDataLoader.LoadDialogues(dataPath);
            
            // Build flat lookup dictionary for all nodes
            _allDialogueNodes.Clear();
            foreach (var tree in _dialogueTrees.Values)
            {
                foreach (var node in tree.Nodes.Values)
                {
                    _allDialogueNodes[node.Id] = node;
                }
                
                // Validate dialogue tree structure
                if (!DialogueDataLoader.ValidateDialogueTree(tree))
                {
                    System.Console.WriteLine($"[DialogueManager] Warning: Invalid dialogue tree structure for {tree.Id}");
                }
            }
            
            System.Console.WriteLine($"[DialogueManager] Loaded {_dialogueTrees.Count} dialogue trees with {_allDialogueNodes.Count} total nodes");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error loading dialogue data: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Triggers a dialogue for a specific PoI type
    /// </summary>
    /// <param name="poiType">Type of PoI that triggered the dialogue</param>
    /// <param name="relatedQuest">Optional quest context</param>
    public void TriggerDialogue(PoIType poiType, Quest relatedQuest = null)
    {
        try
        {
            // Find appropriate dialogue tree for this PoI type
            var availableDialogues = GetDialoguesForPoI(poiType);
            
            if (!availableDialogues.Any())
            {
                System.Console.WriteLine($"[DialogueManager] No dialogues available for PoI type: {poiType}");
                return;
            }
            
            // Select the highest priority dialogue that meets conditions
            var selectedDialogue = SelectBestDialogue(availableDialogues, relatedQuest);
            
            if (selectedDialogue == null)
            {
                System.Console.WriteLine($"[DialogueManager] No suitable dialogue found for {poiType}");
                return;
            }
            
            // Start the conversation
            StartConversation(selectedDialogue, poiType, relatedQuest);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error triggering dialogue: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Triggers a specific dialogue by ID
    /// </summary>
    /// <param name="dialogueId">ID of the dialogue tree to trigger</param>
    /// <param name="poiType">PoI type for context</param>
    /// <param name="relatedQuest">Optional quest context</param>
    public void TriggerDialogue(string dialogueId, PoIType poiType, Quest relatedQuest = null)
    {
        try
        {
            if (!_dialogueTrees.TryGetValue(dialogueId, out var dialogueTree))
            {
                System.Console.WriteLine($"[DialogueManager] Dialogue tree not found: {dialogueId}");
                return;
            }
            
            // Check if dialogue can be triggered
            if (!CanTriggerDialogue(dialogueId, CreateQuestContext(relatedQuest)))
            {
                System.Console.WriteLine($"[DialogueManager] Dialogue conditions not met for: {dialogueId}");
                return;
            }
            
            StartConversation(dialogueTree, poiType, relatedQuest);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error triggering specific dialogue: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Starts a conversation with the given dialogue tree
    /// </summary>
    private void StartConversation(DialogueTree dialogueTree, PoIType poiType, Quest relatedQuest)
    {
        try
        {
            // Get starting node
            if (!dialogueTree.Nodes.TryGetValue(dialogueTree.StartNodeId, out var startNode))
            {
                System.Console.WriteLine($"[DialogueManager] Start node not found: {dialogueTree.StartNodeId}");
                return;
            }
            
            // Create conversation state
            _currentConversation = new ConversationState
            {
                DialogueTree = dialogueTree,
                CurrentNode = startNode,
                PoIType = poiType,
                RelatedQuest = relatedQuest,
                StartTime = DateTime.Now
            };
            
            // Load event image if specified
            LoadEventImage(startNode);
            
            // Show the dialogue in EventCardBox
            _eventCardBox?.ShowDialogue(startNode);
            
            // Fire events
            ConversationStarted?.Invoke();
            DialogueTriggered?.Invoke(startNode);
            
            System.Console.WriteLine($"[DialogueManager] Started conversation: {dialogueTree.Name}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error starting conversation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handles choice selection from EventCardBox
    /// </summary>
    private void OnChoiceSelected(DialogueChoice choice)
    {
        try
        {
            if (_currentConversation == null)
            {
                System.Console.WriteLine("[DialogueManager] No active conversation for choice selection");
                return;
            }
            
            System.Console.WriteLine($"[DialogueManager] Processing choice: {choice.Text}");
            
            // Process choice outcomes first
            ProcessChoiceOutcome(choice);
            
            // Fire choice selected event
            ChoiceSelected?.Invoke(choice);
            
            // Handle conversation flow
            if (string.IsNullOrEmpty(choice.NextNodeId))
            {
                // End conversation
                EndConversation();
            }
            else
            {
                // Continue to next node
                ContinueConversation(choice.NextNodeId);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error processing choice: {ex.Message}");
            // End conversation on error to prevent getting stuck
            EndConversation();
        }
    }
    
    /// <summary>
    /// Continues conversation to the next dialogue node
    /// </summary>
    private void ContinueConversation(string nextNodeId)
    {
        try
        {
            if (!_allDialogueNodes.TryGetValue(nextNodeId, out var nextNode))
            {
                System.Console.WriteLine($"[DialogueManager] Next node not found: {nextNodeId}");
                EndConversation();
                return;
            }
            
            // Update conversation state
            _currentConversation.CurrentNode = nextNode;
            
            // Load new event image if specified
            LoadEventImage(nextNode);
            
            // Update EventCardBox with new dialogue
            _eventCardBox?.UpdateDialogue(nextNode);
            
            System.Console.WriteLine($"[DialogueManager] Continued to node: {nextNodeId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error continuing conversation: {ex.Message}");
            EndConversation();
        }
    }
    
    /// <summary>
    /// Processes the outcomes of a dialogue choice
    /// </summary>
    public void ProcessChoiceOutcome(DialogueChoice choice)
    {
        try
        {
            // Apply stat rewards
            if (choice.StatRewards != null && choice.StatRewards.Any())
            {
                foreach (var statReward in choice.StatRewards)
                {
                    ApplyStatReward(statReward.Key, statReward.Value);
                }
            }
            
            // Process quest updates
            if (!string.IsNullOrEmpty(choice.QuestUpdate))
            {
                ProcessQuestUpdate(choice.QuestUpdate);
            }
            
            // Add journal entry
            if (!string.IsNullOrEmpty(choice.JournalEntry))
            {
                _journalManager?.OnSpecialEvent("Dialogue", choice.JournalEntry);
            }
            
            System.Console.WriteLine($"[DialogueManager] Processed outcomes for choice: {choice.Text}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error processing choice outcome: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Applies a stat reward to the player
    /// </summary>
    private void ApplyStatReward(string statName, int amount)
    {
        try
        {
            if (_statsManager?.CurrentStats == null)
            {
                System.Console.WriteLine("[DialogueManager] StatsManager not available for stat rewards");
                return;
            }
            
            switch (statName.ToLower())
            {
                case "experience":
                    _statsManager.CurrentStats.AddExperience(amount);
                    System.Console.WriteLine($"[DialogueManager] Awarded {amount} experience");
                    break;
                    
                case "mood":
                    _statsManager.CurrentStats.AddRecentActivityBonus(amount);
                    System.Console.WriteLine($"[DialogueManager] Applied {amount} mood bonus");
                    break;
                    
                case "health":
                    // Apply as tiredness regeneration (health concept)
                    _statsManager.RegenerateTiredness(amount);
                    System.Console.WriteLine($"[DialogueManager] Restored {amount} health/tiredness");
                    break;
                    
                case "hunger":
                    var currentHunger = _statsManager.CurrentStats.Hunger;
                    _statsManager.CurrentStats.SetStat(StatType.Hunger, currentHunger + amount);
                    System.Console.WriteLine($"[DialogueManager] Applied {amount} hunger");
                    break;
                    
                case "tiredness":
                    var currentTiredness = _statsManager.CurrentStats.Tiredness;
                    _statsManager.CurrentStats.SetStat(StatType.Tiredness, currentTiredness + amount);
                    System.Console.WriteLine($"[DialogueManager] Applied {amount} tiredness");
                    break;
                    
                default:
                    System.Console.WriteLine($"[DialogueManager] Unknown stat type: {statName}");
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error applying stat reward {statName}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Processes quest updates from dialogue choices
    /// </summary>
    private void ProcessQuestUpdate(string questUpdate)
    {
        try
        {
            if (string.IsNullOrEmpty(questUpdate))
                return;
                
            // Parse quest update format: "questId:action" or just "questId" (defaults to complete)
            string questId = questUpdate;
            string action = "complete";
            
            if (questUpdate.Contains(':'))
            {
                var parts = questUpdate.Split(':');
                questId = parts[0];
                action = parts.Length > 1 ? parts[1] : "complete";
            }
            
            // Use QuestManager's new dialogue outcome processing
            _questManager?.ProcessDialogueOutcome(questId, action);
            
            System.Console.WriteLine($"[DialogueManager] Quest update processed: {questId} ({action})");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error processing quest update: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Loads event image for a dialogue node using lazy loading
    /// </summary>
    private void LoadEventImage(DialogueNode node)
    {
        try
        {
            if (string.IsNullOrEmpty(node.EventImage))
            {
                _eventCardBox?.SetEventImage(null);
                return;
            }
                
            // Load image on-demand through ContentManager (lazy loading)
            string imagePath = $"events/{System.IO.Path.GetFileNameWithoutExtension(node.EventImage)}";
            var eventImage = _assetManager?.GetContent()?.Load<Texture2D>(imagePath);
            
            if (eventImage != null && _eventCardBox != null)
            {
                _eventCardBox.SetEventImage(eventImage);
                System.Console.WriteLine($"[DialogueManager] Loaded event image: {node.EventImage}");
            }
            else
            {
                System.Console.WriteLine($"[DialogueManager] Could not load event image: {node.EventImage}");
                _eventCardBox?.SetEventImage(null);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error loading event image {node.EventImage}: {ex.Message}");
            _eventCardBox?.SetEventImage(null);
        }
    }
    
    /// <summary>
    /// Ends the current conversation
    /// </summary>
    private void EndConversation()
    {
        try
        {
            if (_currentConversation != null)
            {
                System.Console.WriteLine($"[DialogueManager] Ending conversation: {_currentConversation.DialogueTree.Name}");
                
                // Add journal entry about the conversation
                var conversationDuration = DateTime.Now - _currentConversation.StartTime;
                _journalManager?.OnSpecialEvent("Conversation Ended", 
                    $"Finished speaking with {_currentConversation.PoIType}. The conversation lasted {conversationDuration.TotalSeconds:F0} seconds.");
            }
            
            _currentConversation = null;
            
            // Fire event
            ConversationEnded?.Invoke();
            
            System.Console.WriteLine("[DialogueManager] Conversation ended");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error ending conversation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handles dialogue closed event from EventCardBox
    /// </summary>
    private void OnDialogueClosed()
    {
        EndConversation();
    }
    
    /// <summary>
    /// Gets available dialogues for a specific PoI type
    /// </summary>
    private List<DialogueTree> GetDialoguesForPoI(PoIType poiType)
    {
        return DialogueDataLoader.GetDialoguesForPoI(_dialogueTrees, poiType);
    }
    
    /// <summary>
    /// Selects the best dialogue from available options
    /// </summary>
    private DialogueTree SelectBestDialogue(List<DialogueTree> availableDialogues, Quest relatedQuest)
    {
        var questContext = CreateQuestContext(relatedQuest);
        
        // Filter dialogues that meet activation conditions
        var validDialogues = availableDialogues.Where(d => CanTriggerDialogue(d.Id, questContext)).ToList();
        
        if (!validDialogues.Any())
            return availableDialogues.FirstOrDefault(); // Fallback to first available
        
        // Return highest priority dialogue
        return validDialogues.OrderByDescending(d => d.Priority).First();
    }
    
    /// <summary>
    /// Checks if a dialogue can be triggered based on conditions
    /// </summary>
    public bool CanTriggerDialogue(string dialogueId, QuestContext context)
    {
        try
        {
            if (!_dialogueTrees.TryGetValue(dialogueId, out var dialogue))
                return false;
            
            // Check activation conditions
            foreach (var condition in dialogue.ActivationConditions)
            {
                if (!EvaluateCondition(condition, context))
                    return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error checking dialogue conditions: {ex.Message}");
            return false; // Fail safe
        }
    }
    
    /// <summary>
    /// Evaluates a single dialogue condition
    /// </summary>
    private bool EvaluateCondition(string condition, QuestContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(condition))
                return true;
            
            var parts = condition.Split(':');
            if (parts.Length != 2)
                return true; // Invalid condition format, allow by default
            
            var conditionType = parts[0].ToLower();
            var conditionValue = parts[1];
            
            switch (conditionType)
            {
                case "quest":
                    return context.CompletedQuests.Contains(conditionValue);
                    
                case "experience":
                    if (int.TryParse(conditionValue, out var requiredExp))
                        return context.PlayerStats?.TotalExperience >= requiredExp;
                    break;
                    
                case "mood":
                    if (int.TryParse(conditionValue, out var requiredMood))
                        return context.PlayerStats?.Mood >= requiredMood;
                    break;
                    
                default:
                    System.Console.WriteLine($"[DialogueManager] Unknown condition type: {conditionType}");
                    break;
            }
            
            return true; // Default to allowing dialogue
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DialogueManager] Error evaluating condition {condition}: {ex.Message}");
            return true; // Fail safe - allow dialogue
        }
    }
    
    /// <summary>
    /// Creates quest context for condition evaluation
    /// </summary>
    private QuestContext CreateQuestContext(Quest relatedQuest)
    {
        var context = _questManager?.GetQuestContext() ?? new QuestContext();
        
        // Add related quest to context if provided
        if (relatedQuest != null)
        {
            // Add any quest-specific context here
        }
        
        return context;
    }
    
    /// <summary>
    /// Gets the current conversation state (for debugging/UI)
    /// </summary>
    public ConversationState GetCurrentConversation()
    {
        return _currentConversation;
    }
    
    /// <summary>
    /// Checks if a conversation is currently active
    /// </summary>
    public bool IsConversationActive()
    {
        return _currentConversation != null;
    }
    
    /// <summary>
    /// Gets all loaded dialogue trees (for debugging)
    /// </summary>
    public Dictionary<string, DialogueTree> GetDialogueTrees()
    {
        return new Dictionary<string, DialogueTree>(_dialogueTrees);
    }
}

/// <summary>
/// Represents the current state of an active conversation
/// </summary>
public class ConversationState
{
    public DialogueTree DialogueTree { get; set; }
    public DialogueNode CurrentNode { get; set; }
    public PoIType PoIType { get; set; }
    public Quest RelatedQuest { get; set; }
    public DateTime StartTime { get; set; }
}