# Project Structure

## Root Directory
- `HiddenHorizons.csproj` - Project file
- `HiddenHorizons.sln` - Solution file
- `Program.cs` - Entry point
- `Game1.cs` - Main game class, orchestrates all systems

## Code Organization

### Managers/ - System Management Classes
All manager classes that handle specific game systems:
- `AssetManager` - Centralized asset loading and tilesheet management
- `AudioManager` - Sound playback with grouping and cooldowns
- `BackgroundMusicManager` - Background music control
- `InventoryManager` - Player inventory system
- `JournalManager` - Event logging and history
- `LightingManager` - Dynamic lighting system
- `PathfindingManager` - Navigation and pathfinding
- `PoIManager` - Points of Interest generation and interaction (JSON-driven)
- `QuestManager` - Quest tracking and objectives
- `StatsManager` - Player statistics tracking
- `TimeManager` - Day/night cycles and seasons
- `TilesheetManager` - Tilesheet coordinate management and sprite definitions
- `UIManager` - UI rendering and input handling
- `WeatherManager` - Weather state and transitions
- `ZoneManager` - Procedural zone generation and transitions

### Core/ - Main Game Objects
Core game entities and entry points:
- `Adventurer` - Player character with movement, animation, stats
- `Camera` - Viewport and following logic
- `WorldObject` - Terrain objects (trees, rocks, etc.)

### Data/ - Data Structures and Loaders
Pure data classes and data loading utilities:
- `AdventurerStats` - Player stats data structure
- `InventoryItem` - Item data structure
- `JournalEntryData` - Journal entry templates
- `PathfindingConfig` - Pathfinding parameters
- `PoIDataLoader` - JSON data loading for PoI definitions
- `Quest` - Quest instance with objectives
- `QuestData` - Quest definitions
- `DialogueDataLoader` - Dialogue system data loader
- `DialogueTree` - Dialogue tree structure

### Systems/ - Game Systems
Interactive game systems and components:
- `PointOfInterest` - Interactive world locations
- `Light` - Lighting system components
- `WeatherEffects` - Weather visual effects
- `DialogueManager` - Dialogue system management
- `DialogueSystemTest` - Dialogue testing utilities

### UI/ - User Interface Components
All user interface and HUD elements:
- `EscapeMenu` - Pause menu
- `EventCardBox` - Event notification UI
- `InventoryUI` - Inventory display
- `JournalUI` - Journal display
- `MiniMap` - Zone minimap
- `StatsHUD` - In-game stats overlay
- `StatsPage` - Statistics screen

### Utilities/ - Helper Classes and Tools
Utility classes and helper functions:
- `GameEnums` - Centralized enum definitions
- `PoIDetector` - PoI proximity detection
- `PoIInteractionHelper` - PoI interaction utilities
- `SimplePathfinder` - Basic pathfinding implementation
- `TerrainGenerator` - Procedural terrain generation
- `VirtualResolution` - Resolution-independent rendering

## Content Directory (`Content/`)
- `adventurer/` - Player character sprites (idle, walking, sleeping, torch)
- `tilesheet/` - Terrain and object tilesheets
- `sounds/` - Audio files (footsteps, ambient, weather)
- `fonts/` - SpriteFont definitions
- `data/` - JSON data files (quests, journal entries, PoI definitions)
  - `poi_animals.json` - Animal PoI definitions with sprites, biomes, behaviors
  - `poi_buildings.json` - Building PoI definitions with lighting, services
  - `poi_gatherables.json` - Gatherable plant definitions with harvest data
  - `poi_monsters.json` - Monster PoI definitions with threat levels
  - `poi_npcs.json` - NPC PoI definitions with dialogue, services
  - `poi_config.json` - Biome spawn rates and rarity weights
- `ui/` - UI textures and sprites
- `Content.mgcb` - MonoGame Content Pipeline configuration

## Architecture Patterns



## Enums (`GameEnums.cs` and `PointOfInterest.cs`)
Centralized enum definitions for:
- `TerrainType` - Grass, Dirt, Water, Stone
- `ObjectType` - Trees, Rocks, Plants, Bushes
- `BiomeType` - Forest, Lake, Mountain, DenseForest, Plains, Swamp
- `Direction` - North, South, East, West
- `WeatherType` - Clear, Rain, Snow, Fog
- `TimeOfDay` - Day, Night
- `PoIType` - 108+ point of interest types (NPCs, Buildings, Animals, Monsters, Gatherables)
- `InteractionType` - Conversation, Building, Exploration, Animal, Combat, etc.
- `QuestObjectiveType` - Quest goal types

## Data-Driven Architecture

### PoI System (98% JSON-driven)
The PoI system uses JSON files for data-driven content:
- **Primary Source**: JSON definitions in `Content/data/poi_*.json`
- **Fallback**: Minimal hardcoded values for legacy types (BerryBush, DeadTreeLandmark)
- **Benefits**: Easy content updates, modding support, no recompilation needed

### JSON Loading Priority
1. Check JSON definitions first (animals, monsters, buildings, NPCs, gatherables)
2. Fall back to hardcoded values only for unmigrated types
3. Sprite coordinates, names, descriptions, and behaviors all loaded from JSON
4. Biome spawn rates and lighting configurations defined in JSON

## Coordinate System
- Tile size: 32x32 pixels
- World coordinates: Pixel-based (tile * 32)
- Zone coordinates: Tile-based grid
- Virtual resolution: 1024x768 (default), supports aspect ratio switching
