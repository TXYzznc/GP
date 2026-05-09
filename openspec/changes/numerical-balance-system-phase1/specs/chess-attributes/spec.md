## MODIFIED Requirements

### Requirement: Summon chess piece base attributes
The system SHALL define base attributes for all chess pieces using a standardized three-dimensional attribute point model. Each chess piece has five base attributes across three advancement tiers: MaxHP, AtkDamage, Armor, MagicResist, and SpellPower. Attributes at each tier are calculated as (base_value × tier_multiplier). All attributes within a quality tier SHALL sum to the quality's total attribute point value (Blue=2.4, Purple=2.7, Gold=3.0, Rainbow=3.3+).

#### Scenario: Attributes scale correctly across tiers
- **WHEN** viewing Houyi (Blue quality, attack-type) attributes at each tier
- **THEN** Tier 1: AtkDamage 10, Armor 20, others distributed, total 2.4 points
- **WHEN** advancing to Tier 2
- **THEN** all values are multiplied by 1.6x: AtkDamage 16, Armor 32, etc.
- **WHEN** advancing to Tier 3
- **THEN** all values are multiplied by 2.4x: AtkDamage 24, Armor 48, etc.

#### Scenario: Quality determines attribute distribution
- **WHEN** examining Chang'e (Purple quality, support-type) Tier 1 attributes
- **THEN** HP 200, Armor 100, others distributed to sum to 2.7 points
- **WHEN** examining Houyi (Purple quality, attack-type) Tier 1 attributes
- **THEN** AtkDamage 110, AtkSpeed 70, HP 90, summing to 2.7 points (different distribution than support)

#### Scenario: Tier 3 advancement enables significant power increase
- **WHEN** a chess piece reaches Tier 3
- **THEN** its combat power (FC = HP × DPS) is approximately 2.4x that of Tier 1
- **WHEN** comparing across qualities
- **THEN** Gold Tier 3 is approximately 13-15x more powerful than Blue Tier 1

### Requirement: Attribute consistency within quality tiers
The system configuration SHALL enforce that chess pieces of the same quality have consistent total attribute points. No chess piece of a given quality SHALL exceed or fall below the defined point total (with ±0.1 tolerance for Boss-tier units allowing 3.3-3.5 points).

#### Scenario: Quality standards are enforced
- **WHEN** creating or validating new chess piece configurations
- **THEN** the system verifies: Blue sum = 2.4 ± 0.01, Purple sum = 2.7 ± 0.01, Gold = 3.0 ± 0.01, Rainbow = 3.3+ ± 0.1

#### Scenario: Super-powerful units have controlled over-allocation
- **WHEN** creating a Boss-tier chess piece (e.g., Dark Yangjianing)
- **THEN** its attribute point total is allowed to be 3.5 (super-model), but not 5.0

### Requirement: Concrete attribute values for all chess pieces
The system configuration SHALL specify concrete HP, Attack, and other attribute values for all existing chess pieces (Houyi, Chang'e, Evil Spirit, etc.) following the standardized point model and linear tier scaling.

#### Scenario: New player sees normalized attributes
- **WHEN** a player examines their summoned chess pieces
- **THEN** Blue quality pieces show consistent attribute distributions (e.g., all Blue attackers have similar stat profiles)

#### Scenario: Attribute imbalance is corrected
- **WHEN** examining Chang'e's previous broken progression (Tier 1: 30 HP, Tier 3: 580 HP, ratio 19.3x)
- **THEN** it is corrected to follow the linear model (Tier 1: ~300 HP, Tier 3: ~720 HP, ratio 2.4x)
