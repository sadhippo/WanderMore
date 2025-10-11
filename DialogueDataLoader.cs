using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HiddenHorizons;

/// <summary>
/// Handles loading and parsing dialogue data from JSON files
/// </summary>
public static class DialogueDataLoader
{
    /// <summary>
    /// Container class for JSON deserialization
    /// </summary>
    public class DialogueData
    {
        public List<DialogueTree> DialogueTrees { get; set; } = new List<DialogueTree>();
    }
    
    /// <summary>
    /// Loads dialogue trees from a JSON file
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    /// <returns>Dictionary of dialogue trees keyed by ID</returns>
    public static Dictionary<string, DialogueTree> LoadDialogues(string filePath)
    {
        var dialogues = new Dictionary<string, DialogueTree>();
        
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Dialogue file not found: {filePath}");
                return dialogues;
            }
            
            string jsonContent = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            
            var dialogueData = JsonSerializer.Deserialize<DialogueData>(jsonContent, options);
            
            if (dialogueData?.DialogueTrees != null)
            {
                foreach (var tree in dialogueData.DialogueTrees)
                {
                    if (!string.IsNullOrEmpty(tree.Id))
                    {
                        dialogues[tree.Id] = tree;

                    }
                }
            }
            
            Console.WriteLine($"Loaded {dialogues.Count} dialogue trees from {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading dialogues from {filePath}: {ex.Message}");
        }
        
        return dialogues;
    }
    
    /// <summary>
    /// Gets dialogue trees associated with a specific PoI type
    /// </summary>
    /// <param name="dialogues">Dictionary of all dialogue trees</param>
    /// <param name="poiType">PoI type to filter by</param>
    /// <returns>List of matching dialogue trees</returns>
    public static List<DialogueTree> GetDialoguesForPoI(Dictionary<string, DialogueTree> dialogues, PoIType poiType)
    {
        var result = new List<DialogueTree>();
        
        foreach (var tree in dialogues.Values)
        {
            if (tree.AssociatedNPC == poiType)
            {
                result.Add(tree);
            }
        }
        
        // Sort by priority (higher priority first)
        result.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        return result;
    }
    
    /// <summary>
    /// Validates that a dialogue tree has proper structure
    /// </summary>
    /// <param name="tree">Dialogue tree to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateDialogueTree(DialogueTree tree)
    {
        if (string.IsNullOrEmpty(tree.Id) || string.IsNullOrEmpty(tree.StartNodeId))
        {
            return false;
        }
        
        if (!tree.Nodes.ContainsKey(tree.StartNodeId))
        {
            return false;
        }
        
        // Validate all node references
        foreach (var node in tree.Nodes.Values)
        {
            foreach (var choice in node.Choices)
            {
                if (!string.IsNullOrEmpty(choice.NextNodeId) && !tree.Nodes.ContainsKey(choice.NextNodeId))
                {
                    Console.WriteLine($"Invalid node reference: {choice.NextNodeId} in tree {tree.Id}");
                    return false;
                }
            }
        }
        
        return true;
    }
}