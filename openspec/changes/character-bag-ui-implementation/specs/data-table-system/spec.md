## MODIFIED Requirements

### Requirement: SummonChessSkillTable stores effect and description texts
The system SHALL extend SummonChessSkillTable to store both EffectText (brief effect) and DescText (full description) for skills.

#### Scenario: EffectText field provides brief skill effect
- **WHEN** skill data is loaded from SummonChessSkillTable
- **THEN** EffectText field contains brief effect description (e.g., "Deals 200% physical damage")

#### Scenario: DescText field provides full skill description
- **WHEN** skill data is loaded from SummonChessSkillTable
- **THEN** DescText field contains full description with mechanics details

#### Scenario: Legacy Desc field renamed to EffectText
- **WHEN** DataTableGenerator processes updated SummonChessSkillTable.xlsx
- **THEN** existing Desc column is renamed to EffectText in generated code

### Requirement: SummonChessTable stores background story
The system SHALL extend SummonChessTable to store chess background stories for different stages.

#### Scenario: StoryText field contains stage-specific stories
- **WHEN** chess data is loaded from SummonChessTable
- **THEN** StoryText field (string[]) contains three story entries (one per stage: 1/2/3)

#### Scenario: Story retrieval by stage
- **WHEN** UI requests chess's background story for a specific stage
- **THEN** StoryText array provides the correct story for that stage index
