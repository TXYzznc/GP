## ADDED Requirements

### Requirement: Linear attribute progression through three advancement tiers
The system SHALL support three advancement tiers for chess pieces (Tier 1, Tier 2, Tier 3). Attributes SHALL scale linearly across tiers using fixed multipliers: Tier 1 = 1.0x (base), Tier 2 = 1.6x (+60%), Tier 3 = 2.4x (+140%).

#### Scenario: Attributes scale proportionally across tiers
- **WHEN** a chess piece advances from Tier 1 to Tier 2
- **THEN** all of its base attributes (HP, Attack, Defense, etc.) are multiplied by 1.6

#### Scenario: Tier 3 is significantly stronger than Tier 1
- **WHEN** viewing Houyi at Tier 1 with 400 HP and Houyi at Tier 3 with 960 HP
- **THEN** the ratio is 2.4x (1.0 → 1.6 → 2.4 linear progression)

#### Scenario: Different qualities have different base values but same multipliers
- **WHEN** comparing Blue Houyi Tier 1 (400 HP) and Gold Houyi Tier 1 (560 HP)
- **THEN** both scale by 1.6x to Tier 2 and 2.4x to Tier 3, but Gold remains roughly 1.4x stronger at each tier

### Requirement: Advancement EXP costs ensure multiple-battle progression
The system SHALL require specific EXP amounts for advancement that enforce a meaningful progression curve. Tier 1→2 advancement requires 85 chess piece EXP. Tier 2→3 advancement requires 150 chess piece EXP. Total EXP to reach Tier 3 is 235, equivalent to defeating approximately 4 Tier 3 enemy chess pieces.

#### Scenario: Tier 1 to Tier 2 advancement requires multiple defeats
- **WHEN** a player's chess piece has earned 85 EXP (from defeating 3 Tier 3 enemies at ~60 EXP each)
- **THEN** the system displays "Ready to advance" and allows the player to promote to Tier 2

#### Scenario: Tier 2 to Tier 3 advancement requires significant effort
- **WHEN** a player's chess piece has earned 150 EXP at Tier 2 (from defeating 2-3 Tier 3 enemies)
- **THEN** the system allows promotion to Tier 3

#### Scenario: Total progression requires long-term engagement
- **WHEN** a player starts with a new chess piece at Tier 1
- **THEN** reaching Tier 3 requires at least 4 battles against equivalent enemies, creating a sense of achievement

### Requirement: Advancement tier is independent of quality
The system SHALL keep advancement tier (1/2/3) and quality (Blue/Purple/Gold/Rainbow) as separate dimensions. A chess piece's quality does not change upon advancement; only its attribute multiplier increases.

#### Scenario: Quality is permanent across tiers
- **WHEN** a Purple quality chess piece advances to Tier 2
- **THEN** it remains Purple quality, with attributes scaled by 1.6x
