## ADDED Requirements

### Requirement: TreasureContent displays all player treasures
The TreasureContent panel SHALL display all treasures from player's warehouse and inventory in a grid layout.

#### Scenario: Treasure list loads on tab switch
- **WHEN** user clicks TreasureSwitchBtn to show TreasureContent
- **THEN** TreasureContent loads and displays all player treasures (warehouse + inventory combined)

#### Scenario: Treasure grid supports scrolling
- **WHEN** treasure count exceeds grid capacity
- **THEN** TreasureContent supports scrolling to view all treasures

#### Scenario: Each treasure shows item info
- **WHEN** treasure is displayed in grid
- **THEN** TreasureItemUI shows treasure icon, name, quality, and quantity

### Requirement: TreasureSwitchBtn toggles between ChessContent and TreasureContent
The TreasureSwitchBtn SHALL toggle display between the chess list and treasure repository.

#### Scenario: Default state shows chess list
- **WHEN** CharacterBagUI opens
- **THEN** ChessContent is visible, TreasureContent is hidden

#### Scenario: Switch to treasure view
- **WHEN** user clicks TreasureSwitchBtn
- **THEN** ChessContent hides, TreasureContent shows with all treasures

#### Scenario: Switch back to chess view
- **WHEN** user clicks TreasureSwitchBtn again
- **THEN** TreasureContent hides, ChessContent shows with previously selected chess still selected

### Requirement: Treasure can be dragged to equipment slots
The system SHALL support dragging treasures from TreasureContent to TreasureUI equipment slots.

#### Scenario: Drag treasure to empty slot
- **WHEN** user drags TreasureItemUI from TreasureContent to empty slot in TreasureUI
- **THEN** treasure is equipped to that slot, TreasureContent updates quantity

#### Scenario: Drag treasure to occupied slot
- **WHEN** user drags TreasureItemUI to an already occupied slot
- **THEN** the treasures swap (old treasure returns to inventory, new treasure equips)

#### Scenario: Invalid drag is rejected
- **WHEN** user tries to drag incompatible item or to invalid location
- **THEN** drag fails with visual feedback (no snap, returns to origin)

### Requirement: Equipped treasures update BaseEffect and SpecialEffect
When treasures are equipped, the TreasureUI SHALL immediately reflect changes in effects.

#### Scenario: Effects update after equipping
- **WHEN** user equips a treasure to a slot
- **THEN** BaseEffect and SpecialEffect in TreasureUI update to show new total effects
