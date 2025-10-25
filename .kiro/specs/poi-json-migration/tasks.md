# Implementation Plan

- [x] 1. Fix PoIDataLoader loading issues






  - Debug and fix JSON file loading methods
  - Add comprehensive error handling and logging
  - Ensure all definition collections are properly initialized
  - _Requirements: 1.5, 3.4_

- [x] 2. Complete poi_gatherables.json with all plants





  - Add all 25 gatherable plants from PoI_Helper.md
  - Include complete sprite coordinates, harvest data, and biome assignments
  - Set appropriate spawn weights based on rarity
  - _Requirements: 4.1_

- [x] 3. Complete poi_buildings.json with all building types





  - Add all building PoI types (farmhouse, inn, cottage, castle, mine, etc.)
  - Include sprite coordinates, services, and lighting configurations
  - Set biome-appropriate spawn weights
  - _Requirements: 4.2_

- [x] 4. Complete poi_npcs.json with all NPC types





  - Add all NPC PoI types (ranger, priest, warrior, hermit, mermaid, etc.)
  - Include sprite coordinates, dialogue topics, and lighting
  - Set biome-appropriate spawn weights
  - _Requirements: 4.3_

- [x] 5. Update PoIManager to use JSON data completely





  - Modify GetAvailablePoITypes to load from all JSON categories
  - Update sprite name and sheet mapping to prioritize JSON definitions
  - Ensure biome spawn rates use JSON configuration
  - _Requirements: 1.1, 3.1, 3.3_

- [ ] 6. Update PointOfInterest to use JSON data for interactions





  - Modify name and description generation to use JSON data
  - Update harvest mechanics to use JSON harvest configurations
  - Ensure interaction messages come from JSON descriptions
  - _Requirements: 3.2_

- [ ] 7. Test and validate the complete system






  - Verify all PoI types spawn correctly in appropriate biomes
  - Test sprite rendering and interaction mechanics
  - Confirm harvest mechanics work as designed
  - Validate graceful fallback behavior
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_