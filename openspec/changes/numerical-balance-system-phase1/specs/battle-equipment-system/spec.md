## ADDED Requirements

### Requirement: Battle equipment system (TFT-style)
The system SHALL implement a temporary battle equipment system where items drop during combat and provide immediate attribute bonuses. Equipment is specific to each battle instance and is automatically removed when the player exits combat.

#### Scenario: Equipment drops during battle
- **WHEN** defeating enemies or completing battle objectives
- **THEN** equipment items appear as battle rewards (separate from normal item drops)

#### Scenario: Equipment is equipped immediately upon pickup
- **WHEN** a player collects a dropped equipment item
- **THEN** the system automatically equips it to the nearest chess piece and shows attribute bonus notifications

#### Scenario: Equipment disappears after battle
- **WHEN** a player exits combat (victory, defeat, or escape)
- **THEN** all equipped battle equipment is removed from all chess pieces

#### Scenario: Equipment provides immediate combat advantage
- **WHEN** a chess piece receives a gold-tier equipment (+200 attack)
- **THEN** its combat power increases by 20-30%, potentially turning the battle outcome

### Requirement: Equipment quality and drop rates
The system configuration SHALL define three equipment quality tiers with drop probabilities: White (basic, 60% drop rate, +50 attack or similar), Blue (enhanced, 30% drop rate, +100 attack), Gold (superior, 10% drop rate, +200 attack).

#### Scenario: Equipment quality affects bonus magnitude
- **WHEN** comparing white equipment (+50 attack) with gold equipment (+200 attack)
- **THEN** gold is 4x more powerful, creating a meaningful difference in combat

#### Scenario: Equipment drop rates ensure variety
- **WHEN** playing multiple battles
- **THEN** white equipment is common (building blocks), blue is frequent (moderate boosts), gold is rare (game-changing)

### Requirement: Equipment types and attributes
The system SHALL support various equipment types including basic items (Sword: +50 attack, Shield: +50 defense, Boots: +20% move speed, Gloves: +15% crit rate) and special items with complex effects (Armor Break Sword: +75 attack, -5 armor, Frozen Armor: +75 defense, slow enemies 15%, Blood Sword: +100 attack, 20% life steal).

#### Scenario: Different equipment types serve different roles
- **WHEN** a chess piece needs offense boost
- **THEN** Sword or Armor Break Sword are more valuable than Shield

#### Scenario: Special equipment provides dual benefits
- **WHEN** equipping Blood Sword
- **THEN** both attack and life steal bonuses are applied simultaneously

### Requirement: Equipment composition/synthesis (Configuration design only)
The system configuration SHALL define equipment synthesis recipes such as Sword + Shield → Sword Shield, Sword + Gloves → Armor Break Sword. Implementation is deferred to Phase 3.

#### Scenario: Small items combine into larger items
- **WHEN** a player collects Sword and Shield during the same battle
- **THEN** the system offers to combine them into Sword Shield (attack +50, defense +50)

#### Scenario: Synthesis creates higher-value equipment
- **WHEN** combining items
- **THEN** the resulting equipment is strictly more valuable than its components
