## ADDED Requirements

### Requirement: Summon chess piece quality system
The system SHALL support four quality tiers for summoned chess pieces: Blue (2.4 points), Purple (2.7 points), Gold (3.0 points), and Rainbow (3.3+ points). Each quality tier defines a standardized three-dimensional attribute point total that is distributed across attack, defense, spell power, and other base attributes.

#### Scenario: Quality affects attribute point total
- **WHEN** a chess piece is created with a specific quality tier
- **THEN** its total attribute point sum matches the quality tier value (Blue=2.4, Purple=2.7, Gold=3.0, Rainbow=3.3+)

#### Scenario: Different roles distribute points differently
- **WHEN** an attack-type chess piece (e.g., Houyi) has Purple quality
- **THEN** its attribute points (2.7) are distributed as Attack 1.1 + AttackSpeed 0.7 + HP 0.9 = 2.7
- **WHEN** a defensive-type chess piece (e.g., Chang'e) has Purple quality
- **THEN** its attribute points (2.7) are distributed as HP 1.5 + Defense 1.0 + AttackSpeed 0.2 = 2.7

#### Scenario: Quality determines character tier in the game
- **WHEN** viewing chess piece rarity in UI
- **THEN** Blue quality shows as "Common", Purple as "Rare", Gold as "Epic", Rainbow as "Legendary"

### Requirement: Quality impacts combat power difference
The system SHALL ensure that combat power difference between qualities is proportional and meaningful. The Gold quality chess piece's fighting capacity (FC = HP × DPS) SHALL be approximately 2.74 times that of a Blue quality chess piece at the same advancement tier.

#### Scenario: Combat power scales with quality
- **WHEN** comparing Blue (2.4pts) and Gold (3.0pts) Houyi at tier 1
- **THEN** Gold Houyi's FC ≈ 5488, Blue Houyi's FC ≈ 2000, ratio ≈ 2.74x

#### Scenario: Quality combinations create clear progression
- **WHEN** a player compares their Blue tier 1 chess piece with a Gold tier 3 chess piece
- **THEN** Gold tier 3 is significantly stronger (approximately 13-15x more powerful)
