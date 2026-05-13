## ADDED Requirements

### Requirement: Character Bag UI form manages three-area layout
The CharacterBagUI form SHALL display a three-area layout: left chess list, center portrait/model display with toggle button, and right detail tabs.

#### Scenario: UI opens and displays all areas
- **WHEN** CharacterBagUI is opened
- **THEN** left panel shows chess list, center shows portrait or 3D model (configurable), right panel shows detail tabs

#### Scenario: User switches between portrait and model mode
- **WHEN** user clicks SwitchBtn
- **THEN** center area toggles between NormalImage (portrait) and occupationImage (3D model)

#### Scenario: User selects a chess from list
- **WHEN** user clicks a chess card in the left panel
- **THEN** all right panels update to show that chess's data (attributes, skills, treasures, story)

### Requirement: Right panel supports four tabs
The detail panel on the right SHALL support four switchable tabs: State, Treasure, LevelUp, and Story.

#### Scenario: Tab switching
- **WHEN** user clicks StateBtn/TreasureBtn/LevelUpBtn/StoryBtn
- **THEN** corresponding StateUI/TreasureUI/LevelUpUI/StoryUI panel shows, others hide

#### Scenario: Tab content persists during tab switch
- **WHEN** user views State tab, switches to Treasure tab, then returns to State tab
- **THEN** State tab content remains unchanged (no data loss)

### Requirement: Close button closes the UI form
The CharacterBagUI form SHALL provide a close button to exit.

#### Scenario: User closes UI
- **WHEN** user clicks CloseBtn
- **THEN** CharacterBagUI form closes and returns to previous screen

### Requirement: Selected chess is highlighted
The CharacterBagUI SHALL visually indicate which chess is currently selected in the left list.

#### Scenario: Chess selection highlighting
- **WHEN** a chess is selected
- **THEN** that chess card shows visual feedback (border, highlight, or color change)
