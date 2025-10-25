using System.Collections.Generic;

namespace HiddenHorizons;

/// <summary>
/// Represents a complete dialogue tree with multiple nodes and conversation flow
/// </summary>
public class DialogueTree
{
    /// <summary>
    /// Unique identifier for this dialogue tree
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for this dialogue tree
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of this dialogue tree's purpose
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// PoI type that this dialogue is associated with (e.g., Ranger, Priest, Hermit)
    /// </summary>
    public PoIType AssociatedNPC { get; set; }
    
    /// <summary>
    /// List of quest IDs that can trigger this dialogue
    /// </summary>
    public List<string> AssociatedQuests { get; set; } = new List<string>();
    
    /// <summary>
    /// ID of the starting dialogue node
    /// </summary>
    public string StartNodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Dictionary of all dialogue nodes in this tree
    /// Key: Node ID, Value: DialogueNode
    /// </summary>
    public Dictionary<string, DialogueNode> Nodes { get; set; } = new Dictionary<string, DialogueNode>();
    
    /// <summary>
    /// Conditions that must be met for this dialogue to be available
    /// Simple format: "stat:value" or "quest:questId"
    /// </summary>
    public List<string> ActivationConditions { get; set; } = new List<string>();
    
    /// <summary>
    /// Priority for this dialogue (higher numbers take precedence)
    /// Used when multiple dialogues could be triggered for the same NPC
    /// </summary>
    public int Priority { get; set; } = 0;
}