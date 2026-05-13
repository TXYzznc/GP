## ADDED Requirements

### Requirement: Treasure item card displays treasure information
The TreasureItemUI card SHALL display treasure's icon, name, quality, and quantity.

#### Scenario: Card displays treasure info
- **WHEN** TreasureItemUI is instantiated for a treasure
- **THEN** it shows treasure icon, name, quality badge, and quantity count

#### Scenario: Card supports hover effects
- **WHEN** user hovers over a TreasureItemUI card
- **THEN** card shows hover visual feedback (glow, scale, or brightness change)

#### Scenario: Quality indicator shows treasure rarity
- **WHEN** card is rendered for different treasure qualities
- **THEN** quality indicator colors appropriately (color mapping from TreasureTable)

### Requirement: Treasure item card supports drag operations
The TreasureItemUI card SHALL act as a drag source for treasure equipment.

#### Scenario: Card can be dragged
- **WHEN** user presses and holds on TreasureItemUI
- **THEN** card enters drag mode with visual feedback (transparency, shadow, or glow)

#### Scenario: Drag shows visual preview
- **WHEN** card is being dragged
- **THEN** a preview image follows the cursor showing the dragged treasure

#### Scenario: Drag quantity indication
- **WHEN** card represents multiple treasures (quantity > 1)
- **THEN** during drag, it shows that one item is being dragged (not the whole stack)

### Requirement: Treasure item card updates quantity display
The TreasureItemUI card SHALL dynamically update its quantity display.

#### Scenario: Quantity updates after equip
- **WHEN** user equips a treasure from TreasureContent
- **THEN** that card's quantity decreases by 1, or card disappears if quantity becomes 0
