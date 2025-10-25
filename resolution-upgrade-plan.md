# Resolution & Screen Size Upgrade Plan

## Goal
Transform the current fixed resolution system into a modern, adjustable resolution system like other games, with:
- Multiple resolution options in settings
- Adjustable screen size (windowed/fullscreen)
- Proper UI scaling and positioning
- Settings persistence

## Current State Analysis

### Current System
- **VirtualResolution class** handles resolution independence
- **Fixed virtual resolution**: 1024x768 (Regular) / 576x1024 (TikTok)
- **Aspect ratio toggle**: R key switches between Regular/TikTok modes
- **UI components**: Some auto-update via `UpdateScreenSize()`, others have hardcoded values

### Components That Need Updates

#### 1. Core Resolution System
- [ ] `Utilities/VirtualResolution.cs` - Replace AspectRatioMode with proper resolution settings
- [ ] `Core/Game1.cs` - Update initialization and resolution change handling

#### 2. UI Components (Auto-updating)
- [ ] `Managers/UIManager.cs` - Already has UpdateScreenSize()
- [ ] `UI/InventoryUI.cs` - Already has UpdateScreenSize()
- [ ] `UI/EventCardBox.cs` - Already has UpdateScreenSize()
- [ ] `UI/EscapeMenu.cs` - Already has UpdateScreenSize()

#### 3. UI Components (Hardcoded - Need Fixes)

**Critical Hardcoded Values:**
- [ ] `UI/JournalUI.cs` - Has hardcoded `screenWidth = 1024; screenHeight = 768;`
- [ ] `UI/JournalUI.cs` - Hardcoded overlay `Rectangle(0, 0, 1024, 768)`
- [ ] `UI/InventoryUI.cs` - Hardcoded centering `(1024 - windowWidth) / 2, (768 - windowHeight) / 2`
- [ ] `UI/StatsPage.cs` - Takes screen dimensions in constructor, needs UpdateScreenSize()
- [ ] `Systems/WeatherEffects.cs` - Hardcoded `_screenBounds = new Rectangle(0, 0, 1024, 768)`
- [ ] `Core/Game1.cs` - Hardcoded initial window size `1024x768`
- [ ] `Core/Game1.cs` - Hardcoded VirtualResolution initialization `1024, 768`
- [ ] `Core/Game1.cs` - Hardcoded minimap positioning `VirtualWidth - 220, 10, 200, 200`

**UI Layout Constants (May Need Scaling):**
- [ ] `UI/StatsPage.cs` - `PAGE_WIDTH = 400, PAGE_HEIGHT = 500`
- [ ] `UI/JournalUI.cs` - Journal dimensions `600x500`
- [ ] `UI/EventCardBox.cs` - Image area `200x200px`
- [ ] `Managers/UIManager.cs` - Various UI areas with fixed positions:
  - Clock area, zone name area, weather area, pause button, etc.
- [ ] `UI/StatsHUD.cs` - Fixed positioning `startY = 170`, HUD area calculations

**VirtualResolution System:**
- [ ] `Utilities/VirtualResolution.cs` - Default constructor `virtualWidth = 1024, virtualHeight = 768`
- [ ] `Utilities/VirtualResolution.cs` - AspectRatioMode.Regular `1024x768`
- [ ] `Utilities/VirtualResolution.cs` - AspectRatioMode.TikTok `576x1024`

#### 4. Render Targets & Graphics
- [ ] Scene render target in Game1.cs
- [ ] LightingManager render targets
- [ ] WeatherEffects screen bounds
- [ ] Camera viewport

## Implementation Plan

### Phase 1: Settings System
Create a new settings system to handle resolution preferences:

#### New Classes Needed:
- [ ] `Data/GameSettings.cs` - Settings data structure
- [ ] `Managers/SettingsManager.cs` - Settings persistence and management
- [ ] `UI/SettingsMenu.cs` - Settings UI interface

#### Settings Structure:
```csharp
public class GameSettings
{
    // Display settings
    public int ScreenWidth { get; set; } = 1024;
    public int ScreenHeight { get; set; } = 768;
    public bool IsFullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
    
    // Common resolution presets
    public static readonly (int width, int height)[] CommonResolutions = {
        (800, 600),
        (1024, 768),
        (1280, 720),
        (1366, 768),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160)
    };
}
```

### Phase 2: Resolution System Overhaul

#### VirtualResolution Changes:
- [ ] Remove AspectRatioMode enum
- [ ] Add SetResolution(width, height, fullscreen) method
- [ ] Add GetSupportedResolutions() method
- [ ] Keep virtual resolution concept but make it configurable
- [ ] Add proper fullscreen support

#### Game1.cs Changes:
- [ ] Initialize SettingsManager
- [ ] Replace R key aspect ratio toggle with settings menu
- [ ] Add resolution change event handling
- [ ] Update all UpdateScreenSize() calls when resolution changes

### Phase 3: UI System Updates

#### Fix Hardcoded Components:
- [ ] JournalUI: Remove hardcoded screen size, add UpdateScreenSize()
- [ ] StatsPage: Add UpdateScreenSize() method
- [ ] MiniMap: Make positioning dynamic

#### Settings Menu Integration:
- [ ] Add settings button to EscapeMenu
- [ ] Create resolution dropdown/list
- [ ] Add fullscreen toggle
- [ ] Add apply/cancel functionality
- [ ] Add "restart required" warnings if needed

### Phase 4: Testing & Polish

#### Test Cases:
- [ ] All common resolutions (16:9, 4:3, ultrawide)
- [ ] Windowed to fullscreen transitions
- [ ] UI positioning at different resolutions
- [ ] Settings persistence across game restarts
- [ ] Performance with different resolutions

#### Edge Cases:
- [ ] Very small resolutions (800x600)
- [ ] Very large resolutions (4K+)
- [ ] Unusual aspect ratios
- [ ] Multi-monitor setups

## Technical Considerations

### Virtual Resolution Strategy
**Option A**: Fixed virtual resolution with scaling
- Keep 1024x768 as base, scale everything
- Pros: No UI repositioning needed
- Cons: May look blurry at non-native resolutions

**Option B**: Dynamic virtual resolution
- Virtual resolution matches actual resolution
- Pros: Crisp rendering at all resolutions
- Cons: Need to reposition all UI elements

**Recommendation**: Start with Option A, consider Option B later

### Settings Storage
- Use JSON file in user data directory
- Load on startup, save on changes
- Provide defaults for first run

### Backwards Compatibility
- Keep existing R key functionality during development
- Migrate existing hardcoded values gradually
- Ensure game works if settings file is missing

## File Structure Changes

### New Files:
```
Data/
  GameSettings.cs
Managers/
  SettingsManager.cs
UI/
  SettingsMenu.cs
```

### Modified Files:
```
Utilities/
  VirtualResolution.cs (major changes)
Core/
  Game1.cs (initialization and event handling)
UI/
  JournalUI.cs (remove hardcoded values)
  StatsPage.cs (add UpdateScreenSize)
  EscapeMenu.cs (add settings button)
Managers/
  UIManager.cs (settings menu integration)
```

## Implementation Order

1. **Create GameSettings and SettingsManager** (foundation)
2. **Update VirtualResolution class** (core functionality)
3. **Fix hardcoded UI components** (compatibility)
4. **Create SettingsMenu UI** (user interface)
5. **Integrate with Game1.cs** (wire everything together)
6. **Test and polish** (quality assurance)

## Complete Hardcoded Values Audit

### Files with Critical Hardcoded Resolution Dependencies:

**Core System Files:**
1. `Utilities/VirtualResolution.cs` - Multiple 1024x768 and 576x1024 references
2. `Core/Game1.cs` - Initial window size, VirtualResolution init, minimap positioning
3. `Systems/WeatherEffects.cs` - Screen bounds rectangle

**UI Files Needing UpdateScreenSize() Methods:**
4. `UI/JournalUI.cs` - Hardcoded screen size AND overlay rectangle
5. `UI/StatsPage.cs` - Constructor takes screen size, needs update method
6. `UI/InventoryUI.cs` - Hardcoded centering calculations

**UI Files with Fixed Layout Constants:**
7. `Managers/UIManager.cs` - Clock, weather, pause button positioning
8. `UI/StatsHUD.cs` - HUD positioning and layout
9. `UI/EventCardBox.cs` - Image area sizing
10. `UI/MiniMap.cs` - Positioning logic in Game1.cs

### Priority Order for Fixes:
**Phase 1 (Critical):** Files 1-3 (Core system)
**Phase 2 (High):** Files 4-6 (UI with hardcoded screen refs)  
**Phase 3 (Medium):** Files 7-10 (UI layout constants)

## Questions to Resolve

- [ ] Should we support custom resolutions or only presets?
- [ ] Do we need different UI scaling factors for very high resolutions?
- [ ] Should settings changes apply immediately or require restart?
- [ ] Do we want to detect monitor capabilities automatically?
- [ ] Should we keep the TikTok aspect ratio mode or remove it?
- [ ] **NEW:** Should UI elements scale proportionally or maintain fixed pixel sizes?
- [ ] **NEW:** Do we want minimum/maximum resolution limits?

## Success Criteria

- [ ] Player can choose from multiple resolution options
- [ ] Fullscreen/windowed toggle works properly
- [ ] All UI elements position correctly at different resolutions
- [ ] Settings persist between game sessions
- [ ] No performance regression
- [ ] Existing gameplay unchanged