## ADDED Requirements

### Requirement: Replace async void with UniTask in ChessPlacementManager
All async void methods in ChessPlacementManager SHALL be replaced with async UniTask methods. The system SHALL ensure all asynchronous operations can be awaited and properly cancelled upon MonoBehaviour destruction.

#### Scenario: StartPlacement method returns UniTask
- **WHEN** `StartPlacement()` is called
- **THEN** method returns UniTask instead of void, allowing callers to await completion

#### Scenario: Cancelled on destruction
- **WHEN** object is destroyed while async operation is pending
- **THEN** CancellationToken from `GetCancellationTokenOnDestroy()` cancels the operation automatically

### Requirement: Replace async void with UniTask in CombatState
All async void methods in CombatState SHALL be replaced with async UniTask methods. SpawnEnemies and InitializeMousePreview SHALL return UniTask.

#### Scenario: SpawnEnemies awaitable
- **WHEN** `SpawnEnemies()` is called
- **THEN** method returns UniTask, allowing sequential dependency management

#### Scenario: Exception propagation
- **WHEN** async method throws exception
- **THEN** exception is propagated to caller (not swallowed like async void)

### Requirement: Replace async void in CombatVFXManager
PlayBuffEffect and related methods SHALL return UniTask, enabling proper error handling and cancellation.

#### Scenario: PlayBuffEffect cancellation
- **WHEN** effect is triggered but object is destroyed before completion
- **THEN** CancellationToken prevents null reference exceptions

### Requirement: Replace async void in other Manager classes
All remaining async void methods (HotfixEntry, PlayerCharacterManager, GameProcedure, LocalizationExtension) SHALL be converted to UniTask.

#### Scenario: Uniform async pattern
- **WHEN** any manager async method is called
- **THEN** method signature includes Async suffix and returns UniTask

### Requirement: Establish Roslyn analyzer rule
The CI pipeline SHALL enforce no async void methods in hotfix code via Roslyn analyzer. Any new async void SHALL be rejected at code review stage.

#### Scenario: CI rejection of async void
- **WHEN** code with async void is committed
- **THEN** CI pipeline fails with clear message about async void violation
