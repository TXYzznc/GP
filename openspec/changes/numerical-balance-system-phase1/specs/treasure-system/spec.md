## ADDED Requirements

### Requirement: Treasure equipment system (Onmyoji-style)
The system SHALL implement a permanent treasure (treasure box/spirit equipment) system where each chess piece can equip up to 2 treasures. Treasures provide persistent attribute bonuses and cannot be consumed during battle. Treasures MAY be unequipped and re-equipped to other chess pieces.

#### Scenario: Chess piece can equip treasures
- **WHEN** a player assigns a treasure to a chess piece
- **THEN** the treasure's bonuses (main attribute + sub-attributes) are applied to that piece

#### Scenario: Treasures can be swapped between pieces
- **WHEN** a player unequips a treasure from chess piece A
- **THEN** they can immediately equip it to chess piece B

#### Scenario: Combat power increases with equipped treasures
- **WHEN** a Tier 3 chess piece with a 3-star treasure gains approximately +500 attack and +150 equivalent attack from sub-attributes
- **THEN** its combat power increases from ~31,633 to ~51,505 (1.63x multiplier)

### Requirement: Treasure quality tiers with progression
The system SHALL support five treasure quality tiers: 1-star (+200 main attr, 0 sub-attrs, Lv10 cap), 2-star (+350 main attr, 1 sub-attr, Lv15 cap), 3-star (+500 main attr, 2 sub-attrs, Lv20 cap), 4-star (+750 main attr, 3 sub-attrs, Lv25 cap), 5-star (+1000 main attr, 4 sub-attrs, Lv30 cap).

#### Scenario: Treasures drop at different rarities
- **WHEN** defeating enemies in normal dungeons
- **THEN** treasures drop as 1-3 star quality

#### Scenario: Higher quality treasures have higher attribute caps
- **WHEN** comparing two treasures at their max levels
- **THEN** a 5-star treasure at Lv30 provides approximately 2.5x more main attribute than a 1-star at Lv10

#### Scenario: Sub-attributes increase treasure value
- **WHEN** a 3-star treasure has 2 sub-attributes (e.g., crit rate +10%, dodge +5%)
- **THEN** it becomes more valuable than a 3-star with 0 sub-attributes

### Requirement: Treasure enhancement/upgrade system (Phase 2, Configuration only)
The system configuration SHALL define upgrade costs using the formula: Cost = (1000 + currentLevel×500) × (1 + 0.5×starRating). This system is designed but implementation is deferred to Phase 2.

#### Scenario: Upgrade costs increase with level and quality
- **WHEN** upgrading a 2-star treasure from Lv1 to Lv2
- **THEN** the cost is (1000 + 1×500) × (1 + 0.5×2) = 3000 gold

#### Scenario: Higher quality treasures cost more to upgrade
- **WHEN** upgrading a 4-star treasure from Lv15 to Lv16
- **THEN** the cost is (1000 + 15×500) × (1 + 0.5×4) = 25,500 gold (much higher than 2-star equivalent)

### Requirement: Treasure set effects (Configuration design only)
The system configuration SHALL define 5 treasure set effects: Fire Set (all team attack +15%), Ice Set (all team defense +15%), Wood Set (all team HP +20%), Thunder Set (all team crit rate +20%), Light Set (all team damage reduction +10%). Specific implementation is deferred.

#### Scenario: Set bonus triggers when 3 treasures of same element are equipped
- **WHEN** a team has 3 pieces equipped with Fire-element treasures
- **THEN** all pieces in that team gain +15% attack bonus

#### Scenario: Set bonuses scale with team composition
- **WHEN** a player equips treasures across 5 chess pieces
- **THEN** they can potentially activate multiple set effects for different teams
