# Design Document

## Overview

The EventCardBox system provides a dialogue-driven UI for interactive conversations with NPCs and quest-related events. The system displays quest text at the top and dialogue options below, supporting branching conversations and reward outcomes.

## Architecture

### Core Components

```
EventCardBox (UI Component)
├── DialogueRenderer (Text Display)
├── ChoiceButtonManager (Button Handling)  
├── DialogueFlowController (Conversation Logic)
└── RewardProcessor (Outcome Handling)

DialogueManager (System Controller)
├── DialogueDataLoader (JSON Loading)
├── ConversationStateTracker (Flow Management)
└── EventTriggerHandler (Integration)
```

## Components and Interfaces

### EventCardBox Class

```csharp
public class EventCardBox
{
    // UI Properties
    public bool IsVisible { get; private set; }
    public Rectangle Bounds { get; set; }           // 80% screen width, 60% screen height
    public SpriteFont Font { get; set; }
    
    // Layout Areas
    public Rectangle ImageArea { get; private set; }    // 200x200px on left
    public Rectangle TextArea { get; private set; }     // Remaining width for text/buttons
    
    // Dialogue State
    public DialogueNode CurrentNode { get; private set; }
    public Texture2D EventImage { get; private set; }
    public List<DialogueChoice> CurrentChoices { get; private set; }
    
    // Timing and Animation
    public float AutoTimeoutSeconds { get; set; } = 60f;
    public float CurrentTimeout { get; private set; }
    
    // Events
    public event Action<DialogueChoice> ChoiceSelected;
    public event Action DialogueClosed;
    
    // Methods
    public void ShowDialogue(DialogueNode startNode)
    public void UpdateDialogue(DialogueNode newNode)
    public void HandleChoiceSelection(int choiceIndex)
    public void Update(GameTime gameTime)
    public void Draw(SpriteBatch spriteBatch)
    
    // Layout calculation
    private void CalculateLayout(int screenWidth, int screenHeight)
}
```

### DialogueManager Class

```csharp
public class DialogueManager
{
    // System References
    private QuestManager _questManager;
    private StatsManager _statsManager;
    private JournalManager _journalManager;
    
    // Dialogue Data
    private Dictionary<string, DialogueTree> _dialogueTrees;
    private Dictionary<string, DialogueNode> _dialogueNodes;
    
    // State Management
    private ConversationState _currentConversation;
    
    // Events
    public event Action<DialogueNode> DialogueTriggered;
    public event Action ConversationEnded;
    
    // Methods
    public void LoadDialogueData(string dataPath)
    public void TriggerDialogue(string dialogueId, PoIType npcType, Quest relatedQuest = null)
    public void ProcessChoiceOutcome(DialogueChoice choice)
    public bool CanTriggerDialogue(string dialogueId, QuestContext context)
}
```

## Important Implementation Notes

### Enum and PoI Validation
- **Check Existing Enums**: Before implementing, verify available PoIType values in PointOfInterest.cs
- **Validate PoI Integration**: Ensure dialogue triggers align with existing PoI types (Ranger, Priest, Hermit, etc.)
- **Quest Integration**: Confirm QuestManager methods and events available for dialogue integration
- **Asset References**: Use existing AssetManager patterns for loading event images

### Demo Assets
- **Demo Event Image**: Located at `/Content/events/demo.png` for testing and reference
- **Image Format**: Follow existing asset conventions (PNG format, appropriate sizing)
- **Asset Loading**: Use AssetManager.GetTexture() method for consistency

## Data Models

### DialogueNode Structure (Simplified)

```csharp
public class DialogueNode
{
    public string Id { get; set; }
    public string QuestText { get; set; }           // Main text displayed at top
    public string SpeakerName { get; set; }         // NPC name
    public string EventImage { get; set; }          // Image filename for the event
    
    public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();
    
    // Simple metadata
    public bool IsEndNode { get; set; }             // True if conversation ends here
}

public class DialogueChoice
{
    public string Text { get; set; }                // Button text
    public string NextNodeId { get; set; }          // Next dialogue node (null for end)
    
    // Simple Rewards (optional)
    public Dictionary<string, int> StatRewards { get; set; } = new Dictionary<string, int>();
    public string QuestUpdate { get; set; }         // Quest ID to complete/progress
    public string JournalEntry { get; set; }        // Journal message to add
}
```

### DialogueTree Structure

```csharp
public class DialogueTree
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // NPC/Context Association
    public PoIType AssociatedNPC { get; set; }
    public List<string> AssociatedQuests { get; set; } = new List<string>();
    
    // Dialogue Flow
    public string StartNodeId { get; set; }
    public Dictionary<string, DialogueNode> Nodes { get; set; } = new Dictionary<string, DialogueNode>();
    
    // Conditions
    public List<DialogueCondition> ActivationConditions { get; set; } = new List<DialogueCondition>();
    public int Priority { get; set; } = 0;
}
```

## Error Handling

### Dialogue Loading Errors
- **Missing Node References**: Log warning and provide fallback "end conversation" option
- **Circular References**: Detect and break cycles with timeout protection
- **Invalid Conditions**: Default to allowing dialogue if condition evaluation fails
- **Asset Loading Failures**: Use placeholder images and continue with text-only dialogue

### Runtime Error Recovery
- **Null Node States**: Return to previous valid node or end conversation gracefully
- **Choice Processing Errors**: Log error but allow conversation to continue
- **Reward Application Failures**: Log error but don't block dialogue progression
- **Animation Failures**: Fall back to instant transitions

## Testing Strategy

### Manual Testing (Prototype Approach)
- **Visual Verification**: Run game and interact with NPCs to verify dialogue display
- **Choice Flow**: Click through dialogue options to ensure conversation progression works
- **Reward Integration**: Verify stat changes and quest updates appear correctly in-game
- **Timeout Behavior**: Wait for auto-timeout to confirm random choice selection works

## Performance Considerations

### Simple Optimization
- **Basic Caching**: Keep current dialogue tree in memory during conversation
- **Minimal Assets**: Use existing game fonts and simple colored rectangles for UI
- **Direct Rendering**: Simple SpriteBatch drawing without complex optimizations

## Integration Points

### QuestManager Integration
```csharp
// Quest events can trigger dialogues
_questManager.ObjectiveCompleted += (quest, objective) => {
    if (ShouldTriggerDialogue(quest, objective)) {
        _dialogueManager.TriggerDialogue(GetDialogueId(quest, objective), quest.QuestGiver);
    }
};

// Dialogue outcomes can update quests
_dialogueManager.ChoiceSelected += (choice) => {
    if (choice.Outcome.QuestUpdates.Any()) {
        _questManager.ProcessDialogueOutcome(choice.Outcome);
    }
};
```

### PoI Interaction Trigger (Primary Mechanism)
```csharp
// MAIN TRIGGER: When adventurer interacts with a PoI that has an associated event
// This is the primary way EventCardBox dialogues are triggered
_poiManager.PoIInteracted += (poi, adventurer) => {
    // Check if this PoI has dialogue/event data
    var dialogueTree = _dialogueManager.GetDialogueForPoI(poi.Type);
    if (dialogueTree != null) {
        // Trigger the EventCardBox with the dialogue
        _dialogueManager.ShowEventCard(dialogueTree.StartNodeId, poi);
        
        // Pause game systems while dialogue is active
        _gameStateManager.PauseGameplay();
    }
    // If no dialogue exists, continue with existing PoI interaction behavior
};
```

**Flow Summary:**
1. Adventurer approaches and interacts with PoI (existing behavior)
2. System checks if PoI has associated dialogue/event
3. If event exists → EventCardBox appears with dialogue
4. If no event → existing PoI interaction continues normally

### Game State Management
```csharp
// Pause game systems when dialogue is active
public void OnDialogueStarted() {
    _gameStateManager.PauseGameplay();
    _inputManager.SetInputMode(InputMode.Dialogue);
}

// Resume game systems when dialogue ends
public void OnDialogueEnded() {
    _gameStateManager.ResumeGameplay();
    _inputManager.SetInputMode(InputMode.Gameplay);
}
```

## Visual Design Specifications

### Layout Structure & Sizing

**EventCardBox Dimensions:**
- **Width**: 80% of screen width (e.g., 820px on 1024px screen)
- **Height**: 60% of screen height (e.g., 460px on 768px screen)  
- **Position**: Centered horizontally, vertically centered with margins top/bottom
- **Margins**: 20% screen height for top/bottom spacing

**Internal Layout (Horizontal):**
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                Event Title                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                    │                                                        │
│   [Event Image]    │  Quest text appears here and wraps                    │
│   (200x200px)      │  naturally. This provides context                     │
│                    │  for the dialogue choices below.                      │
│                    │                                                        │
│                    │  ┌──────────────────────────────────────────────────┐ │
│                    │  │ > Continue the conversation                      │ │
│                    │  └──────────────────────────────────────────────────┘ │
│                    │  ┌──────────────────────────────────────────────────┐ │
│                    │  │ > Ask about the quest                            │ │
│                    │  └──────────────────────────────────────────────────┘ │
│                    │  ┌──────────────────────────────────────────────────┐ │
│                    │  │ > End conversation                               │ │
│                    │  └──────────────────────────────────────────────────┘ │
│                    │                                                        │
│                    │                           [Timeout: 45s]              │
└────────────────────┴────────────────────────────────────────────────────────┘
```

**Component Sizing:**
- **Event Image**: 200x200px fixed size on the left
- **Text Area**: Remaining width minus padding (≈400-500px)
- **Choice Buttons**: Full text area width, 40px height each
- **Padding**: 20px around all edges, 10px between elements

### Styling Guidelines
- **Card Background**: Semi-transparent dark background with border
- **Text Color**: High contrast white/light text on dark background
- **Button Style**: Consistent with existing UI (similar to escape menu buttons)
- **Fonts**: Use existing game fonts for consistency
- **Animations**: Smooth fade-in/out and scale transitions (< 300ms)
- **Spacing**: Adequate padding and margins for readability