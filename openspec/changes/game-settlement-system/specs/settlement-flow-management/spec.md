## ADDED Requirements

### Requirement: Orchestrate settlement flow sequence
The system SHALL manage the complete settlement flow: data collection → UI display → async scene loading → state transition, ensuring proper sequencing and synchronization.

#### Scenario: Settlement flow on teleport
- **WHEN** player interacts with teleport gate in exploration scene
- **THEN** system executes: (1) collect settlement data, (2) open settlement UI, (3) start async scene load, (4) wait for player to close UI, (5) complete scene transition, (6) change game state to base state

#### Scenario: Settlement flow on death
- **WHEN** all player entities are defeated in combat
- **THEN** system executes: (1) collect settlement data (with death marker), (2) open settlement UI, (3) start async scene load to base, (4) wait for player to close UI, (5) complete scene transition, (6) change game state to base state

#### Scenario: Scene load happens asynchronously
- **WHEN** settlement UI is displayed
- **THEN** the target scene loads asynchronously in the background while settlement UI is visible; scene loading does not block the UI

### Requirement: Handle settlement entry points
The system SHALL accept settlement triggers from multiple sources (teleport interaction, player death, other triggers) and route them to the settlement flow.

#### Scenario: Route teleport trigger
- **WHEN** TeleportGateInteractable.OnInteract() is called
- **THEN** system invokes SettlementManager.TriggerSettlement(targetScene, triggerSource)

#### Scenario: Route death trigger
- **WHEN** CombatState detects player death (all entities defeated)
- **THEN** system invokes SettlementManager.TriggerSettlement(baseScene, triggerSource="death")

#### Scenario: Multiple triggers queued safely
- **WHEN** two settlement triggers fire in quick succession
- **THEN** system queues them or prevents duplicate settlement execution

### Requirement: Manage settlement data lifecycle
The system SHALL create, populate, and destroy settlement data at appropriate lifecycle stages.

#### Scenario: Data created on settlement start
- **WHEN** SettlementManager.TriggerSettlement() is called
- **THEN** a new SettlementData object is created and populated with scene context and target destination

#### Scenario: Data destroyed after transition complete
- **WHEN** scene transition is complete (new scene loaded, old scene unloaded)
- **THEN** settlement data is cleared and disposed
