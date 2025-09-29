using System;
using System.Collections.Generic;

namespace HiddenHorizons
{
    /// <summary>
    /// Save data structure for the quest system, designed to be extensible and support various quest types
    /// </summary>
    public class QuestSaveData
    {
        /// <summary>
        /// Currently active quests that the player is working on
        /// </summary>
        public List<QuestInstanceSaveData> ActiveQuests { get; set; } = new List<QuestInstanceSaveData>();

        /// <summary>
        /// IDs of quests that have been completed by the player
        /// </summary>
        public List<Guid> CompletedQuestIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Global quest variables that can be shared across quests (e.g., reputation, faction standing)
        /// </summary>
        public Dictionary<string, object> QuestVariables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Progress tracking for quest chains and storylines
        /// </summary>
        public List<QuestChainProgressSaveData> ChainProgress { get; set; } = new List<QuestChainProgressSaveData>();

        /// <summary>
        /// Failed or abandoned quests for potential retry mechanics
        /// </summary>
        public List<Guid> FailedQuestIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Timestamp of when quest data was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Save data for an individual quest instance
    /// </summary>
    public class QuestInstanceSaveData
    {
        /// <summary>
        /// Unique identifier for this quest instance
        /// </summary>
        public Guid QuestId { get; set; }

        /// <summary>
        /// Template ID that defines the quest type and behavior
        /// </summary>
        public string QuestTemplateId { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the quest
        /// </summary>
        public QuestStatus Status { get; set; } = QuestStatus.Active;

        /// <summary>
        /// When this quest was started
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Progress tracking for quest objectives
        /// </summary>
        public Dictionary<string, QuestObjectiveSaveData> Objectives { get; set; } = new Dictionary<string, QuestObjectiveSaveData>();

        /// <summary>
        /// Quest-specific variables and state
        /// </summary>
        public Dictionary<string, object> QuestState { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Current step or phase of the quest
        /// </summary>
        public int CurrentStep { get; set; } = 0;

        /// <summary>
        /// Priority level for quest ordering in UI
        /// </summary>
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Save data for quest objectives
    /// </summary>
    public class QuestObjectiveSaveData
    {
        /// <summary>
        /// Unique identifier for this objective
        /// </summary>
        public string ObjectiveId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this objective has been completed
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Current progress value (e.g., items collected, enemies defeated)
        /// </summary>
        public int CurrentProgress { get; set; } = 0;

        /// <summary>
        /// Target progress value needed to complete the objective
        /// </summary>
        public int TargetProgress { get; set; } = 1;

        /// <summary>
        /// Optional objective-specific data
        /// </summary>
        public Dictionary<string, object> ObjectiveData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Save data for quest chain progression
    /// </summary>
    public class QuestChainProgressSaveData
    {
        /// <summary>
        /// Identifier for the quest chain
        /// </summary>
        public string ChainId { get; set; } = string.Empty;

        /// <summary>
        /// Current position in the quest chain
        /// </summary>
        public int CurrentChainStep { get; set; } = 0;

        /// <summary>
        /// Whether the entire chain has been completed
        /// </summary>
        public bool IsChainCompleted { get; set; } = false;

        /// <summary>
        /// Chain-specific variables and flags
        /// </summary>
        public Dictionary<string, object> ChainVariables { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Enumeration of possible quest statuses
    /// </summary>
    public enum QuestStatus
    {
        /// <summary>
        /// Quest is currently active and can be progressed
        /// </summary>
        Active,

        /// <summary>
        /// Quest has been completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Quest has failed and cannot be completed
        /// </summary>
        Failed,

        /// <summary>
        /// Quest has been abandoned by the player
        /// </summary>
        Abandoned,

        /// <summary>
        /// Quest is paused or on hold
        /// </summary>
        Paused
    }
}