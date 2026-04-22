# Game Settlement System - Implementation Tasks

## 1. Create Core Data Structures

- [x] 1.1 Create `SettlementData.cs` class with fields: targetScene, triggerSource, experience, currency, droppedItems, enemiesDefeated, sessionDuration, isDefeat
- [x] 1.2 Add methods to `SettlementData`: GetTotalExperience(), GetTotalCurrency(), GetItemList(), IsDefeatScenario()
- [x] 1.3 Create `SettlementTriggerSource` enum: Teleport, Death, Other

## 2. Create SettlementManager (Core Coordinator)

- [x] 2.1 Create `SettlementManager.cs` as singleton (implement as required by GF framework)
- [x] 2.2 Implement `TriggerSettlement(targetScene, triggerSource)` method
- [x] 2.3 Implement `GetCurrentSettlementData()` method to query current settlement state
- [x] 2.4 Implement internal coroutine/UniTask to orchestrate settlement sequence (collect → UI → load → transition)
- [x] 2.5 Implement `CollectSettlementDataFromCombat()` to query CombatManager for rewards
- [x] 2.6 Implement `CollectSettlementDataFromExplore()` to query exploration systems for rewards
- [x] 2.7 Add method `CompleteSettlement()` to cleanup settlement data after transition
- [x] 2.8 Add safeguard to prevent duplicate settlements (debounce/queue mechanism)

## 3. Create Settlement UI (SettlementUIForm)

- [x] 3.1 Create `SettlementUIForm.cs` inheriting from `StateAwareUIForm`
- [x] 3.2 Design UI Variables file (SettlementUIVariables.cs) with fields for: title text, exp display, currency display, item list container, close button
- [x] 3.3 Create Prefab: `SettlementUIForm` with Canvas, Text labels for statistics, ScrollView for items, Close button
- [x] 3.4 Implement `OnOpen()` to fetch settlement data from SettlementManager and populate UI
- [x] 3.5 Implement `OnClose()` to signal settlement completion to SettlementManager
- [x] 3.6 Add timeout fallback (auto-close after 60 seconds if player doesn't click)
- [x] 3.7 Add Canvas sort order configuration to ensure UI displays on top
- [x] 3.8 Configure UI in UITable with appropriate UI ID

## 4. Create SummonerDeathHandler (Corruption-Based Death Detection)

- [x] 4.1 Create `SummonerDeathHandler.cs` as a MonoBehaviour component (attach to PlayerCharacter)
- [x] 4.2 Implement `Update()` to monitor `PlayerRuntimeDataManager.CorruptionPercent`
- [x] 4.3 Implement death trigger timer: when corruption ≥ 100%, start 3-second countdown
- [x] 4.4 Implement `CheckResurrectionItems()`: Query inventory for resurrection cards and consume if available
- [x] 4.5 Implement `CheckDeathShield()`: Check for active shield effects/items that block death
- [x] 4.6 Implement `TriggerCompleteDeath()`: Call `SettlementManager.TriggerSettlement(BaseScene, "death")` when no items can save
- [x] 4.7 Reset timer if corruption drops below 100% (player managed to reduce it)
- [ ] 4.8 Test: Verify corruption death triggers settlement with and without item protection

## 5. Integrate Settlement with Teleport System

- [x] 5.1 Modify `TeleportGateInteractable.cs`: Replace `GF.Scene.LoadScene()` call with `SettlementManager.TriggerSettlement(targetScene, "teleport")`
- [x] 5.2 Ensure target scene ID is correctly passed to SettlementManager
- [x] 5.3 Add settlement trigger source logging for debugging
- [ ] 5.4 Test: Verify teleport trigger opens settlement UI instead of direct scene load

## 6. Ensure Combat Loss Increases Corruption (No Direct Settlement Trigger)

- [x] 6.1 Verify `CombatState.OnCombatEnd()` already calls `PlayerRuntimeDataManager.Instance.OnCombatDefeat()` when !args.IsVictory
- [x] 6.2 Verify `OnCombatDefeat()` increases corruption by 50% of current value
- [x] 6.3 Confirm CombatState does **not** directly call `SettlementManager.TriggerSettlement()` on death
- [x] 6.4 SummonerDeathHandler will detect corruption reaching max and trigger settlement (handles 3s delay + item checks)
- [ ] 6.5 Test: Verify combat loss increases corruption, then settlement triggers if no protection

## 7. Implement Async Scene Loading in Settlement Flow

- [x] 6.1 In `SettlementManager`, after settlement UI opens, call `GF.Scene.LoadSceneAsync(targetScene)` to load scene in background
- [x] 6.2 Wait for scene load to complete using UniTask (no blocking)
- [x] 6.3 Keep settlement UI visible during async load (UI remains on top)
- [x] 6.4 After scene loaded, wait for player to close settlement UI
- [x] 6.5 Only then complete state transition and cleanup

## 8. Implement State Transitions

- [x] 8.1 In `SettlementManager`, after scene load + UI close, pop current state (CombatState/ExploreState)
- [x] 8.2 Ensure new scene's base state systems initialize after state pop
- [ ] 8.3 Re-enable player controller and input after state transition
- [ ] 8.4 Verify all combat/exploration UI is properly closed
- [ ] 8.5 Test: Verify state transitions correctly from combat → base state

## 9. Reward Application and Data Persistence

- [x] 9.1 After state transition completes, apply rewards via existing managers (e.g., add experience to PlayerAccountDataManager)
- [x] 9.2 Add item drops to player inventory via existing inventory system
- [x] 9.3 Update currency/gold in player data
- [ ] 9.4 Verify save data is updated (may require SaveGame() call)
- [ ] 9.5 Test: Verify rewards are correctly applied and saved

## 10. Integration Testing

- [ ] 10.1 Create test scenario: Enter combat → lose → verify corruption increases but settlement doesn't trigger (yet)
- [ ] 10.2 Create test scenario: Combat loss reduces health to trigger high corruption → wait 3s → verify settlement triggers
- [ ] 10.3 Create test scenario: Corruption max → player uses resurrection card → verify settlement cancelled, corruption resets
- [ ] 10.4 Create test scenario: Corruption max → player has shield item → verify death is averted
- [ ] 10.5 Create test scenario: Teleport from exploration → verify teleport settlement triggers immediately
- [ ] 10.6 Create test scenario: Settlement UI displays correct rewards (defeat vs victory)
- [ ] 10.7 Create test scenario: Player closes settlement UI → verify scene transition completes
- [ ] 10.8 Create test scenario: Scene loads asynchronously while settlement UI is visible
- [ ] 10.9 Create test scenario: Base state systems initialize correctly after settlement
- [ ] 10.10 Create test scenario: Player controller is re-enabled after settlement
- [ ] 10.11 Test edge case: Multiple settlement triggers in quick succession (verify debounce/queue)

## 11. Documentation and Code Review

- [ ] 11.1 Add inline documentation to SettlementManager explaining settlement flow
- [ ] 11.2 Document SettlementData fields and usage
- [ ] 11.3 Document SummonerDeathHandler corruption monitoring and item check logic
- [ ] 11.4 Document modifications to CombatState and TeleportGateInteractable
- [ ] 11.5 Create a quick reference guide for settlement system usage
- [ ] 11.6 Code review: Verify all modified systems follow project conventions
- [ ] 11.7 Update CHANGELOG/commit messages to describe the change
