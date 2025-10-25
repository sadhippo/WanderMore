# Requirements Document

## Introduction

Complete the migration of the Point of Interest (PoI) system from hardcoded C# definitions to a flexible JSON-based configuration system. The current implementation is partially migrated but not loading PoIs properly, so we need to finish the migration and ensure all existing PoI types are preserved and working.

## Requirements

### Requirement 1

**User Story:** As a developer, I want all PoI types (gatherables, buildings, NPCs, animals, monsters) to be defined in JSON files, so that I can easily add new content without modifying C# code.

#### Acceptance Criteria

1. WHEN the game loads THEN all existing PoI types SHALL be available from JSON definitions
2. WHEN I add a new gatherable plant to poi_gatherables.json THEN it SHALL spawn in appropriate biomes
3. WHEN I add a new building to poi_buildings.json THEN it SHALL appear with correct sprites and interactions
4. WHEN I add a new NPC to poi_npcs.json THEN it SHALL spawn with proper dialogue and lighting
5. IF a JSON file is missing or malformed THEN the system SHALL fallback gracefully without crashing

### Requirement 2

**User Story:** As a player, I want to see the same variety of PoIs as before the migration, so that the game world remains rich and diverse.

#### Acceptance Criteria

1. WHEN I explore different biomes THEN I SHALL encounter all previously available PoI types
2. WHEN I interact with gatherable plants THEN they SHALL provide items as before
3. WHEN I approach NPCs and buildings THEN they SHALL have appropriate lighting effects
4. WHEN I harvest resources THEN they SHALL be one-time only as designed
5. WHEN I visit different biomes THEN each SHALL have biome-appropriate PoI distributions

### Requirement 3

**User Story:** As a developer, I want the PoI system to use the JSON data for all sprite mapping, names, and descriptions, so that content creators can modify everything through data files.

#### Acceptance Criteria

1. WHEN a PoI is rendered THEN its sprite SHALL come from JSON sprite definitions
2. WHEN a PoI is interacted with THEN its name and description SHALL come from JSON
3. WHEN PoIs spawn in biomes THEN spawn weights SHALL be respected from JSON
4. WHEN the system loads THEN it SHALL log successful loading of all JSON files
5. IF sprite coordinates are invalid THEN the system SHALL log warnings but continue

### Requirement 4

**User Story:** As a content creator, I want comprehensive JSON files that include all existing PoI types with complete data, so that I have a full reference for creating new content.

#### Acceptance Criteria

1. WHEN I examine poi_gatherables.json THEN it SHALL contain all 25+ gatherable plants from the helper file
2. WHEN I examine poi_buildings.json THEN it SHALL contain all building types (farmhouse, inn, cottage, castle, mine, etc.)
3. WHEN I examine poi_npcs.json THEN it SHALL contain all NPC types (ranger, priest, warrior, hermit, etc.)
4. WHEN I examine poi_config.json THEN it SHALL contain biome spawn rates and rarity weights
5. WHEN I examine any JSON file THEN it SHALL have consistent structure and complete data fields