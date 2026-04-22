# Game Settlement System - Design Document

## Context

The current project has multiple systems that need to coordinate around a unified settlement flow when gameplay ends. The death mechanic is **two-layered**:

1. **Chess-level death** (棋子死亡): Irrelevant to outcome; adds corruption penalty but does not end combat
2. **Summoner-level death** (召唤师死亡): Player's actual HP reaches zero; increases corruption value (already in CombatState.OnCombatEnd)
3. **Complete death** (完全死亡): Corruption ≥ MaxCorruption + sustained 3s + no death-avoidance items → triggers settlement

Currently:
- **TeleportGateInteractable** directly loads scenes without data collection
- **Corruption death** is not implemented; needs SummonerDeathHandler to monitor and trigger settlement
- No unified UI to display session statistics before returning to base
- State transitions from combat/exploration to base are inconsistent

This design proposes:
- **SettlementManager**: Orchestrate settlement flow (data → UI → load scene → state transition)
- **SummonerDeathHandler**: Monitor corruption and trigger settlement when death condition is met
- **Integration points**: TeleportGate + SummonerDeathHandler both feed into SettlementManager

## Goals / Non-Goals

**Goals:**
1. Unify settlement entry points (death, teleport, potential future triggers)
2. Display settlement statistics UI on top of async scene loading
3. Ensure clean state transitions from combat/exploration to base state
4. Collect and display session rewards (experience, currency, items)
5. Support both victory (teleport) and defeat (death) scenarios with different handling
6. Allow player to manually dismiss settlement UI
7. Maintain clear separation of concerns between settlement, combat, and scene management

**Non-Goals:**
1. Creating a full progression/leveling system (only pass data to existing systems)
2. Implementing settlement configuration tables (use defaults initially)
3. Adding settlement analytics or logging beyond basic event tracking
4. Supporting undo/retry from settlement screen
5. Implementing settlement-specific animations (show static UI only)

## Decisions

### Decision 1: Core Settlement Architecture
**Choice**: Create a **SettlementManager** (singleton via GF.Event pattern) + **SettlementData** (transient data object)

**Rationale**: 
- SettlementManager acts as a coordinator/state machine for settlement flow
- SettlementData is a lightweight, disposable container for session statistics
- Keeps settlement logic isolated and testable
- Integrates naturally with GameFramework's Manager pattern

**Alternatives Considered**:
1. Put logic directly in CombatState/TeleportGateInteractable → Violates separation of concerns, hard to test
2. Use a Procedure for settlement → Overkill for a linear sequence; procedures are for multi-state flows
3. Pure data-driven approach (config table driven) → Too complex for initial implementation

### Decision 2: Settlement Trigger Integration
**Choice**: Modify TeleportGateInteractable and CombatState to call `SettlementManager.TriggerSettlement()` instead of directly changing state/loading scenes

**Rationale**:
- Minimal changes to existing code (just call a new method)
- Settlement manager controls the full flow sequence
- Both sources route through the same pipeline for consistency

**Alternatives Considered**:
1. Use event broadcast → Decoupled but harder to sequence and debug
2. Create a wrapper Procedure that wraps combat → Too heavy-weight
3. Implement settlement hooks in FSM → Requires FSM modifications

### Decision 3: Settlement Data Collection Strategy
**Choice**: Pull model — SettlementManager queries CombatManager and other systems for data at settlement trigger time

**Rationale**:
- Clean interfaces (query-based)
- No instrumentation needed across combat systems
- Data is collected once, at a well-defined point

**Alternatives Considered**:
1. Push model (each system broadcasts rewards as they happen) → Creates timing dependencies
2. Post-combat data persistence → Requires saving/loading, more complex
3. Run mock combat resolution → Redundant calculation

### Decision 4: Settlement UI Positioning and Management
**Choice**: SettlementUIForm is a StateAwareUIForm that displays at high Canvas sort order; async scene load happens in background

**Rationale**:
- Uses existing StateAwareUIForm pattern (consistent with project)
- Canvas sort order ensures it's on top of loading UI
- Player sees statistics while scene loads asynchronously
- No blocking on scene load; better UX

**Alternatives Considered**:
1. Block settlement UI display until scene loads → Adds artificial delay, worse UX
2. Use a procedural loading bar UI → Unnecessary complexity for async load
3. Display in world space → Inconsistent with project's UI patterns

### Decision 5: State Transition Timing
**Choice**: State transition (pop CombatState/ExploreState) happens AFTER settlement UI closes and new scene is loaded

**Rationale**:
- Ensures old scene is fully unloaded before exiting its state
- New scene is ready when state changes to base state
- Prevents edge cases where old state logic runs during load

**Alternatives Considered**:
1. Pop state immediately, load scene in background → Race condition risk
2. Pop state before UI opens → Loses state context for cleanup
3. Create intermediate "Settlement" state → Over-engineers the flow

### Decision 6: Settlement Data Lifecycle
**Choice**: SettlementData is created at trigger time, stored on SettlementManager, and cleared after transition completes

**Rationale**:
- Single owner (SettlementManager) for the data
- Clear creation/destruction lifecycle
- UI can query via `SettlementManager.GetCurrentSettlementData()`
- Prevents memory leaks and stale data

**Alternatives Considered**:
1. Persist data to a global singleton → Creates temporal coupling
2. Pass data through scene parameters → Fragile, hard to track
3. Store in PlayerAccountDataManager → Mixes settlement with player state

### Decision 7: Summoner Death Detection (Corruption-Based)
**Choice**: Create a **SummonerDeathHandler** component to monitor corruption value and trigger settlement when complete death condition is met (corruption ≥ max + sustained 3s + no death-avoidance items)

**Rationale**:
- Corruption death is **distinct from combat-based death** (chess death is irrelevant; only corruption matters)
- 3-second delay allows player time to use resurrection cards or shields before death is finalized
- Item/effect checks (resurrection cards, shields, etc.) are localized to this handler
- Keeps SettlementManager **agnostic of why settlement was triggered** — it just orchestrates the flow
- Can monitor both combat and exploration phases seamlessly

**Alternatives Considered**:
1. Monitor corruption directly in PlayerRuntimeDataManager → Violates single responsibility
2. Put logic in ExploreState/CombatState → Can't track across state transitions
3. Use coroutine in player character → Coupling player to death logic
4. Integrate into CombatEntityTracker → Tracker should only handle chess entities

### Decision 8: Reward Distribution and Save Persistence
**Choice**: SettlementManager collects data; actual reward application (exp, currency, items) happens via existing systems (LevelUpManager, InventoryManager, etc.) after settlement completes

**Rationale**:
- Minimal modification to existing reward systems
- SettlementManager is just a data aggregator, not a reward engine
- Existing save logic handles persistence

**Alternatives Considered**:
1. SettlementManager directly applies rewards → Creates dependency on all reward systems
2. Defer all saves until base state → May cause data loss if game crashes

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| **Race condition: Old state cleanup overlaps with new scene init** | Strictly order: new scene load → state pop → new scene init. Verify in tests. |
| **Settlement data not available if scene load fails** | Store settlement data in static cache; retry logic can re-access it. |
| **UI lingering if close button fails** | Implement timeout fallback; close UI after X seconds if player doesn't click. |
| **Multiple settlements triggered simultaneously** | Queue settlements or reject duplicates with debounce. |
| **Performance: Scene loading + UI rendering** | Scene load is async, UI is lightweight. Monitor frame rate in playtests. |
| **Death settlement showing wrong rewards** | Mark `isDefeat=true` in settlement data; verify reward calculation includes this flag. |

## Architecture Diagram

```
Two Death Trigger Paths:

【Corruption Death Path】
PlayerRuntimeDataManager.CurrentCorruption → 100%
  ↓ (monitored by)
SummonerDeathHandler (3s delay + item checks)
  ↓ (if no items)
SettlementManager.TriggerSettlement("BaseScene", "death")

【Teleport Path】
TeleportGateInteractable.OnInteract()
  ↓
SettlementManager.TriggerSettlement(targetScene, "teleport")

【Both converge to:】
SettlementManager.TriggerSettlement(targetScene, source)
  ├─> Collect settlement data (exp, currency, items)
  ├─> Create SettlementData object
  ├─> Open SettlementUIForm (displays data)
  ├─> Start async scene load (targetScene)
  ├─> Wait for: (a) scene loaded, (b) UI closed
  ├─> Apply rewards via existing managers
  ├─> Change game state: Pop CombatState/ExploreState
  └─> Cleanup: Destroy settlement data
```

## Implementation Sequence

1. **Create SettlementManager** (Core coordinator)
   - Singleton or GF.Event-managed
   - Methods: `TriggerSettlement()`, `GetCurrentSettlementData()`, `CompleteSettlement()`

2. **Create SettlementData** (Data container)
   - Fields: target scene, trigger source, experience, currency, items, defeat flag, metadata

3. **Create SettlementUIForm** (State-aware UI)
   - Displays: experience, currency, item list, close button
   - Queries SettlementManager for data

4. **Create SummonerDeathHandler** (Corruption death detector)
   - Monitor PlayerRuntimeDataManager.CurrentCorruption
   - Implement 3-second delay when corruption reaches max
   - Implement item/effect checks (resurrection cards, shields, etc.)
   - Call `SettlementManager.TriggerSettlement(BaseScene, "death")` if death unavoidable

5. **Modify CombatState** (Corruption increase on chess death)
   - In `OnCombatEnd()`: When player loses (args.IsVictory == false), OnCombatDefeat() already increases corruption
   - **Note**: Don't directly trigger settlement here; SummonerDeathHandler will detect complete death

6. **Modify TeleportGateInteractable** (Teleport trigger)
   - Replace `GF.Scene.LoadScene()` with `SettlementManager.TriggerSettlement(targetScene, "teleport")`

7. **Integrate reward systems** (Reward application)
   - After settlement completion, call existing reward managers to apply exp/currency/items

8. **Test state transitions** (State management)
   - Verify CombatState/ExploreState properly exit after settlement
   - Verify base state systems (inventory, character panel) initialize correctly

## Open Questions

1. Should settlement rewards be configurable per scene/difficulty, or use defaults?
   - **Tentative**: Use defaults initially; add config table later if needed
2. Should partial rewards be applied immediately or only after settlement closes?
   - **Tentative**: Apply only after settlement UI closes to avoid confusion
3. Should teleport during active combat phase be allowed, blocked, or require confirmation?
   - **Tentative**: Decision deferred to combat system team; settlement will handle any trigger source
4. Should settlement data be saved to player save file?
   - **Tentative**: Only if needed for progression; otherwise discard after settlement
