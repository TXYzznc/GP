## ADDED Requirements

### Requirement: Treasure tab displays equipment slots
The Treasure tab SHALL display three treasure equipment slots (TreasureSlot1Arr[3]) where player can view equipped treasures.

#### Scenario: Three treasure slots are visible
- **WHEN** Treasure tab is active
- **THEN** three treasure slot containers are displayed in grid layout

#### Scenario: Treasure slots display equipped items
- **WHEN** a chess has treasures equipped
- **THEN** treasure slots show the equipped treasure items' icons and names

#### Scenario: Empty treasure slots show placeholders
- **WHEN** a treasure slot has no equipped item
- **THEN** slot shows an empty placeholder

### Requirement: BaseEffect displays total treasure effects
The BaseEffect text SHALL display the combined basic attributes from all equipped treasures.

#### Scenario: Base effects summary
- **WHEN** Treasure tab displays
- **THEN** BaseEffect shows aggregated basic attributes (e.g., "+500 HP", "+15% Crit Rate") from TreasureTable.BaseAttributes of all equipped treasures

#### Scenario: Empty base effects when no treasures
- **WHEN** chess has no equipped treasures
- **THEN** BaseEffect shows empty or "None" state

### Requirement: SpecialEffect displays special effects and synergies
The SpecialEffect text SHALL display special effects from equipped treasures and activated synergy effects.

#### Scenario: Special effects display
- **WHEN** Treasure tab displays
- **THEN** SpecialEffect shows all special effects from equipped treasures' SpecialEffectTable entries

#### Scenario: Synergy effects inclusion
- **WHEN** treasure synergies are activated (items match SynergyTable.RequireIds)
- **THEN** SpecialEffect appends synergy descriptions from SynergyTable

#### Scenario: Empty special effects state
- **WHEN** chess has no special effects or synergies
- **THEN** SpecialEffect shows empty or "None" state
