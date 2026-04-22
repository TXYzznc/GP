## ADDED Requirements

### Requirement: Transition from combat/exploration state to base state
The system SHALL change the game state from in-combat (CombatState / CombatPreparationState / ExploreState) to an appropriate out-of-combat base state.

#### Scenario: Exit combat state on death settlement
- **WHEN** player death settlement completes and scene transition finishes
- **THEN** game state changes from CombatState/CombatPreparationState to BaseState (or defined out-of-combat state)

#### Scenario: Exit exploration state on teleport settlement
- **WHEN** player teleports from exploration and settlement completes
- **THEN** game state changes from ExploreState to BaseState

#### Scenario: UI visibility changes with state
- **WHEN** game state transitions to base state
- **THEN** combat/exploration UI elements (combat manager, explorer controls, etc.) are disabled/hidden; base state UI becomes active

### Requirement: Manage controller and input during state transition
The system SHALL properly enable/disable player controller and input based on the state change.

#### Scenario: Re-enable player controller on transition to base
- **WHEN** game state changes to base state
- **THEN** player controller is re-enabled (movement, interaction input)

#### Scenario: Input locked during settlement
- **WHEN** settlement UI is displayed
- **THEN** player input (except for close button) may be limited to prevent unintended actions

#### Scenario: Input restored after settlement closes
- **WHEN** settlement UI is closed and base scene is loaded
- **THEN** normal player input is fully restored

### Requirement: Reset scene-specific systems on state transition
The system SHALL perform necessary cleanup and reset of exploration/combat systems when leaving those states.

#### Scenario: Combat manager disabled on exit
- **WHEN** exiting combat state due to settlement
- **THEN** CombatManager is disabled, turn order reset, card system cleaned up

#### Scenario: Enemy entities cleared on exit
- **WHEN** exiting combat state
- **THEN** enemy entities are properly destroyed or disabled; exploration enemy data is cleaned up

#### Scenario: Base scene systems initialized on entry
- **WHEN** entering base state on new scene load
- **THEN** base-specific systems (inventory, character selection, etc.) are initialized

### Requirement: Handle UI state transitions
The system SHALL ensure proper UI layer management during state changes.

#### Scenario: Close all in-combat UI forms
- **WHEN** transitioning away from combat/exploration state
- **THEN** all associated UI forms (skill panels, combat menus, exploration tooltips) are closed cleanly

#### Scenario: Open base state UI forms
- **WHEN** entering base state
- **THEN** base UI (main menu, character panel, inventory, etc.) is displayed as configured
