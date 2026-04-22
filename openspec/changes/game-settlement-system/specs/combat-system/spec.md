## MODIFIED Requirements

### Requirement: Death detection triggers settlement instead of direct state exit
**Previously**: CombatState.OnCombatEnd() or death detection directly ended combat and returned to some default state.
**Now**: Death detection routes through SettlementManager.TriggerSettlement() with trigger source "death".

The system SHALL detect player death and initiate the settlement flow rather than exiting combat immediately.

#### Scenario: All player entities defeated triggers settlement
- **WHEN** all player entities reach 0 HP or are removed from combat
- **THEN** system detects this condition and calls SettlementManager.TriggerSettlement(baseScene, "death")

#### Scenario: Settlement data includes defeat marker
- **WHEN** settlement is triggered by death
- **THEN** settlement data is marked with `isDefeat=true` for proper UI and reward handling

#### Scenario: Combat state does not directly exit on death
- **WHEN** player death is detected
- **THEN** CombatState does not call GF.FSM.PopState() or change state directly; settlement manager orchestrates the state change

#### Scenario: Combat rewards collected on defeat
- **WHEN** death settlement is triggered
- **THEN** system still collects combat rewards (experience, drops, gold) before settlement UI displays

### Requirement: Combat manager and entities properly cleaned up during settlement
The system SHALL ensure combat-related managers and entities are cleaned up when settlement transitions out of combat state.

#### Scenario: CombatManager disabled on settlement exit
- **WHEN** settlement completes and scene transition finishes
- **THEN** CombatManager is disabled and all turn management, card systems, and combat UI are cleaned up

#### Scenario: Enemy entities destroyed
- **WHEN** exiting combat state due to settlement
- **THEN** all enemy entity instances are properly destroyed and unloaded
