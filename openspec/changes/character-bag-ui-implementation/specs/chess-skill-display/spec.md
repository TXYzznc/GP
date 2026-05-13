## ADDED Requirements

### Requirement: State tab displays chess skills
The State tab SHALL display five skill buttons: PassiveSkill, NormalAtk, Skill_1, Skill_2 (hidden by default), and UltimateSkill.

#### Scenario: Five skill buttons layout
- **WHEN** State tab is active
- **THEN** PassiveSkill, NormalAtk, Skill_1, UltimateSkill buttons are visible; Skill_2 is hidden by default

#### Scenario: Skill_2 visibility depends on chess configuration
- **WHEN** selected chess has Skill2Id configured in SummonChessTable
- **THEN** Skill_2 button is visible; otherwise it remains hidden

#### Scenario: Clicking skill button updates skill info
- **WHEN** user clicks any skill button
- **THEN** SkillEffectText and SkillDescText update to show the selected skill's effect and description

### Requirement: Skill effect text displays skill effects
The SkillEffectText SHALL show the brief effect description from SummonChessSkillTable.EffectText field.

#### Scenario: Effect text updates on skill selection
- **WHEN** user selects a skill
- **THEN** SkillEffectText displays that skill's EffectText (e.g., "Deals 200% physical damage")

### Requirement: Skill description text displays full skill details
The SkillDescText SHALL show the full description from SummonChessSkillTable.DescText field.

#### Scenario: Description text updates on skill selection
- **WHEN** user selects a skill
- **THEN** SkillDescText displays that skill's DescText with complete mechanics explanation

### Requirement: State tab displays chess attributes
The State tab SHALL display selected chess's attributes (HP, Attack, Defense, Magic Resist, etc.).

#### Scenario: Attributes display for selected chess
- **WHEN** a chess is selected in State tab
- **THEN** StateText shows all relevant attributes from SummonChessTable for the chess's current stage
