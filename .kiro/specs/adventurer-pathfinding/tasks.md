# Adventurer Pathfinding Implementation Plan

## Task Overview

Convert the pathfinding design into a series of incremental coding tasks that enhance the existing Adventurer wandering behavior with intelligent PoI navigation and optional quest target biasing.

- [x] 1. Create pathfinding system and basic PoI detection





  - Create PathfindingManager class with state management (Wandering, Pathfinding, Moving)
  - Implement PoI detection logic with 5-tile (160px) detection range and cooldown system
  - Add pathfinding state tracking to existing Adventurer class
  - Create PoI type filtering for interactable vs non-interactable PoIs
  - _Requirements: 1.1, 1.2, 3.1, 3.2, 3.3_

- [x] 2. Implement simple pathfinding algorithm and integration





  - Create SimplePathfinder class with direct line pathfinding and basic obstacle avoidance
  - Add simple edge-following algorithm for navigating around obstacles using waypoint queue
  - Modify Adventurer.Update() to switch between wandering and pathfinding modes
  - Integrate pathfinding direction calculation with existing movement and collision systems
  - _Requirements: 2.1, 2.2, 2.3, 1.4, 4.2, 4.4_

- [x] 3. Add failure handling and quest system integration





  - Implement stuck detection, path abandonment, and timeout mechanisms for robust fallback
  - Create smooth transitions back to wandering mode after pathfinding completion or failure
  - Add quest target bias integration with existing QuestManager for location objectives
  - Create PathfindingConfig class with tunable parameters and enable/disable flag
  - _Requirements: 1.3, 1.5, 2.4, Quest integration, 4.5_

- [x] 4. Test and refine complete pathfinding behavior






  - Test pathfinding around various obstacles (buildings, terrain, water) and PoI interactions
  - Verify quest target biasing works with sample quests from QuestManager
  - Add console logging for debugging and adjust parameters for natural movement
  - Validate all requirements and ensure seamless integration with existing systems
  - _Requirements: All requirements validation_