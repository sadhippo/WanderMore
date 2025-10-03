# Design Document

## Overview

The adventurer stats system adds five core stats (experience, hunger, tiredness, comfort, mood) that enhance the game's depth while maintaining its relaxing, non-punishing nature. The system integrates seamlessly with existing game systems (TimeManager, WeatherManager, QuestManager, PoIManager) and provides both a simple HUD for at-a-glance monitoring and a detailed stats page for comprehensive information.

## Architecture

### Core Components

1. **StatsManager** - Central manager for all stat calculations and updates
2. **AdventurerStats** - Data structure holding current stat values
3. **StatsHUD** - Simple bar-based UI overlay for real-time monitoring
4. **StatsPage** - Detailed stats view with descriptions and exact values
5. **StatsConfig** - Configuration for stat rates, regeneration, and interactions

### Integration Points

- **TimeManager**: Drives hunger decrease and tiredness from time passage
- **WeatherManager**: Influences comfort and mood through weather conditions
- **QuestManager**: Awards experience and mood boosts from quest completion
- **PoIManager**: Provides stat regeneration opportunities at specific PoI types
- **UIManager**: Hosts the stats HUD and manages the stats page display
- **JournalManager**: Records stat-related events and milestones for immersive storytelling
- **JournalManager**: Post updates about the stats, i.e. I am hungry, level gained, i am sad, happy...

## Components and Interfaces

### StatsManager Class

```csharp
public class StatsManager
{
    public AdventurerStats CurrentStats { get; private set; }
    public event Action<StatType, float> StatChanged;
    
    public void Update(GameTime gameTime)
    public void OnQuestCompleted(Quest quest)
    public void OnPoIInteracted(PointOfInterest poi)
    public void OnWeatherChanged(WeatherType weather)
    public void OnTimeChanged(TimeOfDay timeOfDay)
    private void RecordJournalEntry(string eventType, string description)
}
```

### AdventurerStats Structure

```csharp
public class AdventurerStats
{
    public int Level { get; set; }            // 1+ (unlimited)
    public float Experience { get; set; }     // Current XP in level
    public float ExperienceToNext { get; set; } // XP needed for next level
    public float TotalExperience { get; set; } // Total XP earned (for journal milestones)
    public float Hunger { get; set; }         // 0-100
    public float Tiredness { get; set; }      // 0-100 (inverted - 0 = tired, 100 = rested)
    public float Comfort { get; set; }        // 0-100 (calculated from hunger/tiredness/weather)
    public float Mood { get; set; }           // 0-100 (calculated from comfort/weather/activities)
}
```

### StatsHUD Class

```csharp
public class StatsHUD
{
    public void Draw(SpriteBatch spriteBatch, AdventurerStats stats)
    public bool HandleMouseHover(Vector2 mousePosition, out string tooltip)
}
```

### StatsPage Class

```csharp
public class StatsPage
{
    public bool IsOpen { get; private set; }
    public void Toggle()
    public void Draw(SpriteBatch spriteBatch, AdventurerStats stats)
    public bool HandleInput(KeyboardState keyboard, MouseState mouse)
}
```

## Data Models

### Stat Calculation Formulas

**Comfort Calculation:**
```
Comfort = (Hunger * 0.4 + Tiredness * 0.4 + WeatherModifier * 0.2)
WeatherModifier = Clear: +20, Cloudy: +10, Rain: -5, Snow: -10, Fog: 0
```

**Mood Calculation:**
```
Mood = (Comfort * 0.6 + RecentActivities * 0.4)
RecentActivities = Quest completion: +15 (temporary), PoI discovery: +5 (temporary)
```

### Stat Change Rates

- **Hunger**: Decreases by 1 point per game hour (configurable)
- **Tiredness**: Increases by 0.5 points per minute of movement
- **Experience**: +20 for quest completion, +10 for PoI discovery, +20 for zone discovery, +50 for biome discovery
- **Regeneration**: Hunger/Tiredness regenerate at 2 points per minute near appropriate PoIs

### Leveling System

**Experience Requirements:**
- Level 1→2: 100 XP
- Level 2→3: 120 XP  
- Level 3→4: 140 XP
- Level 4→5: 160 XP
- Formula: `XP_Required = 80 + (Level * 20)` (fast early progression, gentle scaling)
- Example: Level 10 requires 280 XP, Level 20 requires 480 XP (much more reasonable)

**Level Benefits:**
- Purely cosmetic/journal-based (maintains non-punishing design)
- Each level unlocks new journal entries and personality development
- Higher levels may influence journal tone (more confident, experienced observations)
- Level-up creates special celebratory journal entries

### PoI Interactions

- **Inn/Tavern**: Regenerates hunger quickly (+3/min)
- **Inn/Cottage**: Regenerates tiredness quickly (+3/min)  
- **Chapel**: Small mood boost (+5)
- **All PoIs**: Small experience gain (+2) on first interaction

### Journal Integration

The stats system will create immersive journal entries for significant stat events:

**Experience & Leveling:**
- "I've learned so much from my adventures! I feel more experienced now." (every 50 total XP)
- "My skills as an adventurer continue to grow with each quest completed."
- "I've reached a new level of expertise! Level [X] - I feel more capable than ever." (level up)
- "The knowledge I've gained from my travels is invaluable. I'm becoming quite the seasoned adventurer." (higher levels)

**Mood & Comfort Entries:**
- "The weather has been lovely today, lifting my spirits considerably."
- "I'm feeling quite content after a good meal and rest at the inn."
- "The constant rain is starting to dampen my mood, but I press on."

**Hunger & Tiredness Observations:**
- "I'm getting quite hungry. Perhaps I should find somewhere to eat soon."
- "My legs are getting tired from all this walking. A rest would be welcome."
- "That meal at the tavern really hit the spot! I feel much better now."

**Daily Stat Summaries:**
- End-of-day entries summarizing overall condition: "Today was a good day - I feel rested, well-fed, and in high spirits."

## Error Handling

### Graceful Degradation
- If font loading fails, HUD uses simple colored rectangles
- If texture loading fails, uses pixel texture fallbacks
- Stats continue updating even if UI fails to render

### Boundary Conditions
- All stats clamped to valid ranges (0-100 for most, 0+ for experience)
- Division by zero protection in calculation formulas
- Null checks for all external system references

## Testing Strategy

### Unit Tests
- Stat calculation formulas with various inputs
- Boundary condition handling (min/max values)
- Event subscription and unsubscription

### Integration Tests
- Stats respond correctly to time passage
- Weather changes affect comfort/mood appropriately
- Quest completion awards correct experience
- PoI interactions provide expected regeneration
- Journal entries are created for appropriate stat events
- Journal entries use natural, immersive language

### Visual Tests
- HUD bars display correctly at different stat levels
- Stats page shows accurate information
- Tooltips appear with correct values
- UI scales properly with different screen sizes

## Implementation Notes

### Performance Considerations
- Stats update only once per frame, not per system event
- UI elements use cached textures and minimal string allocations
- Calculation results cached when inputs haven't changed

### Extensibility
- Easy to add new stats by extending AdventurerStats
- PoI interaction effects configurable through data files
- Stat formulas adjustable through StatsConfig

### Visual Design
- HUD bars positioned in top-left, below existing UI elements
- Uses same color scheme and fonts as existing UI
- Stats page accessible via 'S' key, similar to journal ('J' key)
- Gentle color transitions for low stats (yellow/orange, never red)