# Project Structure

## Root Directory
- `Game1.cs` - Main game class, orchestrates all systems
- `Program.cs` - Entry point
- `*.cs` files - Game systems and managers (see below)

## Content Directory (`Content/`)
- `adventurer/` - Player character sprites (idle, walking, sleeping, torch)
- `tilesheet/` - Terrain and object tilesheets
- `sounds/` - Audio files (footsteps, ambient, weather)
- `fonts/` - SpriteFont definitions
- `data/` - JSON data files (quests, journal entries)
- `ui/` - UI textures and sprites
- `Content.mgcb` - MonoGame Content Pipeline configuration

## Architecture Patterns

### Manager Pattern
Most game systems follow a manager pattern with dedicated classes:
- `AssetManager` - Centralized asset loading and tilesheet management
- `AudioManager` - Sound playback with grouping and cooldowns
- `ZoneManager` - Procedural zone generation and transitions
- `TimeManager` - Day/night cycles and seasons
- `WeatherManager` - Weather state and transitions
- `QuestManager` - Quest tracking and objectives
- `JournalManager` - Event logging and history
- `PoIManager` - Points of Interest generation and interaction
- `StatsManager` - Player statistics tracking
- `UIManager` - UI rendering and input handling
- `LightingManager` - Dynamic lighting system
- `PathfindingManager` - Navigation and pathfinding

### Data Classes
- `AdventurerStats` - Player stats data structure
- `QuestData` - Quest definitions
- `JournalEntryData` - Journal entry templates
- `PathfindingConfig` - Pathfinding parameters

### Core Game Objects
- `Adventurer` - Player character with movement, animation, stats
- `Camera` - Viewport and following logic
- `Zone` - Represents a game area with terrain and objects
- `PointOfInterest` - Interactive world locations
- `Quest` - Quest instance with objectives
- `WorldObject` - Terrain objects (trees, rocks, etc.)

### UI Components
- `EscapeMenu` - Pause menu
- `JournalUI` - Journal display
- `StatsPage` - Statistics screen
- `StatsHUD` - In-game stats overlay
- `MiniMap` - Zone minimap

### Utilities
- `VirtualResolution` - Resolution-independent rendering
- `TilesheetManager` - Tilesheet coordinate management
- `TerrainGenerator` - Procedural terrain generation
- `SimplePathfinder` - Basic pathfinding implementation
- `PoIDetector` - PoI proximity detection

## Enums (`GameEnums.cs`)
Centralized enum definitions for:
- `TerrainType` - Grass, Dirt, Water, Stone
- `ObjectType` - Trees, Rocks, Plants, Bushes
- `BiomeType` - Forest, Lake, Mountain, DenseForest, Plains, Swamp
- `Direction` - North, South, East, West
- `WeatherType` - Clear, Rain, Snow, Fog
- `TimeOfDay` - Day, Night
- `PoIType` - Various point of interest types
- `QuestObjectiveType` - Quest goal types

## Coordinate System
- Tile size: 32x32 pixels
- World coordinates: Pixel-based (tile * 32)
- Zone coordinates: Tile-based grid
- Virtual resolution: 1024x768 (default), supports aspect ratio switching
