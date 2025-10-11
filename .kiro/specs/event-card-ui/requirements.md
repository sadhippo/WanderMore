# Requirements Document

## Introduction

The EventCardBox UI system provides an interactive card-based interface for displaying story events, quest interactions, and choice-driven encounters. This system creates immersive moments when players interact with Points of Interest (PoIs) or reach specific quest milestones, allowing for branching narratives and meaningful player choices.

## Requirements

### Requirement 1: Core EventCardBox Component

**User Story:** As a player, I want to see an attractive event card popup when I interact with important NPCs or locations, so that I can make meaningful choices that affect my adventure.

#### Acceptance Criteria

1. WHEN an event is triggered THEN the system SHALL display a modal EventCardBox that overlays the game world
2. WHEN the EventCardBox is displayed THEN the game simulation SHALL pause (adventurer movement, time progression, weather updates)
3. WHEN the EventCardBox shows THEN it SHALL contain an event image, title, description text, and choice buttons
4. WHEN the EventCardBox appears THEN it SHALL use consistent styling with existing UI components (fonts, colors, borders)
5. WHEN the EventCardBox is active THEN background game elements SHALL be dimmed or blurred to focus attention

### Requirement 2: Dialogue-Based Conversation System

**User Story:** As a player, I want to engage in conversations with NPCs through dialogue options, so that I can continue conversations, receive rewards, or progress quests based on my choices.

#### Acceptance Criteria

1. WHEN an event card displays THEN it SHALL show quest/dialogue text at the top and 2-4 dialogue option buttons below
2. WHEN I click a dialogue option THEN it SHALL either continue the conversation with new text or provide a reward/outcome
3. WHEN a dialogue continues THEN the card SHALL update with new quest text and new dialogue options
4. WHEN a dialogue option provides a reward THEN the system SHALL display feedback and update player stats/quest progress
5. WHEN a conversation ends THEN the card SHALL close and return control to the player

### Requirement 3: Auto-Timeout Mechanism

**User Story:** As a player, I want the game to continue automatically if I don't make a choice within a reasonable time, so that the game doesn't get stuck waiting for my input.

#### Acceptance Criteria

1. WHEN an EventCardBox is displayed THEN it SHALL start a 60-second countdown timer
2. WHEN the timer reaches zero THEN the system SHALL automatically select a random available choice
3. WHEN auto-selection occurs THEN the system SHALL provide visual feedback indicating which choice was made
4. WHEN the timer is active THEN it SHALL display a subtle countdown indicator (progress bar or timer text)
5. WHEN I interact with any choice THEN the timer SHALL be cancelled

### Requirement 4: PoI Interaction Triggering

**User Story:** As a player, I want event cards to appear when my adventurer interacts with Points of Interest that have associated events, so that I can engage with the world through meaningful dialogue.

#### Acceptance Criteria

1. WHEN my adventurer interacts with a PoI THEN the system SHALL check if that PoI has an associated event/dialogue
2. WHEN a PoI has an associated event THEN the EventCardBox SHALL automatically appear with the appropriate dialogue
3. WHEN the EventCardBox is triggered by PoI interaction THEN the game SHALL pause and focus on the dialogue
4. WHEN the dialogue ends THEN control SHALL return to the adventurer and gameplay SHALL resume
5. WHEN a PoI has no associated event THEN the existing interaction behavior SHALL continue unchanged

### Requirement 5: Quest Integration

**User Story:** As a developer, I want the EventCardBox to integrate seamlessly with the quest system, so that quest events can trigger cards and process results.

#### Acceptance Criteria

1. WHEN a quest objective involves NPC interaction THEN it SHALL be able to trigger an EventCardBox
2. WHEN an event card choice is made THEN the QuestManager SHALL receive the choice data for processing
3. WHEN quest branching occurs THEN the EventCardBox SHALL support multiple outcome paths
4. WHEN event results are processed THEN they SHALL integrate with existing quest reward and progression systems
5. WHEN quest completion occurs through dialogue THEN it SHALL update quest status appropriately

### Requirement 5: Data-Driven Dialogue System

**User Story:** As a content creator, I want to define dialogue trees through data files, so that I can easily create branching conversations and rewards without code changes.

#### Acceptance Criteria

1. WHEN creating dialogues THEN they SHALL be defined in JSON data files with conversation nodes and branching paths
2. WHEN a dialogue node is loaded THEN it SHALL support quest text, dialogue options, and outcome specifications
3. WHEN dialogue options are defined THEN they SHALL specify either next dialogue nodes or reward outcomes (stats, quest progress, items)
4. WHEN conversations reference rewards THEN they SHALL integrate with existing reward systems (experience, mood, quest completion)
5. WHEN dialogue trees are complex THEN they SHALL support conditional branching based on player stats or quest states

### Requirement 6: Visual Polish and Animation

**User Story:** As a player, I want event cards to appear and disappear smoothly, so that the experience feels polished and engaging.

#### Acceptance Criteria

1. WHEN an EventCardBox appears THEN it SHALL animate in with a smooth transition (fade in, scale up)
2. WHEN an EventCardBox closes THEN it SHALL animate out gracefully
3. WHEN hovering over choice buttons THEN they SHALL provide visual feedback (highlight, scale)
4. WHEN the card is displayed THEN it SHALL have appropriate shadows and depth to feel modal
5. WHEN animations play THEN they SHALL be fast enough to feel responsive (< 300ms)

### Requirement 7: Accessibility and Input Handling

**User Story:** As a player, I want to interact with event cards using both mouse and keyboard, so that the interface is accessible and convenient.

#### Acceptance Criteria

1. WHEN an EventCardBox is active THEN I SHALL be able to click choice buttons with the mouse
2. WHEN using keyboard input THEN I SHALL be able to navigate choices with arrow keys or number keys
3. WHEN a choice is highlighted THEN it SHALL have clear visual indication
4. WHEN pressing Enter or Space THEN it SHALL activate the currently highlighted choice
5. WHEN pressing Escape THEN it SHALL close the card (if a "dismiss" option exists)

### Requirement 8: Conversation Flow Management

**User Story:** As a player, I want conversations to flow naturally with multiple exchanges, so that I can have meaningful interactions with NPCs that feel like real dialogue.

#### Acceptance Criteria

1. WHEN starting a conversation THEN the system SHALL load the appropriate dialogue tree for the NPC/quest context
2. WHEN I select a dialogue option THEN it SHALL either advance to the next conversation node or trigger an outcome
3. WHEN a conversation continues THEN the card SHALL smoothly update the quest text and present new dialogue options
4. WHEN reaching a conversation endpoint THEN it SHALL provide appropriate rewards, quest updates, or story conclusions
5. WHEN conversations have multiple paths THEN player choices SHALL determine which branches are available

### Requirement 9: Integration with Existing Systems

**User Story:** As a developer, I want the EventCardBox to work harmoniously with existing game systems, so that it enhances rather than disrupts the player experience.

#### Acceptance Criteria

1. WHEN an EventCardBox is active THEN it SHALL properly pause the game loop components (adventurer, time, weather)
2. WHEN the card closes THEN it SHALL resume all paused systems seamlessly
3. WHEN events trigger THEN they SHALL integrate with the existing journal system for logging
4. WHEN stat changes occur THEN they SHALL use the existing StatsManager
5. WHEN audio is needed THEN it SHALL use the existing AudioManager for sound effects