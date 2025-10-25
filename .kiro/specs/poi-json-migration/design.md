# Design Document

## Overview

Complete the PoI system migration to JSON by fixing the current loading issues, populating all JSON files with existing PoI data, and ensuring the PoIManager correctly uses JSON data for all PoI operations. The system will maintain backward compatibility while providing a flexible data-driven approach.

## Architecture

### JSON File Structure
- **poi_gatherables.json** - All gatherable plants with harvest data, sprites, and biome assignments
- **poi_buildings.json** - Buildings with services, lighting, and interaction types  
- **poi_npcs.json** - NPCs with dialogue topics, services, and lighting
- **poi_config.json** - Global configuration for spawn rates and rarity weights

### Data Flow
1. PoIDataLoader loads all JSON files during game initialization
2. PoIManager uses loaded data for sprite definitions, name generation, and biome spawning
3. PointOfInterest uses JSON data for interaction messages and harvest configuration
4. System falls back to hardcoded values if JSON data is missing

## Components and Interfaces

### PoIDataLoader Updates
- Fix loading methods to handle missing files gracefully
- Add comprehensive logging for debugging
- Ensure all definition types are properly loaded and accessible

### PoIManager Integration
- Update GetAvailablePoITypes to use JSON data for all PoI categories
- Modify sprite name and sheet mapping to prioritize JSON definitions
- Ensure biome spawn rates use JSON configuration

### JSON Data Population
- Transfer all existing hardcoded PoI definitions to appropriate JSON files
- Include complete sprite coordinates, names, descriptions
- Set appropriate spawn weights and biome assignments

## Data Models

### Complete Gatherable Plant Data
All 25+ plants from PoI_Helper.md with:
- Sprite coordinates from the 5x5 grid
- Harvest items and quantities
- Rarity and category classifications
- Biome-specific spawn weights

### Building Definitions
All building types with:
- Sprite sheet references (buildings sheet)
- Service offerings (rest, trade, information)
- Lighting configurations
- Interaction types

### NPC Definitions  
All NPC types with:
- Sprite sheet references (poi sheet)
- Dialogue topic categories
- Service capabilities
- Lighting effects

## Error Handling

### Graceful Degradation
- Missing JSON files: Log warning, use empty collections
- Invalid JSON syntax: Log error, skip malformed entries
- Missing sprite coordinates: Log warning, use fallback sprites
- Invalid PoI types: Log warning, skip invalid entries

### Debugging Support
- Comprehensive logging during JSON loading
- Validation of sprite coordinates against sheet dimensions
- Reporting of successful vs failed PoI definitions loaded

## Testing Strategy

### Validation Tests
- Verify all existing PoI types still spawn correctly
- Confirm sprite rendering matches original implementation
- Test biome-specific PoI distributions
- Validate harvest mechanics work as before

### JSON Integrity Tests
- Ensure all JSON files parse correctly
- Verify sprite coordinates are within valid ranges
- Confirm all required fields are present
- Test fallback behavior with missing/corrupt files