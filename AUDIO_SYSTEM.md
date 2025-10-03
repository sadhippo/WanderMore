# Audio System Documentation

## Overview
The AudioManager provides a flexible, expandable audio system for HiddenHorizons with built-in sound grouping, cooldowns, and terrain-specific audio support.

## Current Features

### Footstep Sounds
- **Current Implementation**: 4 dirt footstep sounds (Steps_dirt-01 through 04)
- **Playback**: Automatic footstep sounds every 0.4 seconds while walking
- **Volume**: 60% of max volume with slight pitch variation for natural feel
- **Cooldown**: 0.3 second minimum between footstep sounds to prevent spam

### Audio Manager Features
- **Sound Groups**: Organize sounds by category (footsteps_dirt, etc.)
- **Random Selection**: Automatically picks random sound from group
- **Volume Control**: Master volume and SFX volume controls
- **Pitch Variation**: Automatic pitch variation for more natural sounds
- **Cooldown System**: Prevents sound spam with configurable cooldowns

## Integration Points

### Game1.cs
- AudioManager initialized in Initialize()
- Audio content loaded in LoadContent()
- Connected to Adventurer in Initialize()
- Properly disposed in UnloadContent()

### Adventurer.cs
- Footstep timer tracks movement
- Plays footstep sounds every 0.4 seconds while moving
- Resets timer when not moving
- Uses terrain-aware footstep system (currently defaults to dirt)

## Future Expansion

### Terrain-Specific Footsteps
The system is designed to support different terrain types:
```csharp
// Future terrain types (ready to implement)
LoadSoundGroup("footsteps_grass", "sounds/steps_grass", 4);
LoadSoundGroup("footsteps_stone", "sounds/steps_stone", 4);
LoadSoundGroup("footsteps_water", "sounds/steps_water", 4);
```

### Dynamic Terrain Detection
```csharp
// Example usage when terrain detection is added
string currentTerrain = GetTerrainType(adventurer.Position);
_audioManager.PlayFootstep(0.6f, currentTerrain);
```

### Additional Sound Categories
The system can easily be expanded for:
- Ambient sounds (wind, birds, water)
- UI sounds (button clicks, menu navigation)
- Combat sounds (if combat is added)
- Environmental sounds (doors, chests, etc.)

## Usage Examples

### Basic Footstep
```csharp
_audioManager.PlayFootstep(); // Uses default volume and terrain
```

### Custom Footstep
```csharp
_audioManager.PlayFootstep(0.8f, "stone"); // Louder stone footsteps
```

### Generic Sound Group
```csharp
_audioManager.PlayRandomSound("ambient_forest", 0.5f, 0.1f, 0f, 2f);
// Play forest ambient with 50% volume, slight pitch up, no pan, 2s cooldown
```

## File Structure
```
Content/sounds/
├── Steps_dirt-01.ogg
├── Steps_dirt-02.ogg
├── Steps_dirt-03.ogg
└── Steps_dirt-04.ogg
```

## Content Pipeline
All sound files are properly registered in Content.mgcb with:
- OggImporter for .ogg files
- SoundEffectProcessor for game audio
- Quality=Best for optimal audio quality

## Testing
The system is currently active and can be tested by:
1. Running the game (`dotnet run`)
2. Observing the adventurer's movement
3. Listening for footstep sounds every 0.4 seconds while walking
4. Checking console output for audio loading confirmation