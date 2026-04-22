## MODIFIED Requirements

### Requirement: Teleport interaction triggers settlement instead of direct scene load
**Previously**: TeleportGateInteractable directly called `GF.Scene.LoadScene()` to load the target scene.
**Now**: TeleportGateInteractable calls `SettlementManager.TriggerSettlement()` with target scene and trigger source.

The system SHALL intercept teleport interaction to initiate the settlement flow rather than loading the scene immediately.

#### Scenario: Teleport gate interaction routes to settlement
- **WHEN** player calls TeleportGateInteractable.OnInteract()
- **THEN** system invokes SettlementManager.TriggerSettlement(targetScene, "teleport") instead of GF.Scene.LoadScene()

#### Scenario: Target scene preserved through settlement
- **WHEN** settlement is triggered from teleport
- **THEN** the target scene is stored in settlement data and loaded after settlement completes and UI is closed

#### Scenario: Direct scene load no longer called from OnInteract
- **WHEN** TeleportGateInteractable.OnInteract() is executed
- **THEN** no direct call to GF.Scene.LoadScene() happens; settlement manager handles the scene loading
