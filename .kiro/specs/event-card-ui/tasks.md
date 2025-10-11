# Implementation Plan

- [x] 1. Create core EventCardBox UI component





  - Create EventCardBox class with horizontal layout (80% screen width, 60% height)
  - Implement image area (200x200px) and text area layout calculation
  - Add basic rendering with background, borders, and text display
  - _Requirements: 1.1, 1.4, 1.5_

- [x] 2. Implement dialogue choice button system





  - Create choice button rendering and layout (full text width, 40px height)
  - Add mouse click detection for choice selection
  - Implement button hover effects and visual feedback
  - _Requirements: 2.1, 2.2, 7.1, 7.3_

- [x] 3. Add auto-timeout mechanism





  - Implement 60-second countdown timer with visual indicator
  - Add random choice selection when timer expires
  - Display timeout feedback and cancel timer on user interaction
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. Create dialogue data structures






  - Define DialogueNode and DialogueChoice classes
  - Add EventImage property for image display (reference: `/Content/events/demo.png`)
  - Create simple JSON structure for dialogue trees (verify PoIType enum values first)
  - _Requirements: 5.1, 5.2, 5.3_

- [x] 5. Implement DialogueManager system





  - Create DialogueManager class with dialogue loading
  - Add conversation flow management (node transitions)
  - Implement choice outcome processing (stats, quests, journal)
  - _Requirements: 4.3, 4.4, 8.4_

- [x] 6. Integrate with PoI system for event triggering





  - Subscribe to PoIManager.PoIInteracted event to detect adventurer-PoI interactions
  - Check if interacted PoI has associated dialogue/event data
  - Trigger EventCardBox when PoI has event, otherwise continue normal PoI behavior
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 6.1. Add QuestManager integration for rewards


  - Implement reward processing through existing quest and stats systems
  - Add quest progression updates from dialogue choices
  - Connect dialogue outcomes to journal entries
  - _Requirements: 5.2, 5.3, 5.4, 5.5_

- [x] 7. Add game state pause/resume functionality





  - Pause adventurer movement, time, and weather when dialogue opens
  - Resume all systems when dialogue closes
  - Ensure smooth transition between game and dialogue states and that dialogue stats can function while paused.
  - _Requirements: 1.2, 9.2, 9.3, 9.4, 9.5_

- [x] 8. Create sample dialogue content






  - Create JSON file with sample event dialogues (check PoIType enum for valid PoI types), example "you have arrived at a mysterious inn. it appeared out of nowhere but the smell of fresh roast drifts out the window. Do you stay the night?" Yes/No or "You have met a friendly ranger. Do you wish to stay the night at his campfire and learn more about this forest? Yes/No, hunger satisfied
  - Use demo event image at `/Content/events/demo.png` for testing
  - Test dialogue trees with branching conversations and rewards
  - _Requirements: 5.4, 8.1, 8.2, 8.3_

- [x] 9. Polish visual presentation and animations





  - Add smooth fade-in/out transitions for dialogue box
  - Implement button hover and press animations
  - Add background dimming effect when dialogue is active
  - Recommend other easy visual polish to event box
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 10. Test and refine the complete system






  - Test dialogue triggering from various NPCs and quest events
  - Verify conversation flow, choice outcomes, and reward integration
  - Ensure timeout behavior works correctly with random selection
  - _Requirements: All requirements validation_