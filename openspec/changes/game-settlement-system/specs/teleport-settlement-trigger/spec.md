## ADDED Requirements

### Requirement: Intercept teleport interaction to trigger settlement
The system SHALL replace the direct scene loading in TeleportGateInteractable with a settlement trigger call.

#### Scenario: Teleport gate interaction starts settlement
- **WHEN** player interacts with teleport gate (calls TeleportGateInteractable.OnInteract())
- **THEN** system calls SettlementManager.TriggerSettlement(targetScene=<teleport destination>, triggerSource="teleport") instead of directly loading the scene

#### Scenario: Settlement receives target destination
- **WHEN** teleport interaction is processed
- **THEN** the target scene ID (from the teleport gate or OverworldUI selection) is passed to settlement and used as the load destination

#### Scenario: Settlement UI displays teleport context
- **WHEN** settlement UI opens from teleport trigger
- **THEN** UI may show context message like "Returning to base" or destination name (optional/cosmetic)

### Requirement: Handle teleport from exploration or combat preparation
The system SHALL support settlement flow from both exploration and combat preparation states.

#### Scenario: Teleport from exploration
- **WHEN** player activates teleport gate while in exploration scene
- **THEN** exploration state exits, settlement flow begins, base scene loads

#### Scenario: Teleport from combat preparation
- **WHEN** player activates teleport gate while in battle preparation state
- **THEN** combat preparation state exits, settlement flow begins, base scene loads without entering actual combat

#### Scenario: Teleport during active combat blocked or confirmed
- **WHEN** player attempts to interact with teleport during active combat phase
- **THEN** either: (a) interaction is disabled, or (b) system shows confirmation UI, or (c) settlement proceeds with current combat rewards (implementation choice)

### Requirement: Collect exploration/preparation rewards on teleport
The system SHALL collect appropriate rewards from the current exploration/preparation session.

#### Scenario: Exploration rewards included
- **WHEN** teleport settlement is triggered from exploration
- **THEN** system collects: experience from defeated enemies, items collected, gold gathered, etc.

#### Scenario: No combat rewards on combat preparation teleport
- **WHEN** teleport settlement is triggered from battle preparation state (before actual combat)
- **THEN** system collects only exploration rewards, no combat rewards
