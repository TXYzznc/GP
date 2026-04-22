## MODIFIED Requirements

### Requirement: Game state machine transitions properly during settlement
**Previously**: State transitions were immediate upon event triggers.
**Now**: Settlement orchestrates state transitions after settlement UI closes and scene loads.

The system SHALL ensure the game state machine properly transitions from in-combat states (CombatState, CombatPreparationState, ExploreState) to base state as part of the settlement flow.

#### Scenario: Deferred state transition during settlement
- **WHEN** settlement is triggered (death or teleport)
- **THEN** the current state is not popped immediately; instead, state transition is deferred until settlement completes

#### Scenario: State change on settlement completion
- **WHEN** settlement flow completes (UI closed + scene loaded)
- **THEN** system calls GF.FSM.PopState() or equivalent to exit the combat/explore state and transition to base state

#### Scenario: Base scene is in base state context
- **WHEN** base scene loads as a result of settlement
- **THEN** the game is in BaseState (or appropriate out-of-combat state) with base systems active

### Requirement: Scene state context aware of settlement trigger
The system SHALL track why the state transition occurred (death vs teleport) for optional behavior customization.

#### Scenario: Settlement trigger source logged
- **WHEN** settlement is triggered
- **THEN** settlement manager logs/stores trigger source ("death" or "teleport") for debugging and optional conditional logic

#### Scenario: State transition behaves consistently regardless of trigger source
- **WHEN** state transition occurs after settlement
- **THEN** the result is the same (base state, systems reset) regardless of whether trigger was death or teleport
