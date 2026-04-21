## ADDED Requirements

### Requirement: Cache GetBuff lookups in hot paths
The system SHALL cache GetBuff(buffId) results when called multiple times in the same frame. Repeated queries for the same buffId within Update SHALL return cached reference.

#### Scenario: Buff existence check followed by update
- **WHEN** both HasBuff(buffId) and GetBuff(buffId) are called in sequence
- **THEN** second call uses cached reference from first call within same frame

#### Scenario: Update efficiency
- **WHEN** BuffManager.Update iterates buff list and checks buff state
- **THEN** no redundant GetBuff calls are made; direct iteration via for loop used instead

### Requirement: Eliminate Update parameter checking
CardSlotContainer SHALL move parameter change detection from Update to OnValidate (editor-time) or reduce frequency to every 0.5 seconds.

#### Scenario: Parameter detection in editor
- **WHEN** CardSlotContainer parameters are modified in Inspector during edit mode
- **THEN** OnValidate detects changes and logs; no per-frame polling in Update

#### Scenario: Runtime parameter stability
- **WHEN** game runs and cardinal parameters are stable
- **THEN** Update does not repeatedly check HasParametersChanged(); check removed or deferred

### Requirement: Cache MonoBehaviour component references
All Manager and MonoBehaviour classes SHALL cache GetComponent results in Awake/OnEnable, avoiding repeated lookups in Update or event handlers.

#### Scenario: ChessAttribute cache
- **WHEN** BuffManager needs ChessAttribute
- **THEN** reference is cached in Awake (m_OwnerAttribute), not looked up repeatedly in AddBuff

#### Scenario: RectTransform cache
- **WHEN** UI item needs RectTransform
- **THEN** reference is cached in SetData or Awake, not retrieved on each access

### Requirement: Use for loops instead of foreach in collection iteration
Hot path collection iterations (Update, event handlers) SHALL use `for (int i = 0; i < list.Count; i++)` instead of foreach to avoid enumerator allocation.

#### Scenario: Buff update loop
- **WHEN** BuffManager.Update iterates m_Buffs list
- **THEN** uses for loop, not foreach, to reduce GC allocation

### Requirement: Measure performance improvement
Frame rate and memory usage SHALL be sampled before and after optimization. Target: +2% frame rate, -5 MB memory for long-running session (30 min combat).

#### Scenario: Performance baseline
- **WHEN** optimization complete
- **THEN** measure and document frame rate improvement (before: X fps, after: Y fps)

#### Scenario: Memory stability
- **WHEN** game runs 30 minutes continuous combat
- **THEN** memory usage stable (not monotonically increasing), indicating leak fixes
