## ADDED Requirements

### Requirement: Add CancellationToken to all async MonoBehaviour methods
All async UniTask methods in MonoBehaviour subclasses SHALL accept and use CancellationToken from GetCancellationTokenOnDestroy() to prevent post-destruction execution.

#### Scenario: Async method with destruction safety
- **WHEN** async method is defined in MonoBehaviour
- **THEN** it includes `cancellationToken: this.GetCancellationTokenOnDestroy()` in delay/await calls

#### Scenario: Cancelled on object destruction
- **WHEN** MonoBehaviour is destroyed while async operation is pending
- **THEN** CancellationToken is triggered, async method throws OperationCanceledException (caught internally)

#### Scenario: No null reference after destruction
- **WHEN** async method would access destroyed object's properties post-destruction
- **THEN** CancellationToken prevents execution, no null reference exception occurs

### Requirement: Protect State and State-like classes
FSM StateBase subclasses (CombatState, CombatPreparationState) and similar state classes SHALL use CancellationToken in async methods for clean state transitions.

#### Scenario: CombatState async initialization
- **WHEN** CombatState.OnEnter launches async initialization
- **THEN** OnLeave can trigger state cancellation, preventing pending operations from interfering

### Requirement: Document CancellationToken pattern in CLAUDE.md
The project rules SHALL include clear pattern for CancellationToken usage in async MonoBehaviour methods as standard practice.

#### Scenario: Developer reference
- **WHEN** new developer writes async method in MonoBehaviour
- **THEN** they reference CLAUDE.md async-coding section and follow CancellationToken pattern

### Requirement: Add logging for cancelled operations
Cancelled async operations SHALL log at debug level, aiding in troubleshooting and ensuring cancellation is intentional.

#### Scenario: Cancellation logging
- **WHEN** CancellationToken triggers OperationCanceledException
- **THEN** method logs (debug level): "[ClassName] async operation cancelled during destruction"

### Requirement: Unit test CancellationToken behavior
Async MonoBehaviour methods with CancellationToken SHALL have unit tests verifying no execution occurs post-destruction.

#### Scenario: Destruction prevents execution
- **WHEN** unit test creates MonoBehaviour, starts async operation, then destroys it immediately
- **THEN** async method does not execute past the cancellation point; logging confirms cancellation
