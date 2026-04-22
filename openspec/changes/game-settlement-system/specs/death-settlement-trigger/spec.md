## ADDED Requirements

### Requirement: Detect player death condition
The system SHALL detect when the player has been completely defeated (all player entities eliminated) in combat.

#### Scenario: Single player entity defeat
- **WHEN** the player's single entity reaches 0 HP and death animation completes
- **THEN** system marks player as dead and triggers settlement

#### Scenario: Multi-entity party defeat
- **WHEN** all entities in player's party reach 0 HP or are removed from combat
- **THEN** system marks party as defeated and triggers settlement

#### Scenario: Defeat via status effect
- **WHEN** player entities are removed from battle due to status effects (e.g., banishment)
- **THEN** system counts this as defeat and triggers settlement if no entities remain

### Requirement: Trigger settlement on death
The system SHALL automatically initiate the settlement flow when player death is confirmed.

#### Scenario: Settlement starts on death confirmation
- **WHEN** death condition is detected in CombatState.OnCombatEnd() or death detection hook
- **THEN** system calls SettlementManager.TriggerSettlement(targetScene="BaseScene", triggerSource="death")

#### Scenario: Mark settlement as defeat
- **WHEN** settlement is triggered by death
- **THEN** settlement data is tagged with `isDefeat=true` for UI display (e.g., "Battle Lost" message)

#### Scenario: No combat state continuation after death trigger
- **WHEN** death settlement is triggered
- **THEN** CombatState exits cleanly and does not attempt to restart or show victory UI

### Requirement: Preserve limited rewards on defeat
The system SHALL still grant some rewards (e.g., partial experience, consolation items) even when the player loses.

#### Scenario: Partial experience on defeat
- **WHEN** settlement is triggered by death
- **THEN** player receives a percentage (e.g., 50%) of the experience they would earn from victory

#### Scenario: Item drops from defeated enemies
- **WHEN** settlement is triggered by death
- **THEN** items dropped by enemies during combat are still included in settlement rewards
