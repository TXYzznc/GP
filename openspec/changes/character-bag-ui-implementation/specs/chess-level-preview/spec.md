## ADDED Requirements

### Requirement: LevelUp tab displays stage progression buttons
The LevelUp tab SHALL display four stage buttons: Stage 1, Stage 2, Stage 3A, Stage 3B (for viewing stage data without actual leveling).

#### Scenario: Four stage buttons layout
- **WHEN** LevelUp tab is active
- **THEN** four stage selection buttons are displayed (Stage 1, 2, 3A, 3B)

#### Scenario: Clicking stage button shows that stage's data
- **WHEN** user clicks a stage button
- **THEN** LevelUp_Base and LevelUp_Skill update to show that stage's attributes and skills

#### Scenario: Current stage button is highlighted
- **WHEN** chess is at a certain stage
- **THEN** that stage button shows visual feedback (border, highlight, or enabled state)

### Requirement: LevelUp_Base displays stage attributes
The LevelUp_Base text SHALL display all attributes (HP, Attack, Defense, etc.) for the selected stage from SummonChessTable.

#### Scenario: Stage attributes display
- **WHEN** user selects a stage
- **THEN** LevelUp_Base shows all attributes for that stage (each stage has 3 rows in SummonChessTable)

#### Scenario: Attributes update when stage changes
- **WHEN** user clicks different stage buttons
- **THEN** LevelUp_Base updates immediately to show the new stage's attributes

### Requirement: LevelUp_Skill displays stage skills
The LevelUp_Skill text SHALL display all skills available at the selected stage (passive, normal attack, skill 1/2, ultimate).

#### Scenario: Stage skills display
- **WHEN** user selects a stage
- **THEN** LevelUp_Skill shows skill names and IDs from SummonChessTable for that stage

#### Scenario: Skills update when stage changes
- **WHEN** user clicks different stage buttons
- **THEN** LevelUp_Skill updates to show the new stage's skills
